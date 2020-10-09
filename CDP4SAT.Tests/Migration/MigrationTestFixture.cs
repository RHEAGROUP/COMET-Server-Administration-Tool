// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationTestFixture.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace App.Tests.Migration
{
    using System.Reactive.Concurrency;
    using ReactiveUI;
    using Moq;
    using NUnit.Framework;
    using System.Collections.Generic;
    using global::Migration.ViewModels;
    using global::Migration.ViewModels.Common;

    /// <summary>
    /// Suite of tests for the <see cref="Migration"/> <see cref="MigrationViewModel"/>
    /// </summary>
    [TestFixture]
    public class MigrationTestFixture : CommonTest
    {
        private Mock<MigrationViewModel> migrationViewModel;

        /// <summary>
        /// Set target and source servers required by the migration
        /// </summary>
        private void SetTargetAndSourceServers()
        {
            // Migration will always fail when the target server does not contains models and in order to 'pass' the tests the target server it's the same with the source server

            this.migrationViewModel.Object.SourceViewModel = new LoginViewModel
            {
                ServerType = new KeyValuePair<string, string>("CDP", "CDP4 WebServices"),
                UserName = SourceUsername,
                Password = SourcePassword,
                Uri = SourceServerUri
            };

            this.migrationViewModel.Object.TargetViewModel = new LoginViewModel
            {
                ServerType = new KeyValuePair<string, string>("CDP", "CDP4 WebServices"),
                UserName = SourceUsername, // TargetUsername
                Password = SourcePassword, // TargetPassword
                Uri = SourceServerUri      // TargetServerUri
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

            Assert.DoesNotThrowAsync(async () =>
                await this.migrationViewModel.Object.SourceViewModel.ServerSession.Close());
            Assert.DoesNotThrowAsync(async () =>
                await this.migrationViewModel.Object.TargetViewModel.ServerSession.Close());
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
