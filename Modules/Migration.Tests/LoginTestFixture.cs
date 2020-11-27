// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginTestFixture.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Tests
{
    using Common.ViewModels;
    using Moq;
    using NUnit.Framework;
    using ReactiveUI;
    using System.Reactive.Concurrency;

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

            this.loginViewModel = new Mock<LoginViewModel>
            {
                Object =
                {
                    SelectedDataSource = DataSource.CDP4,
                    UserName = SourceUsername,
                    Password = SourcePassword,
                    Uri = SourceServerUri
                }
            };
        }

        [Test]
        public void VerifyGetterSetters()
        {
            Assert.AreEqual(this.loginViewModel.Object.SelectedDataSource,
                DataSource.CDP4);
            Assert.AreEqual("admin", this.loginViewModel.Object.UserName);
            Assert.AreEqual("pass", this.loginViewModel.Object.Password);
            Assert.AreEqual("https://cdp4services-public.cdp4.org", this.loginViewModel.Object.Uri);
        }

        [Test]
        public void VerifyIfLoginSucceeded()
        {
            //loginViewModel.SetupProperty(vm => vm.LoginSuccessfully, true);
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
            Assert.AreEqual(this.loginViewModel.Object.SelectedDataSource,
                DataSource.CDP4);
            Assert.AreEqual("admin1", this.loginViewModel.Object.UserName);
            Assert.AreEqual("pass1", this.loginViewModel.Object.Password);
            Assert.AreEqual("https://cdp4services-public1.cdp4.org", this.loginViewModel.Object.Uri);

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
