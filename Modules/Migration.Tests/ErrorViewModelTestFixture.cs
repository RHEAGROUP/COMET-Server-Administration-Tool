// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorViewModelTestFixture.cs" company="RHEA System S.A.">
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
    using CDP4Dal;
    using CDP4Dal.DAL;
    using Common.Settings;
    using Common.ViewModels;
    using Moq;
    using NUnit.Framework;
    using ViewModels;

    [TestFixture]
    public class ErrorViewModelTestFixture
    {
        private Mock<ISession> session;
        private Mock<IDal> dal;
        private Assembler assembler;

        private readonly Credentials credentials = new Credentials(
            "admin",
            "password",
            new Uri("http://www.rheagroup.com/"));


        [SetUp]
        public void SetUp()
        {
            AppSettingsHandler.Settings = new AppSettings
            {
                SavedUris = new List<string>()
            };

            this.dal = new Mock<IDal>();
            this.dal.SetupProperty(d => d.Session);
            this.assembler = new Assembler(this.credentials.Uri);

            this.session = new Mock<ISession>();
            this.session.Setup(x => x.Dal).Returns(this.dal.Object);
            this.session.Setup(x => x.DalVersion).Returns(new Version(1, 1, 0));
            this.session.Setup(x => x.Credentials).Returns(this.credentials);
            this.session.Setup(x => x.Assembler).Returns(this.assembler);
        }

        [Test]
        public void VerifyPropertiesSet()
        {
            var vm = new ErrorViewModel(this.session.Object);
            Assert.IsEmpty(vm.PocoErrors);
            Assert.IsEmpty(vm.RuleCheckerErrors);
            Assert.IsNull(vm.CurrentModelError);
            Assert.IsNull(vm.CurrentPocoError);
            Assert.IsEmpty(vm.ErrorDetails);
        }

        [Test]
        public void VerifyCommandsExecute()
        {
            var vm = new ErrorViewModel(this.session.Object);

            Assert.DoesNotThrow(() => vm.ModelSelectRowCommand.Execute());

            Assert.DoesNotThrow(() => vm.PocoSelectRowCommand.Execute());
        }
    }
}