// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginTestFixture.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Tests
{
    using global::Migration.ViewModels.Common;
    using Moq;
    using NUnit.Framework;
    using ReactiveUI;
    using System.Reactive.Concurrency;
    using System.Collections.Generic;

    /// <summary>
    /// Suite of tests for the <see cref="Migration"/> <see cref="LoginViewModel"/>
    /// </summary>
    [TestFixture]
    public class LoginTestFixture : CommonTest
    {
        private Mock<LoginViewModel> loginViewModel;

        [SetUp]
        public void SetUp()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.loginViewModel = new Mock<LoginViewModel>
            {
                Object =
                {
                    ServerType = new KeyValuePair<string, string>("CDP", "CDP4 WebServices"),
                    UserName = SourceUsername,
                    Password = SourcePassword,
                    Uri = SourceServerUri
                }
            };
        }

        [Test]
        public void VerifyGetterSetters()
        {
            Assert.AreEqual(this.loginViewModel.Object.ServerType,
                new KeyValuePair<string, string>("CDP", "CDP4 WebServices"));
            Assert.AreEqual("admin", this.loginViewModel.Object.UserName);
            Assert.AreEqual("pass", this.loginViewModel.Object.Password);
            Assert.AreEqual("https://cdp4services-public.cdp4.org", this.loginViewModel.Object.Uri);
        }

        [Test]
        public void VerifyIfLoginSucceeded()
        {
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }

        [Test]
        public void VerifyIfLoginFailed()
        {
            this.loginViewModel.Object.UserName = "admin1";
            this.loginViewModel.Object.Password = "pass1";
            this.loginViewModel.Object.Uri = "https://cdp4services-public1.cdp4.org";
            Assert.AreEqual(this.loginViewModel.Object.ServerType,
                new KeyValuePair<string, string>("CDP", "CDP4 WebServices"));
            Assert.AreEqual("admin1", this.loginViewModel.Object.UserName);
            Assert.AreEqual("pass1", this.loginViewModel.Object.Password);
            Assert.AreEqual("https://cdp4services-public1.cdp4.org", this.loginViewModel.Object.Uri);

            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(false, this.loginViewModel.Object.LoginSuccessfully);
        }

        [Test]
        public void VerifyIfEngineeringModelsAreLoaded()
        {
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.NotZero(this.loginViewModel.Object.EngineeringModels.Count);
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.ServerSession.Close());
        }

        [Test]
        public void VerifyIfReferenceDataLibrariesAreLoaded()
        {
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.NotZero(this.loginViewModel.Object.SiteReferenceDataLibraries.Count);
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.ServerSession.Close());
        }

        [Test]
        public void VerifyIfExecuteCommandsWorks()
        {
            Assert.DoesNotThrowAsync(async () => await loginViewModel.Object.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.loginViewModel.Object.LoginSuccessfully);
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.CheckUncheckModel.ExecuteAsyncTask());
            //Assert.DoesNotThrow(() => this.loginViewModel.Object.LoadSourceFile.Execute(null));
            Assert.DoesNotThrowAsync(async () => await this.loginViewModel.Object.ServerSession.Close());
        }
    }
}
