// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FixCoordinalityErrorsDialogViewModel.cs" company="RHEA System S.A.">
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
    using Common.ViewModels.PlainObjects;
    using ReactiveUI;

    /// <summary>
    /// The viewmodel of the migration coordinality fix wizard.
    /// </summary>
    public class FixCoordinalityErrorsDialogViewModel : ReactiveObject, IFixCoordinalityErrorsDialogViewModel
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
        /// Initializes a new instance of the <see cref="FixCoordinalityErrorsDialogViewModel" /> class.
        /// </summary>
        /// <param name="migrationSourceSession">The migration source <see cref="ISession" /></param>
        public FixCoordinalityErrorsDialogViewModel(ISession migrationSourceSession)
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
                return;
            }

            this.Errors.Clear();
            this.IsBusy = true;

            var d = Task.Run(this.GetErrorRows).Result;

            this.Errors.AddRange(d);

            this.IsBusy = false;
        }

        /// <summary>
        /// Gets the list of <see cref="PocoErrorRowViewModel" />
        /// </summary>
        /// <returns>A list of rows containing all errors in cache.</returns>
        private List<PocoErrorRowViewModel> GetErrorRows()
        {
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
        /// Executes the command to fix POCO coordinality errors
        /// </summary>
        private void ExecuteFixCommand()
        {
            this.IsBusy = true;

            foreach (var rowError in this.Errors)
            {
                if (rowError.Error.Contains("ShortName"))
                {
                    if (rowError.Thing is IShortNamedThing shortNamedThing)
                    {
                        shortNamedThing.ShortName = "UndefinedShortName";
                    }
                }
                else if (rowError.Error.Contains("Name"))
                {
                    if (rowError.Thing is INamedThing namedThing)
                    {
                        namedThing.Name = "Undefined Name";
                    }
                }
                else if (rowError.Error.Contains("Content"))
                {
                    if (rowError.Thing is Definition contentThing)
                    {
                        contentThing.Content = "No Content";
                    }
                }
                else if (rowError.Error.Contains("Extension"))
                {
                    if (rowError.Thing is FileType fileThing)
                    {
                        fileThing.Extension = "UnknownExtension";
                    }
                }
                else if (rowError.Error.Contains("Description"))
                {
                    if (rowError.Thing is IterationSetup iterationSetupThing)
                    {
                        iterationSetupThing.Description = "No Description";
                    }
                }
                else if (rowError.Error.Contains("Source is null"))
                {
                    // broken citations are a result of 10-25 paradox thus shall be removed
                    if (rowError.Thing is Citation citationThing)
                    {
                        if (citationThing.Container is Definition container)
                        {
                            container.Citation.Remove(citationThing);
                            container.Cache.TryRemove(citationThing.CacheKey, out _);
                        }
                    }
                }

                rowError.Thing.ValidatePoco();
            }

            this.Errors.Clear();
            var d = Task.Run(this.GetErrorRows).Result;

            this.Errors.AddRange(d);

            this.IsBusy = false;
        }
    }
}
