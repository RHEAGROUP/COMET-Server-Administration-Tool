// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILoginViewModel.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using PlainObjects;
    using CDP4Dal;
    using CDP4Dal.DAL;

    /// <summary>
    /// The interface for the view model <see cref="LoginViewModel"/>
    /// </summary>
    public interface ILoginViewModel
    {
        DataSource SelectedDataSource { get; set; }

        string UserName { get; set; }

        string Password { get; set; }

        string Uri { get; set; }

        /// <summary>
        /// Gets or sets server session
        /// </summary>
        ISession ServerSession { get; set; }

        /// <summary>
        /// Gets or sets login successfully flag
        /// </summary>
        bool LoginSuccessfully { get; }

        /// <summary>
        /// Gets or sets dal
        /// </summary>
        IDal Dal { get; }

        /// <summary>
        ///
        /// </summary>
        string Output { get; }

        /// <summary>
        ///
        /// </summary>
        List<EngineeringModelRowViewModel> EngineeringModels { get; set; }
    }
}
