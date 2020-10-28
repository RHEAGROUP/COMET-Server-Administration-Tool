// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineeringModelRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Common.SiteDirectoryData;
    using ReactiveUI;

    /// <summary>
    /// Row class representing a <see cref="EngineeringModelSetup"/> as a plain object
    /// </summary>
    public class EngineeringModelRowViewModel : DefinedThingRowViewModel<EngineeringModelSetup>
    {
        /// <summary>
        /// Backing field for <see cref="Kind"/>
        /// </summary>
        private EngineeringModelKind kind;

        /// <summary>
        /// Gets or sets the kind
        /// </summary>
        public EngineeringModelKind Kind
        {
            get => this.kind;
            set => this.RaiseAndSetIfChanged(ref this.kind, value);
        }

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
        /// Initializes a new instance of the <see cref="EngineeringModelRowViewModel"/> class
        /// </summary>
        /// <param name="modelSetup">
        /// The <see cref="EngineeringModelSetup"/> associated with this row
        /// </param>
        public EngineeringModelRowViewModel(EngineeringModelSetup modelSetup)
            : base(modelSetup)
        {
            this.Kind = modelSetup.Kind;
            this.IsSelected = true;
        }
    }
}
