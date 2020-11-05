// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SeverityToImageConverter.cs" company="RHEA System S.A.">
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
