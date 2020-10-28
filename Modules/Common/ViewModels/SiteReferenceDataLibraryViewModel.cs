﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibraryViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels
{
    using CDP4Dal;
    using DevExpress.Mvvm.Native;
    using PlainObjects;
    using ReactiveUI;
    using System;
    using System.Linq;

    /// <summary>
    /// The view-model for the Source and target server that is responsible for getting referenced data libraries
    /// </summary>
    public class SiteReferenceDataLibraryViewModel : ReactiveObject
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
        /// Backing field for the <see cref="SiteReferenceDataLibraries"/> property
        /// </summary>
        private ReactiveList<SiteReferenceDataLibraryRowViewModel> siteReferenceDataLibraries;

        /// <summary>
        /// Gets or sets site reference data libraries
        /// </summary>
        public ReactiveList<SiteReferenceDataLibraryRowViewModel> SiteReferenceDataLibraries
        {
            get => this.siteReferenceDataLibraries;
            private set => this.RaiseAndSetIfChanged(ref this.siteReferenceDataLibraries, value);
        }

        /// <summary>
        /// Gets or sets the command to select/unselect thing
        /// </summary>
        public ReactiveCommand<object> CheckUncheckThing { get; set; }

        /// <summary>
        /// Gets or sets the command to select/unselect all things
        /// </summary>
        public ReactiveCommand<object> CheckUncheckAllThings { get; set; }

        /// <summary>
        /// Out property for the <see cref="SelectAllThings"/> property
        /// </summary>
        private bool selectAllThings;

        /// <summary>
        /// Gets a value indicating whether all things are selected
        /// </summary>
        public bool SelectAllThings
        {
            get => this.selectAllThings;
            set => this.RaiseAndSetIfChanged(ref this.selectAllThings, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteReferenceDataLibraryViewModel"/> class.
        /// </summary>
        public SiteReferenceDataLibraryViewModel(ISession serverSession)
        {
            this.serverSession = serverSession;

            this.CheckUncheckThing = ReactiveCommand.Create();
            this.CheckUncheckThing.Subscribe(_ =>
                this.SelectAllThings = !(this.SiteReferenceDataLibraries.Any(d => !d.IsSelected)));

            this.CheckUncheckAllThings = ReactiveCommand.Create();
            this.CheckUncheckAllThings.Subscribe(_ =>
                this.SiteReferenceDataLibraries.ForEach(d => d.IsSelected = this.SelectAllThings));

            this.SiteReferenceDataLibraries = new ReactiveList<SiteReferenceDataLibraryRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            foreach (var rdl in this.ServerSession.RetrieveSiteDirectory().SiteReferenceDataLibrary.OrderBy(m => m.Name))
            {
                this.SiteReferenceDataLibraries.Add(new SiteReferenceDataLibraryRowViewModel(rdl));
            }
        }
    }
}
