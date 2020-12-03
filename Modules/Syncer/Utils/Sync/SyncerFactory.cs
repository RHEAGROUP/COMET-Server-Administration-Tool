// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SyncerFactory.cs" company="RHEA System S.A.">
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
    using System;

    using CDP4Dal;

    /// <summary>
    /// A factory class used to build the helper <see cref="Syncer"/> classes
    /// </summary>
    public class SyncerFactory
    {
        /// <summary>
        /// The singleton class instance
        /// </summary>
        private static readonly SyncerFactory Instance = new SyncerFactory();

        /// <summary>
        /// Gets the singleton class instance
        /// </summary>
        /// <returns>
        /// The singleton class instance
        /// </returns>
        public static SyncerFactory GetInstance() => Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncerFactory"/> class
        /// </summary>
        private SyncerFactory() { }

        /// <summary>
        /// Creates a new <see cref="Syncer"/> helper class
        /// </summary>
        /// <param name="type">
        /// The <see cref="ThingType"/> describing the ClassKind to be synced
        /// </param>
        /// <param name="sourceSession">
        /// The <see cref="ISession"/> for the source server session
        /// </param>
        /// <param name="targetSession">
        /// The <see cref="ISession"/> for the target server session
        /// </param>
        /// <returns>
        /// The newly created helper <see cref="Syncer"/> class
        /// </returns>
        public Syncer CreateSyncer(ThingType type, ISession sourceSession, ISession targetSession)
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
}
