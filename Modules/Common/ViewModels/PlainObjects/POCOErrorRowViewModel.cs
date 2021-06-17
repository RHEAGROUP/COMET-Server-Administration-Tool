// --------------------------------------------------------------------------------------------------------------------
// <copyright file="POCOErrorRowViewModel.cs" company="RHEA System S.A.">
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

namespace Common.ViewModels.PlainObjects
{
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.CommonData;
    using CDP4Common.Exceptions;
    using ReactiveUI;
    using System;

    /// <summary>
    /// Row class representing a <see cref="PocoErrorRowViewModel"/> as a plain object
    /// </summary>
    public class PocoErrorRowViewModel : ReactiveObject
    {
        /// <summary>
        /// Gets or sets the identifier or code of the Rule that may have been broken
        /// </summary>
        private string Id { get; }

        /// <summary>
        /// Gets the <see cref="ClassKind"/> of the <see cref="Thing"/> that contains the error.
        /// </summary>
        public string ContainerThingClassKind { get; }

        /// <summary>
        /// Gets or sets the human readable content of the Error.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Gets or sets the human readable content of the Error.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets or sets top container name
        /// </summary>
        public string TopContainerName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Thing"/>
        /// </summary>
        public Thing Thing { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PocoErrorRowViewModel"/> class
        /// <param name="thing">
        /// The thing <see cref="Thing" />.
        /// </param>
        /// <param name="error">
        /// The error content.
        /// </param>
        /// </summary>
        public PocoErrorRowViewModel(Thing thing, string error)
        {
            this.ContainerThingClassKind = thing.ClassKind.ToString();
            this.Error = error;
            this.Thing = thing;
            this.Id = thing.Iid.ToString();
            this.TopContainerName = thing.TopContainer is SiteDirectory
                ? "SiteDirectory"
                : thing.TopContainer.UserFriendlyShortName;

            this.Path = GetPath(thing);
        }

        /// <summary>
        /// Gets the path for the given <paramref name="thing"/>.
        /// </summary>
        /// <param name="thing">
        /// The given <see cref="Thing"/>.
        /// </param>
        /// <returns>
        /// The path.
        /// </returns>
        public static string GetPath(Thing thing)
        {
            try
            {
                var dto = thing.ToDto();
                var dtoRoute = dto.Route;
                var uriBuilder = new UriBuilder(thing.IDalUri) { Path = dtoRoute };
                return uriBuilder.ToString();
            }
            catch (ContainmentException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Override ToString() method
        /// </summary>
        /// <returns>string object representation</returns>
        public override string ToString()
        {
            return
                $"Thing: {this.ContainerThingClassKind} ({this.Id}){Environment.NewLine}" +
                $"Top container: {this.TopContainerName}{Environment.NewLine}" +
                $"Error: {this.Error}{Environment.NewLine}" +
                $"Path: {this.Path}";
        }
    }
}
