// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Syncer.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Syncer.Utils.Sync
{
    using CDP4Dal;
    using System;
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
        private readonly ISession sourceSession;
        private readonly ISession targetSession;

        protected Syncer(ISession sourceSession, ISession targetSession)
        {
            this.sourceSession = sourceSession;
            this.targetSession = targetSession;
        }

        protected internal abstract Task Sync();
    }
}
