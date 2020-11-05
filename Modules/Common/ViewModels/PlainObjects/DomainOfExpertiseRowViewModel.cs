// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DomainOfExpertiseRowViewModel.cs" company="RHEA System S.A.">
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

namespace Common.ViewModels.PlainObjects
{
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;
    using ReactiveUI;

    /// <summary>
    /// Row class representing a <see cref="DomainOfExpertise"/> or <see cref="DomainOfExpertiseGroup"/>
    /// as a plain object
    /// </summary>
    public class DomainOfExpertiseRowViewModel : DefinedThingRowViewModel<DefinedThing>
    {
        /// <summary>
        /// Backing field for <see cref="IsSelected"/>
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// Gets or sets the if object is selected
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.RaiseAndSetIfChanged(ref this.isSelected, value);
        }

        /// <summary>
        /// Backing field for <see cref="ClassKind"/>
        /// </summary>
        private ClassKind classKind;

        /// <summary>
        /// Gets or sets the ClassKind
        /// </summary>
        public ClassKind ClassKind
        {
            get => this.classKind;
            private set => this.RaiseAndSetIfChanged(ref this.classKind, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainOfExpertiseRowViewModel"/> class
        /// </summary>
        /// <param name="definedThing">
        /// The <see cref="DefinedThing"/> associated with this row
        /// </param>
        public DomainOfExpertiseRowViewModel(DefinedThing definedThing)
            : base(definedThing)
        {
            this.ClassKind = definedThing.ClassKind;
        }
    }
}
