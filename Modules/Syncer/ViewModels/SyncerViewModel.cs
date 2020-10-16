// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Syncer.ViewModels
{
    using Common.ViewModels;
    using ReactiveUI;
    using Syncer.Utils;
    using System;
    using System.Collections.Generic;
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

        private readonly ObservableAsPropertyHelper<bool> canSync;

        public bool CanSync => this.canSync.Value;

        public ReactiveCommand<Unit> SyncCommand { get; }

        private string output;

        public string Output
        {
            get => this.output;
            set => this.RaiseAndSetIfChanged(ref this.output, value);
        }

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
        }

        private void UpdateOutput(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            this.Output += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        }

        private async Task ExecuteSync()
        {
            // TODO add business logic
        }
    }
}
