// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibraryTestFixture.cs" company="RHEA System S.A.">
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

namespace Syncer.Tests
{
    using CDP4Common.DTO;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4Dal.Operations;
    using Moq;
    using NUnit.Framework;
    using Syncer.Utils;
    using Syncer.Utils.Sync;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class SiteReferenceDataLibraryTestFixture
    {
        private readonly Credentials credentials = new Credentials(
            "John", 
            "Doe", 
            new Uri("http://www.rheagroup.com/"));

        private Dictionary<Guid, Thing> sourceThings;
        private Dictionary<Guid, Thing> targetThings;

        private Mock<IDal> sourceDal;
        private Mock<IDal> targetDal;

        private ISession sourceSession;
        private ISession targetSession;

        private SiteDirectory sourceSiteDirectory;
        private SiteDirectory targetSiteDirectory;
        private Person person;

        private static void AddToThings(in Dictionary<Guid, Thing> things, Thing thing)
        {
            things.Add(thing.Iid, thing);
        }

        [SetUp]
        public void Setup()
        {
            this.sourceThings = new Dictionary<Guid, Thing>();
            this.targetThings = new Dictionary<Guid, Thing>();
            
            // initialize common things
            this.sourceSiteDirectory = new SiteDirectory(Guid.NewGuid(), 0);
            AddToThings(this.sourceThings, this.sourceSiteDirectory);
            this.targetSiteDirectory = new SiteDirectory(Guid.NewGuid(), 0);
            AddToThings(this.targetThings, this.targetSiteDirectory);

            this.person = new Person(Guid.NewGuid(), 0)
            {
                ShortName = "John",
                GivenName = "John",
                Password = "Doe",
                IsActive = true
            };
            this.sourceSiteDirectory.Person.Add(this.person.Iid);
            AddToThings(this.sourceThings, this.person);
            this.targetSiteDirectory.Person.Add(this.person.Iid);
            AddToThings(this.targetThings, this.person);

            // setup dal and session
            this.sourceDal = new Mock<IDal>();
            this.targetDal = new Mock<IDal>();

            this.sourceDal.SetupProperty(d => d.Session);
            this.targetDal.SetupProperty(d => d.Session);

            this.sourceSession = new Session(this.sourceDal.Object, credentials);
            this.targetSession = new Session(this.targetDal.Object, credentials);

            // setup source read
            this.sourceDal
                .Setup(x => x.Open(It.IsAny<Credentials>(), It.IsAny<CancellationToken>()))
                .Returns<Credentials, CancellationToken>((credentials, cancellationToken) =>
                {
                    var result = this.sourceThings.Values.ToList() as IEnumerable<Thing>;
                    return Task.FromResult(result);
                });

            // setup target read
            this.targetDal
                .Setup(x => x.Open(It.IsAny<Credentials>(), It.IsAny<CancellationToken>()))
                .Returns<Credentials, CancellationToken>((credentials, cancellationToken) =>
                {
                    var result = this.targetThings.Values.ToList() as IEnumerable<Thing>;
                    return Task.FromResult(result);
                });

            // setup target write
            this.targetDal
                .Setup(x => x.Write(It.IsAny<OperationContainer>(), It.IsAny<IEnumerable<string>>()))
                .Returns<OperationContainer, IEnumerable<string>>((operationContainer, files) =>
                {
                    var result = new List<Thing>();

                    foreach (var operation in operationContainer.Operations)
                    {
                        var modThing = operation.ModifiedThing;

                        switch (operation.OperationKind)
                        {
                            case OperationKind.Create:
                                this.targetThings.Add(modThing.Iid, modThing);
                                break;
                            case OperationKind.Update:
                                this.targetThings[modThing.Iid] = modThing;
                                break;
                            default:
                                throw new ArgumentException($"Invalid operation kind ${operation.OperationKind}");
                        }
                    }

                    return Task.FromResult((IEnumerable<Thing>)result);
                });
        }

        [Test]
        public async Task VerifySyncNewRdl()
        {
            // setup
            var siteRdl = new SiteReferenceDataLibrary(Guid.NewGuid(), 0);
            this.sourceSiteDirectory.SiteReferenceDataLibrary.Add(siteRdl.Iid);
            AddToThings(this.sourceThings, siteRdl);

            // verification
            await this.sourceSession.Open();
            await this.targetSession.Open();

            Assert.AreEqual(2, this.targetThings.Count);

            var syncer = SyncerFactory.GetInstance()
                .CreateSyncer(ThingType.SiteReferenceDataLibrary, this.sourceSession, this.targetSession);

            await syncer.Sync(new List<Guid> { siteRdl.Iid });

            Assert.AreEqual(3, this.targetThings.Count);
            Assert.IsTrue(this.targetThings.ContainsKey(siteRdl.Iid));
        }

        [Test]
        public async Task VerifySyncNewRdlContained()
        {
            // setup
            var siteRdl = new SiteReferenceDataLibrary(Guid.NewGuid(), 0);
            this.sourceSiteDirectory.SiteReferenceDataLibrary.Add(siteRdl.Iid);
            AddToThings(this.sourceThings, siteRdl);

            var definition = new Definition(Guid.NewGuid(), 0);
            siteRdl.Definition.Add(definition.Iid);
            AddToThings(this.sourceThings, definition);

            var citation = new Citation(Guid.NewGuid(), 0);
            definition.Citation.Add(citation.Iid);
            AddToThings(this.sourceThings, citation);

            // verification
            await this.sourceSession.Open();
            await this.targetSession.Open();

            Assert.AreEqual(2, this.targetThings.Count);

            var syncer = SyncerFactory.GetInstance()
                .CreateSyncer(ThingType.SiteReferenceDataLibrary, this.sourceSession, this.targetSession);

            await syncer.Sync(new List<Guid> { siteRdl.Iid });

            Assert.AreEqual(5, this.targetThings.Count);
            Assert.IsTrue(this.targetThings.ContainsKey(siteRdl.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(definition.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(citation.Iid));
        }

        [Test]
        public async Task VerifySyncNewRdlReferenced()
        {
            // setup
            var siteRdl = new SiteReferenceDataLibrary(Guid.NewGuid(), 0);
            this.sourceSiteDirectory.SiteReferenceDataLibrary.Add(siteRdl.Iid);
            AddToThings(this.sourceThings, siteRdl);

            var definition = new Definition(Guid.NewGuid(), 0);
            siteRdl.Definition.Add(definition.Iid);
            AddToThings(this.sourceThings, definition);

            var citation = new Citation(Guid.NewGuid(), 0);
            definition.Citation.Add(citation.Iid);
            AddToThings(this.sourceThings, citation);

            // references
            var measurementUnit = new SimpleUnit(Guid.NewGuid(), 0);
            siteRdl.BaseUnit.Add(measurementUnit.Iid);
            AddToThings(this.sourceThings, measurementUnit);

            var referenceSource = new ReferenceSource(Guid.NewGuid(), 0);
            citation.Source = referenceSource.Iid;
            AddToThings(this.sourceThings, referenceSource);

            // verification
            await this.sourceSession.Open();
            await this.targetSession.Open();

            Assert.AreEqual(2, this.targetThings.Count);

            var syncer = SyncerFactory.GetInstance()
                .CreateSyncer(ThingType.SiteReferenceDataLibrary, this.sourceSession, this.targetSession);

            await syncer.Sync(new List<Guid> { siteRdl.Iid });

            Assert.AreEqual(5, this.targetThings.Count);
            Assert.IsTrue(this.targetThings.ContainsKey(siteRdl.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(definition.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(citation.Iid));
            Assert.IsFalse(this.targetThings.ContainsKey(measurementUnit.Iid));
            Assert.IsFalse(this.targetThings.ContainsKey(referenceSource.Iid));
        }

        [Test]
        public async Task VerifySyncExistingRdl()
        {
            // setup
            var siteRdl = new SiteReferenceDataLibrary(Guid.NewGuid(), 0);
            this.sourceSiteDirectory.SiteReferenceDataLibrary.Add(siteRdl.Iid);
            AddToThings(this.sourceThings, siteRdl);
            this.targetSiteDirectory.SiteReferenceDataLibrary.Add(siteRdl.Iid);
            AddToThings(this.targetThings, siteRdl);

            // verification
            await this.sourceSession.Open();
            await this.targetSession.Open();

            Assert.AreEqual(3, this.targetThings.Count);
            Assert.IsTrue(this.targetThings.ContainsKey(siteRdl.Iid));

            var syncer = SyncerFactory.GetInstance()
                .CreateSyncer(ThingType.SiteReferenceDataLibrary, this.sourceSession, this.targetSession);

            await syncer.Sync(new List<Guid> { siteRdl.Iid });

            Assert.AreEqual(3, this.targetThings.Count);
        }

        [Test]
        public async Task VerifySyncExistingRdlContained()
        {
            // setup
            var commonRdlSource = new SiteReferenceDataLibrary(Guid.NewGuid(), 0);
            this.sourceSiteDirectory.SiteReferenceDataLibrary.Add(commonRdlSource.Iid);
            AddToThings(this.sourceThings, commonRdlSource);

            var commonRdlTarget = new SiteReferenceDataLibrary(commonRdlSource.Iid, 0);
            this.targetSiteDirectory.SiteReferenceDataLibrary.Add(commonRdlTarget.Iid);
            AddToThings(this.targetThings, commonRdlTarget);

            var commonDefinitionSource = new Definition(Guid.NewGuid(), 0);
            commonRdlSource.Definition.Add(commonDefinitionSource.Iid);
            AddToThings(this.sourceThings, commonDefinitionSource);

            var commonDefinitionTarget = new Definition(commonDefinitionSource.Iid, 0);
            commonRdlTarget.Definition.Add(commonDefinitionTarget.Iid);
            AddToThings(this.targetThings, commonDefinitionTarget);

            var newDefinition = new Definition(Guid.NewGuid(), 0);
            commonRdlSource.Definition.Add(newDefinition.Iid);
            AddToThings(this.sourceThings, newDefinition);

            var commonCitationSource = new Citation(Guid.NewGuid(), 0);
            commonDefinitionSource.Citation.Add(commonCitationSource.Iid);
            AddToThings(this.sourceThings, commonCitationSource);

            var commonCitationTarget = new Citation(commonCitationSource.Iid, 0);
            commonDefinitionTarget.Citation.Add(commonCitationTarget.Iid);
            AddToThings(this.targetThings, commonCitationTarget);

            var newCitation = new Citation(Guid.NewGuid(), 0);
            commonDefinitionSource.Citation.Add(newCitation.Iid);
            AddToThings(this.sourceThings, newCitation);

            // verification
            await this.sourceSession.Open();
            await this.targetSession.Open();

            Assert.AreEqual(5, this.targetThings.Count);
            Assert.IsTrue(this.targetThings.ContainsKey(commonRdlTarget.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(commonDefinitionTarget.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(commonCitationTarget.Iid));

            var syncer = SyncerFactory.GetInstance()
                .CreateSyncer(ThingType.SiteReferenceDataLibrary, this.sourceSession, this.targetSession);

            await syncer.Sync(new List<Guid> { commonRdlSource.Iid });

            Assert.AreEqual(7, this.targetThings.Count);
            Assert.IsTrue(this.targetThings.ContainsKey(newDefinition.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(newCitation.Iid));
        }

        [Test]
        public async Task VerifySyncExistingRdlReferenced()
        {
            // setup
            var commonRdlSource = new SiteReferenceDataLibrary(Guid.NewGuid(), 0);
            this.sourceSiteDirectory.SiteReferenceDataLibrary.Add(commonRdlSource.Iid);
            AddToThings(this.sourceThings, commonRdlSource);

            var commonRdlTarget = new SiteReferenceDataLibrary(commonRdlSource.Iid, 0);
            this.targetSiteDirectory.SiteReferenceDataLibrary.Add(commonRdlTarget.Iid);
            AddToThings(this.targetThings, commonRdlTarget);

            var commonDefinitionSource = new Definition(Guid.NewGuid(), 0);
            commonRdlSource.Definition.Add(commonDefinitionSource.Iid);
            AddToThings(this.sourceThings, commonDefinitionSource);

            var commonDefinitionTarget = new Definition(commonDefinitionSource.Iid, 0);
            commonRdlTarget.Definition.Add(commonDefinitionTarget.Iid);
            AddToThings(this.targetThings, commonDefinitionTarget);

            var commonCitationSource = new Citation(Guid.NewGuid(), 0);
            commonDefinitionSource.Citation.Add(commonCitationSource.Iid);
            AddToThings(this.sourceThings, commonCitationSource);

            var commonCitationTarget = new Citation(commonCitationSource.Iid, 0);
            commonDefinitionTarget.Citation.Add(commonCitationTarget.Iid);
            AddToThings(this.targetThings, commonCitationTarget);

            // references
            var measurementUnit = new SimpleUnit(Guid.NewGuid(), 0);
            commonRdlSource.BaseUnit.Add(measurementUnit.Iid);
            AddToThings(this.sourceThings, measurementUnit);

            var referenceSource = new ReferenceSource(Guid.NewGuid(), 0);
            commonCitationSource.Source = referenceSource.Iid;
            AddToThings(this.sourceThings, referenceSource);

            // verification
            await this.sourceSession.Open();
            await this.targetSession.Open();

            Assert.AreEqual(5, this.targetThings.Count);
            Assert.IsTrue(this.targetThings.ContainsKey(commonRdlTarget.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(commonDefinitionTarget.Iid));
            Assert.IsTrue(this.targetThings.ContainsKey(commonCitationTarget.Iid));

            var syncer = SyncerFactory.GetInstance()
                .CreateSyncer(ThingType.SiteReferenceDataLibrary, this.sourceSession, this.targetSession);

            await syncer.Sync(new List<Guid> { commonRdlSource.Iid });

            Assert.AreEqual(5, this.targetThings.Count);
            Assert.IsFalse(this.targetThings.ContainsKey(measurementUnit.Iid));
            Assert.IsFalse(this.targetThings.ContainsKey(referenceSource.Iid));
        }
    }
}
