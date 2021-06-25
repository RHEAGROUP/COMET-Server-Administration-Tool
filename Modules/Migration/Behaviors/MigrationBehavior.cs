// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationBehavior.cs" company="RHEA System S.A.">
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

namespace Migration.Behaviors
{
    using System.Diagnostics.CodeAnalysis;
    using Common.ViewModels;
    using ViewModels;
    using Views;
    using DevExpress.Mvvm.UI.Interactivity;

    /// <summary>
    /// The purpose of this class is to implement a behavior that is specific to migration flow and allows
    /// the login and source sections (of the migration tab) to share the same view model
    /// </summary>
    public class MigrationBehavior : Behavior<Layout>
    {
        /// <summary>
        /// The on attached event handler
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override void OnAttached()
        {
            base.OnAttached();

            if (!(AssociatedObject.DataContext is MigrationViewModel currentDataContext)) return;

            currentDataContext.SourceViewModel = AssociatedObject.LoginSource.DataContext as LoginViewModel;
            currentDataContext.TargetViewModel = AssociatedObject.LoginTarget.DataContext as LoginViewModel;
            //currentDataContext.AddSubscriptions();
        }

        /// <summary>
        /// The on detached event handler
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override void OnDetaching()
        {
            if (AssociatedObject.DataContext is MigrationViewModel currentDataContext)
            {
                currentDataContext.SourceViewModel = null;
                currentDataContext.TargetViewModel = null;
            }

            base.OnDetaching();
        }
    }
}
