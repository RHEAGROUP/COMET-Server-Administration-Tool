// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibrarySyncer.cs">
//    Copyright (c) 2020
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
