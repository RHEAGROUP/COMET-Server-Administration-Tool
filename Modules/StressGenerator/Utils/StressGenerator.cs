// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StressGenerator.cs" company="RHEA System S.A.">
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
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using CDP4Dal.Operations;
    using Common.Events;
    using NLog;
    using ViewModels;

    /// <summary>
    /// The purpose of this class is to assist in the simulation of concurrent multi-user testing
    /// </summary>
    internal class StressGenerator
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
        private static readonly StressGenerator Instance = new StressGenerator();

        /// <summary>
        /// Delegate used for notifying stress generator progress message
        /// </summary>
        /// <param name="message">Progress message</param>
        public delegate void NotifyMessageDelegate(string message);

        /// <summary>
        /// Associated event with the <see cref="NotifyMessageDelegate"/>
        /// </summary>
        public event NotifyMessageDelegate NotifyMessageEvent;

        /// <summary>
        /// Gets the singleton class instance
        /// </summary>
        /// <returns>
        /// The singleton class instance
        /// </returns>
        internal static StressGenerator GetInstance() => Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="StressGenerator"/> class
        /// </summary>
        private StressGenerator()
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

        // TODO #81 Unify output messages mechanism inside SAT solution
        /// <summary>
        /// Invoke NotifyMessageEvent and optionally log
        /// </summary>
        /// <param name="message">Progress message</param>
        /// <param name="logLevel">Log verbosity level(optional) <see cref="LogVerbosity"/></param>
        /// <param name="ex">Exception(optional) <see cref="Exception"/></param>
        private void NotifyMessage(string message, LogVerbosity? logLevel = null, Exception ex = null)
        {
            NotifyMessageEvent?.Invoke(message);

            switch (logLevel)
            {
                case LogVerbosity.Info:
                    Logger.Info(message);
                    break;
                case LogVerbosity.Warn:
                    Logger.Warn(message);
                    break;
                case LogVerbosity.Debug:
                    Logger.Debug(message);
                    break;
                case LogVerbosity.Error:
                    Logger.Error(ex?.Message != null ? message + ex.Message : message);
                    break;
                default:
                    Logger.Trace(message);
                    break;
            }
        }

        /// <summary>
        /// Generate test objects in the engineering model.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public async Task GenerateTestObjects()
        {
            if (this.configuration == null)
            {
                this.NotifyMessage("Stress generator configuration is not initialized.", LogVerbosity.Error);
                return;
            }

            var session = this.configuration.Session;

            if (this.configuration.OperationMode == SupportedOperationModes.Create ||
                this.configuration.OperationMode == SupportedOperationModes.CreateOverwrite)
            {
                if (this.configuration.OperationMode == SupportedOperationModes.CreateOverwrite)
                {
                    await EngineeringModelSetupGenerator.Delete(session, this.configuration.TestModelSetup);

                    await session.Refresh();
                }

                // Generate and write EngineeringModelSetup
                var engineeringModelSetup = await EngineeringModelSetupGenerator.Create(
                    session, this.configuration.TestModelSetupName, this.configuration.SourceModelSetup);

                await session.Refresh();

                this.configuration.TestModelSetup = session.Assembler.Cache.Select(x => x.Value)
                    .Select(lazy => lazy.Value).OfType<EngineeringModelSetup>()
                    .SingleOrDefault(em => em.Iid == engineeringModelSetup?.Iid);
            }

            if (this.configuration.TestModelSetup == null)
            {
                return;
            }

            var iteration = await this.ReadIteration(this.configuration.TestModelSetup);

            if (iteration == null)
            {
                return;
            }

            // Generate and write ElementDefinition list
            var generatedElementsList = await this.GenerateAndWriteElementDefinitions(iteration);

            await session.Refresh();

            // Generate and write ParameterValueSets
            await this.GenerateAndWriteParameterValueSets(generatedElementsList);

            await session.Refresh();
        }

        /// <summary>
        /// Cleanup test objects.
        /// </summary>
        /// <returns></returns>
        public async Task CleanUpTestObjects()
        {
            var session = this.configuration.Session;

            if (this.configuration.TestModelSetup == null)
            {
                this.NotifyMessage("EngineeringModelSetup is not initialized.", LogVerbosity.Error);
                return;
            }

            if (this.configuration.DeleteModel)
            {
                await EngineeringModelSetupGenerator.Delete(session, this.configuration.TestModelSetup);
            }

            CDPMessageBus.Current.SendMessage(new LogoutAndLoginEvent {CurrentSession = this.configuration.Session});
        }

        /// <summary>
        /// Read latest iteration from the model
        /// </summary>
        /// <param name="engineeringModelSetup">
        /// The selected engineering model for test <see cref="EngineeringModelSetup"/>
        /// </param>
        /// <returns>
        /// A <see cref="Task{Iteration}"/>, or null if the iteration cannot be created
        /// </returns>
        private async Task<Iteration> ReadIteration(EngineeringModelSetup engineeringModelSetup)
        {
            Iteration iteration;

            try
            {
                this.NotifyMessage(
                    $"Loading last iteration from EngineeringModel {engineeringModelSetup.ShortName}...");

                iteration = await IterationGenerator.Create(this.configuration.Session, engineeringModelSetup);

                this.NotifyMessage(
                    $"Successfully loaded EngineeringModel {engineeringModelSetup.ShortName} (Iteration {iteration.IterationSetup?.IterationNumber}).");
            }
            catch (Exception ex)
            {
                this.NotifyMessage(
                    $"Invalid iteration. Engineering model {engineeringModelSetup.ShortName} must contain at least one active iteration. Exception: {ex.Message}",
                    LogVerbosity.Error);

                return null;
            }

            if (IterationGenerator.CheckIfIterationReferencesGenericRdl(iteration))
            {
                return iteration;
            }

            this.NotifyMessage(
                $"Invalid RDL chain. Engineering model {(iteration.Container as EngineeringModel)?.EngineeringModelSetup.ShortName} must reference Site RDL \"{StressGeneratorConfiguration.GenericRdlShortName}\".",
                LogVerbosity.Error);

            return null;
        }

        /// <summary>
        /// Generate and write a set of test element definition base on configuration
        /// </summary>
        /// <param name="iteration">Latest server session read <see cref="Iteration"/></param>
        /// <returns>
        /// A <see cref="Task{List}"/> of <see cref="ElementDefinition"/>, or null if the iteration could not be found
        /// </returns>
        private async Task<List<ElementDefinition>> GenerateAndWriteElementDefinitions(Iteration iteration)
        {
            if (iteration == null)
            {
                this.NotifyMessage("Cannot find Iteration that contains generated ElementDefinition list.",
                    LogVerbosity.Error);
                return null;
            }

            if (this.configuration.Session.OpenIterations == null)
            {
                this.NotifyMessage("This session does not contains open Iterations.",
                    LogVerbosity.Error);
                return null;
            }

            var start = this.FindHighestNumberOnElementDefinitions(iteration) + 1;
            var clearRequested = false;
            var generatedElementsList = new List<ElementDefinition>();

            for (var number = start; number < start + this.configuration.TestObjectsNumber; number++)
            {
                iteration =
                    this.configuration.Session.OpenIterations.Keys.FirstOrDefault(it =>
                        iteration != null && it.Iid == iteration.Iid);
                var clonedIteration = iteration?.Clone(true);

                if (clonedIteration == null)
                {
                    continue;
                }

                if (this.configuration.DeleteAllElements && !clearRequested)
                {
                    clearRequested = true;
                    clonedIteration.Element.Clear();
                }

                var elementDefinition = ElementDefinitionGenerator.Create(
                    $"{configuration.ElementName}#{number:D3}",
                    $"{configuration.ElementShortName}#{number:D3}",
                    clonedIteration,
                    this.configuration.Session.ActivePerson.DefaultDomain);

                clonedIteration.Element.Add(elementDefinition);
                generatedElementsList.Add(elementDefinition);

                await WriteElementDefinition(elementDefinition, iteration, clonedIteration);

                Thread.Sleep(this.configuration.TimeInterval);
            }

            return generatedElementsList;
        }

        /// <summary>
        /// Generate and write ParameterValueSet for each parameter that belongs to a generated element definition
        /// </summary>
        /// <param name="generatedElementsList">The generated element definition list</param>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        private async Task GenerateAndWriteParameterValueSets(List<ElementDefinition> generatedElementsList)
        {
            if (generatedElementsList == null)
            {
                this.NotifyMessage("Generated ElementDefinition list is empty.", LogVerbosity.Error);
                return;
            }

            var generatedIteration = this.configuration.Session.OpenIterations
                .FirstOrDefault(it => it.Key.Iid == generatedElementsList.FirstOrDefault()?.Container.Iid).Key;

            if (generatedIteration == null)
            {
                this.NotifyMessage("Cannot find Iteration that contains generated ElementDefinition list.",
                    LogVerbosity.Error);
                return;
            }

            var index = 0;
            foreach (var elementDefinition in generatedIteration.Element)
            {
                // Write value sets only for the generated elements
                if (generatedElementsList.All(el => el.Iid != elementDefinition.Iid))
                {
                    continue;
                }

                this.NotifyMessage(
                    $"Generating ParameterValueSet for {generatedIteration.Element[index].Name} ({generatedIteration.Element[index].ShortName}).",
                    LogVerbosity.Info);

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
        [ExcludeFromCodeCoverage]
        private int FindHighestNumberOnElementDefinitions(Iteration iteration)
        {
            var regexName = new Regex($@"^{this.configuration.ElementName}\s*#(\d+)$", RegexOptions.IgnoreCase);
            var regexShortName = new Regex($@"^{this.configuration.ElementShortName}_(\d+)$", RegexOptions.IgnoreCase);
            var highestNumber = 0;

            if (iteration == null)
            {
                return 0;
            }

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
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
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

                this.NotifyMessage(
                    $"Successfully generated ElementDefinition {elementDefinition.Name} ({elementDefinition.ShortName}).",
                    LogVerbosity.Info);
            }
            catch (Exception ex)
            {
                this.NotifyMessage(
                    $"Cannot generate ElementDefinition {elementDefinition.Name} ({elementDefinition.ShortName}). Exception: {ex.Message}",
                    LogVerbosity.Error);
            }
        }

        /// <summary>
        /// Write parameters value sets
        /// </summary>
        /// <param name="parameter">The parameter whose values will be written <see cref="Parameter"/></param>
        /// <param name="elementIndex">The element definition index(used to see different parameter values)</param>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        [ExcludeFromCodeCoverage]
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

                this.NotifyMessage(
                    $"Successfully generated ValueSet (Published value: {parameterValue}) for parameter {parameter.ParameterType.Name} ({parameter.ParameterType.ShortName}).");
            }
            catch (Exception ex)
            {
                this.NotifyMessage(
                    $"Cannot update ValueSet (Published value: {parameterValue}) for parameter {parameter.ParameterType.Name} ({parameter.ParameterType.ShortName}). Exception: {ex.Message}");
            }
        }
    }
}
