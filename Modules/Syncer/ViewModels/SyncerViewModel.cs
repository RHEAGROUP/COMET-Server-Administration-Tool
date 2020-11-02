// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationViewModel.cs" company="RHEA System S.A.">
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

namespace Syncer.ViewModels
{
    using CDP4Common.CommonData;
    using Common.ViewModels;
    using ReactiveUI;
    using Syncer.Utils;
    using Syncer.Utils.Sync;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;

    /// <summary>
    /// The view-model for the Syncer tool
    /// </summary>
    public class SyncerViewModel : ReactiveObject
    {
        /// <summary>
        /// Property describing the possible ClassKinds to be synced
        /// </summary>
        public static Dictionary<ThingType, string> ThingTypes { get; } =
            new Dictionary<ThingType, string>
            {
                { ThingType.DomainOfExpertise, "Domain Of Expertise" },
                { ThingType.SiteReferenceDataLibrary, "Site Reference Data Library" }
            };

        /// <summary>
        /// Backing field for the <see cref="SelectedThingType"/> property
        /// </summary>
        private ThingType selectedThingType;

        /// <summary>
        /// Gets or sets selected ClassKinds to be synced value
        /// </summary>
        public ThingType SelectedThingType
        {
            get => this.selectedThingType;
            set => this.RaiseAndSetIfChanged(ref this.selectedThingType, value);
        }

        /// <summary>
        /// Backing field for the source view model <see cref="LoginViewModel"/>
        /// </summary>
        private LoginViewModel sourceViewModel;

        /// <summary>
        /// Gets or sets the source view model
        /// </summary>
        public LoginViewModel SourceViewModel
        {
            get => this.sourceViewModel;
            set => this.RaiseAndSetIfChanged(ref this.sourceViewModel, value);
        }

        /// <summary>
        /// Backing field for the target view model <see cref="LoginViewModel"/>
        /// </summary>
        private LoginViewModel targetViewModel;

        /// <summary>
        /// Gets or sets the target view model
        /// </summary>
        public LoginViewModel TargetViewModel
        {
            get => this.targetViewModel;
            set => this.RaiseAndSetIfChanged(ref this.targetViewModel, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Common.ViewModels.SiteReferenceDataLibraryViewModel"/> tab
        /// </summary>
        private SiteReferenceDataLibraryViewModel siteReferenceDataLibraryViewModel;

        /// <summary>
        /// Gets or sets the <see cref="Common.ViewModels.SiteReferenceDataLibraryViewModel"/> tab
        /// </summary>
        public SiteReferenceDataLibraryViewModel SiteReferenceDataLibraryViewModel
        {
            get => this.siteReferenceDataLibraryViewModel;
            set => this.RaiseAndSetIfChanged(ref this.siteReferenceDataLibraryViewModel, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Common.ViewModels.DomainOfExpertiseViewModel"/> tab
        /// </summary>
        private DomainOfExpertiseViewModel domainOfExpertiseViewModel;

        /// <summary>
        /// Gets or sets the <see cref="Common.ViewModels.DomainOfExpertiseViewModel"/> tab
        /// </summary>
        public DomainOfExpertiseViewModel DomainOfExpertiseViewModel
        {
            get => this.domainOfExpertiseViewModel;
            set => this.RaiseAndSetIfChanged(ref this.domainOfExpertiseViewModel, value);
        }

        /// <summary>
        /// Out property for the <see cref="CanSync"/> property
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> canSync;

        /// <summary>
        /// Gets a value indicating whether a sync operation can start
        /// </summary>
        public bool CanSync => this.canSync.Value;

        /// <summary>
        /// Gets the server sync command
        /// </summary>
        public ReactiveCommand<Unit> SyncCommand { get; }

        /// <summary>
        /// Backing field for the the <see cref="Output"/> messages property
        /// </summary>
        private string output;

        /// <summary>
        /// Gets or sets operation output messages
        /// </summary>
        public string Output
        {
            get => this.output;
            set => this.RaiseAndSetIfChanged(ref this.output, value);
        }

        /// <summary>
        /// The <see cref="SyncerFactory"/> used to build the helper sync classes
        /// </summary>
        private readonly SyncerFactory syncerFactory = SyncerFactory.GetInstance();

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncerViewModel"/> class
        /// </summary>
        public SyncerViewModel()
        {
            var canExecuteSync = this.WhenAnyValue(
                vm => vm.SourceViewModel.LoginSuccessfully,
                vm => vm.TargetViewModel.LoginSuccessfully,
                (sourceLoggedIn, targetLoggedIn) => sourceLoggedIn && targetLoggedIn);

            canExecuteSync.ToProperty(this, vm => vm.CanSync, out this.canSync);

            this.SyncCommand = ReactiveCommand.CreateAsyncTask(
                canExecuteSync,
                _ => this.ExecuteSync(),
                RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Add subscription to the login view models
        /// </summary>
        public void AddSubscriptions()
        {
            this.WhenAnyValue(vm => vm.SourceViewModel.Output).Subscribe(UpdateOutput);
            this.WhenAnyValue(vm => vm.TargetViewModel.Output).Subscribe(UpdateOutput);

            this.WhenAnyValue(vm => vm.SourceViewModel.LoginSuccessfully).Subscribe(
                (sourceLoggedIn) =>
                {
                    if (!sourceLoggedIn) return;

                    this.SiteReferenceDataLibraryViewModel =
                        new SiteReferenceDataLibraryViewModel(this.SourceViewModel.ServerSession);

                    this.DomainOfExpertiseViewModel =
                        new DomainOfExpertiseViewModel(this.SourceViewModel.ServerSession);
                });
        }

        /// <summary>
        /// Add a text message to the output panel
        /// </summary>
        /// <param name="message">
        /// The text message
        /// </param>
        private void UpdateOutput(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            this.Output += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        }

        /// <summary>
        /// Executes the sync command
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        private async Task ExecuteSync()
        {
            var syncer = syncerFactory.CreateSyncer(
                this.SelectedThingType,
                this.SourceViewModel.ServerSession,
                this.TargetViewModel.ServerSession);

            IEnumerable<Thing> selectedThings;
            switch (this.SelectedThingType)
            {
                case ThingType.DomainOfExpertise:
                    selectedThings = this.DomainOfExpertiseViewModel.DomainsOfExpertise
                        .Where(r => r.IsSelected)
                        .Select(r => r.Thing);
                    break;
                case ThingType.SiteReferenceDataLibrary:
                    selectedThings = this.SiteReferenceDataLibraryViewModel.SiteReferenceDataLibraries
                        .Where(r => r.IsSelected)
                        .Select(r => r.Thing);
                    break;
                default:
                    throw new ArgumentException("Invalid value", nameof(this.SelectedThingType));
            }

            try
            {
                await syncer.Sync(selectedThings);
                UpdateOutput("Sync successful");
            }
            catch (Exception e)
            {
                UpdateOutput(e.Message);
            }
        }
    }
}
