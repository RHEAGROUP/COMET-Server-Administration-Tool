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

namespace Common.ViewModels
{
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using System;
    using System.Linq;
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
        /// Initializes a new instance of the <see cref="SiteReferenceDataLibraryViewModel"/> class.
        /// </summary>
        public SiteReferenceDataLibraryViewModel(ISession serverSession)
        {
            this.serverSession = serverSession;

            this.SiteReferenceDataLibraries = new ReactiveList<SiteReferenceDataLibraryRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            this.WhenAnyValue(vm => vm.ServerSession).Subscribe(session =>
            {
                this.BindSiteReferenceDataLibraries(this.ServerSession.RetrieveSiteDirectory());
            });
        }

        /// <summary>
        /// Bind site reference data libraries to the reactive list
        /// </summary>
        /// <param name="siteDirectory">The <see cref="SiteDirectory"/> top container</param>
        private void BindSiteReferenceDataLibraries(SiteDirectory siteDirectory)
        {
            this.SiteReferenceDataLibraries.Clear();

            foreach (var rdl in siteDirectory.SiteReferenceDataLibrary.OrderBy(m => m.Name))
            {
                this.SiteReferenceDataLibraries.Add(new SiteReferenceDataLibraryRowViewModel(rdl));
            }
        }
    }
}
