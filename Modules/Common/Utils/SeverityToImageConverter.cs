// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SeverityToImageConverter.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.Utils
{
    using DevExpress.Xpf.Core;
    using DevExpress.Xpf.Core.Native;
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// The purpose of the <see cref="SeverityToImageConverter"/> is to return the severity values as images
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class SeverityToImageConverter : IValueConverter
    {
        /// <summary>
        /// Returns the correspondent image based on the value provided
        /// </summary>
        /// <param name="value">The string value that will be processed</param>
        /// <param name="targetType">Target type (string)</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return new DXImageExtension { Image = new DXImageConverter().ConvertFrom("Question_16x16.png") as DXImageInfo }.ProvideValue(null);
            }

            switch (value.ToString())
            {
                case "Warning":
                    return new DXImageExtension { Image = new DXImageConverter().ConvertFrom("Warning_16x16.png") as DXImageInfo }.ProvideValue(null);
                case "Error":
                    return new DXImageExtension { Image = new DXImageConverter().ConvertFrom("Error_16x16.png") as DXImageInfo }.ProvideValue(null);
                default:
                    break;
            }

            return null;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="value">The parameter is not used</param>
        /// <param name="targetType">The parameter is not used</param>
        /// <param name="parameter">The parameter is not used</param>
        /// <param name="culture">The parameter is not used</param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
