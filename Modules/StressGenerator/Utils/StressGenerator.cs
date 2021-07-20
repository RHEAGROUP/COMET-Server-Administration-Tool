﻿// --------------------------------------------------------------------------------------------------------------------
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
    using ViewModels;

    /// <summary>
    /// The purpose of this class is to assist in the simulation of concurrent multi-user testing
    /// </summary>
    internal class StressGenerator
    {
        /// <summary>
        /// The singleton class instance
        /// </summary>
        private static readonly StressGenerator Instance = new StressGenerator();

        /// <summary>
        /// Gets the singleton class instance
        /// </summary>
        /// <returns>
        /// The singleton class instance
        /// </returns>
        internal static StressGenerator GetInstance() => Instance;

        /// <summary>
        /// Stress generator configuration
        /// </summary>
        private StressGeneratorConfiguration configuration;

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
        /// Generate test objects in the engineering model.
        /// </summary>
        public async Task Generate()
        {
            if (this.configuration == null)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Stress generator configuration is not initialized.",
                    Verbosity = LogVerbosity.Error,
                    Type = typeof(StressGeneratorViewModel)
                });

                return;
            }

            var session = this.configuration.Session;

            if (this.configuration.OperationMode == SupportedOperationMode.Create ||
                this.configuration.OperationMode == SupportedOperationMode.CreateOverwrite)
            {
                var newModelName = this.configuration.TestModelSetupName;

                if (this.configuration.OperationMode == SupportedOperationMode.CreateOverwrite)
                {
                    newModelName = session.RetrieveSiteDirectory().Model
                        .Single(ems => ems.Iid == this.configuration.TestModelSetup.Iid)
                        .Name;
                    
                    await EngineeringModelSetupGenerator.Delete(session, this.configuration.TestModelSetup.Iid);

                    await session.Refresh();
                }

                // Generate and write EngineeringModelSetup
                var engineeringModelSetup = await EngineeringModelSetupGenerator.Create(
                    session,
                    newModelName,
                    this.configuration.SourceModelSetup);

                await session.Refresh();

                this.configuration.TestModelSetup = engineeringModelSetup;
            }

            var testModelSetup = session.RetrieveSiteDirectory().Model
                .SingleOrDefault(ems => ems.Iid == this.configuration.TestModelSetup?.Iid);

            if (testModelSetup == null)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = $"Could not find test EngineeringModelSetup {this.configuration.TestModelSetup?.ShortName} " +
                              $"({this.configuration.TestModelSetup?.ShortName}) " +
                              $"with iid {this.configuration.TestModelSetup?.Iid}.",
                    Verbosity = LogVerbosity.Error,
                    Type = typeof(StressGeneratorViewModel)
                });

                return;
            }

            var iteration = await this.ReadLastIteration(testModelSetup);

            if (iteration == null)
            {
                return;
            }

            // Generate and write ElementDefinition list
            var generatedElementsList = await this.GenerateAndWriteElementDefinitions(iteration);

            await session.Refresh();

            // Generate and write ParameterValueSets
            await this.GenerateAndWriteParameterValueSets(generatedElementsList);
        }

        /// <summary>
        /// Cleanup test objects.
        /// </summary>
        public async Task CleanUp()
        {
            if (this.configuration.DeleteModel)
            {
                await EngineeringModelSetupGenerator.Delete(
                    this.configuration.Session,
                    this.configuration.TestModelSetup?.Iid);
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = "Finished StressGenerator run.",
                Verbosity = LogVerbosity.Info,
                Type = typeof(StressGeneratorViewModel)
            });

            CDPMessageBus.Current.SendMessage(new LogoutAndLoginEvent
            {
                CurrentSession = this.configuration.Session
            });
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
        private async Task<Iteration> ReadLastIteration(EngineeringModelSetup engineeringModelSetup)
        {
            Iteration iteration;

            try
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = $"Loading last iteration from EngineeringModel {engineeringModelSetup.ShortName}...",
                    Verbosity = LogVerbosity.Info,
                    Type = typeof(StressGeneratorViewModel)
                });

                iteration = await IterationGenerator.OpenLastIteration(this.configuration.Session, engineeringModelSetup);

                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = $"Successfully loaded EngineeringModel {engineeringModelSetup.ShortName} " +
                              $"(Iteration {iteration.IterationSetup?.IterationNumber}).",
                    Verbosity = LogVerbosity.Info,
                    Type = typeof(StressGeneratorViewModel)
                });
            }
            catch (Exception exception)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = $"Could not open last iteration for EngineeringModelSetup {engineeringModelSetup.ShortName}. " +
                              $"Exception: {exception.Message}",
                    Verbosity = LogVerbosity.Error,
                    Type = typeof(StressGeneratorViewModel)
                });

                return null;
            }

            if (IterationGenerator.IterationReferencesGenericRdl(iteration))
            {
                return iteration;
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = $"Invalid RDL chain. Engineering model {(iteration.Container as EngineeringModel)?.EngineeringModelSetup.ShortName} must reference Site RDL \"{StressGeneratorConfiguration.GenericRdlShortName}\".",
                Verbosity = LogVerbosity.Error,
                Type = typeof(StressGeneratorViewModel)
            });

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
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Cannot find Iteration that contains generated ElementDefinition list.",
                    Verbosity = LogVerbosity.Error,
                    Type = typeof(StressGeneratorViewModel)
                });

                return null;
            }

            if (this.configuration.Session.OpenIterations == null)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "This session does not contains open Iterations.",
                    Verbosity = LogVerbosity.Error,
                    Type = typeof(StressGeneratorViewModel)
                });

                return null;
            }

            var start = this.FindHighestNumberOnElementDefinitions(iteration) + 1;
            var clearRequested = false;
            var generatedElementsList = new List<ElementDefinition>();
            var stopwatch = new Stopwatch();

            CDPMessageBus.Current.SendMessage(new AddConstantLineEvent()
            {
                Text = "ElementDefinitions",
                Timestamp = DateTime.Now
            });

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

                Thread.Sleep((int)Math.Max(0, this.configuration.TimeInterval - stopwatch.ElapsedMilliseconds));
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
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Generated ElementDefinition list is empty.",
                    Verbosity = LogVerbosity.Error,
                    Type = typeof(StressGeneratorViewModel)
                });

                return;
            }

            var generatedIteration = this.configuration.Session.OpenIterations
                .FirstOrDefault(it => it.Key.Iid == generatedElementsList.FirstOrDefault()?.Container.Iid).Key;

            if (generatedIteration == null)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Cannot find Iteration that contains generated ElementDefinition list.",
                    Verbosity = LogVerbosity.Error,
                    Type = typeof(StressGeneratorViewModel)
                });

                return;
            }

            var index = 0;

            CDPMessageBus.Current.SendMessage(new AddConstantLineEvent()
            {
                Text = "ParameterValueSets",
                Timestamp = DateTime.Now
            });

            foreach (var elementDefinition in generatedIteration.Element.ToList())
            {
                // Write value sets only for the generated elements
                if (generatedElementsList.All(el => el.Iid != elementDefinition.Iid))
                {
                    continue;
                }

                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = $"Generating ParameterValueSet for {generatedIteration.Element[index].Name} ({generatedIteration.Element[index].ShortName}).",
                    Verbosity = LogVerbosity.Info,
                    Type = typeof(StressGeneratorViewModel)
                });

                foreach (var parameter in elementDefinition.Parameter.ToList())
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

            await WriteHelper.WriteWithRetries(
                this.configuration.Session,
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
        private async Task WriteParametersValueSets(Parameter parameter, int elementIndex)
        {
            var valueConfigPair = StressGeneratorConfiguration.ParamValueConfig
                .FirstOrDefault(pvc => pvc.Key == parameter.ParameterType.ShortName);
            var parameterSwitchKind = elementIndex % 2 == 0
                ? ParameterSwitchKind.MANUAL
                : ParameterSwitchKind.REFERENCE;
            var parameterValue = (valueConfigPair.Value + elementIndex).ToString(CultureInfo.InvariantCulture);
            var valueSetClone = ParameterGenerator.UpdateValueSets(parameter.ValueSet, parameterSwitchKind, parameterValue);

            var transactionContext = TransactionContextResolver.ResolveContext(valueSetClone);
            var transaction = new ThingTransaction(transactionContext);
            transaction.CreateOrUpdate(valueSetClone);

            await WriteHelper.WriteWithRetries(
                this.configuration.Session,
                transaction.FinalizeTransaction(),
                "writing to server ParameterValueSet " +
                $"(published value {parameterValue}) " +
                $"for Parameter \"{parameter.ParameterType.Name} ({parameter.ParameterType.ShortName})\".");
        }
    }
}
