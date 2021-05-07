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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Threading.Tasks;
    using CDP4Common.DTO;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using Common.Events;
    using Common.Settings;
    using Common.ViewModels;
    using Common.ViewModels.PlainObjects;
    using Moq;
    using NUnit.Framework;
    using ReactiveUI;

    /// <summary>
    /// Suite of tests for the <see cref="LoginViewModel" />
    /// </summary>
    [TestFixture]
    public class LoginViewModelTestFixture
    {
        private Dictionary<Guid, Thing> dictionaryThings;
        private SiteDirectory siteDirectory;
        private Person person;

        private const string ServerUrl = "https://www.rheagroup.com/";

        private Mock<LoginViewModel> loginViewModel;

        [SetUp]
        public void SetUp()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.dictionaryThings = new Dictionary<Guid, Thing>();

            this.siteDirectory = new SiteDirectory(Guid.NewGuid(), 0);
            this.dictionaryThings.Add(this.siteDirectory.Iid, this.siteDirectory);

            this.person = new Person(Guid.NewGuid(), 0)
            {
                ShortName = "John",
                GivenName = "John",
                Password = "Doe",
                IsActive = true
            };
            this.siteDirectory.Person.Add(this.person.Iid);
            this.dictionaryThings.Add(this.person.Iid, this.person);

            var mockDal = new Mock<IDal>();
            mockDal.SetupProperty(m => m.Session);

            AppSettingsHandler.Settings = new AppSettings
            {
                SavedUris = new List<string>()
            };

            this.loginViewModel = new Mock<LoginViewModel>
            {
                Object =
                {
                    SelectedDataSource = DataSource.CDP4,
                    UserName = this.person.ShortName,
                    Password = this.person.Password,
                    Uri = ServerUrl,
                    SavedUris = new ReactiveList<string>()
                }
            };

            this.loginViewModel.Object.Dal = mockDal.Object;

            mockDal.Setup(x => x.Open(It.IsAny<Credentials>(), It.IsAny<CancellationToken>())).Returns<Credentials, CancellationToken>((credentials, cancellationToken) =>
            {
                var result = this.dictionaryThings.Values.ToList() as IEnumerable<Thing>;
                return Task.FromResult(result);
            });
        }

        [Test]
        public void VerifyGetterSetters()
        {
            Assert.AreEqual(DataSource.CDP4, this.loginViewModel.Object.SelectedDataSource);
            this.loginViewModel.Object.SelectedDataSource = DataSource.WSP;
            Assert.AreEqual(DataSource.WSP, this.loginViewModel.Object.SelectedDataSource);

            this.loginViewModel.Object.UserName = this.person.ShortName;
            Assert.AreEqual(this.person.ShortName, this.loginViewModel.Object.UserName);

            this.loginViewModel.Object.Password = this.person.Password;
            Assert.AreEqual(this.person.Password, this.loginViewModel.Object.Password);

            this.loginViewModel.Object.Uri = ServerUrl;
            Assert.AreEqual(ServerUrl, this.loginViewModel.Object.Uri);

            Assert.AreEqual(false, this.loginViewModel.Object.JsonIsSelected);

            Assert.IsNull(this.loginViewModel.Object.Output);

            Assert.IsTrue(this.loginViewModel.Object.CanSaveUri);

            this.loginViewModel.Object.EngineeringModels = new List<EngineeringModelRowViewModel>();
            Assert.IsEmpty(this.loginViewModel.Object.EngineeringModels);
        }

        [Test]
        public void VerifyIfLoginSucceeded()
        {
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());

            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }

        [Test]
        public void VerifyIfLoginAfterLoginSucceeded()
        {
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());

            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());

            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }

        [Test]
        public void VerifyIfLogoutAndLoginSucceeded()
        {
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());

            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);

            CDPMessageBus.Current.SendMessage(new LogoutAndLoginEvent
            {
                CurrentSession = this.loginViewModel.Object.ServerSession
            });

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }

        [Test]
        public void VerifyIfLogoutAndLoginWithInvalidSession()
        {
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());

            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);

            CDPMessageBus.Current.SendMessage(new LogoutAndLoginEvent
            {
                CurrentSession = null
            });

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
                    UserName = this.person.ShortName,
                    Password = this.person.Password,
                    Uri = ServerUrl,
                    SavedUris = new ReactiveList<string>()
                }
            };

            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(false, this.loginViewModel.Object.LoginSuccessfully);
        }
    }
}
