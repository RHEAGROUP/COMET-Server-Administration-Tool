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

namespace Migration.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using CDP4Dal;
    using Common.Events;
    using Common.ViewModels;
    using Microsoft.Win32;
    using NLog;
    using ReactiveUI;
    using Utils;
    using Views;

    /// <summary>
    /// The view-model for the Migration that lets users to migrate models between different data servers
    /// </summary>
    public class MigrationViewModel : ReactiveObject
    {
        /// <summary>
        /// The NLog logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Gets data source server type
        /// </summary>
        public static Dictionary<DataSource, string> MigrationTargetServerTypes { get; } =
            new Dictionary<DataSource, string>
            {
                {DataSource.CDP4, "CDP4 WebServices"}
            };

        /// <summary>
        /// Migration class reference
        /// </summary>
        private Migration MigrationFactory { get; set; }

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
        private ILoginViewModel loginSourceViewModel;

        /// <summary>
        /// Gets or sets the source view model
        /// </summary>
        public ILoginViewModel SourceViewModel
        {
            get => this.loginSourceViewModel;
            set => this.RaiseAndSetIfChanged(ref this.loginSourceViewModel, value);
        }

        /// <summary>
        /// Backing field for the target view model <see cref="LoginViewModel"/>
        /// </summary>
        private ILoginViewModel loginTargetViewModel;

        /// <summary>
        /// Gets or sets the target view model
        /// </summary>
        public ILoginViewModel TargetViewModel
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
            this.WhenAnyValue(vm => vm.SourceViewModel.Output).Subscribe(_ => {
                OperationMessageHandler(this.SourceViewModel.Output);
            });
            this.WhenAnyValue(vm => vm.TargetViewModel.Output).Subscribe(_ => {
                OperationMessageHandler(this.TargetViewModel.Output);
            });

            this.WhenAnyValue(
                vm => vm.SourceViewModel.LoginSuccessfully,
                vm => vm.SourceViewModel.ServerSession,
                (loginSuccessfully, dataSourceSession) => loginSuccessfully && dataSourceSession != null)
                .Where(canContinue => canContinue)
                .Subscribe(_ =>
            {
                this.MigrationFactory.SourceSession = this.SourceViewModel.ServerSession;
            });

            this.WhenAnyValue(
                vm => vm.TargetViewModel.LoginSuccessfully,
                vm => vm.TargetViewModel.ServerSession,
                (loginSuccessfully, dataSourceSession) => loginSuccessfully && dataSourceSession != null)
                .Where(canContinue => canContinue)
                .Subscribe(_ =>
            {
                this.MigrationFactory.TargetSession = this.TargetViewModel.ServerSession;
            });

            CDPMessageBus.Current.Listen<LogEvent>().Subscribe((operationEvent) => {
                var message = operationEvent.Message;
                var exception = operationEvent.Exception;
                var logLevel = operationEvent.Verbosity;

                if (operationEvent.Exception != null)
                {
                    message += $"\n\tException: {exception.Message}";

                    if (exception.InnerException != null)
                    {
                        message += $"\n\tInner exception: {exception.InnerException.Message}";
                        message += $"\n{exception.InnerException.StackTrace}";
                    }
                    else
                    {
                        message += $"\n{exception.StackTrace}";
                    }
                }

                this.OperationMessageHandler(message, logLevel);
            });
        }

        /// <summary>
        /// Gets the migration file <see cref="IReactiveCommand"/>
        /// </summary>
        public ReactiveCommand<object> LoadMigrationFileCommand { get; private set; }

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

            this.WhenAnyValue(vm => vm.FileIsChecked).Subscribe(_ =>
            {
                if (!this.FileIsChecked && !string.IsNullOrEmpty(this.MigrationFile))
                {
                    this.MigrationFile = null;
                }
            });

            this.FileIsChecked = false;

            this.MigrationFactory = new Migration();

            this.LoadMigrationFileCommand = ReactiveCommand.Create();
            this.LoadMigrationFileCommand.Subscribe(_ => this.ExecuteLoadMigrationFile());

            this.MigrateCommand = ReactiveCommand.CreateAsyncTask(canExecuteMigrate,
                x => this.ExecuteMigration(), RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Trigger loading of the person migration.json file to the application
        /// </summary>
        private void ExecuteLoadMigrationFile()
        {
            if (!this.FileIsChecked)
            {
                return;
            }

            if (Application.Current == null)
            {
                return;
            }

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
            var result = await this.MigrationFactory.ImportData(this.SourceViewModel?.EngineeringModels);

            if (!result)
            {
                this.OperationMessageHandler("Migration import of data failed");
                return;
            }

            // Pop a wizard with POCO errors for whole session
            if (Application.Current != null)
            {
                var fixCardinalityDialog = new FixCardinalityErrorsDialog
                {
                    DataContext = new FixCardinalityErrorsDialogViewModel(this.MigrationFactory.SourceSession)
                };
                var dialogResult = fixCardinalityDialog.ShowDialog();

                if (dialogResult != true)
                {
                    this.OperationMessageHandler("Migration canceled");
                    return;
                }
            }

            result = await this.MigrationFactory.PackData(this.MigrationFile);

            if (!result)
            {
                this.OperationMessageHandler("Migration pack of data failed");
                return;
            }

            if (Application.Current != null)
            {
                result = await this.MigrationFactory.ExportData();

                if (!result)
                {
                    this.OperationMessageHandler("Migration export failed");
                }
            }

            // TODO #33 add cleanup after migration
        }

        /// <summary>
        /// Add text message to the output panel
        /// </summary>
        /// <param name="message">The text message</param>
        /// <param name="logLevel"></param>
        private void OperationMessageHandler(string message, LogVerbosity? logLevel = null)
        {
            if (string.IsNullOrEmpty(message)) return;

            this.Output += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";

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
    }
}
