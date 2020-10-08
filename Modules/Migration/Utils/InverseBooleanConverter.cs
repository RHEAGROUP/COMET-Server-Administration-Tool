// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InverseBooleanConverter.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Utils
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// The purpose of the <see cref="InverseBooleanConverter"/> is to return the opposite for the given boolean value
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Returns the opposite of the boolean value provided
        /// </summary>
        /// <param name="value">The boolean value that will be inverted</param>
        /// <param name="targetType">Target type (bool)</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return value != null && !(bool)value;
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
            throw new NotSupportedException();
        }
    }
}
