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

using CDP4Common.Types;

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
    using CDP4Dal.Operations;
    using NLog;
    using ViewModels;

    /// <summary>
    /// The purpose of this class is to assist in the simulation of concurrent multi-user testing
    /// </summary>
    internal class StressGeneratorManager
    {
        private const string GenericRdlShortName = "Generic_RDL";

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
                Logger.Error("Stree generator configuration is not initialised.");
                return;
            }

            var activeDomains = engineeringModelSetup.ActiveDomain.ToList();
            var siteDirectory = this.configuration.Session.RetrieveSiteDirectory();

            if (siteDirectory == null)
            {
                await this.configuration.Session.Open();
                this.configuration.Session.RetrieveSiteDirectory();
            }

            var lastIteration = await this.OpenLastIterationOfEngineeringModel(engineeringModelSetup);

            if (lastIteration is null)
            {
                Logger.Error($"Invalid iteration. Engineering model {engineeringModelSetup.ShortName} must contain at least one iteration");
                return;
            }

            var cloneLastIteration = lastIteration.Clone(true);

            if (!this.CheckIfIterationReferencesGenericRdl(cloneLastIteration))
            {
                Logger.Error($"Invalid RDL chain. Engineering model {engineeringModelSetup.ShortName} must reference Site RDL \"{GenericRdlShortName}\".");
                return;
            }

            await this.DeleteAllElementsIfRequested(cloneLastIteration);

            var start = this.FindHighestNumberOnElementDefinitions(cloneLastIteration) + 1;

            for (var number = start; number < start + this.configuration.TestObjectsNumber; number++)
            {
                var elementOwner = activeDomains[number % activeDomains.Count];
                var parameterOwner = activeDomains[(number + 1) % activeDomains.Count];

                await this.GenerateElementDefinition(lastIteration, cloneLastIteration, elementOwner, parameterOwner, number);

                Thread.Sleep(this.configuration.TimeInterval);
            }

            await this.configuration.Session.Close();
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
            var model = new EngineeringModel(
                modelSetup.EngineeringModelIid,
                this.configuration.Session.Assembler.Cache,
                this.configuration.Session.Credentials.Uri)
            {
                EngineeringModelSetup = modelSetup
            };
            var lastIterationSetup = modelSetup.IterationSetup.OrderBy(iterationSetups => iterationSetups.IterationNumber)
                .LastOrDefault(iterationSetup => !iterationSetup.IsDeleted);

            if (lastIterationSetup is null)
            {
                return null;
            }

            var lastIteration = new Iteration(
                lastIterationSetup.IterationIid,
                this.configuration.Session.Assembler.Cache,
                this.configuration.Session.Credentials.Uri);

            model.Iteration.Add(lastIteration);

            try
            {
                await Task.Run(async () => { await this.configuration.Session.Read(lastIteration, this.configuration.Session.ActivePerson.DefaultDomain); });

                Logger.Info($"Engineering Model {model.EngineeringModelSetup.ShortName} Iteration {lastIterationSetup.IterationNumber} \"{lastIterationSetup.Description}\" created on {lastIterationSetup.CreatedOn} was successfully loaded");
            }
            catch (Exception ex)
            {
                Logger.Error($"Engineering Model {model.EngineeringModelSetup.ShortName} Iteration cannot be created. Exception: {ex.Message}");
                lastIteration = null;
            }

            return lastIteration;
        }

        /// <summary>
        /// Delete all existing element definitions from the iteration, if requested
        /// </summary>
        /// <param name="iteration">Last iteration from the model <see cref="Iteration" /></param>
        /// <returns></returns>
        private async Task DeleteAllElementsIfRequested(Iteration iteration)
        {
            if (!this.configuration.DeleteAllElements)
            {
                return;
            }

            try
            {
                var transactionContext = TransactionContextResolver.ResolveContext(iteration);
                var containerTransaction = new ThingTransaction(transactionContext, iteration);

                iteration.Element.Clear();

                await Task.Run(async () => { await this.configuration.Session.Write(containerTransaction.FinalizeTransaction()); });
                Logger.Info(
                    $"Deleted all existing Element Definitions from model \"{(iteration.IterationSetup.Container as EngineeringModelSetup)?.ShortName}\"");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Exception occurs when deleting existing Element Definitions. Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Check that the engineering model references the Generic RDL
        /// </summary>
        /// <param name="iteration">Last iteration from the model <see cref="Iteration" /></param>
        /// <returns></returns>
        private bool CheckIfIterationReferencesGenericRdl(Iteration iteration)
        {
            var referencesGenericRdl = false;
            var modelRdl = (iteration.Container as EngineeringModel)?.EngineeringModelSetup.RequiredRdl.FirstOrDefault();
            var chainedRdl = modelRdl?.RequiredRdl;

            while (chainedRdl != null)
            {
                if (chainedRdl.ShortName == GenericRdlShortName)
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
        /// <param name="iteration">The iteration <see cref="Iteration"/></param>
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
        /// <param name="iteration">The original iteration <see cref="Iteration" /></param>
        /// <param name="cloneIteration">The clone of the original iteration <see cref="Iteration" /></param>
        /// <param name="elementOwner">The element owner <see cref="DomainOfExpertise"/></param>
        /// <param name="lastParameterOwner">The parameter owner <see cref="DomainOfExpertise"/> for the last parameter</param>
        /// <param name="objectNumber">The object number</param>
        private async Task GenerateElementDefinition(Iteration iteration, Iteration cloneIteration, DomainOfExpertise elementOwner,
            DomainOfExpertise lastParameterOwner, int objectNumber)
        {
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), this.configuration.Session.Assembler.Cache, new Uri(this.configuration.Session.DataSourceUri))
                {
                    Name = $"{this.configuration.ElementName} #{objectNumber:D3}",
                    ShortName = $"{this.configuration.ElementShortName} #{objectNumber:D3}",
                    Container = iteration,
                    Owner = elementOwner
                };

            var parameters =  this.GenerateParameters(elementOwner, lastParameterOwner, objectNumber);

            if (parameters != null)
            {
                elementDefinition.Parameter.AddRange(parameters);
            }

            try
            {
                var transactionContext = TransactionContextResolver.ResolveContext(iteration);
                var operationContainer = new OperationContainer(transactionContext.ContextRoute());
                operationContainer.AddOperation(new Operation(iteration.ToDto(), cloneIteration.ToDto(), OperationKind.Update));

                await this.configuration.Session.Dal.Write(operationContainer);

                Logger.Info($"Generated Element Definition \"{elementDefinition.Name} ({elementDefinition.ShortName})\" owned by {elementOwner.ShortName}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Cannot generate Element Definition \"{elementDefinition.Name} ({elementDefinition.ShortName})\" owned by {elementOwner.ShortName}. Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate parameters list <see cref="Parameter"/> with given <see cref="StressGeneratorConfiguration"/>
        /// </summary>
        /// <param name="elementOwner">The element owner <see cref="DomainOfExpertise"/></param>
        /// <param name="lastParameterOwner">The parameter owner <see cref="DomainOfExpertise"/> for the last parameter</param>
        /// <param name="objectNumber">The object number</param>
        /// <returns>The generated parameters list</returns>
        private List<Parameter> GenerateParameters(DomainOfExpertise elementOwner, DomainOfExpertise lastParameterOwner, int objectNumber) {
            var parameterCount = 0;
            var switchAlternator = true;
            var configList = new List<Tuple<ParameterType, double>>();
            var parametersList = new List<Parameter>();

            var siteReferenceDataLibraries = this.configuration.Session.OpenReferenceDataLibraries.OfType<SiteReferenceDataLibrary>();
            var parameterTypes = siteReferenceDataLibraries.FirstOrDefault()?.ParameterType.ToList();

            if (parameterTypes == null) return parametersList;

            foreach (var keyValue in this.configuration.ParamValueConfig)
            {
                configList.Add(new Tuple<ParameterType, double>(parameterTypes.Single(x => x.ShortName == keyValue.Key), keyValue.Value));
            }

            foreach (var config in configList)
            {
                var (paramType, paramValue) = config;
                var parameterValue = (paramValue + objectNumber - 1).ToString(CultureInfo.InvariantCulture);

                var parameter = new Parameter(Guid.NewGuid(), this.configuration.Session.Assembler.Cache, new Uri(this.configuration.Session.DataSourceUri))
                {
                    ParameterType = paramType as QuantityKind,
                    Scale = (paramType as QuantityKind)?.DefaultScale,
                    Owner = parameterCount == configList.Count - 1 ? lastParameterOwner : elementOwner
                };

                var parameterValueSet = new ParameterValueSet();
                if (switchAlternator)
                {
                    parameterValueSet.ValueSwitch = ParameterSwitchKind.MANUAL;
                    parameterValueSet.Manual = new ValueArray<string>(new List<string> { parameterValue });
                }
                else
                {
                    parameterValueSet.ValueSwitch = ParameterSwitchKind.COMPUTED;
                    parameterValueSet.Formula = new ValueArray<string>(new List<string> { "=" + parameterValue });
                    parameterValueSet.Computed = new ValueArray<string>(new List<string> { parameterValue });
                }

                parameter.ValueSet.Add(parameterValueSet);

                parametersList.Add(parameter);

                parameterCount++;
                switchAlternator = !switchAlternator;
            }

            return parametersList;
        }
    }
}
