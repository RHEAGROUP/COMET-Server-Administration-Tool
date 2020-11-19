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
        /// <param name="config">Default configuration <see cref="StressGeneratorConfiguration" /></param>
        public void Init(StressGeneratorConfiguration config)
        {
            this.configuration = config;
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

            // Open session and retrieve SiteDirectory
            if (session.RetrieveSiteDirectory() == null)
            {
                await session.Open();
                session.RetrieveSiteDirectory();
            }

            // Generate ElementDefinition list
            var generatedElementsList = await this.GenerateElementDefinitions(engineeringModelSetup);

            // Refresh session
            await session.Refresh();

            // Generate ParameterValueSets
            await GenerateParameterValueSets(generatedElementsList);

            // Close session
            await session.Close();
        }

        /// <summary>
        /// Generate a set of test element definition base on configuration specified <see cref="StressGeneratorConfiguration"/>
        /// </summary>
        /// <param name="engineeringModelSetup">The selected engineering model for test <see cref="EngineeringModelSetup"/></param>
        /// <returns>List of <see cref="ElementDefinition"/></returns>
        private async Task<List<ElementDefinition>> GenerateElementDefinitions(EngineeringModelSetup engineeringModelSetup)
        {
            var iteration = await IterationGenerator.Create(this.configuration.Session, engineeringModelSetup);

            if (iteration is null)
            {
                Logger.Error(
                    $"Invalid iteration. Engineering model {engineeringModelSetup.ShortName} must contain at least one active iteration");
                return null;
            }

            if (!IterationGenerator.CheckIfIterationReferencesGenericRdl(iteration))
            {
                Logger.Error(
                    $"Invalid RDL chain. Engineering model {engineeringModelSetup.ShortName} must reference Site RDL \"{StressGeneratorConfiguration.GenericRdlShortName}\".");
                return null;
            }

            var start = this.FindHighestNumberOnElementDefinitions(iteration) + 1;
            var clearRequested = false;
            var generatedElementsList = new List<ElementDefinition>();

            for (var number = start; number < start + this.configuration.TestObjectsNumber; number++)
            {
                // Always read and clone the iteration
                iteration =
                    this.configuration.Session.OpenIterations.Keys.FirstOrDefault(it =>
                        iteration != null && it.Iid == iteration.Iid);
                var clonedIteration = iteration?.Clone(true);

                if (clonedIteration != null)
                {
                    if (this.configuration.DeleteAllElements && !clearRequested)
                    {
                        clearRequested = true;
                        clonedIteration.Element.Clear();
                    }

                    var elementDefinition = ElementDefinitionGenerator.Create(this.configuration.Session,
                        $"{configuration.ElementName} #{number:D3}", $"{configuration.ElementShortName} #{number:D3}",
                        clonedIteration);

                    clonedIteration.Element.Add(elementDefinition);
                    generatedElementsList.Add(elementDefinition);

                    await WriteElementDefinition(elementDefinition, iteration, clonedIteration);
                }

                Thread.Sleep(this.configuration.TimeInterval);
            }

            return generatedElementsList;
        }

        /// <summary>
        /// Generate ParameterValueSet for each parameter that belongs to a generated element definition
        /// </summary>
        /// <param name="generatedElementsList">The generated element definition list</param>
        /// <returns></returns>
        private async Task GenerateParameterValueSets(List<ElementDefinition> generatedElementsList)
        {
            if (generatedElementsList == null)
            {
                Logger.Error("Generated ElementDefinition list is empty.");
                return;
            }

            var generatedIteration = this.configuration.Session.OpenIterations
                .FirstOrDefault(it => it.Key.Iid == generatedElementsList.FirstOrDefault()?.Container.Iid).Key;

            if (generatedIteration == null)
            {
                Logger.Error("Cannot found Iteration that contains generated ElementDefinition list.");
                return;
            }

            var index = 0;
            foreach (var elementDefinition in generatedIteration.Element)
            {
                foreach (var parameter in elementDefinition.Parameter)
                {
                    await WriteParametersValueSets(parameter, index);
                }

                index++;
            }
        }

        /// <summary>Find highest number in the name or short name of the element definitions.</summary>
        /// <param name="iteration">The last iteration from the model <see cref="Iteration"/></param>
        /// <returns>The highest number that was found, or zero if no matching element definitions were found.</returns>
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

        /// <summary>Write generated element definition</summary>
        /// <param name="elementDefinition">Element definition that will be written <see cref="ElementDefinition"/></param>
        /// <param name="originalIteration">Current iteration used for creating write transaction <see cref="Iteration"/></param>
        /// <param name="clonedIteration">Cloned iteration that will contain element definition <see cref="Iteration"/></param>
        /// <returns></returns>
        private async Task WriteElementDefinition(ElementDefinition elementDefinition, Iteration originalIteration,
            Iteration clonedIteration)
        {
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
                    $"Generated ElementDefinition \"{elementDefinition.Name} ({elementDefinition.ShortName})\"");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Cannot generate ElementDefinition \"{elementDefinition.Name} ({elementDefinition.ShortName})\". Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Write parameters value sets
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="elementIndex"></param>
        /// <returns></returns>
        private async Task WriteParametersValueSets(Parameter parameter, int elementIndex)
        {
            var valueConfigPair =
                StressGeneratorConfiguration.ParamValueConfig.FirstOrDefault(pvc =>
                    pvc.Key == parameter.ParameterType.ShortName);
            var parameterSwitchKind =
                elementIndex % 2 == 0 ? ParameterSwitchKind.MANUAL : ParameterSwitchKind.REFERENCE;
            var parameterValue = (valueConfigPair.Value + elementIndex).ToString(CultureInfo.InvariantCulture);
            var valueSetClone = ParameterGenerator.UpdateValueSets(parameter.ValueSets,
                parameterSwitchKind, parameterValue);

            try
            {
                var transactionContext = TransactionContextResolver.ResolveContext(valueSetClone);
                var transaction = new ThingTransaction(transactionContext);
                transaction.CreateOrUpdate(valueSetClone);
                var operationContainer = transaction.FinalizeTransaction();
                await this.configuration.Session.Write(operationContainer);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Cannot update ParameterValueSet \"{parameter.ParameterType.Name}. Exception: {ex.Message}");
            }
        }
    }
}
