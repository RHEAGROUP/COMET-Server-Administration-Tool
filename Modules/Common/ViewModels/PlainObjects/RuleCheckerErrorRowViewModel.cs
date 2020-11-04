// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RuleCheckerErrorRowViewModel.cs" company="RHEA System S.A.">
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
    using CDP4Rules.Common;
    using ReactiveUI;
    using System;

    /// <summary>
    /// Row class representing a <see cref="RuleCheckerErrorRowViewModel"/> as a plain object
    /// </summary>
    public class RuleCheckerErrorRowViewModel : ReactiveObject
    {
        /// <summary>
        /// Gets or sets the <see cref="ClassKind"/> of the <see cref="Thing"/> that contains the error.
        /// </summary>
        public string ContainerThingClassKind { get; private set; }

        /// <summary>
        /// Gets or sets the identifier or code of the Rule that may have been broken
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the description of the Rule that may have been broken
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="SeverityKind"/>
        /// </summary>
        public SeverityKind Severity { get; private set; }

        /// <summary>
        /// Gets or sets top container name
        /// </summary>
        public string TopContainerName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuleCheckerErrorRowViewModel"/> class
        /// </summary>
        /// <param name="thing">
        /// a reference to te <see cref="Thing"/> that has been checked by a Rule
        /// </param>
        /// <param name="id">
        /// the identifier or code of the Rule that may have been broken
        /// </param>
        /// <param name="description">
        /// the description of the Rule that may have been broken
        /// </param>
        /// <param name="severity">
        /// the <see cref="SeverityKind"/>
        /// </param>
        public RuleCheckerErrorRowViewModel(Thing thing, string id, string description, SeverityKind severity)
        {
            this.ContainerThingClassKind = thing.ClassKind.ToString();
            this.Id = id;
            this.Description = description;
            this.Severity = severity;
            this.TopContainerName = thing.TopContainer is SiteDirectory
                ? "SiteDirectory"
                : thing.TopContainer.UserFriendlyShortName;
        }

        /// <summary>
        /// Override ToString() method
        /// </summary>
        /// <returns>string object representation</returns>
        public override string ToString()
        {
            return
                $"{this.ContainerThingClassKind}({this.Id}) " +
                $"Top container: {this.TopContainerName}{Environment.NewLine}{this.Description}{Environment.NewLine}" +
                $"Severity: {this.Severity}";
        }
    }
}
