// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineeringModelLayoutGroup.xaml.cs" company="RHEA System S.A.">
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

namespace Common.Views.Tabs
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for EngineeringModelLayoutGroup.xaml
    /// </summary>
    public partial class EngineeringModelLayoutGroup
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the JsonIsAvailable is available
        /// </summary>
        public static readonly DependencyProperty GridSelectionIsAvailableProperty = DependencyProperty.RegisterAttached(
            "GridSelectionIsAvailable",
            typeof(bool),
            typeof(EngineeringModelLayoutGroup),
            new PropertyMetadata(true, OnGridSelectionIsAvailableChanged));

        /// <summary>
        /// Gets or sets the <see cref="GridSelectionIsAvailableProperty"/> dependency property.
        /// </summary>
        public bool GridSelectionIsAvailable
        {
            get => (bool)this.GetValue(GridSelectionIsAvailableProperty);

            set => this.SetValue(GridSelectionIsAvailableProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineeringModelLayoutGroup"/> class.
        /// </summary>
        public EngineeringModelLayoutGroup()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Static callback handler which will handle any changes that occurs globally
        /// </summary>
        /// <param name="d">The dependency object user control <see cref="DependencyObject" /></param>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private static void OnGridSelectionIsAvailableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as EngineeringModelLayoutGroup)?.OnInstanceChanged(e);
        }

        /// <summary>
        /// Instance handler which will handle any changes that occur to a particular instance.
        /// </summary>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private void OnInstanceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                this.EngineeringModelGridControl.Columns[0].Visible = false;
            }
        }
    }
}
