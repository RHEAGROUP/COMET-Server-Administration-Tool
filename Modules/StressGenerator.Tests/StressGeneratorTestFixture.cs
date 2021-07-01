// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StressGeneratorTestFixture.cs" company="RHEA System S.A.">
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

namespace StressGenerator.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CDP4Common.DTO;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4Dal.Operations;
    using Common.Settings;
    using Common.ViewModels;
    using Moq;
    using NUnit.Framework;
    using ViewModels;

    /// <summary>
    /// Suite of tests for the <see cref="StressGeneratorViewModel" />
    /// </summary>
    [TestFixture]
    public class StressGeneratorTestFixture
    {
        private readonly Credentials credentials = new Credentials(
            "John",
            "Doe",
            new Uri("http://www.rheagroup.com/"));

        private ISession session;
        private Mock<IDal> dal;

        private Mock<ILoginViewModel> sourceViewModel;
        private StressGeneratorViewModel stressGeneratorViewModel;

        private SiteDirectory siteDirectory;
        private DomainOfExpertise domain;
        private Person person;
        private Participant participant;
        private QuantityKind quantityKindParamType;

        private SiteReferenceDataLibrary siteReferenceDataLibrary;
        private ModelReferenceDataLibrary modelReferenceDataLibrary;

        private EngineeringModel engineeringModel;
        private EngineeringModelSetup engineeringModelSetup;
        private Iteration iteration;
        private IterationSetup iterationSetup;

        private Dictionary<Guid, Thing> sessionThings;

        [SetUp]
        public void Setup()
        {
            this.dal = new Mock<IDal>();
            this.dal.SetupProperty(d => d.Session);
            this.session = new Session(this.dal.Object, credentials);

            AppSettingsHandler.Settings = new AppSettings
            {
                SavedUris = new List<string>()
            };

            this.InitSessionThings();

            this.InitDalOperations();

            this.InitViewModel();
        }

        private void InitSessionThings()
        {
            // Site Directory
            this.siteDirectory = new SiteDirectory(Guid.NewGuid(), 0);

            this.domain = new DomainOfExpertise(Guid.NewGuid(), 0)
            {
                Name = "domain"
            };
            this.siteDirectory.Domain.Add(this.domain.Iid);

            this.person = new Person(Guid.NewGuid(), 0)
            {
                ShortName = this.credentials.UserName,
                GivenName = this.credentials.UserName,
                Password = this.credentials.Password,
                DefaultDomain = this.domain.Iid,
                IsActive = true
            };
            this.siteDirectory.Person.Add(this.person.Iid);

            this.participant = new Participant(Guid.NewGuid(), 0)
            {
                Person = this.person.Iid
            };
            this.participant.Domain.Add(this.domain.Iid);

            // Site Rld
            this.siteReferenceDataLibrary = new SiteReferenceDataLibrary(Guid.NewGuid(), 0)
            {
                ShortName = "Generic_RDL"
            };
            this.quantityKindParamType = new SimpleQuantityKind(Guid.NewGuid(), 0)
            {
                Name = "m",
                ShortName = "m"
            };
            this.siteReferenceDataLibrary.ParameterType.Add(this.quantityKindParamType.Iid);

            // Model Rdl
            this.modelReferenceDataLibrary = new ModelReferenceDataLibrary(Guid.NewGuid(), 0)
            {
                RequiredRdl = this.siteReferenceDataLibrary.Iid
            };
            this.siteDirectory.SiteReferenceDataLibrary.Add(this.siteReferenceDataLibrary.Iid);

            // Iteration
            this.iteration = new Iteration(Guid.NewGuid(), 0);
            this.iterationSetup = new IterationSetup(Guid.NewGuid(), 0)
            {
                IterationIid = this.iteration.Iid
            };
            this.iteration.IterationSetup = this.iterationSetup.Iid;

            // Engineering Model & Setup
            this.engineeringModel = new EngineeringModel(Guid.NewGuid(), 0);
            this.engineeringModel.Iteration.Add(this.iteration.Iid);

            this.engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), 0)
            {
                EngineeringModelIid = this.engineeringModel.Iid,
                Name = StressGeneratorConfiguration.ModelPrefix + "_UnitTest",
                ShortName = StressGeneratorConfiguration.ModelPrefix + "_UnitTest"
            };
            this.engineeringModel.EngineeringModelSetup = this.engineeringModelSetup.Iid;
            this.engineeringModelSetup.RequiredRdl.Add(this.modelReferenceDataLibrary.Iid);
            this.engineeringModelSetup.IterationSetup.Add(this.iterationSetup.Iid);
            this.engineeringModelSetup.Participant.Add(this.participant.Iid);
            this.siteDirectory.Model.Add(this.engineeringModelSetup.Iid);

            this.sessionThings = new Dictionary<Guid, Thing>
            {
                { this.siteDirectory.Iid, this.siteDirectory },
                { this.domain.Iid, this.domain },
                { this.person.Iid, this.person },
                { this.participant.Iid, this.participant },
                { this.siteReferenceDataLibrary.Iid, this.siteReferenceDataLibrary },
                { this.quantityKindParamType.Iid, this.quantityKindParamType },
                { this.modelReferenceDataLibrary.Iid, this.modelReferenceDataLibrary },
                { this.engineeringModelSetup.Iid, this.engineeringModelSetup },
                { this.iteration.Iid, this.iteration },
                { this.iterationSetup.Iid, this.iterationSetup },
                { this.engineeringModel.Iid, this.engineeringModel }
            };
        }

        private void InitDalOperations()
        {
            this.dal
                .Setup(x => x.Open(this.credentials, It.IsAny<CancellationToken>()))
                .Returns<Credentials, CancellationToken>((dalCredentials, cancellationToken) =>
                {
                    var result = this.sessionThings.Values.ToList() as IEnumerable<Thing>;
                    return Task.FromResult(result);
                });

            // basic mock needed for session.Refresh()
            this.dal
                .Setup(x => x.Read(It.IsAny<Thing>(), It.IsAny<CancellationToken>(), It.IsAny<IQueryAttributes>()))
                .Returns<Thing, CancellationToken, IQueryAttributes>((thing, cancellationToken, queryAttributes) =>
                {
                    var result = this.sessionThings.Values.ToList() as IEnumerable<Thing>;
                    return Task.FromResult(result);
                });

            this.dal
                .Setup(x => x.Write(It.IsAny<OperationContainer>(), It.IsAny<IEnumerable<string>>()))
                .Returns<OperationContainer, IEnumerable<string>>(async (operationContainer, files) =>
                {
                    foreach (var operation in operationContainer.Operations)
                    {
                        var operationThing = operation.ModifiedThing;

                        switch (operation.OperationKind)
                        {
                            case OperationKind.Create:
                                this.sessionThings.Add(operationThing.Iid, operationThing);

                                if (operationThing is EngineeringModelSetup newModelSetup)
                                {
                                    // create EngineeringModel
                                    var newModel = new EngineeringModel(newModelSetup.EngineeringModelIid, 0)
                                    {
                                        EngineeringModelSetup = newModelSetup.Iid,
                                    };
                                    this.sessionThings.Add(newModel.Iid, newModel);

                                    // create Participant
                                    var newParticipant = new Participant(Guid.NewGuid(), 0)
                                    {
                                        Person = this.person.Iid
                                    };
                                    newParticipant.Domain.Add(this.domain.Iid);
                                    newModelSetup.Participant.Add(newParticipant.Iid);
                                    this.sessionThings.Add(newParticipant.Iid, newParticipant);

                                    // create Iteration
                                    var newIteration = new Iteration(Guid.NewGuid(), 0);
                                    newModel.Iteration.Add(newIteration.Iid);
                                    this.sessionThings.Add(newIteration.Iid, newIteration);

                                    // create IterationSetup
                                    var newIterationSetup = new IterationSetup(Guid.NewGuid(), 0)
                                    {
                                        IterationIid = newIteration.Iid
                                    };
                                    this.sessionThings.Add(newIterationSetup.Iid, newIterationSetup);
                                    newIteration.IterationSetup = newIterationSetup.Iid;

                                    newModelSetup.IterationSetup.Add(newIterationSetup.Iid);
                                }

                                if (operationThing is Parameter newParameter)
                                {
                                    var newValueSet = new ParameterValueSet(Guid.NewGuid(), 0);
                                    this.sessionThings.Add(newValueSet.Iid, newValueSet);

                                    newParameter.ValueSet.Add(newValueSet.Iid);
                                }

                                break;
                            case OperationKind.Update:
                                this.sessionThings[operationThing.Iid] = operationThing;

                                break;

                            case OperationKind.Delete:
                                this.sessionThings.Remove(operationThing.Iid);

                                break;
                        }
                    }

                    return await Task.FromResult((IEnumerable<Thing>)new List<Thing>());
                });
        }

        private void InitViewModel()
        {
            this.sourceViewModel = new Mock<ILoginViewModel>();
            this.sourceViewModel.Setup(x => x.SelectedDataSource).Returns(DataSource.CDP4);
            this.sourceViewModel.Setup(x => x.UserName).Returns(this.credentials.UserName);
            this.sourceViewModel.Setup(x => x.Password).Returns(this.credentials.Password);
            this.sourceViewModel.Setup(x => x.Uri).Returns(this.credentials.Uri.ToString());
            this.sourceViewModel.Setup(x => x.ServerSession).Returns(this.session);

            this.stressGeneratorViewModel = new StressGeneratorViewModel
            {
                SourceViewModel = this.sourceViewModel.Object,
                TimeInterval = 1,
                TestObjectsNumber = StressGeneratorConfiguration.MinNumberOfTestObjects,
                ElementName = StressGeneratorConfiguration.GenericElementName,
                ElementShortName = StressGeneratorConfiguration.GenericElementShortName,
                SelectedEngineeringModelSetup = new CDP4Common.SiteDirectoryData.EngineeringModelSetup(
                    this.engineeringModelSetup.Iid,
                    null,
                    null)
                {
                    EngineeringModelIid = this.engineeringModel.Iid,
                    Name = StressGeneratorConfiguration.ModelPrefix + "_UnitTest",
                    ShortName = StressGeneratorConfiguration.ModelPrefix + "_UnitTest"
                }
            };
        }

        [Test]
        public void VerifyConfigurationTestObjectNumberLimits()
        {
            var configuration = new StressGeneratorConfiguration(this.session);

            configuration.TestObjectsNumber = StressGeneratorConfiguration.MinNumberOfTestObjects - 1;
            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                configuration.TestObjectsNumber);

            configuration.TestObjectsNumber = StressGeneratorConfiguration.MaxNumberOfTestObjects + 1;
            Assert.AreEqual(
                StressGeneratorConfiguration.MaxNumberOfTestObjects,
                configuration.TestObjectsNumber);

            var randomValidValue = new Random().Next(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                StressGeneratorConfiguration.MaxNumberOfTestObjects);
            configuration.TestObjectsNumber = randomValidValue;
            Assert.AreEqual(
                randomValidValue,
                configuration.TestObjectsNumber);
        }

        [Test]
        public void VerifyThatStressGeneratorFailedIfSessionHasNoOpenIteration()
        {
            // setup test before open so updated objects are read
            this.siteDirectory.Model.Remove(this.engineeringModelSetup.Iid);

            // open session
            this.session.Open();

            // verify
            var initialObjectsCount = this.sessionThings.Values.Count;

            Assert.DoesNotThrow(() => this.stressGeneratorViewModel.StressCommand.Execute(null));
            Assert.AreEqual(initialObjectsCount, this.sessionThings.Values.Count);
        }

        [Test]
        public void VerifyThatStressGeneratorPropertiesAreSets()
        {
            Assert.AreEqual(this.sourceViewModel.Object, this.stressGeneratorViewModel.SourceViewModel);
            Assert.AreEqual(1, this.stressGeneratorViewModel.TimeInterval);
            Assert.AreEqual(StressGeneratorConfiguration.MinNumberOfTestObjects, this.stressGeneratorViewModel.TestObjectsNumber);
            Assert.AreEqual(StressGeneratorConfiguration.GenericElementName, this.stressGeneratorViewModel.ElementName);
            Assert.AreEqual(StressGeneratorConfiguration.GenericElementShortName, this.stressGeneratorViewModel.ElementShortName);
            Assert.AreEqual(this.engineeringModelSetup.Iid, this.stressGeneratorViewModel.SelectedEngineeringModelSetup.Iid);
            Assert.AreEqual(SupportedOperationMode.Open, this.stressGeneratorViewModel.SelectedOperationMode);
            Assert.AreEqual(StressGeneratorConfiguration.ModelPrefix, this.stressGeneratorViewModel.NewModelName);
        }

        [Test]
        public void VerifyThatStressGeneratorWorksInOpenMode()
        {
            // open session
            this.session.Open();

            // setup test
            this.stressGeneratorViewModel.SelectedOperationMode = SupportedOperationMode.Open;

            // verify
            Assert.DoesNotThrow(() => this.stressGeneratorViewModel.StressCommand.Execute(null));

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<ElementDefinition>().Count());

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<Parameter>().Count());

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<ParameterValueSet>().Count());
        }

        [Test]
        public void VerifyThatStressGeneratorFailedIfModelDoesNotReferenceGenericRdl()
        {
            // setup test before open so updated objects are read
            this.siteReferenceDataLibrary.ShortName = "Random_RDL";

            // open session
            this.session.Open();

            // verify
            var initialObjectsCount = this.sessionThings.Values.Count;

            Assert.DoesNotThrow(() => this.stressGeneratorViewModel.StressCommand.Execute(null));
            Assert.AreEqual(initialObjectsCount, this.sessionThings.Values.Count);
        }

        [Test]
        public void VerifyThatStressGeneratorWorksInCreateMode()
        {
            // open session
            this.session.Open();

            // setup test
            this.stressGeneratorViewModel.SelectedOperationMode = SupportedOperationMode.Create;

            // verify
            Assert.DoesNotThrow(() => this.stressGeneratorViewModel.StressCommand.Execute(null));

            Assert.AreEqual(2, this.sessionThings.Values.OfType<EngineeringModelSetup>().Count());

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<ElementDefinition>().Count());

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<Parameter>().Count());

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<ParameterValueSet>().Count());
        }

        [Test]
        public void VerifyThatStressGeneratorDeleteCreatedTestModel()
        {
            // open session
            this.session.Open();

            // setup test
            this.stressGeneratorViewModel.SelectedOperationMode = SupportedOperationMode.Create;
            this.stressGeneratorViewModel.DeleteModel = true;

            // verify
            Assert.DoesNotThrow(() => this.stressGeneratorViewModel.StressCommand.Execute(null));

            Assert.AreEqual(1, this.sessionThings.Values.OfType<EngineeringModelSetup>().Count());
            Assert.IsTrue(this.sessionThings.ContainsKey(this.engineeringModelSetup.Iid));
        }

        [Test]
        public void VerifyThatStressGeneratorWorksInCreateOverwriteMode()
        {
            // open session
            this.session.Open();

            //setup test
            this.stressGeneratorViewModel.SelectedOperationMode = SupportedOperationMode.CreateOverwrite;

            // selected model needs to be reinitialized as selection is reset on CreateOverwrite
            this.stressGeneratorViewModel.SelectedEngineeringModelSetup =
                new CDP4Common.SiteDirectoryData.EngineeringModelSetup(
                    this.engineeringModelSetup.Iid,
                    null,
                    null);

            // verify
            Assert.DoesNotThrow(() => this.stressGeneratorViewModel.StressCommand.Execute(null));

            var engineeringModelSetups = this.sessionThings.Values.OfType<EngineeringModelSetup>().ToList();
            Assert.AreEqual(1, engineeringModelSetups.Count);

            var newEngineeringModelSetup = engineeringModelSetups.Single();
            Assert.AreEqual(this.engineeringModelSetup.Name, newEngineeringModelSetup.Name);

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<ElementDefinition>().Count());

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<Parameter>().Count());

            Assert.AreEqual(
                StressGeneratorConfiguration.MinNumberOfTestObjects,
                this.sessionThings.Values.OfType<ParameterValueSet>().Count());
        }
    }
}
