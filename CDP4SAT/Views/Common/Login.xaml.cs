// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Login.xaml.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SAT.Views.Common
{
    using CDP4SAT.ViewModels.Common;
    using System.Linq;
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
            new PropertyMetadata(true, new PropertyChangedCallback(OnSetJsonIsAvailableChanged)));

        /// <summary>
        /// Gets or sets the <see cref="JsonIsAvailableProperty"/> dependency property.
        /// </summary>
        public bool JsonIsAvailable
        {
            get => (bool)this.GetValue(JsonIsAvailableProperty);

            set => this.SetValue(JsonIsAvailableProperty, value);
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
        private static void OnSetJsonIsAvailableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Login).OnSetTextChanged(e);
        }

        /// <summary>
        /// Instance handler which will handle any changes that occur to a particular instance.
        /// </summary>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private void OnSetTextChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                this.ServerType.ItemsSource = LoginViewModel.DataSourceList.Where(item => item.Key != "JSON");
                this.BrowseJson.Visibility = Visibility.Hidden;
            }
        }
    }
}
