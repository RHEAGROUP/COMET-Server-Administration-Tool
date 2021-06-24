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
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text;
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
    internal class StressGenerator
    {
        /// <summary>
        /// The NLog logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The maximum retry count for a write operation.
        /// </summary>
        private static readonly int MaxRetryCount = 3;

        /// <summary>
        /// Stress generator configuration
        /// </summary>
        private StressGeneratorConfiguration configuration;

        /// <summary>
        /// The singleton class instance
        /// </summary>
        private static readonly StressGenerator Instance = new StressGenerator();

        // TODO #81 Unify output messages mechanism inside SAT solution
        /// <summary>
        /// Log verbosity
        /// </summary>
        private enum LogVerbosity
        {
            Info,
            Warn,
            Debug,
            Error
        };

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

        /// <summary>
        /// Invoke <see cref="NotifyMessageEvent"/> and optionally log.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="logLevel">
        /// <see cref="LogVerbosity"/> level (optional)
        /// </param>
        /// <param name="ex">
        /// <see cref="Exception"/> (optional)
        /// </param>
        private void NotifyMessage(string message, LogVerbosity? logLevel = null, Exception ex = null)
        {
            if (ex != null)
            {
                message += Environment.NewLine + "    " + ex.Message;
            }

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
                    Logger.Error(message);
                    break;
                default:
                    Logger.Trace(message);
                    break;
            }
        }

        /// <summary>
        /// Generate test objects in the engineering model with the given short name.
        /// </summary>
        /// <param name="engineeringModelSetup">
        /// The <see cref="EngineeringModelSetup"/>.
        /// </param>
        [ExcludeFromCodeCoverage]
        public async Task GenerateTestObjects(EngineeringModelSetup engineeringModelSetup)
        {
            if (this.configuration == null)
            {
                this.NotifyMessage("Stress generator configuration is not initialized.", LogVerbosity.Error);
                return;
            }

            var session = this.configuration.Session;

            // Open session and retrieve SiteDirectory
            if (session.RetrieveSiteDirectory() == null)
            {
                await session.Open();
                session.RetrieveSiteDirectory();
            }

            // Read latest iteration
            await session.Refresh();
            var iteration = await ReadIteration(engineeringModelSetup);

            if (iteration == null)
            {
                return;
            }

            // Generate and write ElementDefinition list
            var generatedElementsList = await this.GenerateAndWriteElementDefinitions(iteration);

            // Refresh session
            await session.Refresh();

            // Generate and write ParameterValueSets
            await GenerateAndWriteParameterValueSets(generatedElementsList);

            // Close session
            await session.Close();
        }

        /// <summary>
        /// Read latest iteration from the given <paramref name="engineeringModelSetup"/>.
        /// </summary>
        /// <param name="engineeringModelSetup">
        /// The selected <see cref="EngineeringModelSetup"/>.
        /// </param>
        /// <returns>
        /// An <see cref="Iteration"/>, or null if the <see cref="Iteration"/> cannot be created.
        /// </returns>
        private async Task<Iteration> ReadIteration(EngineeringModelSetup engineeringModelSetup)
        {
            Iteration iteration;

            try
            {
                this.NotifyMessage($"Loading last iteration from EngineeringModel {engineeringModelSetup.ShortName}...");

                iteration = await IterationGenerator.Create(this.configuration.Session, engineeringModelSetup);

                this.NotifyMessage($"Successfully loaded EngineeringModel {engineeringModelSetup.ShortName} (Iteration {iteration.IterationSetup.IterationNumber}).");
            }
            catch (Exception ex)
            {
                this.NotifyMessage($"Invalid iteration. Engineering model {engineeringModelSetup.ShortName} must contain at least one active iteration. Exception: {ex.Message}", LogVerbosity.Error);

                return null;
            }

            if (IterationGenerator.CheckIfIterationReferencesGenericRdl(iteration))
            {
                return iteration;
            }

            this.NotifyMessage($"Invalid RDL chain. Engineering model {(iteration.Container as EngineeringModel)?.EngineeringModelSetup.ShortName} must reference Site RDL \"{StressGeneratorConfiguration.GenericRdlShortName}\".", LogVerbosity.Error);

            return null;
        }

        /// <summary>
        /// Generate and write a set of test <see cref="ElementDefinition"/> based on configuration.
        /// </summary>
        /// <param name="iteration">
        /// The given <see cref="Iteration"/>.
        /// </param>
        /// <returns>
        /// A <see cref="List{T}"/> of <see cref="ElementDefinition"/>s, or null if the <see cref="Iteration"/> could not be found.
        /// </returns>
        private async Task<List<ElementDefinition>> GenerateAndWriteElementDefinitions(Iteration iteration)
        {
            if (iteration == null)
            {
                this.NotifyMessage("Cannot find Iteration that contains generated ElementDefinition list.", LogVerbosity.Error);
                return null;
            }

            if (this.configuration.Session.OpenIterations == null)
            {
                this.NotifyMessage("This session does not contains open Iterations.", LogVerbosity.Error);
                return null;
            }

            var start = this.FindHighestNumberOnElementDefinitions(iteration) + 1;
            var clearRequested = false;
            var generatedElementsList = new List<ElementDefinition>();
            var stopwatch = new Stopwatch();

            for (var number = start; number < start + this.configuration.TestObjectsNumber; number++)
            {
                iteration = this.configuration.Session.OpenIterations.Keys
                    .FirstOrDefault(it => iteration != null && it.Iid == iteration.Iid);
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
                    $"{configuration.ElementName} #{number:D3}",
                    $"{configuration.ElementShortName} #{number:D3}",
                    clonedIteration,
                    this.configuration.Session.ActivePerson.DefaultDomain);

                clonedIteration.Element.Add(elementDefinition);
                generatedElementsList.Add(elementDefinition);

                stopwatch.Restart();
                await WriteElementDefinition(elementDefinition, iteration, clonedIteration);
                stopwatch.Stop();

                Thread.Sleep((int)Math.Max(0, this.configuration.TimeInterval * 1000 - stopwatch.ElapsedMilliseconds));
            }

            return generatedElementsList;
        }

        /// <summary>
        /// Generate and write <see cref="ParameterValueSet"/>s for each <see cref="Parameter"/>
        /// that belongs to a generated <see cref="ElementDefinition"/>.
        /// </summary>
        /// <param name="generatedElementsList">
        /// The generated <see cref="ElementDefinition"/> list.
        /// </param>
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
                this.NotifyMessage("Cannot find Iteration that contains generated ElementDefinition list.", LogVerbosity.Error);
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

                this.NotifyMessage($"Generating ParameterValueSet for {generatedIteration.Element[index].Name} ({generatedIteration.Element[index].ShortName}).",
                    LogVerbosity.Info);

                foreach (var parameter in elementDefinition.Parameter)
                {
                    await WriteParametersValueSets(parameter, index);
                }

                index++;
            }
        }

        /// <summary>
        /// Find the highest number in the name or short name of the <see cref="ElementDefinition"/>s.
        /// </summary>
        /// <param name="iteration">
        /// The given <see cref="Iteration"/>.
        /// </param>
        /// <returns>
        /// The highest number that was found, or zero if no matching <see cref="ElementDefinition"/>s were found.
        /// </returns>
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

        /// <summary>
        /// Write the generated <see cref="ElementDefinition"/>.
        /// </summary>
        /// <param name="elementDefinition">
        /// <see cref="ElementDefinition"/> that will be written.
        /// </param>
        /// <param name="originalIteration">
        /// Current <see cref="Iteration"/> used for creating write transaction.
        /// </param>
        /// <param name="clonedIteration">
        /// Cloned <see cref="Iteration"/> that will contain the <see cref="ElementDefinition"/>.
        /// </param>
        private async Task WriteElementDefinition(
            ElementDefinition elementDefinition,
            Iteration originalIteration,
            Iteration clonedIteration)
        {
            var transactionContext = TransactionContextResolver.ResolveContext(originalIteration);
            var operationContainer = new OperationContainer(transactionContext.ContextRoute());
            operationContainer.AddOperation(new Operation(
                originalIteration.ToDto(), 
                clonedIteration.ToDto(),
                OperationKind.Update));

            foreach (var newThing in elementDefinition.QueryContainedThingsDeep())
            {
                operationContainer.AddOperation(new Operation(
                    null,
                    newThing.ToDto(),
                    OperationKind.Create));
            }

            await this.WriteWithRetries(
                operationContainer,
                "writing to server ElementDefinition " +
                $"\"{elementDefinition.Name} ({elementDefinition.ShortName})\" " +
                $"owned by {elementDefinition.Owner.ShortName}.");
        }

        /// <summary>
        /// Write <see cref="ParameterValueSet"/>s.
        /// </summary>
        /// <param name="parameter">
        /// The <see cref="Parameter"/> whose values will be written.
        /// </param>
        /// <param name="elementIndex">
        /// The <see cref="ElementDefinition"/> index (used to see different parameter values).
        /// </param>
        [ExcludeFromCodeCoverage]
        private async Task WriteParametersValueSets(Parameter parameter, int elementIndex)
        {
            var valueConfigPair = StressGeneratorConfiguration.ParamValueConfig
                .FirstOrDefault(pvc => pvc.Key == parameter.ParameterType.ShortName);
            var parameterSwitchKind = elementIndex % 2 == 0
                ? ParameterSwitchKind.MANUAL
                : ParameterSwitchKind.REFERENCE;
            var parameterValue = (valueConfigPair.Value + elementIndex).ToString(CultureInfo.InvariantCulture);
            var valueSetClone = ParameterGenerator.UpdateValueSets(parameter.ValueSets, parameterSwitchKind, parameterValue);

            var transactionContext = TransactionContextResolver.ResolveContext(valueSetClone);
            var transaction = new ThingTransaction(transactionContext);
            transaction.CreateOrUpdate(valueSetClone);

            await this.WriteWithRetries(
                transaction.FinalizeTransaction(),
                "writing to server ParameterValueSet " +
                $"(published value {parameterValue}) " +
                $"for Parameter \"{parameter.ParameterType.Name} ({parameter.ParameterType.ShortName})\".");
        }

        /// <summary>
        /// Write the given <paramref name="operationContainer"/> to the server, retrying on failure.
        /// </summary>
        /// <param name="operationContainer">
        /// The given <see cref="OperationContainer"/>.
        /// </param>
        /// <param name="actionDescription">
        /// The description of the action.
        /// </param>
        private async Task WriteWithRetries(OperationContainer operationContainer, string actionDescription)
        {
            for (var currentAttempt = 1; currentAttempt <= MaxRetryCount; currentAttempt++)
            {
                try
                {
                    await this.configuration.Session.Dal.Write(operationContainer);

                    this.LogOperationResult(true, actionDescription);
                    return;
                }
                catch (Exception e)
                {
                    this.LogOperationResult(false, actionDescription, e, currentAttempt);
                }
            }
        }

        /// <summary>
        /// Log the result of on operation.
        /// </summary>
        /// <param name="success">
        /// The operation success.
        /// </param>
        /// <param name="actionDescription">
        /// The description of the action.
        /// </param>
        /// <param name="exception">
        /// Optionally, the <see cref="Exception"/> which caused the operation to fail.
        /// </param>
        /// <param name="attempt">
        /// Optionally, the attempt number of the failed operation.
        /// </param>
        private void LogOperationResult(
            bool success,
            string actionDescription,
            Exception exception = null,
            int? attempt = null)
        {
            var sb = new StringBuilder();
            
            sb.Append(success ? "Succeeded" : "Failed");
            sb.Append(" ");

            if (attempt != null)
            {
                sb.Append($"(attempt {attempt})");
                sb.Append(" ");
            }

            sb.Append(actionDescription);
            sb.Append(" ");
            
            this.NotifyMessage(sb.ToString(),
                success ? (LogVerbosity?) null : LogVerbosity.Error,
                exception);
        }
    }
}
