// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginTestFixture.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Tests
{
    using System.Collections.Generic;
    using Common.ViewModels;
    using Moq;
    using NUnit.Framework;
    using ReactiveUI;
    using System.Reactive.Concurrency;
    using Common.Settings;

    /// <summary>
    /// Suite of tests for the <see cref="LoginViewModel"/>
    /// </summary>
    [TestFixture]
    public class LoginTestFixture
    {
        private Mock<LoginViewModel> loginViewModel;

        private const string SourceServerUri = "https://cdp4services-public.cdp4.org";
        private const string SourceUsername = "admin";
        private const string SourcePassword = "pass";

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

        [Test]
        public void VerifyGetterSetters()
        {
            Assert.AreEqual(DataSource.CDP4, this.loginViewModel.Object.SelectedDataSource);
            Assert.AreEqual(SourceUsername, this.loginViewModel.Object.UserName);
            Assert.AreEqual(SourcePassword, this.loginViewModel.Object.Password);
            Assert.AreEqual(SourceServerUri, this.loginViewModel.Object.Uri);
        }

        [Test]
        public void VerifyIfLoginSucceeded()
        {
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
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

            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
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

            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(false, this.loginViewModel.Object.LoginSuccessfully);
        }

        [Test]
        public void VerifyIfExecuteCommandsWorks()
        {
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }
    }
}
