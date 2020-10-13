// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerInfo.xaml.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Views
{
    using Common.ViewModels;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for ServerInfo.xaml
    /// </summary>
    public partial class ServerInfo : UserControl
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the JsonIsAvailable is set
        /// </summary>
        public static readonly DependencyProperty ShowErrorsProperty = DependencyProperty.RegisterAttached(
            "ShowErrors",
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
        /// Gets or sets the <see cref="ShowErrorsProperty"/> dependency property.
        /// </summary>
        public bool ShowErrors
        {
            get => (bool)this.GetValue(ShowErrorsProperty);
            set => this.SetValue(ShowErrorsProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="LoginSuccessfullyProperty"/> dependency property.
        /// </summary>
        public bool LoginSuccessfully
        {
            get => (bool)this.GetValue(LoginSuccessfullyProperty);
            set => this.SetValue(LoginSuccessfullyProperty, value);
        }

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

            if (e.Property.Name == "ShowErrors")
            {
                ((ServerInfo) d).ShowErrorsValueChanged(e);
            }
            else if (e.Property.Name == "LoginSuccessfully")
            {
                ((ServerInfo) d).LoginSuccessfullyValueChanged(e);
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
            if (!(bool)e.NewValue) return;

            if (this.PocoErrorsLayoutGroup.DataContext is ErrorViewModel pocoErrorsViewModel)
            {
                pocoErrorsViewModel.ServerSession = (this.DataContext as LoginViewModel)?.ServerSession;
            }
            if (this.ModelErrorsLayoutGroup.DataContext is ErrorViewModel modelErrorsViewModel)
            {
                modelErrorsViewModel.ServerSession = (this.DataContext as LoginViewModel)?.ServerSession;
            }
        }
    }
}
