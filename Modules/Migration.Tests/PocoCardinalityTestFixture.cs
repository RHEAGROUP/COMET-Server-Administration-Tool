﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PocoCardinalityTestFixture.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Tests
{
    using System;
    using System.Linq;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using Moq;
    using NUnit.Framework;
    using ViewModels;

    /// <summary>
    /// Suite of tests for the <see cref="FixCardinalityErrorsDialogViewModel"/>
    /// </summary>
    [TestFixture]
    public class PocoCardinalityTestFixture
    {
        private readonly Credentials credentials = new Credentials(
            "John",
            "Doe",
            new Uri("http://www.rheagroup.com/"));

        private Mock<ISession> session;
        private Mock<IDal> dal;
        private Mock<IDal> jsonFileDal;
        private Assembler assembler;

        private EngineeringModelSetup engineeringModelSetup;
        private SiteDirectory siteDirectory;
        private Participant participant;
        private SiteReferenceDataLibrary siteReferenceDataLibrary;
        private Iteration iteration;
        private DomainOfExpertise domain;
        private Person person;
        private SimpleQuantityKind quantityKindParamType;
        private ModelReferenceDataLibrary modelReferenceDataLibrary;
        private EngineeringModel engineeringModel;
        private IterationSetup iterationSetup;

        private FixCardinalityErrorsDialogViewModel viewModel;

        [SetUp]
        public void SetUp()
        {
            this.dal = new Mock<IDal>();
            this.jsonFileDal = new Mock<IDal>();
            this.jsonFileDal.Setup(x => x.DalVersion).Returns(new Version("1.0.0"));
            this.dal.SetupProperty(d => d.Session);
            this.assembler = new Assembler(this.credentials.Uri);

            this.session = new Mock<ISession>();
            this.session.Setup(x => x.Dal).Returns(this.dal.Object);
            this.session.Setup(x => x.DalVersion).Returns(new Version(1, 1, 0));
            this.session.Setup(x => x.Credentials).Returns(this.credentials);
            this.session.Setup(x => x.Assembler).Returns(this.assembler);

            // Site Directory
            this.siteDirectory = new SiteDirectory(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri);

            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri)
            {
                Name = "domain"

            };
            this.siteDirectory.Domain.Add(domain);

            this.person = new Person(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri)
            {
                ShortName = credentials.UserName,
                GivenName = credentials.UserName,
                Password = credentials.Password,
                DefaultDomain = domain,
                IsActive = true
            };
            this.siteDirectory.Person.Add(this.person);

            this.participant = new Participant(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri) { Person = person };
            this.participant.Domain.Add(this.domain);

            // Site Rld
            this.siteReferenceDataLibrary =
                new SiteReferenceDataLibrary(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri)
                {
                    ShortName = "Generic_RDL"
                };
            this.quantityKindParamType = new SimpleQuantityKind(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri)
            {
                Name = "m",
                ShortName = "m"
            };
            this.siteReferenceDataLibrary.ParameterType.Add(this.quantityKindParamType);

            // Model Rdl
            this.modelReferenceDataLibrary =
                new ModelReferenceDataLibrary(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri)
                {
                    RequiredRdl = this.siteReferenceDataLibrary
                };
            this.siteDirectory.SiteReferenceDataLibrary.Add(this.siteReferenceDataLibrary);

            // Iteration
            this.iteration = new Iteration(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri);
            this.iterationSetup = new IterationSetup(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri)
            {
                IterationIid = this.iteration.Iid
            };

            // Engineering Model & Setup
            this.engineeringModel = new EngineeringModel(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri);
            this.engineeringModel.Iteration.Add(this.iteration);

            this.engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.session.Object.Assembler.Cache, this.session.Object.Credentials.Uri)
            { EngineeringModelIid = engineeringModel.Iid };
            this.engineeringModel.EngineeringModelSetup = engineeringModelSetup;
            this.engineeringModelSetup.RequiredRdl.Add(modelReferenceDataLibrary);
            this.engineeringModelSetup.IterationSetup.Add(iterationSetup);
            this.engineeringModelSetup.Participant.Add(participant);
            this.siteDirectory.Model.Add(engineeringModelSetup);

            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(this.siteDirectory.Iid, null),
                new Lazy<Thing>(() => this.siteDirectory));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(this.domain.Iid, null),
                new Lazy<Thing>(() => this.domain));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(this.person.Iid, null),
                new Lazy<Thing>(() => this.person));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(participant.Iid, null),
                new Lazy<Thing>(() => participant));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(siteReferenceDataLibrary.Iid, null),
                new Lazy<Thing>(() => siteReferenceDataLibrary));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(quantityKindParamType.Iid, null),
                new Lazy<Thing>(() => quantityKindParamType));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(modelReferenceDataLibrary.Iid, null),
                new Lazy<Thing>(() => modelReferenceDataLibrary));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(this.engineeringModelSetup.Iid, null),
                new Lazy<Thing>(() => this.engineeringModelSetup));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(this.iteration.Iid, null),
                new Lazy<Thing>(() => this.iteration));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(iterationSetup.Iid, null),
                new Lazy<Thing>(() => iterationSetup));
            this.session.Object.Assembler.Cache.TryAdd(new CacheKey(engineeringModel.Iid, null),
                new Lazy<Thing>(() => engineeringModel));

            this.session.Setup(x => x.RetrieveSiteDirectory()).Returns(this.siteDirectory);

            this.viewModel = new FixCardinalityErrorsDialogViewModel(this.session.Object);
        }

        [Test]
        public void VerifyIfExecuteCommandsWorks()
        {
            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());
        }

        [Test]
        public void VerifyThatValidateNotStartsIfSessionIsNull()
        {
            this.viewModel = new FixCardinalityErrorsDialogViewModel(null);
            Assert.DoesNotThrow(() => this.viewModel.BindPocoErrors());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsFileTypeError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var fileType = new FileType(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);

            this.siteReferenceDataLibrary.FileType.Add(fileType);

            fileType.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(fileType.Iid, null),
                new Lazy<Thing>(() => fileType));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Extension is null or empty")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.AreEqual(0, this.viewModel.Errors.Count(e => e.Error.Contains("The property Extension is null or empty")));

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsTelephoneNumberError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var telephoneNumber = new TelephoneNumber(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);

            this.person.TelephoneNumber.Add(telephoneNumber);

            telephoneNumber.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(telephoneNumber.Iid, null),
                new Lazy<Thing>(() => telephoneNumber));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Value is null or empty")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.AreEqual(0, this.viewModel.Errors.Count(e => e.Error.Contains("The property Value is null or empty")));

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsUserPreferenceError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var userPreference = new UserPreference(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);

            this.person.UserPreference.Add(userPreference);

            userPreference.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(userPreference.Iid, null),
                new Lazy<Thing>(() => userPreference));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Value is null or empty")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.AreEqual(0, this.viewModel.Errors.Count(e => e.Error.Contains("The property Value is null or empty")));

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsCitationError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var elementDefinition = new ElementDefinition(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            var definition = new Definition(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            var citation = new Citation(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            this.iteration.Element.Add(elementDefinition);
            elementDefinition.Definition.Add(definition);
            definition.Citation.Add(citation);

            citation.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(citation.Iid, null),
                new Lazy<Thing>(() => citation));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Source is null")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsDefinitionError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var category = new Category(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            var definition = new Definition(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            this.siteReferenceDataLibrary.DefinedCategory.Add(category);
            category.Definition.Add(definition);

            definition.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(definition.Iid, null),
                new Lazy<Thing>(() => definition));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Content is null or empty")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.AreEqual(0, this.viewModel.Errors.Count(e => e.Error.Contains("The property Content is null or empty")));

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsIterationSetupError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            this.iterationSetup.ValidatePoco();

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Description is null or empty")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.AreEqual(0, this.viewModel.Errors.Count(e => e.Error.Contains("The property Description is null or empty")));

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsScaleValueDefinitionError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var ratioScale = new RatioScale(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            var scaleValueDefinition = new ScaleValueDefinition(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            ratioScale.ValueDefinition.Add(scaleValueDefinition);

            this.siteReferenceDataLibrary.Scale.Add(ratioScale);

            scaleValueDefinition.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(scaleValueDefinition.Iid, null),
                new Lazy<Thing>(() => scaleValueDefinition));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Name is null or empty")));
            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property ShortName is null or empty")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.AreEqual(0, this.viewModel.Errors.Count(e => e.Error.Contains("The property Name is null or empty")));
            Assert.AreEqual(0, this.viewModel.Errors.Count(e => e.Error.Contains("The property ShortName is null or empty")));

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsParticipantError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            this.participant.ValidatePoco();

            this.viewModel.BindPocoErrors();

            Assert.IsTrue(this.viewModel.Errors.Count > 0);

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.IsTrue(this.viewModel.Errors.Count == 0);

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatSelectedErrorWorksAsExpected()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            this.iterationSetup.ValidatePoco();

            this.viewModel.BindPocoErrors();

            var firstError = this.viewModel.Errors.FirstOrDefault();

            Assert.That(firstError != null);
            Assert.That(this.viewModel.SelectedError == null);
            Assert.That(this.viewModel.ErrorDetails == null);

            this.viewModel.SelectedError = firstError;

            Assert.That(this.viewModel.SelectedError != null);
            Assert.That(this.viewModel.ErrorDetails != null);

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesAddsScalarParameterTypeError()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var scalarParameterType = new SimpleQuantityKind(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri) as ScalarParameterType;
            this.siteReferenceDataLibrary.ParameterType.Add(scalarParameterType);

            scalarParameterType.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(scalarParameterType.Iid, null),
                new Lazy<Thing>(() => scalarParameterType));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e => e.Error.Contains("The property Symbol is null")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesFixValueSets()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var elementDefinition = new ElementDefinition(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);

            var definition = new Definition(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);

            var parameter = new Parameter(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri)
            {
                Owner = this.domain
            };

            var valueSet1 = new ParameterValueSet(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            parameter.ValueSet.Add(valueSet1);

            var valueSet2 = new ParameterValueSet(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            parameter.ValueSet.Add(valueSet2);

            elementDefinition.Parameter.Add(parameter);

            this.iteration.Element.Add(elementDefinition);
            elementDefinition.Definition.Add(definition);

            valueSet1.ValidatePoco();
            valueSet2.ValidatePoco();
            parameter.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(parameter.Iid, null),
                new Lazy<Thing>(() => parameter));
            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(valueSet1.Iid, null),
                new Lazy<Thing>(() => valueSet1));
            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(valueSet2.Iid, null),
                new Lazy<Thing>(() => valueSet2));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e =>
                e.ContainerThingClassKind == ClassKind.ParameterValueSet &&
                e.Error.Contains("The number of elements in the property Formula is wrong. It should be at least 1")));

            Assert.That(this.viewModel.Errors.Any(e =>
                e.ContainerThingClassKind == ClassKind.ParameterValueSet &&
                e.Error.Contains("The number of elements in the property Manual is wrong. It should be at least 1.")));

            Assert.That(this.viewModel.Errors.Any(e =>
                e.ContainerThingClassKind == ClassKind.ParameterValueSet &&
                e.Error.Contains("The number of elements in the property Published is wrong. It should be at least 1.")));

            Assert.That(this.viewModel.Errors.Any(e =>
                e.ContainerThingClassKind == ClassKind.ParameterValueSet &&
                e.Error.Contains("The number of elements in the property Reference is wrong. It should be at least 1.")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }

        [Test]
        public void VerifyThatValidatePocoPropertiesFixValueSetsSubscriptions()
        {
            Assert.DoesNotThrowAsync(async () => await this.session.Object.Open());

            var elementDefinition = new ElementDefinition(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            this.iteration.Element.Add(elementDefinition);

            var parameter = new Parameter(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri)
            {
                Owner = this.domain,
                ParameterType = new BooleanParameterType(
                    Guid.NewGuid(),
                    this.session.Object.Assembler.Cache,
                    this.session.Object.Credentials.Uri)
            };
            elementDefinition.Parameter.Add(parameter);

            var valueSet = new ParameterValueSet(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri);
            parameter.ValueSet.Add(valueSet);

            var parameterSubscription = new ParameterSubscription(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri)
            {
                Owner = this.domain
            };
            parameter.ParameterSubscription.Add(parameterSubscription);

            var parameterSubscriptionValueSet1 = new ParameterSubscriptionValueSet(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri)
            {
                SubscribedValueSet = valueSet
            };
            parameterSubscription.ValueSet.Add(parameterSubscriptionValueSet1);

            var parameterSubscriptionValueSet2 = new ParameterSubscriptionValueSet(
                Guid.NewGuid(),
                this.session.Object.Assembler.Cache,
                this.session.Object.Credentials.Uri)
            {
                SubscribedValueSet = valueSet
            };
            parameterSubscription.ValueSet.Add(parameterSubscriptionValueSet2);

            parameterSubscriptionValueSet1.ValidatePoco();
            parameterSubscriptionValueSet2.ValidatePoco();
            parameterSubscription.ValidatePoco();

            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(parameterSubscription.Iid, null),
                new Lazy<Thing>(() => parameterSubscription));
            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(parameterSubscriptionValueSet1.Iid, null),
                new Lazy<Thing>(() => parameterSubscriptionValueSet1));
            this.session.Object.Assembler.Cache.TryAdd(
                new CacheKey(parameterSubscriptionValueSet2.Iid, null),
                new Lazy<Thing>(() => parameterSubscriptionValueSet2));

            this.viewModel.BindPocoErrors();

            Assert.That(this.viewModel.Errors.Any(e =>
                e.ContainerThingClassKind == ClassKind.ParameterSubscription &&
                e.Error.Contains("Duplicated value-sets were found for the")));

            Assert.DoesNotThrow(() => this.viewModel.FixCommand.Execute());

            Assert.DoesNotThrowAsync(async () => await this.session.Object.Close());
        }
    }
}
