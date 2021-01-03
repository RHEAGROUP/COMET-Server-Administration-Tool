// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SeverityToImageConverterTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2021 RHEA System S.A.
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

namespace Migration.Tests
{
    using System;
    using Common.Utils;
    using NUnit.Framework;

    [TestFixture]
    internal class SeverityToImageConverterTestFixture
    {
        [Test]
        public void VerifyConverterConverts()
        {
            var converter = new SeverityToImageConverter();

            var value = converter.Convert(null, null, null, null);

            Assert.IsNotNull(value);
            Assert.IsTrue(value.ToString().Contains("Question"));

            value = converter.Convert("Warning", null, null, null);

            Assert.IsNotNull(value);
            Assert.IsTrue(value.ToString().Contains("Warning"));

            value = converter.Convert("Error", null, null, null);

            Assert.IsNotNull(value);
            Assert.IsTrue(value.ToString().Contains("Error"));

            value = converter.Convert("Info", null, null, null);

            Assert.IsNull(value);
        }

        [Test]
        public void VerifyConverterConvertsBackThrows()
        {
            var converter = new SeverityToImageConverter();

            Assert.Throws<NotSupportedException>(() => converter.ConvertBack(null, null, null, null));
        }
    }
}