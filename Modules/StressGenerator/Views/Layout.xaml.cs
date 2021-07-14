// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Layout.xaml.cs" company="RHEA System S.A.">
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

namespace StressGenerator.Views
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using DevExpress.Xpf.Bars;
    using DevExpress.Xpf.Dialogs;
    using DevExpress.Xpf.Editors;

    /// <summary>
    /// Interaction logic for Layout.xaml
    /// </summary>
    public partial class Layout
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Layout"/> class.
        /// </summary>
        public Layout()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Scroll up output window
        /// </summary>
        /// <param name="sender">The sender control <see cref="TextEdit"/></param>
        /// <param name="e">The <see cref="EditValueChangedEventArgs"/></param>
        private void BaseEdit_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (!(sender is TextEdit textEdit))
            {
                return;
            }

            textEdit.Focus();
            textEdit.SelectionStart = textEdit.Text.Length;
        }

        /// <summary>
        /// Saves the graph to an image file
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The arguments</param>
        private void SaveToImage_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var dialog = new DXSaveFileDialog
            {
                DefaultExt = "jpg",
                AddExtension = true,
                Filter = "Image | *.jpg"
            };

            var result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                var encoder = this.GetImageEncoder();

                // start stream
                var file = new FileStream(dialog.FileName, FileMode.Create);
                encoder.Save(file);

                // close stream
                file.Close();
            }
        }

        /// <summary>
        /// Saves image to clipboard
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The arguments</param>
        private void SaveToClipboard_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var encoder = this.GetImageEncoder();

            var bs = encoder.Frames[0] as BitmapSource;

            Clipboard.SetImage(bs);
        }

        /// <summary>
        /// Computes and returns the Bitmap Image encoder with the chart of the correct size
        /// </summary>
        /// <returns>The <see cref="JpegBitmapEncoder"/> containing the bitmap of the exported chart</returns>
        private JpegBitmapEncoder GetImageEncoder()
        {
            var actualWidth = this.ResponseChartControl.ActualWidth;
            var actualHeight = this.ResponseChartControl.ActualHeight;

            // init contexts
            var brush = new VisualBrush(this.ResponseChartControl);
            var visual = new DrawingVisual();
            var context = visual.RenderOpen();

            context.DrawRectangle(
                brush, null, new Rect(0, 0, actualWidth, actualHeight));
            context.Close();

            // set up redenr target
            var bmp = new RenderTargetBitmap(
                (int)actualWidth,
                (int)actualHeight,
                96,
                96,
                PixelFormats.Pbgra32);

            bmp.Render(visual);

            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            return encoder;
        }
    }
}
