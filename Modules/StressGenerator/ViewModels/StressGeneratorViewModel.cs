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
    /// The view-model for the StressGenerator tool
    /// </summary>
    public class StressGeneratorViewModel : ReactiveObject
    {
        /// <summary>
        /// The <see cref="StressGeneratorManager"/> used to build the helper sync classes
        /// </summary>
        private readonly StressGeneratorManager stressGenerator = StressGeneratorManager.GetInstance();

        /// <summary>
        /// Backing field for the source view model <see cref="LoginViewModel"/>
        /// </summary>
        private LoginViewModel sourceViewModel;

        /// <summary>
        /// Gets or sets the source view model
        /// </summary>
        public LoginViewModel SourceViewModel
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
        /// Backing field for the the <see cref="EngineeringModelSetupList"/> property
        /// </summary>
        private ReactiveList<EngineeringModelSetup> engineeringModelSetupList;

        /// <summary>
        /// Gets or sets domains of expertise
        /// </summary>
        public ReactiveList<EngineeringModelSetup> EngineeringModelSetupList
        {
            get => this.engineeringModelSetupList;
            private set => this.RaiseAndSetIfChanged(ref this.engineeringModelSetupList, value);
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
        private readonly ObservableAsPropertyHelper<bool> canStress;

        /// <summary>
        /// Gets a value indicating whether a stress operation can start
        /// </summary>
        public bool CanStress => this.canStress.Value;

        /// <summary>
        /// Gets the server sync command
        /// </summary>
        public ReactiveCommand<Unit> StressCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StressGeneratorViewModel"/> class
        /// </summary>
        public StressGeneratorViewModel()
        {
            this.TimeInterval = StressGeneratorConfiguration.MinTimeInterval;
            this.TestObjectsNumber = StressGeneratorConfiguration.MinNumberOfTestObjects;
            this.ElementName = "Element";
            this.ElementShortName = "ED";
            this.EngineeringModelSetupList = new ReactiveList<EngineeringModelSetup>();

            var canExecuteStress = this.WhenAnyValue(
                vm => vm.SourceViewModel.LoginSuccessfully,
                vm => vm.TimeInterval,
                vm => vm.TestObjectsNumber,
                vm => vm.ElementName,
                vm => vm.ElementShortName,
                vm => vm.SelectedEngineeringModelSetup,
                (sourceLoggedIn, timeInterval, testObjectsNumber, name, shortName, modelSetup) =>
                    sourceLoggedIn &&
                    timeInterval >= StressGeneratorConfiguration.MinTimeInterval &&
                    testObjectsNumber >= StressGeneratorConfiguration.MinNumberOfTestObjects &&
                    testObjectsNumber <= StressGeneratorConfiguration.MaxNumberOfTestObjects &&
                    !string.IsNullOrEmpty(name) &&
                    !string.IsNullOrEmpty(shortName) &&
                    modelSetup != null);

            canExecuteStress.ToProperty(this, vm => vm.CanStress, out this.canStress);

            this.WhenAnyValue(vm => vm.SourceViewModel.LoginSuccessfully, vm => vm.SourceViewModel.ServerSession)
                .Subscribe(
                    delegate(Tuple<bool, ISession> tuple)
                    {
                        var (success, session) = tuple;

                        if (success && session != null)
                        {
                            this.BindEngineeringModels(session.RetrieveSiteDirectory());
                        }
                    });

            this.StressCommand = ReactiveCommand.CreateAsyncTask(
                canExecuteStress,
                _ => this.ExecuteStressCommand(),
                RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Executes the stress command
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        private async Task ExecuteStressCommand()
        {
            this.stressGenerator.Init(new StressGeneratorConfiguration(
                this.SourceViewModel.ServerSession,
                this.TimeInterval,
                this.TestObjectsNumber,
                this.ElementName,
                this.ElementShortName,
                this.DeleteAllElements));
            await this.stressGenerator.GenerateTestObjects(this.SelectedEngineeringModelSetup);
        }

        /// <summary>
        /// Bind engineering models to the reactive list
        /// </summary>
        /// <param name="siteDirectory">The <see cref="SiteDirectory"/> top container</param>
        private void BindEngineeringModels(SiteDirectory siteDirectory)
        {
            this.EngineeringModelSetupList.Clear();

            foreach (var modelSetup in siteDirectory.Model.Where(m => m.Name.StartsWith("Stresser")).OrderBy(m => m.Name))
            {
                this.EngineeringModelSetupList.Add(modelSetup);
            }
        }
    }
}
