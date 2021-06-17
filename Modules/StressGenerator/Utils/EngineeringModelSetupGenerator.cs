// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineeringModelSetupGenerator.cs" company="RHEA System S.A.">
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

using System;
using System.Linq;

namespace StressGenerator.Utils
{
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;

    /// <summary>
    /// Helper class for creating iteration <see cref="Iteration"/> for test purposes
    /// </summary>
    internal static class EngineeringModelSetupGenerator
    {
        /// <summary>
        /// Create a new instance of <see cref="Iteration" />
        /// </summary>
        /// <param name="session">Server session <see cref="ISession"/></param>
        /// <param name="siteDirectory"></param>
        /// <param name="modelName"></param>
        /// <param name="sourceEngineeringModelSetup"></param>
        /// <returns>An iteration instance <see cref="Iteration"/></returns>
        public static EngineeringModelSetup Create(ISession session, SiteDirectory siteDirectory, string modelName, EngineeringModelSetup sourceEngineeringModelSetup = null)
        {
            var participant = new Participant(Guid.NewGuid(), session.Assembler.Cache, session.Credentials.Uri) { Person = siteDirectory.Person.FirstOrDefault() };
            participant.Domain.Add(siteDirectory.Person.FirstOrDefault()?.DefaultDomain);

            var iterationSetup = new IterationSetup(Guid.NewGuid(), session.Assembler.Cache, session.Credentials.Uri)
            {
                Description = "Iteration_1"
            };

            var iteration = new Iteration(Guid.NewGuid(), session.Assembler.Cache, session.Credentials.Uri)
            {
                IterationSetup = iterationSetup
            };
            iterationSetup.IterationIid = iteration.Iid;

            var engineeringModel = new EngineeringModel(Guid.NewGuid(), session.Assembler.Cache, session.Credentials.Uri);
            engineeringModel.Iteration.Add(iteration);

            var modelReferenceDataLibrary =
                new ModelReferenceDataLibrary(Guid.NewGuid(), session.Assembler.Cache, session.Credentials.Uri)
                {
                    Name = $"ModelReferenceDataLibrary_{modelName}",
                    ShortName = $"ModelReferenceDataLibrary_{modelName}",
                    RequiredRdl = siteDirectory.SiteReferenceDataLibrary.FirstOrDefault()
                };

            var engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), session.Assembler.Cache, session.Credentials.Uri)
            {
                EngineeringModelIid = engineeringModel.Iid,
                Name = modelName,
                ShortName = modelName
            };
            engineeringModel.EngineeringModelSetup = engineeringModelSetup;
            engineeringModelSetup.RequiredRdl.Add(modelReferenceDataLibrary);
            engineeringModelSetup.IterationSetup.Add(iterationSetup);
            engineeringModelSetup.Participant.Add(participant);

            siteDirectory.Model.Add(engineeringModelSetup);

            return engineeringModelSetup;
        }
    }
}
