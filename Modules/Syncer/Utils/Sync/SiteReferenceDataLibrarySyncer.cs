// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibrarySyncer.cs" company="RHEA System S.A.">
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
    using CDP4Dal;
    using CDP4Dal.Operations;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class SiteReferenceDataLibrarySyncer : Syncer
    {
        public SiteReferenceDataLibrarySyncer(ISession sourceSession, ISession targetSession)
            : base(sourceSession, targetSession) { }

        protected internal override async Task Sync(IEnumerable<Thing> selectedThings)
        {
            var selectedIids = new HashSet<Guid>(selectedThings.Select(thing => thing.Iid));

            var operationContainer = new OperationContainer(this.TargetSiteDirectory.Route);

            var cloneTargetSiteDirectory = this.TargetSiteDirectory.Clone(false);

            foreach (var sourceRdl in
                this.SourceSiteDirectory.SiteReferenceDataLibrary
                    .Where(t => selectedIids.Contains(t.Iid))
                    .ToList())
            {
                await this.SourceSession.Read(sourceRdl);

                var cloneSourceThing = sourceRdl.Clone(true);
                cloneSourceThing.Container = this.TargetSiteDirectory;

                var index = cloneTargetSiteDirectory.SiteReferenceDataLibrary
                    .FindIndex(t => t.Iid == cloneSourceThing.Iid);
                if (index == -1)
                {
                    cloneTargetSiteDirectory.SiteReferenceDataLibrary.Add(cloneSourceThing);
                }
                else
                {
                    cloneTargetSiteDirectory.SiteReferenceDataLibrary[index] = cloneSourceThing;
                }

                var targetRdl = this.TargetSiteDirectory
                    .SiteReferenceDataLibrary
                    .FirstOrDefault(t => t.ShortName == sourceRdl.ShortName);

                if (targetRdl != null)
                {
                    await this.TargetSession.Read(targetRdl);
                }

                var allTargetThings = targetRdl?.QueryContainedThingsDeep()
                    .ToDictionary(thing => thing.Iid, thing => thing) ?? new Dictionary<Guid, Thing>();

                foreach (var toBeMovedThing in cloneSourceThing.QueryContainedThingsDeep())
                {
                    operationContainer.AddOperation(this.GetOperation(toBeMovedThing, allTargetThings));
                }
            }

            operationContainer.AddOperation(new Operation(
                this.TargetSiteDirectory.ToDto(),
                cloneTargetSiteDirectory.ToDto(),
                OperationKind.Update));

            await this.TargetSession.Write(operationContainer);
        }
    }
}
