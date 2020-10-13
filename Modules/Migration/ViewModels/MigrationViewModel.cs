// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.ViewModels
{
    using Utils;
    using Microsoft.Win32;
    using ReactiveUI;
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Common.ViewModels;

    /// <summary>
    /// The view-model for the Migration that lets users to migrate models between different data servers
    /// </summary>
    public class MigrationViewModel : ReactiveObject
    {
        /// <summary>
        /// Migration class reference
        /// </summary>
        private readonly Migration migration;

        /// <summary>
        /// Backing field for <see cref="FileIsChecked"/>
        /// </summary>
        private bool fileIsChecked;

        /// <summary>
        /// Gets or sets file source as option for migration
        /// </summary>
        public bool FileIsChecked
        {
            get => this.fileIsChecked;
            set => this.RaiseAndSetIfChanged(ref this.fileIsChecked, value);
        }

        /// <summary>
        /// Backing field for the source view model <see cref="LoginViewModel"/>
        /// </summary>
        private LoginViewModel loginSourceViewModel;

        /// <summary>
        /// Gets or sets the logging source view model
        /// </summary>
        public LoginViewModel SourceViewModel
        {
            get => this.loginSourceViewModel;
            set => this.RaiseAndSetIfChanged(ref this.loginSourceViewModel, value);
        }

        /// <summary>
        /// Backing field for the target view model <see cref="LoginViewModel"/>
        /// </summary>
        private LoginViewModel loginTargetViewModel;

        /// <summary>
        /// Gets or sets the target source view model
        /// </summary>
        public LoginViewModel TargetViewModel
        {
            get => this.loginTargetViewModel;
            set => this.RaiseAndSetIfChanged(ref this.loginTargetViewModel, value);
        }

        /// <summary>
        /// Backing field for the the output messages <see cref="Output"/>
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
        /// Backing field for the the migration file <see cref="MigrationFile"/>
        /// </summary>
        private string migrationFile;

        /// <summary>
        /// Gets or sets operation migration file path
        /// </summary>
        public string MigrationFile
        {
            get => this.migrationFile;
            set => this.RaiseAndSetIfChanged(ref this.migrationFile, value);
        }

        /// <summary>
        /// Out property for the <see cref="CanMigrate"/> property
        /// </summary>
        private readonly ObservableAsPropertyHelper<bool> canMigrate;

        /// <summary>
        /// Gets a value indicating whether a migration operation can start
        /// </summary>
        public bool CanMigrate => this.canMigrate.Value;

        /// <summary>
        /// Add subscription to the login view models
        /// </summary>
        public void AddSubscriptions()
        {
            this.WhenAnyValue(vm => vm.SourceViewModel.Output).Subscribe(UpdateOutput);
            this.WhenAnyValue(vm => vm.TargetViewModel.Output).Subscribe(UpdateOutput);

            this.WhenAnyValue(
                vm => vm.SourceViewModel.LoginSuccessfully,
                vm => vm.SourceViewModel.ServerSession,
                (loginSuccessfully, dataSourceSession) => loginSuccessfully && dataSourceSession != null)
                .Where(canContinue => canContinue)
                .Subscribe(_ =>
            {
                this.migration.SourceSession = this.SourceViewModel.ServerSession;
            });

            this.WhenAnyValue(
                vm => vm.TargetViewModel.LoginSuccessfully,
                vm => vm.TargetViewModel.ServerSession,
                (loginSuccessfully, dataSourceSession) => loginSuccessfully && dataSourceSession != null)
                .Where(canContinue => canContinue)
                .Subscribe(_ =>
            {
                this.migration.TargetSession = this.TargetViewModel.ServerSession;
            });
        }

        /// <summary>
        /// Gets the migration file <see cref="IReactiveCommand"/>
        /// </summary>
        public ReactiveCommand<object> LoadMigrationFile { get; private set; }

        /// <summary>
        /// Gets the server migrate command
        /// </summary>
        public ReactiveCommand<Unit> MigrateCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationViewModel"/> class
        /// </summary>
        public MigrationViewModel()
        {
            var canExecuteMigrate = this.WhenAnyValue(
                vm => vm.SourceViewModel.LoginSuccessfully,
                vm => vm.SourceViewModel.ServerSession,
                vm => vm.TargetViewModel.LoginSuccessfully,
                vm => vm.TargetViewModel.ServerSession,
                (sourceLoginSuccessfully, sourceSession, targetLoginSuccessfully, targetSession) =>
                    sourceLoginSuccessfully && sourceSession != null && targetLoginSuccessfully && targetSession != null);
            canExecuteMigrate.ToProperty(this, vm => vm.CanMigrate, out this.canMigrate);

            this.FileIsChecked = false;

            this.migration = new Migration();
            this.migration.OperationMessageEvent += this.UpdateOutput;
            this.migration.OperationStepEvent += this.UpdateUi;

            this.LoadMigrationFile = ReactiveCommand.Create();
            this.LoadMigrationFile.Subscribe(_ => this.ExecuteLoadMigrationFile());

            this.MigrateCommand = ReactiveCommand.CreateAsyncTask(canExecuteMigrate,
                x => this.ExecuteMigration(), RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Trigger loading of the person migration.json file to the application
        /// </summary>
        private void ExecuteLoadMigrationFile()
        {
            var openFileDialog = new OpenFileDialog()
            {
                InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\",
                Filter = "Json files (*.json)|*.json"
            };

            var dialogResult = openFileDialog.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value && openFileDialog.FileNames.Length == 1)
            {
                this.MigrationFile = openFileDialog.FileNames[0];
            }
        }

        /// <summary>
        /// Executes migration command
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        private async Task ExecuteMigration()
        {
            await this.migration.ImportData(this.SourceViewModel.EngineeringModels);
            await this.migration.ExportData();
            // TODO #33 add cleanup after migration
        }

        /// <summary>
        /// Add migration log to the output panel
        /// </summary>
        /// <param name="step">
        /// Migration operation step <see cref="MigrationStep"/>
        /// </param>
        private void UpdateUi(MigrationStep step)
        {
            switch (step)
            {
                case MigrationStep.ImportStart:
                    this.UpdateOutput("Import operation start");
                    break;
                case MigrationStep.ImportEnd:
                    this.UpdateOutput("Import operation end");
                    break;
                case MigrationStep.ExportStart:
                    this.UpdateOutput("Export operation start");
                    break;
                case MigrationStep.ExportEnd:
                    this.UpdateOutput("Export operation end");
                    break;
            }
        }

        /// <summary>
        /// Add text message to the output panel
        /// </summary>
        /// <param name="message">The text message</param>
        private void UpdateOutput(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            this.Output += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        }
    }
}
