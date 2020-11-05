// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineeringModelViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using System;
    using System.Linq;
    using PlainObjects;
    using ReactiveUI;
    using System.Collections.Generic;

    /// <summary>
    /// The view-model for the Source and target server that is responsible for getting engineering models
    /// </summary>
    public class EngineeringModelViewModel : ReactiveObject
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
        /// Backing field for the <see cref="EngineeringModels"/> property
        /// </summary>
        private ReactiveList<EngineeringModelRowViewModel> engineeringModels;

        /// <summary>
        /// Gets or sets engineering models list
        /// </summary>
        public ReactiveList<EngineeringModelRowViewModel> EngineeringModels
        {
            get => this.engineeringModels;
            private set => this.RaiseAndSetIfChanged(ref this.engineeringModels, value);
        }

        /// <summary>
        /// Gets or sets the command to select/unselect model for import
        /// </summary>
        public ReactiveCommand<object> CheckUncheckModel { get; set; }

        /// <summary>
        /// Gets or sets the command to select/unselect all models for import
        /// </summary>
        public ReactiveCommand<object> CheckUncheckAllModels { get; set; }

        /// <summary>
        /// Out property for the <see cref="SelectAllModels"/> property
        /// </summary>
        private bool selectAllModels;

        /// <summary>
        /// Gets a value indicating whether all models are selected
        /// </summary>
        public bool SelectAllModels
        {
            get => this.selectAllModels;
            set => this.RaiseAndSetIfChanged(ref this.selectAllModels, value);
        }

        /// <summary>
        /// Delegate used for notifying current engineering models selection changes
        /// </summary>
        public delegate void EngineeringModelsListSelectionChangedDelegate(List<EngineeringModelRowViewModel> engineeringModels);

        /// <summary>
        /// Associated event with the <see cref="EngineeringModelsListSelectionChangedDelegate"/>
        /// </summary>
        public event EngineeringModelsListSelectionChangedDelegate ModelListChangedEvent;

        /// <summary>
        /// Invoke ModelListChangedEvent
        /// </summary>
        /// <param name="models">Currently selected engineering models</param>
        private void NotifyEngineeringModelsListChanges(List<EngineeringModelRowViewModel> models)
        {
            ModelListChangedEvent?.Invoke(models);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineeringModelViewModel"/> class.
        /// </summary>
        public EngineeringModelViewModel(ISession serverSession)
        {
            this.serverSession = serverSession;
            this.EngineeringModels = new ReactiveList<EngineeringModelRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            this.CheckUncheckModel = ReactiveCommand.Create();
            this.CheckUncheckModel.Subscribe(_ => this.ExecuteCheckUncheckModel());

            this.CheckUncheckAllModels = ReactiveCommand.Create();
            this.CheckUncheckAllModels.Subscribe(_ => this.ExecuteCheckUncheckAllModels());

            this.WhenAnyValue(vm => vm.ServerSession).Subscribe(session =>
            {
                this.BindEngineeringModels(this.ServerSession.RetrieveSiteDirectory());
            });
        }

        /// <summary>
        /// Select model for the migration procedure
        /// </summary>
        private void ExecuteCheckUncheckModel()
        {
            this.SelectAllModels = !(this.EngineeringModels.Where(em => !em.IsSelected).Count() > 0);
            NotifyEngineeringModelsListChanges(this.EngineeringModels.Where(em => em.IsSelected).ToList());
        }

        /// <summary>
        /// Select/unselect all models for the migration procedure
        /// </summary>
        private void ExecuteCheckUncheckAllModels()
        {
            foreach (var model in this.EngineeringModels)
            {
                model.IsSelected = this.SelectAllModels;
            }
            NotifyEngineeringModelsListChanges(this.EngineeringModels.Where(em => em.IsSelected).ToList());
        }

        /// <summary>
        /// Bind engineering models to the reactive list
        /// </summary>
        /// <param name="siteDirectory">The <see cref="SiteDirectory"/> top container</param>
        private void BindEngineeringModels(SiteDirectory siteDirectory)
        {
            this.EngineeringModels.Clear();

            foreach (var modelSetup in siteDirectory.Model.OrderBy(m => m.Name))
            {
                this.EngineeringModels.Add(new EngineeringModelRowViewModel(modelSetup));
            }

            this.SelectAllModels = true;
        }
    }
}
