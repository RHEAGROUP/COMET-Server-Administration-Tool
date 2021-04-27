// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using Common.Settings;
    using Common.ViewModels;
    using Moq;
    using NUnit.Framework;
    using ReactiveUI;

    /// <summary>
    /// Suite of tests for the <see cref="LoginViewModel" />
    /// </summary>
    [TestFixture]
    public class LoginViewModelTestFixture
    {
        [SetUp]
        public void SetUp()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            AppSettingsHandler.Settings = new AppSettings
            {
                SavedUris = new List<string>()
            };

            this.loginViewModel = new Mock<LoginViewModel>
            {
                Object =
                {
                    SelectedDataSource = DataSource.CDP4,
                    UserName = SourceUsername,
                    Password = SourcePassword,
                    Uri = SourceServerUri,
                    SavedUris = new ReactiveList<string>()
                }
            };
        }

        private Mock<LoginViewModel> loginViewModel;

        private const string SourceServerUri = "https://cdp4services-public.cdp4.org";
        private const string SourceUsername = "admin";
        private const string SourcePassword = "pass";

        [Test]
        public void VerifyGetterSetters()
        {
            Assert.AreEqual(DataSource.CDP4, this.loginViewModel.Object.SelectedDataSource);
            Assert.IsFalse(this.loginViewModel.Object.JsonIsSelected);
            Assert.IsTrue(this.loginViewModel.Object.CanSaveUri);
            Assert.IsNull(this.loginViewModel.Object.EngineeringModels);
            Assert.AreEqual(SourceUsername, this.loginViewModel.Object.UserName);
            Assert.AreEqual(SourcePassword, this.loginViewModel.Object.Password);
            Assert.AreEqual(SourceServerUri, this.loginViewModel.Object.Uri);
        }

        [Test]
        public void VerifyIfExecuteCommandsWorks()
        {
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }

        [Test]
        public void VerifyIfLoginFailed()
        {
            this.loginViewModel = new Mock<LoginViewModel>
            {
                Object =
                {
                    SelectedDataSource = DataSource.JSON,
                    UserName = SourceUsername,
                    Password = SourcePassword,
                    Uri = SourceServerUri,
                    SavedUris = new ReactiveList<string>()
                }
            };

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(false, this.loginViewModel.Object.LoginSuccessfully);
        }

        [Test]
        public void VerifyIfLoginSucceeded()
        {
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());

            this.loginViewModel = new Mock<LoginViewModel>
            {
                Object =
                {
                    SelectedDataSource = DataSource.WSP,
                    UserName = SourceUsername,
                    Password = SourcePassword,
                    Uri = SourceServerUri,
                    SavedUris = new ReactiveList<string>()
                }
            };

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);

            // relogin

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }
    }
}