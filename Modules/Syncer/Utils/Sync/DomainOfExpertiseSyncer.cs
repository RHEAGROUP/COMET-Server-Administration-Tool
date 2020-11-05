// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DomainOfExpertiseSyncer.cs" company="RHEA System S.A.">
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

namespace Syncer.Utils.Sync
{
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using CDP4Dal.Operations;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A helper class used for syncing things of type
    /// <see cref="DomainOfExpertise"/> and <see cref="DomainOfExpertiseGroup"/>
    /// </summary>
    internal class DomainOfExpertiseSyncer : Syncer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainOfExpertiseSyncer"/> class
        /// </summary>
        /// <param name="sourceSession">
        /// The <see cref="ISession"/> for the source server session
        /// </param>
        /// <param name="targetSession">
        /// The <see cref="ISession"/> for the target server session
        /// </param>
        public DomainOfExpertiseSyncer(ISession sourceSession, ISession targetSession)
            : base(sourceSession, targetSession) { }

        /// <summary>
        /// Method syncing the given <paramref name="selectedThings"/> from the source server to the target server
        /// </summary>
        /// <param name="selectedThings">
        /// A list of things to sync
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        protected internal override async Task Sync(IEnumerable<Thing> selectedThings)
        {
            var selectedIids = new HashSet<Guid>(selectedThings.Select(thing => thing.Iid));

            var operationContainer = new OperationContainer(this.TargetSiteDirectory.Route);

            var cloneTargetSiteDirectory = this.TargetSiteDirectory.Clone(false);

            var allTargetThings = this.TargetSiteDirectory.QueryContainedThingsDeep()
                .ToDictionary(thing => thing.Iid, thing => thing);

            // sync DomainOfExpertise
            foreach (var domain in this.SourceSiteDirectory.Domain
                .Where(t => selectedIids.Contains((t.Iid))))
            {
                var cloneSourceThing = domain.Clone(true);
                cloneSourceThing.Container = cloneTargetSiteDirectory;

                var index = cloneTargetSiteDirectory.Domain
                    .FindIndex(t => t.Iid == cloneSourceThing.Iid);
                if (index == -1)
                {
                    cloneTargetSiteDirectory.Domain.Add(cloneSourceThing);
                }
                else
                {
                    cloneTargetSiteDirectory.Domain[index] = cloneSourceThing;
                }

                foreach (var toBeMovedThing in cloneSourceThing.QueryContainedThingsDeep())
                {
                    operationContainer.AddOperation(this.GetOperation(toBeMovedThing, allTargetThings));
                }
            }

            // sync DomainOfExpertiseGroup
            foreach (var domainGroup in this.SourceSiteDirectory.DomainGroup
                .Where(t => selectedIids.Contains((t.Iid))))
            {
                var cloneSourceThing = domainGroup.Clone(true);
                cloneSourceThing.Container = cloneTargetSiteDirectory;

                var index = cloneTargetSiteDirectory.DomainGroup
                    .FindIndex(t => t.Iid == cloneSourceThing.Iid);
                if (index == -1)
                {
                    cloneTargetSiteDirectory.DomainGroup.Add(cloneSourceThing);
                }
                else
                {
                    cloneTargetSiteDirectory.DomainGroup[index] = cloneSourceThing;
                }

                foreach (var toBeMovedThing in cloneSourceThing.QueryContainedThingsDeep())
                {
                    operationContainer.AddOperation(this.GetOperation(toBeMovedThing, allTargetThings));
                }
            }

            // update target SiteDirectory
            operationContainer.AddOperation(new Operation(
                this.TargetSiteDirectory.ToDto(),
                cloneTargetSiteDirectory.ToDto(),
                OperationKind.Update));

            await this.TargetSession.Write(operationContainer);
        }
    }
}
