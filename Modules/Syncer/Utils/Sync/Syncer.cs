// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Syncer.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Syncer.Utils.Sync
{
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using CDP4Dal.Operations;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class SyncerFactory
    {
        internal Syncer CreateSyncer(ThingType type, ISession sourceSession, ISession targetSession)
        {
            switch (type)
            {
                case ThingType.DomainOfExpertise:
                    return new DomainOfExpertiseSyncer(sourceSession, targetSession);
                case ThingType.SiteReferenceDataLibrary:
                    return new SiteReferenceDataLibrarySyncer(sourceSession, targetSession);
                default:
                    throw new ArgumentException("Invalid value", nameof(type));
            }
        }
    }

    internal abstract class Syncer
    {
        protected readonly Logger Logger;

        protected readonly ISession SourceSession;
        protected readonly ISession TargetSession;

        protected readonly SiteDirectory SourceSiteDirectory;
        protected readonly SiteDirectory TargetSiteDirectory;

        protected Syncer(ISession sourceSession, ISession targetSession)
        {
            this.Logger = LogManager.GetCurrentClassLogger();

            this.SourceSession = sourceSession;
            this.TargetSession = targetSession;

            this.SourceSiteDirectory = this.SourceSession.RetrieveSiteDirectory();
            this.TargetSiteDirectory = this.TargetSession.RetrieveSiteDirectory();
        }

        protected internal abstract Task Sync(IEnumerable<Thing> selectedThings);

        protected Operation GetOperation(Thing sourceThing, in Dictionary<Guid, Thing> allTargetThings)
        {
            var operationKind = allTargetThings.TryGetValue(sourceThing.Iid, out var targetThing)
                ? OperationKind.Update
                : OperationKind.Create;

            return new Operation(
                targetThing?.ToDto(),
                sourceThing.ToDto(),
                operationKind);
        }
    }
}
