// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerInfo.xaml.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Views
{
    using CDP4Dal;
    using Common.ViewModels;
    using System.Windows;

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
        /// New model <see cref="ErrorViewModel"/> instance for error tabs
        /// </summary>
        private ErrorViewModel errorViewModel;

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

            if (!this.DisplayErrorsTabs)
            {
                return;
            }

            errorViewModel = new ErrorViewModel(this.serverSession);
            this.PocoErrorsLayoutGroup.DataContext = errorViewModel;
            this.ModelErrorsLayoutGroup.DataContext = errorViewModel;
            this.ErrorDetailLayoutGroup.DataContext = errorViewModel;
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
