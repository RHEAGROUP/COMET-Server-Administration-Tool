// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StressGeneratorManager.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2020 RHEA System S.A.
//
//    Author: Adrian Chivu, Cozmin Velciu, Alex Vorobiev
//
//    This file is part of CDP4-Server-Administration-Tool.
//    The CDP4-Server-Administration-Tool is an ECSS-E-TM-10-25 Compliant tool
//    for advanced server administration.
//
//    The CDP4-Server-Administration-Tool is free software; you can redistribute it and/or modify
//    it under the terms of the GNU Affero General Public License as
//    published by the Free Software Foundation; either version 3 of the
//    License, or (at your option) any later version.
//
//    The CDP4-Server-Administration-Tool is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//    Affero General Public License for more details.
//
//    You should have received a copy of the GNU Affero General Public License
//    along with this program. If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace StressGenerator.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Dal.Operations;
    using NLog;
    using ViewModels;

    /// <summary>
    /// The purpose of this class is to assist in the simulation of concurrent multi-user testing
    /// </summary>
    internal class StressGeneratorManager
    {
        /// <summary>
        /// The NLog logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Stress generator configuration
        /// </summary>
        private StressGeneratorConfiguration configuration;

        /// <summary>
        /// The singleton class instance
        /// </summary>
        private static readonly StressGeneratorManager Instance = new StressGeneratorManager();

        /// <summary>
        /// Gets the singleton class instance
        /// </summary>
        /// <returns>
        /// The singleton class instance
        /// </returns>
        internal static StressGeneratorManager GetInstance() => Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="StressGeneratorManager"/> class
        /// </summary>
        private StressGeneratorManager()
        {
        }

        /// <summary>
        /// Set stress generator configuration
        /// </summary>
        /// <param name="stressGeneratorConfiguration"></param>
        public void Init(StressGeneratorConfiguration stressGeneratorConfiguration)
        {
            this.configuration = stressGeneratorConfiguration;
        }

        /// <summary>
        /// Generate test objects in the engineering model with the given short name.
        /// </summary>
        /// <param name="engineeringModelSetup">The engineering model</param>
        public async Task GenerateTestObjects(EngineeringModelSetup engineeringModelSetup)
        {
            if (this.configuration == null)
            {
                Logger.Error("Stress generator configuration is not initialised.");
                return;
            }

            var session = this.configuration.Session;
            var siteDirectory = session.RetrieveSiteDirectory();

            if (siteDirectory == null)
            {
                await session.Open();
                session.RetrieveSiteDirectory();
            }

            var lastIteration = await this.OpenLastIterationOfEngineeringModel(engineeringModelSetup);

            if (lastIteration is null)
            {
                Logger.Error(
                    $"Invalid iteration. Engineering model {engineeringModelSetup.ShortName} must contain at least one active iteration");
                return;
            }

            if (!this.CheckIfIterationReferencesGenericRdl(lastIteration))
            {
                Logger.Error(
                    $"Invalid RDL chain. Engineering model {engineeringModelSetup.ShortName} must reference Site RDL \"{StressGeneratorConfiguration.GenericRdlShortName}\".");
                return;
            }

            Iteration clonedLastIteration = null;

            if (this.configuration.DeleteAllElements)
            {
                clonedLastIteration = lastIteration.Clone(true);
                clonedLastIteration.Element.Clear();
            }

            var start = this.FindHighestNumberOnElementDefinitions(lastIteration) + 1;
            var activeDomains = engineeringModelSetup.ActiveDomain.ToList();

            for (var number = start; number < start + this.configuration.TestObjectsNumber; number++)
            {
                lastIteration =
                    session.OpenIterations.Keys.FirstOrDefault(it =>
                        lastIteration != null && it.Iid == lastIteration.Iid);
                clonedLastIteration = lastIteration?.Clone(true);

                var elementOwner = activeDomains[number % activeDomains.Count];
                var parameterOwner = activeDomains[(number + 1) % activeDomains.Count];

                await this.GenerateElementDefinition(lastIteration, clonedLastIteration, elementOwner, parameterOwner,
                    number);

                Thread.Sleep(this.configuration.TimeInterval);
            }

            await session.Close();
        }

        /// <summary>
        /// Open and load last iteration of given <see cref="EngineeringModelSetup"/>.
        /// </summary>
        /// <param name="modelSetup">
        /// The <see cref="EngineeringModelSetup"/> from which to load the last <see cref="Iteration"/>.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task<Iteration> OpenLastIterationOfEngineeringModel(EngineeringModelSetup modelSetup)
        {
            var session = this.configuration.Session;

            var model = new EngineeringModel(
                modelSetup.EngineeringModelIid,
                session.Assembler.Cache,
                session.Credentials.Uri)
            {
                EngineeringModelSetup = modelSetup
            };
            var lastIterationSetup = modelSetup.IterationSetup
                .OrderBy(iterationSetups => iterationSetups.IterationNumber)
                .LastOrDefault(iterationSetup => !iterationSetup.IsDeleted);

            if (lastIterationSetup is null)
            {
                return null;
            }

            var lastIteration = new Iteration(
                lastIterationSetup.IterationIid,
                session.Assembler.Cache,
                session.Credentials.Uri);

            model.Iteration.Add(lastIteration);

            try
            {
                await session.Read(lastIteration, session.ActivePerson.DefaultDomain);

                Logger.Info(
                    $"Engineering Model {model.EngineeringModelSetup.ShortName} Iteration {lastIterationSetup.IterationNumber} \"{lastIterationSetup.Description}\" created on {lastIterationSetup.CreatedOn} was successfully loaded");
            }
            catch (Exception ex)
            {
                lastIteration = null;
                Logger.Error(
                    $"Engineering Model {model.EngineeringModelSetup.ShortName} Iteration cannot be created. Exception: {ex.Message}");
            }

            return lastIteration;
        }

        /// <summary>
        /// Check that the engineering model references the Generic RDL
        /// </summary>
        /// <param name="iteration">Last originalIteration from the model <see cref="Iteration" /></param>
        /// <returns></returns>
        private bool CheckIfIterationReferencesGenericRdl(Iteration iteration)
        {
            var referencesGenericRdl = false;
            var modelRdl = (iteration.Container as EngineeringModel)?.EngineeringModelSetup.RequiredRdl
                .FirstOrDefault();
            var chainedRdl = modelRdl?.RequiredRdl;

            while (chainedRdl != null)
            {
                if (chainedRdl.ShortName == StressGeneratorConfiguration.GenericRdlShortName)
                {
                    referencesGenericRdl = true;
                    break;
                }

                chainedRdl = chainedRdl.RequiredRdl;
            }

            return referencesGenericRdl;
        }

        /// <summary>
        /// Find highest number in the name or short name of the element definitions.
        /// </summary>
        /// <param name="iteration">The originalIteration <see cref="Iteration"/></param>
        /// <returns>
        /// The highest number that was found, or zero if no matching element definitions were found.
        /// </returns>
        private int FindHighestNumberOnElementDefinitions(Iteration iteration)
        {
            var regexName = new Regex($@"^{this.configuration.ElementName}\s*#(\d+)$", RegexOptions.IgnoreCase);
            var regexShortName = new Regex($@"^{this.configuration.ElementShortName}_(\d+)$", RegexOptions.IgnoreCase);
            var highestNumber = 0;

            foreach (var elementDefinition in iteration.Element)
            {
                var nameMatch = regexName.Match(elementDefinition.Name);
                if (nameMatch.Success)
                {
                    highestNumber = Math.Max(highestNumber, int.Parse(nameMatch.Groups[1].Value));
                }

                var shortNameMatch = regexShortName.Match(elementDefinition.ShortName);
                if (!shortNameMatch.Success)
                {
                    continue;
                }

                highestNumber = Math.Max(highestNumber, int.Parse(shortNameMatch.Groups[1].Value));
            }

            return highestNumber;
        }

        /// <summary>
        /// Generate an <see cref="ElementDefinition"/> with given <see cref="StressGeneratorConfiguration"/>
        /// </summary>
        /// <param name="originalIteration">The original originalIteration <see cref="Iteration" /></param>
        /// <param name="clonedIteration">The clone of the original originalIteration <see cref="Iteration" /></param>
        /// <param name="elementOwner">The element owner <see cref="DomainOfExpertise"/></param>
        /// <param name="lastParameterOwner">The parameter owner <see cref="DomainOfExpertise"/> for the last parameter</param>
        /// <param name="objectNumber">The object number</param>
        private async Task GenerateElementDefinition(Iteration originalIteration, Iteration clonedIteration,
            DomainOfExpertise elementOwner,
            DomainOfExpertise lastParameterOwner, int objectNumber)
        {
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), this.configuration.Session.Assembler.Cache,
                new Uri(this.configuration.Session.DataSourceUri))
            {
                Name = $"{this.configuration.ElementName} #{objectNumber:D3}",
                ShortName = $"{this.configuration.ElementShortName} #{objectNumber:D3}",
                Container = clonedIteration,
                Owner = elementOwner
            };

            var parameters = this.GenerateParameters(elementOwner, lastParameterOwner, objectNumber);

            if (parameters != null)
            {
                elementDefinition.Parameter.AddRange(parameters);
            }

            clonedIteration.Element.Add(elementDefinition);

            try
            {
                var transactionContext = TransactionContextResolver.ResolveContext(originalIteration);
                var operationContainer = new OperationContainer(transactionContext.ContextRoute());
                operationContainer.AddOperation(new Operation(originalIteration.ToDto(), clonedIteration.ToDto(),
                    OperationKind.Update));

                foreach (var newThing in elementDefinition.QueryContainedThingsDeep())
                {
                    operationContainer.AddOperation(new Operation(null, newThing.ToDto(),
                        OperationKind.Create));
                }

                await this.configuration.Session.Dal.Write(operationContainer);

                Logger.Info(
                    $"Generated Element Definition \"{elementDefinition.Name} ({elementDefinition.ShortName})\" owned by {elementOwner.ShortName}");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Cannot generate Element Definition \"{elementDefinition.Name} ({elementDefinition.ShortName})\" owned by {elementOwner.ShortName}. Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate parameters list <see cref="Parameter"/> with given <see cref="StressGeneratorConfiguration"/>
        /// </summary>
        /// <param name="elementOwner">The element owner <see cref="DomainOfExpertise"/></param>
        /// <param name="lastParameterOwner">The parameter owner <see cref="DomainOfExpertise"/> for the last parameter</param>
        /// <param name="objectNumber">The object number</param>
        /// <returns>The generated parameters list</returns>
        private List<Parameter> GenerateParameters(DomainOfExpertise elementOwner, DomainOfExpertise lastParameterOwner,
            int objectNumber)
        {
            var parameterCount = 0;
            var switchAlternator = true;
            var configList = new List<Tuple<ParameterType, double>>();
            var parametersList = new List<Parameter>();
            var session = this.configuration.Session;

            var siteReferenceDataLibraries = session.OpenReferenceDataLibraries.OfType<SiteReferenceDataLibrary>();
            var parameterTypes = siteReferenceDataLibraries.FirstOrDefault()?.ParameterType.ToList();

            if (parameterTypes == null) return parametersList;

            foreach (var keyValue in this.configuration.ParamValueConfig)
            {
                configList.Add(new Tuple<ParameterType, double>(parameterTypes.Single(x => x.ShortName == keyValue.Key),
                    keyValue.Value));
            }

            foreach (var config in configList)
            {
                var (paramType, paramValue) = config;
                var parameterValue = (paramValue + objectNumber - 1).ToString(CultureInfo.InvariantCulture);

                var parameter = new Parameter(Guid.NewGuid(), session.Assembler.Cache,
                    new Uri(session.DataSourceUri))
                {
                    ParameterType = paramType as QuantityKind,
                    Scale = (paramType as QuantityKind)?.DefaultScale,
                    Owner = parameterCount == configList.Count - 1 ? lastParameterOwner : elementOwner
                };

                var parameterValueSet = new ParameterValueSet
                {
                    ValueSwitch = switchAlternator ? ParameterSwitchKind.MANUAL : ParameterSwitchKind.COMPUTED,
                    Manual = new ValueArray<string>(new List<string> {parameterValue}),
                    Computed = new ValueArray<string>(new List<string> {parameterValue}),
                    Reference = new ValueArray<string>(new List<string> {parameterValue}),
                    Formula = new ValueArray<string>(new List<string> {"=" + parameterValue}),
                    Published = new ValueArray<string>(new List<string> {parameterValue})
                };

                parameter.ValueSet.Add(parameterValueSet);

                parametersList.Add(parameter);

                parameterCount++;
                switchAlternator = !switchAlternator;
            }

            return parametersList;
        }
    }
}
