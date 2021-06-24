// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StressGeneratorConfiguration.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;

    /// <summary>
    /// This class is used to define the start configuration for the StressGenerator tool
    /// </summary>
    internal class StressGeneratorConfiguration
    {
        /// <summary>
        /// Stress generator models prefix
        /// </summary>
        public const string ModelPrefix = "StressTester_TemporaryTestModel";

        /// <summary>
        /// Generic RDL short name
        /// </summary>
        public const string GenericRdlShortName = "Generic_RDL";

        /// <summary>
        /// Generic ElementDefinition name
        /// </summary>
        public const string GenericElementName = "Element";

        /// <summary>
        /// Generic ElementDefinition short name
        /// </summary>
        public const string GenericElementShortName = "ED";

        /// <summary>
        /// Minimum time interval in seconds for test data generation
        /// </summary>
        public const int MinTimeInterval = 0;

        /// <summary>
        /// Minimum number of test objects
        /// </summary>
        public const int MinNumberOfTestObjects = 5;

        /// <summary>
        /// Maximum number of test objects
        /// </summary>
        public const int MaxNumberOfTestObjects = 500;

        /// <summary>
        /// Parameters values configuration
        /// </summary>
        public static readonly List<KeyValuePair<string, double>> ParamValueConfig = new List<KeyValuePair<string, double>>
        {
            new KeyValuePair<string, double>("m", 12.5),
            new KeyValuePair<string, double>("mass_margin", 10),
            new KeyValuePair<string, double>("P_on", 33.5),
            new KeyValuePair<string, double>("P_stby", 5.7),
            new KeyValuePair<string, double>("V", 15.7)
        };

        /// <summary>
        /// Gets or sets the time interval in seconds for test data generation
        /// </summary>
        public int TimeInterval { get; set; }

        /// <summary>
        /// Back-tier field of the <see cref="TestObjectsNumber"/>
        /// </summary>
        private int testObjectsNumber;

        /// <summary>
        /// Gets or sets the number of the test objects to be generated
        /// </summary>
        public int TestObjectsNumber
        {
            get => this.testObjectsNumber;
            set
            {
                if (value < MinNumberOfTestObjects)
                {
                    this.testObjectsNumber = MinNumberOfTestObjects;
                }

                if (value > MaxNumberOfTestObjects)
                {
                    this.testObjectsNumber = MaxNumberOfTestObjects;
                }
            }
        }

        /// <summary>
        /// Gets or sets the first part of generated element definition name.
        /// </summary>
        public string ElementName { get; set; }

        /// <summary>
        /// Gets or sets the first part of generated element definition short name.
        /// </summary>
        public string ElementShortName { get; set; }

        /// <summary>
        /// Gets or sets a flag that trigger deleting of all elements in the engineering model.
        /// </summary>
        public bool DeleteAllElements { get; set; }

        /// <summary>
        /// Gets or sets a flag that trigger deleting of the engineering model.
        /// </summary>
        public bool DeleteModel { get; set; }

        /// <summary>
        /// Currently open server session
        /// </summary>
        public ISession Session { get; private set; }

        /// <summary>
        /// EngineeringModelSetup used for stress test <see cref="EngineeringModelSetup"/>
        /// </summary>
        public EngineeringModelSetup TestModelSetup { get; set; }

        /// <summary>
        /// Source EngineeringModelSetup for the TestModelSetup
        /// </summary>
        public EngineeringModelSetup SourceModelSetup { get; set; }

        /// <summary>
        /// EngineeringModelSetup name used for stress test
        /// </summary>
        public string TestModelSetupName { get; set; }

        /// <summary>
        /// Supported mode <see cref="SupportedOperationModes"/>
        /// </summary>
        public SupportedOperationModes OperationMode { get; set; }

        /// <summary>
        /// Initialize a new instance of <see cref="StressGeneratorConfiguration" />
        /// </summary>
        /// <param name="session">Server session <see cref="ISession"/></param>
        public StressGeneratorConfiguration(ISession session)
        {
            this.Session = session;
            this.TestObjectsNumber = 5;

            this.ElementName = GenericElementName;
            this.ElementShortName = GenericElementShortName;
        }
    }
}
