

namespace Migration.Views
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for ServerInfo.xaml
    /// </summary>
    public partial class ServerInfo : UserControl
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the JsonIsAvailable is available
        /// </summary>
        public static readonly DependencyProperty ShowErrorsProperty = DependencyProperty.RegisterAttached(
            "ShowErrors",
            typeof(bool),
            typeof(ServerInfo),
            new PropertyMetadata(true, new PropertyChangedCallback(ShowErrorsPropertyChanged)));

        /// <summary>
        /// Gets or sets the <see cref="ShowErrorsProperty"/> dependency property.
        /// </summary>
        public bool ShowErrors
        {
            get => (bool)this.GetValue(ShowErrorsProperty);

            set => this.SetValue(ShowErrorsProperty, value);
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
        private static void ShowErrorsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ServerInfo)?.ShowErrorValueChanged(e);
        }

        /// <summary>
        /// Instance handler which will handle any changes that occur to a particular instance.
        /// </summary>
        /// <param name="e">The dependency object changed event args <see cref="DependencyPropertyChangedEventArgs"/></param>
        private void ShowErrorValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) return;

            this.PocoErrorsLayoutGroup.Visibility = Visibility.Hidden;
            this.ModelErrorsLayoutGroup.Visibility = Visibility.Hidden;
        }
    }
}
