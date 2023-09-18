// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibraryViewModel.cs" company="RHEA System S.A.">
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

using System.Reactive;
using Common.Utils;

namespace Common.ViewModels
{
    using System;
    using System.Linq;
    using CDP4Dal;
    using DevExpress.Mvvm.Native;
    using PlainObjects;
    using ReactiveUI;

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
        public ReactiveCommand<Unit, Unit> CheckUncheckThing { get; set; }

        /// <summary>
        /// Gets or sets the command to select/unselect all things
        /// </summary>
        public ReactiveCommand<Unit, Unit> CheckUncheckAllThings { get; set; }

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

            this.CheckUncheckThing = ReactiveCommandCreator.Create();
            this.CheckUncheckThing.Subscribe(_ =>
                this.SelectAllThings = !(this.SiteReferenceDataLibraries.Any(d => !d.IsSelected)));

            this.CheckUncheckAllThings = ReactiveCommandCreator.Create();
            this.CheckUncheckAllThings.Subscribe(_ =>
                this.SiteReferenceDataLibraries.ForEach(d => d.IsSelected = this.SelectAllThings));

            this.SiteReferenceDataLibraries = new ReactiveList<SiteReferenceDataLibraryRowViewModel>();

            foreach (var rdl in this.ServerSession.RetrieveSiteDirectory().SiteReferenceDataLibrary.OrderBy(m => m.Name))
            {
                this.SiteReferenceDataLibraries.Add(new SiteReferenceDataLibraryRowViewModel(rdl));
            }
        }
    }
}
