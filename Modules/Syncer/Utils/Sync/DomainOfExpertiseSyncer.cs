// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DomainOfExpertiseSyncer.cs">
//    Copyright (c) 2020
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

    internal class DomainOfExpertiseSyncer : Syncer
    {
        public DomainOfExpertiseSyncer(ISession sourceSession, ISession targetSession)
            : base(sourceSession, targetSession) { }

        protected internal override async Task Sync(IEnumerable<Thing> selectedThings)
        {
            var selectedIids = new HashSet<Guid>(selectedThings.Select(thing => thing.Iid));

            var operationContainer = new OperationContainer(this.TargetSiteDirectory.Route);

            var cloneTargetSiteDirectory = this.TargetSiteDirectory.Clone(false);

            var allTargetThings = this.TargetSiteDirectory.QueryContainedThingsDeep()
                .ToDictionary(thing => thing.Iid, thing => thing);

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

            operationContainer.AddOperation(new Operation(
                this.TargetSiteDirectory.ToDto(),
                cloneTargetSiteDirectory.ToDto(),
                OperationKind.Update));

            await this.TargetSession.Write(operationContainer);
        }
    }
}
