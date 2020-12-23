// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationServiceDialog.cs" company="RHEA System S.A.">
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

namespace Migration.Utils
{
    using DevExpress.Xpf.Core;
    using Microsoft.Win32;

    /// <summary>
    /// Helper class for opening dialogs
    /// </summary>
    public class MigrationServiceDialog : IMigrationServiceDialog
    {
        /// <summary>
        /// Gets or sets the migration file dialog
        /// </summary>
        public OpenFileDialog OpenFileWindowDialog { get; set; }

        /// <summary>
        /// Open the migration file dialog
        /// </summary>
        /// <param name="initialDirectory">Initial folder path</param>
        /// <param name="extensionFilter">File extension filter</param>
        /// <returns>true if the dialog is confirmed, false if otherwise.</returns>
        public bool? OpenMigrationFileDialog(string initialDirectory, string extensionFilter)
        {
            this.OpenFileWindowDialog = new OpenFileDialog()
            {
                InitialDirectory = initialDirectory,
                Filter = extensionFilter
            };

            return this.OpenFileWindowDialog.ShowDialog();
        }

        /// <summary>
        /// Open cardinality fix window
        /// </summary>
        /// <param name="window">Window that will be opened <see cref="ThemedWindow"/></param>
        /// <returns>true if the dialog is confirmed, false if otherwise.</returns>
        public bool? OpenFixWindowDialog(ThemedWindow window)
        {
            return window.ShowDialog();
        }
    }
}
