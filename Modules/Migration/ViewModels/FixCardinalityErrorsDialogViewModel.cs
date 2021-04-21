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
    using CDP4Common.SiteDirectoryData;
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
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "The source session is not defined"
                });
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
            CDPMessageBus.Current.SendMessage(new LogEvent { Message = "Get the cardinality errors list for the selected models" });

            var result = new List<PocoErrorRowViewModel>();

            foreach (var thing in this.migrationSourceSession.Assembler.Cache.Select(item => item.Value.Value)
                .Where(t => t.ValidationErrors.Any()))
            {
                foreach (var error in thing.ValidationErrors)
                {
                    result.Add(new PocoErrorRowViewModel(thing, error));
                }
            }

            return result;
        }

        /// <summary>
        /// Executes the command to fix POCO cardinality errors
        /// </summary>
        private void ExecuteFixCommand()
        {
            this.IsBusy = true;

            CDPMessageBus.Current.SendMessage(new LogEvent { Message = "Fix the cardinality errors list for the selected models" });

            foreach (var rowError in this.Errors)
            {
                FixNameAndShortName(rowError);

                FixSpecificError(rowError);

                rowError.Thing.ValidatePoco();
            }

            this.Errors.Clear();

            var d = Task.Run(this.GetErrorRows).Result;

            this.Errors.AddRange(d);

            if (this.Errors.Count == 0)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent { Message = "The cardinality errors list has been succesfully fixed" });
            }
            else
            {
                CDPMessageBus.Current.SendMessage(new LogEvent { Message = "The cardinality errors list has not been fixed" });
            }

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
                case FileType fileThing:
                    fileThing.Extension = rowError.Error.Contains("Extension")
                        ? "UnknownExtension"
                        : fileThing.Extension;
                    break;
                case TelephoneNumber telephoneThing:
                    telephoneThing.Value = rowError.Error.Contains("Value")
                        ? "No Value"
                        : telephoneThing.Value;
                    break;
                case UserPreference userPreferenceThing:
                    userPreferenceThing.Value = rowError.Error.Contains("Value")
                        ? "No Value"
                        : userPreferenceThing.Value;
                    break;
                case Citation citationThing:
                    // broken citations are a result of 10-25 paradox thus shall be removed
                    if (citationThing.Container is Definition container)
                    {
                        container.Citation.Remove(citationThing);
                        container.Cache?.TryRemove(citationThing.CacheKey, out _);
                    }

                    break;
                case Participant participantThing:
                    if (participantThing.Container is EngineeringModelSetup modelSetup)
                    {
                        modelSetup.Participant.Remove(participantThing);
                        modelSetup.Cache.TryRemove(participantThing.CacheKey, out _);
                    }

                    break;
                case Definition contentThing:
                    contentThing.Content = rowError.Error.Contains("Content")
                        ? "No Value"
                        : contentThing.Content;
                    break;
                case IterationSetup iterationSetupThing:
                    iterationSetupThing.Description = rowError.Error.Contains("Description")
                        ? "No Description"
                        : iterationSetupThing.Description;
                    break;
            }
        }

        /// <summary>
        /// Fix name/short-name errors
        /// </summary>
        /// <param name="rowError">Error row <see cref="PocoErrorRowViewModel"/></param>
        private static void FixNameAndShortName(PocoErrorRowViewModel rowError)
        {
            if (rowError.Thing is IShortNamedThing shortNamedThing && rowError.Error.Contains("ShortName"))
            {
                shortNamedThing.ShortName = "UndefinedShortName";
            }

            if (rowError.Thing is INamedThing namedThing && rowError.Error.Contains("Name"))
            {
                namedThing.Name = "Undefined Name";
            }
        }
    }
}
