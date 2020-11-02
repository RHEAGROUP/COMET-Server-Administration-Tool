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

    public class SyncerViewModel : ReactiveObject
    {
        public static Dictionary<ThingType, string> ThingTypes { get; } =
            new Dictionary<ThingType, string>
            {
                { ThingType.DomainOfExpertise, "Domain Of Expertise" },
                { ThingType.SiteReferenceDataLibrary, "Site Reference Data Library" }
            };

        private ThingType selectedThingType;

        public ThingType SelectedThingType
        {
            get => this.selectedThingType;
            set => this.RaiseAndSetIfChanged(ref this.selectedThingType, value);
        }

        private LoginViewModel sourceViewModel;

        public LoginViewModel SourceViewModel
        {
            get => this.sourceViewModel;
            set => this.RaiseAndSetIfChanged(ref this.sourceViewModel, value);
        }

        private LoginViewModel targetViewModel;

        public LoginViewModel TargetViewModel
        {
            get => this.targetViewModel;
            set => this.RaiseAndSetIfChanged(ref this.targetViewModel, value);
        }

        private SiteReferenceDataLibraryViewModel siteReferenceDataLibraryViewModel;

        public SiteReferenceDataLibraryViewModel SiteReferenceDataLibraryViewModel
        {
            get => this.siteReferenceDataLibraryViewModel;
            set => this.RaiseAndSetIfChanged(ref this.siteReferenceDataLibraryViewModel, value);
        }

        private DomainOfExpertiseViewModel domainOfExpertiseViewModel;

        public DomainOfExpertiseViewModel DomainOfExpertiseViewModel
        {
            get => this.domainOfExpertiseViewModel;
            set => this.RaiseAndSetIfChanged(ref this.domainOfExpertiseViewModel, value);
        }

        private readonly ObservableAsPropertyHelper<bool> canSync;

        public bool CanSync => this.canSync.Value;

        public ReactiveCommand<Unit> SyncCommand { get; }

        private string output;

        public string Output
        {
            get => this.output;
            set => this.RaiseAndSetIfChanged(ref this.output, value);
        }

        private readonly SyncerFactory syncerFactory = new SyncerFactory();

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

        private void UpdateOutput(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            this.Output += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        }

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
