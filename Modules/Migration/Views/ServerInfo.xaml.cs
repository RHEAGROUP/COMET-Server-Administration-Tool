// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerInfo.xaml.cs" company="RHEA System S.A.">
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

namespace Migration.Views
{
    using System.Collections.Generic;
    using System.Windows;
    using CDP4Dal;
    using Common.ViewModels;
    using Common.ViewModels.PlainObjects;

    /// <summary>
    /// Interaction logic for ServerInfo.xaml
    /// </summary>
    public partial class ServerInfo
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the DisplayErrorsTabs is set
        /// </summary>
        public static readonly DependencyProperty DisplayErrorsTabsProperty = DependencyProperty.RegisterAttached(
            "DisplayErrorsTabs",
            typeof(bool),
            typeof(ServerInfo),
            new PropertyMetadata(true, PropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the LoginSuccessfully is set
        /// </summary>
        public static readonly DependencyProperty LoginSuccessfullyProperty = DependencyProperty.RegisterAttached(
            "LoginSuccessfully",
            typeof(bool),
            typeof(ServerInfo),
            new PropertyMetadata(false, PropertyChanged));

        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the ServerSession is set
        /// </summary>
        public static readonly DependencyProperty ServerSessionProperty = DependencyProperty.RegisterAttached(
            "ServerSession",
            typeof(ISession),
            typeof(ServerInfo),
            new PropertyMetadata(default(ISession), PropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="DisplayErrorsTabsProperty"/> dependency property.
        /// </summary>
        public bool DisplayErrorsTabs
        {
            get => (bool)this.GetValue(DisplayErrorsTabsProperty);
            set => this.SetValue(DisplayErrorsTabsProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="LoginSuccessfullyProperty"/> dependency property.
        /// </summary>
        public bool LoginSuccessfully
        {
            get => (bool)this.GetValue(LoginSuccessfullyProperty);
            set => this.SetValue(LoginSuccessfullyProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ServerSessionProperty"/> dependency property.
        /// </summary>
        public ISession ServerSession
        {
            get => (ISession)this.GetValue(ServerSessionProperty);
            set => this.SetValue(ServerSessionProperty, value);
        }

        /// <summary>
        /// Current server session <see cref="ISession"/>
        /// </summary>
        private ISession serverSession;

        /// <summary>
        /// New model instance for error tabs <see cref="ErrorViewModel"/>
        /// </summary>
        private ErrorViewModel errorViewModel;

        /// <summary>
        /// New model instance for engineering model tab <see cref="EngineeringModelViewModel"/>
        /// </summary>
        private EngineeringModelViewModel engineeringModelViewModel;

        /// <summary>
        /// New model instance for engineering model tab <see cref="SiteReferenceDataLibraryViewModel"/>
        /// </summary>
        private SiteReferenceDataLibraryViewModel siteReferenceDataLibraryViewModel;

        public ServerInfo()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Static callback handler which will handle any changes that occurs globally
        /// </summary>
        /// <param name="d">The dependency object user control <see cref="DependencyObject" /></param>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ServerInfo))
                return;

            switch(e.Property.Name)
            {
                case "DisplayErrorsTabs":
                    ((ServerInfo)d).ShowErrorsValueChanged(e);
                    break;
                case "LoginSuccessfully":
                    ((ServerInfo)d).LoginSuccessfullyValueChanged(e);
                    break;
                case "ServerSession":
                    ((ServerInfo)d).ServerSessionValueChanged(e);
                    break;
            }
        }

        /// <summary>
        /// Instance handler which will handle any changes that occur to a particular instance.
        /// </summary>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private void ShowErrorsValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) return;

            this.PocoErrorsLayoutGroup.Visibility = Visibility.Hidden;
            this.ModelErrorsLayoutGroup.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Instance handler which will handle any changes that occur to a particular instance.
        /// </summary>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private void LoginSuccessfullyValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!(bool) e.NewValue)
            {
                return;
            }

            this.engineeringModelViewModel = new EngineeringModelViewModel(this.serverSession);
            this.engineeringModelViewModel.ModelListChangedEvent += EngineeringModelViewModelModelListChangedEvent;
            this.EngineeringModelLayoutGroup.DataContext = this.engineeringModelViewModel;
            this.engineeringModelViewModel.CheckUncheckAllModels.Execute(null);
            this.siteReferenceDataLibraryViewModel = new SiteReferenceDataLibraryViewModel(this.serverSession);
            this.SiteRdlLayoutGroup.DataContext = this.siteReferenceDataLibraryViewModel;

            this.errorViewModel = new ErrorViewModel(this.serverSession);
            this.PocoErrorsLayoutGroup.DataContext = this.errorViewModel;
            this.ModelErrorsLayoutGroup.DataContext = this.errorViewModel;
            this.ErrorDetailLayoutGroup.DataContext = this.errorViewModel;
        }

        /// <summary>
        /// Update engineering models list
        /// </summary>
        /// <param name="engineeringModelsList">
        /// The models list that will be migrated <see cref="EngineeringModelRowViewModel" />
        /// </param>
        private void EngineeringModelViewModelModelListChangedEvent(List<EngineeringModelRowViewModel> engineeringModelsList)
        {
            if (this.DataContext is LoginViewModel viewModel)
            {
                viewModel.EngineeringModels = engineeringModelsList;
            }
        }

        /// <summary>
        /// Instance handler which will handle any changes that occur to a particular instance.
        /// </summary>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private void ServerSessionValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is ISession))
            {
                return;
            }

            this.serverSession = (ISession) e.NewValue;
        }
    }
}
