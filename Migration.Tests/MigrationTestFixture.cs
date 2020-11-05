// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationTestFixture.cs">
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
    using ViewModels;

    /// <summary>
    /// Suite of tests for the <see cref="MigrationViewModel"/>
    /// </summary>
    [TestFixture]
    public class MigrationTestFixture
    {
        private Mock<MigrationViewModel> migrationViewModel;

        private const string SourceServerUri = "https://cdp4services-public.cdp4.org";
        private const string SourceUsername = "admin";
        private const string SourcePassword = "pass";

        /// <summary>
        /// Set target and source servers required by the migration
        /// </summary>
        private void SetTargetAndSourceServers()
        {
            this.migrationViewModel.Object.SourceViewModel = new LoginViewModel
            {
                SelectedDataSource = DataSource.CDP4,
                UserName = SourceUsername,
                Password = SourcePassword,
                Uri = SourceServerUri
            };

            this.migrationViewModel.Object.TargetViewModel = new LoginViewModel
            {
                SelectedDataSource = DataSource.CDP4,
                UserName = SourceUsername,
                Password = SourcePassword,
                Uri = SourceServerUri
            };
        }

        [SetUp]
        public void SetUp()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            this.migrationViewModel = new Mock<MigrationViewModel>();
            this.migrationViewModel.Object.AddSubscriptions();
        }

        [Test]
        public void VerifyIfMigrationStartWithSourceAndTargetSessionSet()
        {
            SetTargetAndSourceServers();

            Assert.DoesNotThrowAsync(async () =>
                await this.migrationViewModel.Object.SourceViewModel.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.migrationViewModel.Object.SourceViewModel.LoginSuccessfully);

            Assert.DoesNotThrowAsync(async () =>
                await this.migrationViewModel.Object.TargetViewModel.LoginCommand.ExecuteAsyncTask());
            Assert.AreEqual(true, this.migrationViewModel.Object.TargetViewModel.LoginSuccessfully);

            Assert.IsTrue(this.migrationViewModel.Object.CanMigrate);

            Assert.DoesNotThrowAsync(async () =>
                await this.migrationViewModel.Object.MigrateCommand.ExecuteAsyncTask());
        }

        [Test]
        public void VerifyIfMigrationNotStartWithoutSourceAndTargetSessionSet()
        {
            SetTargetAndSourceServers();

            Assert.IsNull(this.migrationViewModel.Object.SourceViewModel.ServerSession);
            Assert.IsNull(this.migrationViewModel.Object.TargetViewModel.ServerSession);
        }
    }
}
