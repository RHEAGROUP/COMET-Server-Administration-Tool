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
    public enum SupportedOperationModes
    {
        Open,
        Create,
        CreateOverwrite
    }

    /// <summary>
    /// The view-model for the StressGenerator tool
    /// </summary>
    public class StressGeneratorViewModel : ReactiveObject
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
        public static Dictionary<SupportedOperationModes, string> StressGeneratorModes { get; } =
            new Dictionary<SupportedOperationModes, string>
            {
                {SupportedOperationModes.Open, SupportedOperationModes.Open.ToString()},
                {SupportedOperationModes.Create, SupportedOperationModes.Create.ToString()},
                {SupportedOperationModes.CreateOverwrite, SupportedOperationModes.CreateOverwrite.ToString()}
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
        /// Backing field for the the <see cref="EngineeringModelSetupList"/> property
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
        /// Backing field for the the <see cref="SourceEngineeringModelSetupList"/> property
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
        /// Backing field for the the <see cref="TimeInterval"/> property
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
        /// Backing field for the the <see cref="TestObjectsNumber"/> property
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
        /// Backing field for the the <see cref="Output"/> messages property
        /// </summary>
        private string output;

        /// <summary>
        /// Gets or sets operation output messages
        /// </summary>
        public string Output
        {
            get => this.output;
            set => this.RaiseAndSetIfChanged(ref this.output, value);
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
        /// Backing field for the <see cref="SupportedOperationModes"/> property
        /// </summary>
        private SupportedOperationModes selectedOperationMode;

        /// <summary>
        /// Gets or sets server operation mode value
        /// </summary>
        public SupportedOperationModes SelectedOperationMode
        {
            get => this.selectedOperationMode;
            set => this.RaiseAndSetIfChanged(ref this.selectedOperationMode, value);
        }

        /// <summary>
        /// Backing field for the <see cref="LoginSuccessfully"/> property
        /// </summary>
        private bool loginSuccessfully;

        /// <summary>
        /// Gets or sets login successfully flag
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

        public bool ModeCreate
        {
            get => this.modeCreate;
            private set => this.RaiseAndSetIfChanged(ref this.modeCreate, value);
        }

        /// <summary>
        /// Backing field for the <see cref="ModeOpenOrOverwrite"/> property
        /// </summary>
        private bool modeOpenOrOverwrite;

        public bool ModeOpenOrOverwrite
        {
            get => this.modeOpenOrOverwrite;
            private set => this.RaiseAndSetIfChanged(ref this.modeOpenOrOverwrite, value);
        }

        /// <summary>
        /// Backing field for the <see cref="SourceModelIsEnabled"/> property
        /// </summary>
        private bool sourceModelIsEnabled;

        public bool SourceModelIsEnabled
        {
            get => this.sourceModelIsEnabled;
            private set => this.RaiseAndSetIfChanged(ref this.sourceModelIsEnabled, value);
        }

        /// <summary>
        /// Backing field for the <see cref="NewModelName"/> property
        /// </summary>
        private string newModelName;

        public string NewModelName
        {
            get => this.newModelName;
            set => this.RaiseAndSetIfChanged(ref this.newModelName, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StressGeneratorViewModel"/> class
        /// </summary>
        public StressGeneratorViewModel()
        {
            this.SetProperties();
            this.AddSubscriptions();
        }

        private void AddSubscriptions()
        {
            this.WhenAnyValue(vm => vm.SourceViewModel.Output).Subscribe(StressGeneratorMessageHandler);

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
                    this.SelectedOperationMode = SupportedOperationModes.Open;
                    this.BindEngineeringModels();
                });

            this.WhenAnyValue(
                    vm => vm.SelectedOperationMode)
                .Subscribe((mode) =>
                {
                    switch (mode)
                    {
                        case SupportedOperationModes.Open:
                            this.ModeOpenOrOverwrite = true;
                            this.ModeCreate = false;
                            this.SourceModelIsEnabled = false;
                            break;
                        case SupportedOperationModes.Create:
                        case SupportedOperationModes.CreateOverwrite:
                            this.ModeOpenOrOverwrite = true;
                            this.ModeCreate = mode == SupportedOperationModes.Create;
                            this.SourceModelIsEnabled = true;
                            this.SelectedEngineeringModelSetup = null;
                            this.BindSourceEngineeringModels();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                    }
                });

            this.StressCommand = ReactiveCommand.CreateAsyncTask(
                canExecuteStress,
                _ => this.ExecuteStressCommand(),
                RxApp.MainThreadScheduler);

            this.stressGenerator.NotifyMessageEvent += StressGeneratorMessageHandler;
        }

        private void SetProperties()
        {
            this.TimeInterval = StressGeneratorConfiguration.MinTimeInterval;
            this.TestObjectsNumber = StressGeneratorConfiguration.MinNumberOfTestObjects;
            this.ElementName = StressGeneratorConfiguration.GenericElementName;
            this.ElementShortName = StressGeneratorConfiguration.GenericElementShortName;
            this.EngineeringModelSetupList = new ReactiveList<EngineeringModelSetup>();
            this.SourceEngineeringModelSetupList = new ReactiveList<EngineeringModelSetup>();
            this.ModeCreate = false;
            this.ModeOpenOrOverwrite = false;
            this.SourceModelIsEnabled = false;
            this.NewModelName = "StressTester_TemporaryTestModel";
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

            await this.stressGenerator.GenerateTestObjects();

            await this.stressGenerator.CleanUpTestObjects();
        }

        /// <summary>
        /// Add text message to the output panel
        /// </summary>
        /// <param name="message">The text message</param>
        private void StressGeneratorMessageHandler(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            this.Output += $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        }

        /// <summary>
        /// Bind engineering models to the reactive list
        /// </summary>
        private void BindEngineeringModels()
        {
            var siteDirectory = this.SourceViewModel.ServerSession.RetrieveSiteDirectory();

            this.EngineeringModelSetupList.Clear();

            foreach (var modelSetup in siteDirectory.Model
                .Where(m => m.Name.StartsWith(StressGeneratorConfiguration.ModelPrefix)).OrderBy(m => m.Name))
            {
                this.EngineeringModelSetupList.Add(modelSetup);
            }
        }

        /// <summary>
        /// Bind engineering models to the reactive list
        /// </summary>
        private void BindSourceEngineeringModels()
        {
            // Retrieve SiteDirectory
            var siteDirectory = this.SourceViewModel.ServerSession.RetrieveSiteDirectory();

            this.SourceEngineeringModelSetupList.Clear();

            foreach (var modelSetup in siteDirectory.Model.OrderBy(m => m.Name))
            {
                this.SourceEngineeringModelSetupList.Add(modelSetup);
            }
        }
    }
}
