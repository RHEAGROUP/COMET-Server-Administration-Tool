// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Dal;
    using CDP4Rules;
    using System;
    using System.Linq;
    using PlainObjects;
    using ReactiveUI;

    /// <summary>
    /// The view-model for the Source server errors that will be displayed before migration
    /// </summary>
    public class ErrorViewModel : ReactiveObject
    {
        /// <summary>
        /// Backing field for the <see cref="ISession"/> property
        /// </summary>
        private ISession serverSession;

        /// <summary>
        /// Gets or sets server session
        /// </summary>
        public ISession ServerSession
        {
            private get => this.serverSession;
            set => this.RaiseAndSetIfChanged(ref this.serverSession, value);
        }

        /// <summary>
        /// Backing field for the <see cref="PocoErrors"/> property
        /// </summary>
        private ReactiveList<PocoErrorRowViewModel> pocoErrors;

        /// <summary>
        /// Gets or sets poco errors list
        /// </summary>
        public ReactiveList<PocoErrorRowViewModel> PocoErrors
        {
            get => this.pocoErrors;
            private set => this.RaiseAndSetIfChanged(ref this.pocoErrors, value);
        }

        /// <summary>
        /// Backing field for the <see cref="RuleCheckerErrors"/> property
        /// </summary>
        private ReactiveList<RuleCheckerErrorRowViewModel> ruleCheckerErrors;

        /// <summary>
        /// Gets or sets rule checker errors list
        /// </summary>
        public ReactiveList<RuleCheckerErrorRowViewModel> RuleCheckerErrors
        {
            get => this.ruleCheckerErrors;
            private set => this.RaiseAndSetIfChanged(ref this.ruleCheckerErrors, value);
        }

        /// <summary>
        /// Gets or sets the selected error item inside errors poco grid
        /// </summary>
        public PocoErrorRowViewModel CurrentPocoError { get; set; }

        /// <summary>
        /// Gets or sets the selected error item inside errors model grid
        /// </summary>
        public RuleCheckerErrorRowViewModel CurrentModelError { get; set; }

        /// <summary>
        /// Gets or sets the poco grid row double click command
        /// </summary>
        public ReactiveCommand<object> PocoSelectRowCommand { get; private set; }

        /// <summary>
        /// Gets or sets  the grid row double click command
        /// </summary>
        public ReactiveCommand<object> ModelSelectRowCommand { get; private set; }

        /// <summary>
        /// Out property for the <see cref="IsDetailsVisible"/> property
        /// </summary>
        private bool isDetailsVisible;

        /// <summary>
        /// Gets a value indicating whether error details should be displayed
        /// </summary>
        public bool IsDetailsVisible
        {
            get => this.isDetailsVisible;
            set => this.RaiseAndSetIfChanged(ref this.isDetailsVisible, value);
        }

        /// <summary>
        /// Out property for the <see cref="ErrorDetails"/> property
        /// </summary>
        private string errorDetails;

        /// <summary>
        /// Gets or sets error details that will be displayed inside error details group
        /// </summary>
        public string ErrorDetails
        {
            get => this.errorDetails;
            set => this.RaiseAndSetIfChanged(ref this.errorDetails, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorViewModel"/> class.
        /// </summary>
        public ErrorViewModel(ISession serverSession)
        {
            this.serverSession = serverSession;

            this.PocoErrors = new ReactiveList<PocoErrorRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            this.RuleCheckerErrors = new ReactiveList<RuleCheckerErrorRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            this.IsDetailsVisible = false;
            this.ErrorDetails = string.Empty;
            this.CurrentPocoError = null;
            this.CurrentModelError = null;

            this.WhenAnyValue(vm => vm.ServerSession).Subscribe(session =>
            {
                BindPocoErrors(session);
                BindRuleCheckerErrors(session);
            });

            this.PocoSelectRowCommand = ReactiveCommand.Create();
            this.PocoSelectRowCommand.Subscribe(_ => this.ExecuteSelectErrorPocoRow());

            this.ModelSelectRowCommand = ReactiveCommand.Create();
            this.ModelSelectRowCommand.Subscribe(_ => this.ExecuteSelectErrorModelRow());
        }

        /// <summary>
        /// Execute selection grid command
        /// </summary>
        private void ExecuteSelectErrorPocoRow()
        {
            if (this.CurrentPocoError is null)
            {
                this.IsDetailsVisible = false;
                return;
            }

            this.IsDetailsVisible = true;
            this.ErrorDetails = this.CurrentPocoError.ToString();
        }

        /// <summary>
        /// Execute selection grid command
        /// </summary>
        private void ExecuteSelectErrorModelRow()
        {
            if (this.CurrentModelError is null)
            {
                this.IsDetailsVisible = false;
                return;
            }

            this.IsDetailsVisible = true;
            this.ErrorDetails = this.CurrentModelError.ToString();
        }

        /// <summary>
        /// Apply PocoCardinality & PocoProperties to the E10-25 data set and bind errors to the reactive list
        /// </summary>
        /// <param name="currentSession">Current source server opened session <see cref="ISession" /></param>
        private void BindPocoErrors(ISession currentSession)
        {
            if (currentSession is null)
            {
                return;
            }

            this.PocoErrors.Clear();

            foreach (var thing in currentSession.Assembler.Cache.Select(item => item.Value.Value)
                .Where(t => t.ValidationErrors.Any()))
            {
                foreach (var error in thing.ValidationErrors)
                {
                    this.PocoErrors.Add(new PocoErrorRowViewModel(thing, error));
                }
            }
        }

        /// <summary>
        /// Apply RuleCheckerEngine to the E10-25 data set and bind errors to the reactive list
        /// </summary>
        /// <param name="currentSession">Current source server opened session <see cref="ISession" /></param>
        private void BindRuleCheckerErrors(ISession currentSession)
        {
            if (currentSession is null)
            {
                return;
            }

            var ruleCheckerEngine = new RuleCheckerEngine();
            var resultList = ruleCheckerEngine.Run(currentSession.Assembler.Cache.Select(item => item.Value.Value));

            this.RuleCheckerErrors.Clear();

            foreach (var result in resultList)
            {
                this.RuleCheckerErrors.Add(new RuleCheckerErrorRowViewModel(result.Thing, result.Id, result.Description,
                    result.Severity));
            }
        }
    }
}
