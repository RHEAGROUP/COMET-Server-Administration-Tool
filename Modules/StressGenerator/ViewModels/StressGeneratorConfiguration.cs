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
    using CDP4Dal;

    /// <summary>
    /// This class is used to define the start configuration for the StressGenerator tool
    /// </summary>
    internal class StressGeneratorConfiguration
    {
        /// <summary>
        /// Maximum number of test objects
        /// </summary>
        private const int MaximumNumberOfTestObjects = 500;

        /// <summary>
        /// Parameters values configuration
        /// </summary>
        public readonly List<KeyValuePair<string, double>> ParamValueConfig = new List<KeyValuePair<string, double>>
        {
            new KeyValuePair<string, double>("m", 12.5),
            new KeyValuePair<string, double>("mass_margin", 10),
            new KeyValuePair<string, double>("P_on", 33.5),
            new KeyValuePair<string, double>("P_stby", 5.7)
        };

        /// <summary>
        /// Gets or sets the time interval in seconds for test data generation
        /// </summary>
        public int TimeInterval { get; private set; }

        /// <summary>
        /// Gets or sets the number of the test objects to be generated
        /// </summary>
        public int TestObjectsNumber { get; private set; }

        /// <summary>
        /// Gets or sets the first part of generated element definition name.
        /// </summary>
        public string ElementName { get; private set; }

        /// <summary>
        /// Gets or sets the first part of generated element definition short name.
        /// </summary>
        public string ElementShortName { get; private set; }

        /// <summary>
        /// Gets or sets a flag that trigger deleting of all elements in the engineering model.
        /// </summary>
        public bool DeleteAllElements { get; private set; }

        /// <summary>
        /// Currently open server session
        /// </summary>
        public ISession Session { get; private set; }

        /// <summary>
        /// Initialize a new instance of <see cref="StressGeneratorConfiguration" />
        /// </summary>
        /// <param name="session"></param>
        /// <param name="timeInterval"></param>
        /// <param name="testObjectsNumber"></param>
        /// <param name="elementName"></param>
        /// <param name="elementShortName"></param>
        /// <param name="deleteAllElements"></param>
        public StressGeneratorConfiguration(ISession session, int timeInterval, int testObjectsNumber, string elementName, string elementShortName, bool deleteAllElements)
        {
            this.Session = session;
            this.TimeInterval = timeInterval * 1000;
            this.TestObjectsNumber = testObjectsNumber;
            if (this.TestObjectsNumber <= 0 || this.TestObjectsNumber > MaximumNumberOfTestObjects)
            {
                this.TestObjectsNumber = MaximumNumberOfTestObjects;
            }
            this.ElementName = elementName.Trim();
            this.ElementShortName = elementShortName.Trim().Replace(" ", "_");
            this.DeleteAllElements = deleteAllElements;
        }
    }
}
