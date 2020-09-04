// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineeringModelLayoutGroup.xaml.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SAT.Views.Tabs
{
    using DevExpress.Xpf.LayoutControl;
    using System.Windows;

    /// <summary>
    /// Interaction logic for EngineeringModelLayoutGroup.xaml
    /// </summary>
    public partial class EngineeringModelLayoutGroup : LayoutGroup
    {
        /// <summary>
        /// <see cref="DependencyProperty"/> that can be set in XAML to indicate that the JsonIsAvailable is available
        /// </summary>
        public static readonly DependencyProperty GridSelectionIsAvailableProperty = DependencyProperty.RegisterAttached(
            "GridSelectionIsAvailable",
            typeof(bool),
            typeof(EngineeringModelLayoutGroup),
            new PropertyMetadata(true, new PropertyChangedCallback(OnGridSelectionIsAvailableChanged)));

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
            (d as EngineeringModelLayoutGroup).OnInstanceChanged(e);
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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
        }
    }
}
