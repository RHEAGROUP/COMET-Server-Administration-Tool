using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;


namespace Migration.Views
{
    using ViewModels;

    /// <summary>
    /// Interaction logic for FixCoordinalityErrorsDialog.xaml
    /// </summary>
    public partial class FixCoordinalityErrorsDialog : ThemedWindow
    {
        public FixCoordinalityErrorsDialog()
        {
            InitializeComponent();
        }

        private void FixCoordinalityErrorsDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            ((IFixCoordinalityErrorsDialogViewModel)this.DataContext).BindPocoErrors();
        }
    }
}
