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

    /// <summary>
    /// A helper class used for syncing <see cref="Thing"/>s of certain kinds
    /// </summary>
    public abstract class Syncer
    {
        /// <summary>
        /// The NLog logger
        /// </summary>
        protected readonly Logger Logger;

        /// <summary>
        /// The <see cref="ISession"/> for the source server session
        /// </summary>
        protected readonly ISession SourceSession;

        /// <summary>
        /// The <see cref="ISession"/> for the target server session
        /// </summary>
        protected readonly ISession TargetSession;

        /// <summary>
        /// The source server <see cref="SiteDirectory"/>
        /// </summary>
        protected readonly SiteDirectory SourceSiteDirectory;

        /// <summary>
        /// The target server <see cref="SiteDirectory"/>
        /// </summary>
        protected readonly SiteDirectory TargetSiteDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Syncer"/> class
        /// </summary>
        /// <param name="sourceSession">
        /// The <see cref="ISession"/> for the source server session
        /// </param>
        /// <param name="targetSession">
        /// The <see cref="ISession"/> for the target server session
        /// </param>
        protected Syncer(ISession sourceSession, ISession targetSession)
        {
            this.Logger = LogManager.GetCurrentClassLogger();

            this.SourceSession = sourceSession;
            this.TargetSession = targetSession;

            this.SourceSiteDirectory = this.SourceSession.RetrieveSiteDirectory();
            this.TargetSiteDirectory = this.TargetSession.RetrieveSiteDirectory();
        }

        /// <summary>
        /// Method syncing the given <paramref name="selectedIids"/> from the source server to the target server
        /// </summary>
        /// <param name="selectedIids">
        /// A list of thing iids to sync
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        public abstract Task Sync(IEnumerable<Guid> selectedIids);

        /// <summary>
        /// Creates a <see cref="OperationKind.Update"/> or <see cref="OperationKind.Create"/> <see cref="Operation"/>
        /// based on the existence of <paramref name="sourceThing"/> in the list of <paramref name="allTargetThings"/>
        /// </summary>
        /// <param name="sourceThing">
        /// The <see cref="Thing"/> on which the <see cref="Operation"/> is based
        /// </param>
        /// <param name="allTargetThings">
        /// The list of target things to consider when checking for an <see cref="OperationKind.Update"/> operation
        /// </param>
        /// <returns>
        /// The <see cref="Operation"/>
        /// </returns>
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
