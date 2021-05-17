// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FixCardinalityErrorsDialogViewModel.cs" company="RHEA System S.A.">
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

namespace Migration.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Dal;
    using Common.Events;
    using Common.ViewModels.PlainObjects;
    using ReactiveUI;

    /// <summary>
    /// The viewmodel of the migration cardinality fix wizard.
    /// </summary>
    public class FixCardinalityErrorsDialogViewModel : ReactiveObject, IFixCardinalityErrorsDialogViewModel
    {
        /// <summary>
        /// The migration source <see cref="ISession" />
        /// </summary>
        private readonly ISession migrationSourceSession;

        /// <summary>
        /// Out property for the <see cref="ErrorDetails" /> property
        /// </summary>
        private string errorDetails;

        /// <summary>
        /// Backing field for <see cref="Errors" />
        /// </summary>
        private ReactiveList<PocoErrorRowViewModel> errors;

        /// <summary>
        /// Backing field for <see cref="IsBusy" />
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// Backing field for <see cref="SelectedError" />
        /// </summary>
        private PocoErrorRowViewModel selectedError;

        /// <summary>
        /// Gets or sets the selected error
        /// </summary>
        public PocoErrorRowViewModel SelectedError
        {
            get => this.selectedError;
            set => this.RaiseAndSetIfChanged(ref this.selectedError, value);
        }

        /// <summary>
        /// Gets or sets error details that will be displayed inside error details group
        /// </summary>
        public string ErrorDetails
        {
            get => this.errorDetails;
            set => this.RaiseAndSetIfChanged(ref this.errorDetails, value);
        }

        /// <summary>
        /// Gets or sets a value indicating the busy status
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.RaiseAndSetIfChanged(ref this.isBusy, value);
        }

        /// <summary>
        /// Gets or sets the list of all errors
        /// </summary>
        public ReactiveList<PocoErrorRowViewModel> Errors
        {
            get => this.errors;
            set => this.RaiseAndSetIfChanged(ref this.errors, value);
        }

        /// <summary>
        /// Gets the fix <see cref="IReactiveCommand" />
        /// </summary>
        public ReactiveCommand<object> FixCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FixCardinalityErrorsDialogViewModel" /> class.
        /// </summary>
        /// <param name="migrationSourceSession">The migration source <see cref="ISession" /></param>
        public FixCardinalityErrorsDialogViewModel(ISession migrationSourceSession)
        {
            this.migrationSourceSession = migrationSourceSession;

            this.Errors = new ReactiveList<PocoErrorRowViewModel> {ChangeTrackingEnabled = true};

            this.IsBusy = false;

            this.FixCommand = ReactiveCommand.Create();
            this.FixCommand.Subscribe(_ => this.ExecuteFixCommand());

            this.WhenAnyValue(vm => vm.SelectedError)
                .Where(s => s != null)
                .Subscribe(_ => this.ErrorDetails = this.SelectedError.ToString());
        }

        /// <summary>
        /// Apply PocoCardinality & PocoProperties to the E10-25 data set and bind errors to the reactive list
        /// </summary>
        public void BindPocoErrors()
        {
            if (this.migrationSourceSession is null)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent { Message = "The source session is not defined" });
                return;
            }

            this.Errors.Clear();
            this.IsBusy = true;

            this.Errors.AddRange(Task.Run(this.GetErrorRows).Result);

            this.IsBusy = false;
        }

        /// <summary>
        /// Gets the list of <see cref="PocoErrorRowViewModel" />
        /// </summary>
        /// <returns>A list of rows containing all errors in cache.</returns>
        private List<PocoErrorRowViewModel> GetErrorRows()
        {
            var result = new List<PocoErrorRowViewModel>();

            foreach (var thing in this.migrationSourceSession.Assembler.Cache
                .Select(item => item.Value.Value)
                .Where(t => t.ValidationErrors.Any()))
            {
                result.AddRange(thing.ValidationErrors.Select(error => new PocoErrorRowViewModel(thing, error)));
            }

            return result;
        }

        /// <summary>
        /// Executes the command to fix POCO cardinality errors
        /// </summary>
        private void ExecuteFixCommand()
        {
            this.IsBusy = true;

            CDPMessageBus.Current.SendMessage(new LogEvent { Message = "Fixing the cardinality errors for the selected models..." });

            // these should be traversed in bottom-up containment order, but for now reverse also works
            foreach (var rowError in this.Errors.OrderBy(e => e.Thing.ClassKind.ToString()).Reverse())
            {
                FixNameAndShortName(rowError);

                FixSpecificError(rowError);

                rowError.Thing.ValidatePoco();
            }

            this.Errors.Clear();

            var d = Task.Run(this.GetErrorRows).Result;

            this.Errors.AddRange(d);

            CDPMessageBus.Current.SendMessage(this.Errors.Count == 0
                ? new LogEvent { Message = "The cardinality errors have been successfully fixed" }
                : new LogEvent { Message = "The cardinality errors have not been fixed" });

            this.IsBusy = false;
        }

        /// <summary>
        /// Fix specific error depends on rowError.Thing class type
        /// </summary>
        /// <param name="rowError">Error row <see cref="PocoErrorRowViewModel"/></param>
        private static void FixSpecificError(PocoErrorRowViewModel rowError)
        {
            switch (rowError.Thing)
            {
                case FileType file:
                    if (rowError.Error.Contains("Extension"))
                    {
                        file.Extension = "Unknown Extension";
                    }
                    break;
                case TelephoneNumber telephoneNumber:
                    if (rowError.Error.Contains("Value"))
                    {
                        telephoneNumber.Value = "No Value";
                    }
                    break;
                case UserPreference userPreference:
                    if (rowError.Error.Contains("Value"))
                    {
                        userPreference.Value = "No Value";
                    }
                    break;
                case Citation citation:
                    // broken citations are a result of 10-25 paradox thus shall be removed
                    if (citation.Container is Definition container)
                    {
                        container.Citation.Remove(citation);
                        container.Cache?.TryRemove(citation.CacheKey, out _);
                    }
                    break;
                case Participant participant:
                    if (participant.Container is EngineeringModelSetup modelSetup)
                    {
                        modelSetup.Participant.Remove(participant);
                        modelSetup.Cache.TryRemove(participant.CacheKey, out _);
                    }
                    break;
                case Definition definition:
                    if (rowError.Error.Contains("Content"))
                    {
                        definition.Content = "No Content";
                    }
                    break;
                case IterationSetup iterationSetup:
                    if (rowError.Error.Contains("Description"))
                    {
                        iterationSetup.Description = "No Description";
                    }
                    break;
                case ScalarParameterType scalarParameterType:
                    if (rowError.Error.Contains("Symbol"))
                    {
                        scalarParameterType.Symbol = "No Symbol";
                    }
                    break;
                case Parameter parameter:
                    FixValueSets(parameter);
                    break;
                case ParameterValueSet parameterValueSet:
                    parameterValueSet.Manual = FixValueArray(parameterValueSet.Manual, parameterValueSet);
                    parameterValueSet.Formula = FixValueArray(parameterValueSet.Formula, parameterValueSet);
                    parameterValueSet.Published = FixValueArray(parameterValueSet.Published, parameterValueSet);
                    parameterValueSet.Reference = FixValueArray(parameterValueSet.Reference, parameterValueSet);
                    break;
            }
        }

        /// <summary>
        /// Generate a new <see cref="Dictionary{TKey,TValue}"/> with the correct values, for each option and state
        /// </summary>
        /// <param name="parameter">
        /// See <see cref="Parameter"/>
        /// </param>
        private static void FixValueSets(Parameter parameter)
        {
            var valueSets = new Dictionary<string, Dictionary<string, List<ParameterValueSet>>>();

            foreach (var valueSet in parameter.ValueSet)
            {
                var optionIid = valueSet.ActualOption?.Iid.ToString() ?? "";
                var stateIid = valueSet.ActualState?.Iid.ToString() ?? "";

                if (!valueSets.ContainsKey(optionIid))
                {
                    valueSets[optionIid] = new Dictionary<string, List<ParameterValueSet>>();
                }

                if (!valueSets[optionIid].ContainsKey(stateIid))
                {
                    valueSets[optionIid][stateIid] = new List<ParameterValueSet>();
                }

                valueSets[optionIid][stateIid].Add(valueSet);
            }

            // no better way to determine which of the ValueSets to keep
            foreach (var dictionary in valueSets.Values)
            {
                foreach (var list in dictionary.Values)
                {
                    for (var i = 1; i < list.Count; ++i)
                    {
                        parameter.ValueSet.Remove(list[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Generate a new <see cref="ValueArray{T}"/> with the correct number of values, containing the values
        /// in <paramref name="oldValues"/>.
        /// </summary>
        /// <param name="oldValues">
        /// The values in the old <see cref="ValueArray{T}"/>.
        /// </param>
        /// <param name="parameterValueSet">
        /// The containing <see cref="ParameterValueSet"/>.
        /// </param>
        /// <returns>
        /// The new <see cref="ValueArray{T}"/>.
        /// </returns>
        private static ValueArray<string> FixValueArray(ValueArray<string> oldValues, ParameterValueSet parameterValueSet)
        {
            var newValues = new List<string>();

            foreach (var oldValue in oldValues)
            {
                newValues.Add(oldValue);
            }

            for (var i = newValues.Count; i < parameterValueSet.QueryParameterType().NumberOfValues; ++i)
            {
                newValues.Add("-");
            }

            return new ValueArray<string>(newValues, parameterValueSet);
        }

        /// <summary>
        /// Fix name/short-name errors
        /// </summary>
        /// <param name="rowError">Error row <see cref="PocoErrorRowViewModel"/></param>
        private static void FixNameAndShortName(PocoErrorRowViewModel rowError)
        {
            if (rowError.Thing is IShortNamedThing shortNamedThing && rowError.Error.Contains("ShortName"))
            {
                shortNamedThing.ShortName = "Undefined ShortName";
            }

            if (rowError.Thing is INamedThing namedThing && rowError.Error.Contains("Name"))
            {
                namedThing.Name = "Undefined Name";
            }
        }
    }
}
