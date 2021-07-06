// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StressGeneratorViewModel.cs" company="RHEA System S.A.">
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

namespace StressGenerator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using Common.ViewModels;
    using ReactiveUI;
    using Utils;

    /// <summary>
    /// Supported operation modes
    /// </summary>
    public enum SupportedOperationMode
    {
        Open,
        Create,
        CreateOverwrite
    }

    /// <summary>
    /// The view-model for the StressGenerator tool
    /// </summary>
    public sealed class StressGeneratorViewModel : BaseModuleViewModel
    {
        /// <summary>
        /// The <see cref="StressGenerator"/> used to build the helper sync classes
        /// </summary>
        private readonly StressGenerator stressGenerator = StressGenerator.GetInstance();

        /// <summary>
        /// Backing field for the source view model <see cref="LoginViewModel"/>
        /// </summary>
        private ILoginViewModel sourceViewModel;

        /// <summary>
        /// Gets or sets the source view model
        /// </summary>
        public ILoginViewModel SourceViewModel
        {
            get => this.sourceViewModel;
            set => this.RaiseAndSetIfChanged(ref this.sourceViewModel, value);
        }

        /// <summary>
        /// Gets data source server type
        /// </summary>
        public static Dictionary<DataSource, string> StressGeneratorTargetServerTypes { get; } =
            new Dictionary<DataSource, string>
            {
                {DataSource.CDP4, "CDP4 WebServices"},
                {DataSource.WSP, "OCDT WSP Server"}
            };

        /// <summary>
        /// Gets supported operation modes
        /// </summary>
        public static Dictionary<SupportedOperationMode, string> StressGeneratorModes { get; } =
            new Dictionary<SupportedOperationMode, string>
            {
                {SupportedOperationMode.Open, SupportedOperationMode.Open.ToString()},
                {SupportedOperationMode.Create, SupportedOperationMode.Create.ToString()},
                {SupportedOperationMode.CreateOverwrite, SupportedOperationMode.CreateOverwrite.ToString()}
            };

        /// <summary>
        /// Backing field for the <see cref="SelectedEngineeringModelSetup"/> property
        /// </summary>
        private EngineeringModelSetup selectedEngineeringModelSetup;

        /// <summary>
        /// Gets or sets selected engineering model
        /// </summary>
        public EngineeringModelSetup SelectedEngineeringModelSetup
        {
            get => this.selectedEngineeringModelSetup;
            set => this.RaiseAndSetIfChanged(ref this.selectedEngineeringModelSetup, value);
        }

        /// <summary>
        /// Backing field for the <see cref="SelectedSourceEngineeringModelSetup"/> property
        /// </summary>
        private EngineeringModelSetup selectedSourceEngineeringModelSetup;

        /// <summary>
        /// Gets or sets selected source engineering model
        /// </summary>
        public EngineeringModelSetup SelectedSourceEngineeringModelSetup
        {
            get => this.selectedSourceEngineeringModelSetup;
            set => this.RaiseAndSetIfChanged(ref this.selectedSourceEngineeringModelSetup, value);
        }

        /// <summary>
        /// Backing field for the <see cref="EngineeringModelSetupList"/> property
        /// </summary>
        private ReactiveList<EngineeringModelSetup> engineeringModelSetupList;

        /// <summary>
        /// Gets or sets stress engineering models list
        /// </summary>
        public ReactiveList<EngineeringModelSetup> EngineeringModelSetupList
        {
            get => this.engineeringModelSetupList;
            private set => this.RaiseAndSetIfChanged(ref this.engineeringModelSetupList, value);
        }

        /// <summary>
        /// Backing field for the <see cref="SourceEngineeringModelSetupList"/> property
        /// </summary>
        private ReactiveList<EngineeringModelSetup> sourceEngineeringModelSetupList;

        /// <summary>
        /// Gets or sets source engineering models list for creating stress models
        /// </summary>
        public ReactiveList<EngineeringModelSetup> SourceEngineeringModelSetupList
        {
            get => this.sourceEngineeringModelSetupList;
            private set => this.RaiseAndSetIfChanged(ref this.sourceEngineeringModelSetupList, value);
        }

        /// <summary>
        /// Backing field for the <see cref="TimeInterval"/> property
        /// </summary>
        private int timeInterval;

        /// <summary>
        /// Gets or sets the time interval in seconds for test data generation
        /// </summary>
        public int TimeInterval
        {
            get => this.timeInterval;
            set => this.RaiseAndSetIfChanged(ref this.timeInterval, value);
        }

        /// <summary>
        /// Backing field for the <see cref="TestObjectsNumber"/> property
        /// </summary>
        private int testObjectsNumber;

        /// <summary>
        /// Gets or sets the number of the test objects to be generated
        /// </summary>
        public int TestObjectsNumber
        {
            get => this.testObjectsNumber;
            set => this.RaiseAndSetIfChanged(ref this.testObjectsNumber, value);
        }

        /// <summary>
        /// Backing field for the <see cref="IsTestObjectsNumberInvalid"/> property
        /// </summary>
        private bool isTestObjectsNumberInvalid;

        /// <summary>
        /// Gets or sets the validity for the test objects number valid
        /// </summary>
        public bool IsTestObjectsNumberInvalid
        {
            get => this.isTestObjectsNumberInvalid;
            set => this.RaiseAndSetIfChanged(ref this.isTestObjectsNumberInvalid, value);
        }

        /// <summary>
        /// Backing field for the the <see cref="ElementName"/> messages property
        /// </summary>
        private string elementName;

        /// <summary>
        /// Gets or sets the first part of generated element definition name.
        /// </summary>
        public string ElementName
        {
            get => this.elementName;
            set => this.RaiseAndSetIfChanged(ref this.elementName, value);
        }

        /// <summary>
        /// Backing field for the the <see cref="ElementShortName"/> messages property
        /// </summary>
        private string elementShortName;

        /// <summary>
        /// Gets or sets the first part of generated element definition short name.
        /// </summary>
        public string ElementShortName
        {
            get => this.elementShortName;
            set => this.RaiseAndSetIfChanged(ref this.elementShortName, value);
        }

        /// <summary>
        /// Backing field for the the <see cref="DeleteAllElements"/> messages property
        /// </summary>
        private bool deleteAllElements;

        /// <summary>
        /// Gets or sets a flag that trigger deleting of all elements in the engineering model.
        /// </summary>
        public bool DeleteAllElements
        {
            get => this.deleteAllElements;
            set => this.RaiseAndSetIfChanged(ref this.deleteAllElements, value);
        }

        /// <summary>
        /// Backing field for the the <see cref="DeleteModel"/> messages property
        /// </summary>
        private bool deleteModel;

        /// <summary>
        /// Gets or sets a flag that trigger deleting of all elements in the engineering model.
        /// </summary>
        public bool DeleteModel
        {
            get => this.deleteModel;
            set => this.RaiseAndSetIfChanged(ref this.deleteModel, value);
        }

        /// <summary>
        /// Out property for the <see cref="CanStress"/> property
        /// </summary>
        private ObservableAsPropertyHelper<bool> canStress;

        /// <summary>
        /// Gets a value indicating whether a stress operation can start
        /// </summary>
        public bool CanStress => this.canStress.Value;

        /// <summary>
        /// Gets the server sync command
        /// </summary>
        public ReactiveCommand<Unit> StressCommand { get; set; }

        /// <summary>
        /// Backing field for the <see cref="SupportedOperationMode"/> property
        /// </summary>
        private SupportedOperationMode selectedOperationMode;

        /// <summary>
        /// Gets or sets the server operation mode value
        /// </summary>
        public SupportedOperationMode SelectedOperationMode
        {
            get => this.selectedOperationMode;
            set => this.RaiseAndSetIfChanged(ref this.selectedOperationMode, value);
        }

        /// <summary>
        /// Backing field for the <see cref="LoginSuccessfully"/> property
        /// </summary>
        private bool loginSuccessfully;

        /// <summary>
        /// Gets or sets the login successfully flag
        /// </summary>
        public bool LoginSuccessfully
        {
            get => this.loginSuccessfully;
            private set => this.RaiseAndSetIfChanged(ref this.loginSuccessfully, value);
        }

        /// <summary>
        /// Backing field for the <see cref="ModeCreate"/> property
        /// </summary>
        private bool modeCreate;

        /// <summary>
        /// Gets or sets the mode create flag
        /// </summary>
        public bool ModeCreate
        {
            get => this.modeCreate;
            private set => this.RaiseAndSetIfChanged(ref this.modeCreate, value);
        }

        /// <summary>
        /// Backing field for the <see cref="SourceModelIsEnabled"/> property
        /// </summary>
        private bool sourceModelIsEnabled;

        /// <summary>
        /// Gets or sets the source model is enabled flag
        /// </summary>
        public bool SourceModelIsEnabled
        {
            get => this.sourceModelIsEnabled;
            private set => this.RaiseAndSetIfChanged(ref this.sourceModelIsEnabled, value);
        }

        /// <summary>
        /// Backing field for the <see cref="NewModelName"/> property
        /// </summary>
        private string newModelName;

        /// <summary>
        /// Gets or sets the new model name
        /// </summary>
        public string NewModelName
        {
            get => this.newModelName;
            set => this.RaiseAndSetIfChanged(ref this.newModelName, value);
        }

        /// <summary>
        /// Get the model prefix information
        /// </summary>
        public string ModelPrefixInformation =>
            $"For safety, the short name of the engineering model must start with '{StressGeneratorConfiguration.ModelPrefix}'";

        /// <summary>
        /// Get the test objects number information
        /// </summary>
        public string TestObjectsNumberInformation =>
            $"The number of the test objects should be between {StressGeneratorConfiguration.MinNumberOfTestObjects} and {StressGeneratorConfiguration.MaxNumberOfTestObjects}.";

        /// <summary>
        /// Initializes a new instance of the <see cref="StressGeneratorViewModel"/> class
        /// </summary>
        public StressGeneratorViewModel()
        {
            this.SetProperties();
            this.AddSubscriptions();
        }

        /// <summary>
        /// Add model subscriptions
        /// </summary>
        public override void AddSubscriptions()
        {
            base.AddSubscriptions();

            this.WhenAnyValue(vm => vm.SourceViewModel.Output).Subscribe(_ => {
                this.OperationMessageHandler(this.SourceViewModel.Output);
            });

            this.WhenAnyValue(vm => vm.TestObjectsNumber).Subscribe(objectsNumber =>
            {
                this.isTestObjectsNumberInvalid = objectsNumber < StressGeneratorConfiguration.MinNumberOfTestObjects ||
                                                  objectsNumber > StressGeneratorConfiguration.MaxNumberOfTestObjects;
            });

            var canExecuteStress = this.WhenAnyValue(
                vm => vm.SourceViewModel.LoginSuccessfully,
                vm => vm.TimeInterval,
                vm => vm.TestObjectsNumber,
                vm => vm.ElementName,
                vm => vm.ElementShortName,
                vm => vm.SelectedEngineeringModelSetup,
                vm => vm.NewModelName,
                (sourceLoggedIn, interval, objectsNumber, name, shortName, modelSetup, modelName) =>
                    sourceLoggedIn &&
                    interval >= StressGeneratorConfiguration.MinTimeInterval &&
                    objectsNumber >= StressGeneratorConfiguration.MinNumberOfTestObjects &&
                    objectsNumber <= StressGeneratorConfiguration.MaxNumberOfTestObjects &&
                    !string.IsNullOrEmpty(name) &&
                    !string.IsNullOrEmpty(shortName) &&
                    (modelSetup != null || !string.IsNullOrEmpty(modelName)));

            canExecuteStress.ToProperty(this, vm => vm.CanStress, out this.canStress);

            this.WhenAnyValue(
                    vm => vm.SourceViewModel.LoginSuccessfully,
                    vm => vm.SourceViewModel.ServerSession)
                .Subscribe(delegate(Tuple<bool, ISession> tuple)
                {
                    var (success, session) = tuple;

                    if (!success || session == null) return;

                    this.LoginSuccessfully = true;
                    this.SelectedOperationMode = SupportedOperationMode.Open;
                    this.EngineeringModelSetupList = this.GetEngineeringModelSetupList(true);
                });

            this.WhenAnyValue(vm => vm.SelectedOperationMode)
                .Subscribe((mode) =>
                {
                    this.SourceModelIsEnabled = mode != SupportedOperationMode.Open;
                    this.ModeCreate = mode == SupportedOperationMode.Create;

                    if (mode == SupportedOperationMode.CreateOverwrite)
                    {
                        this.SelectedEngineeringModelSetup = null;
                        this.SourceEngineeringModelSetupList = this.GetEngineeringModelSetupList(false);
                    }
                });

            this.StressCommand = ReactiveCommand.CreateAsyncTask(
                canExecuteStress,
                _ => this.ExecuteStressCommand(),
                RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Set properties for this model
        /// </summary>
        protected override void SetProperties()
        {
            base.SetProperties();

            this.EngineeringModelSetupList = new ReactiveList<EngineeringModelSetup>
            {
                ChangeTrackingEnabled = true
            };
            this.SourceEngineeringModelSetupList = new ReactiveList<EngineeringModelSetup>
            {
                ChangeTrackingEnabled = true
            };

            this.TimeInterval = StressGeneratorConfiguration.MinTimeInterval;
            this.TestObjectsNumber = StressGeneratorConfiguration.MinNumberOfTestObjects;
            this.ElementName = StressGeneratorConfiguration.GenericElementName;
            this.ElementShortName = StressGeneratorConfiguration.GenericElementShortName;
            this.EngineeringModelSetupList = new ReactiveList<EngineeringModelSetup>();
            this.SourceEngineeringModelSetupList = new ReactiveList<EngineeringModelSetup>();
            this.ModeCreate = false;
            this.SourceModelIsEnabled = false;
            this.NewModelName = StressGeneratorConfiguration.ModelPrefix;
        }

        /// <summary>
        /// Executes the stress command
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        private async Task ExecuteStressCommand()
        {
            this.stressGenerator.Init(new StressGeneratorConfiguration(this.SourceViewModel.ServerSession)
            {
                TimeInterval = this.TimeInterval * 1000,
                OperationMode = this.SelectedOperationMode,
                TestModelSetupName = this.NewModelName,
                TestModelSetup = this.SelectedEngineeringModelSetup,
                SourceModelSetup = this.SelectedSourceEngineeringModelSetup,
                ElementName = this.ElementName.Trim(),
                ElementShortName = this.ElementShortName.Trim().Replace(" ", "_"),
                DeleteAllElements = this.DeleteAllElements,
                DeleteModel = this.DeleteModel
            });

            await this.stressGenerator.Generate();

            await this.stressGenerator.CleanUp();
        }

        /// <summary>
        /// Bind engineering models to the reactive list.
        /// </summary>
        /// <param name="filterPrefix">
        /// Boolean flag that specifies whether the model list should be
        /// filtered by <see cref="StressGeneratorConfiguration.ModelPrefix"/>.
        /// </param>
        /// <returns>
        /// Reactive list <see cref="ReactiveList{T}"/> of <see cref="EngineeringModelSetup"/>.
        /// </returns>
        private ReactiveList<EngineeringModelSetup> GetEngineeringModelSetupList(bool filterPrefix)
        {
            var siteDirectory = this.SourceViewModel.ServerSession.RetrieveSiteDirectory();

            IEnumerable<EngineeringModelSetup> modelSetups = siteDirectory.Model;

            if (filterPrefix)
            {
                modelSetups = modelSetups.Where(m => m.Name.StartsWith(StressGeneratorConfiguration.ModelPrefix));
            }

            return new ReactiveList<EngineeringModelSetup>(modelSetups.OrderBy(m => m.Name));
        }
    }
}
