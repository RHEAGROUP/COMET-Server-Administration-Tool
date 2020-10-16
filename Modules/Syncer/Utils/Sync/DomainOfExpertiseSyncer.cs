// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DomainOfExpertiseSyncer.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Syncer.Utils.Sync
{
    using CDP4Dal;
    using System.Threading.Tasks;

    internal class DomainOfExpertiseSyncer : Syncer
    {
        public DomainOfExpertiseSyncer(ISession sourceSession, ISession targetSession)
            : base(sourceSession, targetSession) { }

        protected internal override Task Sync()
        {
            throw new System.NotImplementedException();
        }
    }
}
