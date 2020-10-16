// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibrarySyncer.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Syncer.Utils.Sync
{
    using CDP4Dal;
    using System.Threading.Tasks;

    internal class SiteReferenceDataLibrarySyncer : Syncer
    {
        public SiteReferenceDataLibrarySyncer(ISession sourceSession, ISession targetSession)
            : base(sourceSession, targetSession) { }

        protected internal override Task Sync()
        {
            throw new System.NotImplementedException();
        }
    }
}
