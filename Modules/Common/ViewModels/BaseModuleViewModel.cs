// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseModuleViewModel.cs" company="RHEA System S.A.">
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

namespace Common.ViewModels
{
    using System;
    using System.Reactive;
    using System.Threading;
    using System.Threading.Tasks;
    using CDP4Dal;
    using Events;
    using NLog;
    using ReactiveUI;

    /// <summary>
    /// The base viewmodel of the SAT modules
    /// </summary>
    public class BaseModuleViewModel : ReactiveObject
    {
        /// <summary>
        /// The NLog logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        ///// <summary>
        ///// Backing field for the source view model <see cref="LoginViewModel"/>
        ///// </summary>
        //private ILoginViewModel sourceViewModel;

        ///// <summary>
        ///// Gets or sets the source view model
        ///// </summary>
        //public ILoginViewModel SourceViewModel
        //{
        //    get => this.sourceViewModel;
        //    set => this.RaiseAndSetIfChanged(ref this.sourceViewModel, value);
        //}

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
        /// Out property for the <see cref="CanExecute"/> property
        /// </summary>
        protected ObservableAsPropertyHelper<bool> CanExecutePropertyHelper;

        /// <summary>
        /// Gets a value indicating whether a migration operation can start
        /// </summary>
        public bool CanExecute => this.CanExecutePropertyHelper.Value;

        /// <summary>
        /// Gets the server migrate command
        /// </summary>
        public ReactiveCommand<Unit> ModuleCommand { get; set; }

        /// <summary>
        /// Set model properties
        /// </summary>
        public virtual void SetProperties()
        {
            this.Output = string.Empty;
        }

        /// <summary>
        /// Add subscriptions
        /// </summary>
        public virtual void AddSubscriptions()
        {
            //this.WhenAnyValue(vm => vm.SourceViewModel.Output).Subscribe(_ =>
            //{
            //    OperationMessageHandler(this.SourceViewModel.Output);
            //});

            //var canExecute = this.WhenAnyValue(
            //    vm => vm.SourceViewModel.LoginSuccessfully,
            //    vm => vm.SourceViewModel.LoginSuccessfully,
            //    (sourceLoggedIn, targetLoggedIn) => sourceLoggedIn && targetLoggedIn);

            //canExecute.ToProperty(this, vm => vm.CanExecute, out this.CanExecutePropertyHelper);

            //this.WhenAnyValue(vm => vm.Output).Subscribe(_ => { OperationMessageHandler(this.Output); });

            //CDPMessageBus.Current.Listen<LogEvent>(this.SourceViewModel).Subscribe(operationEvent =>
            //{

            //});

            CDPMessageBus.Current.Listen<LogEvent>().Subscribe(operationEvent =>
            {
                if (!this.GetType().Equals(operationEvent.Type))
                {
                    return;
                }

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

        // TODO #81 Unify output messages mechanism inside SAT solution
        /// <summary>
        /// Add text message to the output panel
        /// </summary>
        /// <param name="message">The text message</param>
        /// <param name="logLevel"></param>
        public void OperationMessageHandler(string message, LogVerbosity? logLevel = null)
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
