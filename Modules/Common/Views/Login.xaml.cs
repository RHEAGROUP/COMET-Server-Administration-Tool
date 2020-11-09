// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Login.xaml.cs" company="RHEA System S.A.">
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

namespace Common.Views
{
    using System.Linq;
    using ViewModels;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : UserControl
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the JsonIsAvailable is available
        /// </summary>
        public static readonly DependencyProperty JsonIsAvailableProperty = DependencyProperty.RegisterAttached(
            "JsonIsAvailable",
            typeof(bool),
            typeof(Login),
            new PropertyMetadata(true, new PropertyChangedCallback(OnDataSourceAvailabilityChanged)));

        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the OnlyCdp4IsAvailable is available
        /// </summary>
        public static readonly DependencyProperty OnlyCdp4IsAvailableProperty = DependencyProperty.RegisterAttached(
            "OnlyCdp4IsAvailable",
            typeof(bool),
            typeof(Login),
            new PropertyMetadata(false, new PropertyChangedCallback(OnDataSourceAvailabilityChanged)));

        /// <summary>
        /// Gets or sets the <see cref="JsonIsAvailableProperty"/> dependency property.
        /// </summary>
        public bool JsonIsAvailable
        {
            get => (bool)this.GetValue(JsonIsAvailableProperty);

            set => this.SetValue(JsonIsAvailableProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="OnlyCdp4IsAvailableProperty"/> dependency property.
        /// </summary>
        public bool OnlyCdp4IsAvailable
        {
            get => (bool)this.GetValue(OnlyCdp4IsAvailableProperty);

            set => this.SetValue(OnlyCdp4IsAvailableProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Login"/> user control class
        /// </summary>
        public Login()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Static callback handler which will handle any changes that occurs globally
        /// </summary>
        /// <param name="d">The dependency object user control <see cref="DependencyObject" /></param>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private static void OnDataSourceAvailabilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Login)?.DataSourceAvailabilityChanged(e);
        }

        /// <summary>
        /// Instance handler which will handle any changes that occur to a particular instance.
        /// </summary>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private void DataSourceAvailabilityChanged(DependencyPropertyChangedEventArgs e)
        {
            var newValue = (bool) e.NewValue;

            switch (e.Property.Name)
            {
                case "JsonIsAvailable":
                    if (!newValue)
                    {
                        this.ServerType.ItemsSource =
                            LoginViewModel.ServerTypes.Where(item => item.Key != ViewModels.DataSource.JSON);
                    }

                    break;
                case "OnlyCdp4IsAvailable":
                    if (newValue)
                    {
                        this.ServerType.ItemsSource =
                            LoginViewModel.ServerTypes.Where(item => item.Key == ViewModels.DataSource.CDP4);
                    }
                    break;
            }

            this.BrowseJson.Visibility = Visibility.Hidden;
        }
    }
}
