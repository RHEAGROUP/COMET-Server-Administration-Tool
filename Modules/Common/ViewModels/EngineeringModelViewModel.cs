// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineeringModelViewModel.cs">
//    Copyright (c) 2020
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
        /// Delegate used for notifying current engineering list changes
        /// </summary>
        public delegate void EngineeringModelsListChangedDelegate(List<EngineeringModelRowViewModel> engineeringModels);

        /// <summary>
        /// Associated event with the <see cref="EngineeringModelsListChangedDelegate"/>
        /// </summary>
        public event EngineeringModelsListChangedDelegate ModelListChangedEvent;

        /// <summary>
        /// Invoke ModelListChangedEvent
        /// </summary>
        private void NotifyEngineeringModelsListChanges(List<EngineeringModelRowViewModel> engineeringModels)
        {
            ModelListChangedEvent?.Invoke(engineeringModels);
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
        ///
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
            NotifyEngineeringModelsListChanges(this.EngineeringModels.Where(em => em.IsSelected).ToList());
        }
    }
}
