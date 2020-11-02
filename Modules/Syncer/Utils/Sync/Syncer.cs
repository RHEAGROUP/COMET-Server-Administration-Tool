// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Syncer.cs" company="RHEA System S.A.">
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
