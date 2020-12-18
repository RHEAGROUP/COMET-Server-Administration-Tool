// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StressGeneratorTestFixtureFixture.cs" company="RHEA System S.A.">
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

using System.Diagnostics;

namespace SAT.Tests
{
    using System;
    using Common.Settings;
    using NUnit.Framework;
    using ViewModels;

    /// <summary>
    /// Suite of tests for the <see cref="MainViewModel"/>
    /// </summary>
    [TestFixture]
    public class MainTestFixture
    {
        private MainViewModel viewModel;
        private string appSettingsFile;
        private const string DefaultUrl = "http://localhost:5000";
        private const string NewUrl = "http://myhost:5000";

        [SetUp]
        public void Setup()
        {
            this.appSettingsFile = $"{AppDomain.CurrentDomain.BaseDirectory}\\appSettings.json";
            System.IO.File.WriteAllText(appSettingsFile, $@"{{""SavedUris"": [""{DefaultUrl}""]}}");
        }

        [TearDown]
        public void TearDown()
        {
            System.IO.File.Delete(this.appSettingsFile);
        }

        [Test]
        public void CheckIfAppSettingsIsLoaded()
        {
            this.viewModel = new MainViewModel();

            Assert.IsNotNull(this.viewModel);
            Assert.IsNotNull(AppSettingsHandler.Settings);
            Assert.IsTrue(AppSettingsHandler.Settings.SavedUris.Contains(DefaultUrl));
        }

        [Test]
        public void CheckIfAppSettingsIsSaved()
        {
            this.viewModel = new MainViewModel();

            Assert.IsNotNull(this.viewModel);
            Assert.IsNotNull(AppSettingsHandler.Settings);

            Assert.IsFalse(AppSettingsHandler.Settings.SavedUris.Contains(NewUrl));

            AppSettingsHandler.Settings.SavedUris.Add(NewUrl);

            AppSettingsHandler.Save();

            Assert.IsTrue(AppSettingsHandler.Settings.SavedUris.Contains(NewUrl));
        }
    }
}
