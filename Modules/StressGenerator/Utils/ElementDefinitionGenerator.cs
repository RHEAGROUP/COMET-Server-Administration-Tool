// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ElementDefinitionGenerator.cs" company="RHEA System S.A.">
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

namespace StressGenerator.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using ViewModels;

    /// <summary>
    /// Helper class for creating element definition for test purposes
    /// </summary>
    internal static class ElementDefinitionGenerator
    {
        /// <summary>
        /// Helper class for creating element definition for test purposes
        /// </summary>
        /// <param name="elementName">Element definition name</param>
        /// <param name="elementShortName">Element definition short name</param>
        /// <param name="iteration">The iteration container <see cref="Iteration"/></param>
        /// <param name="elementOwner">Element definition owner <see cref="DomainOfExpertise"/></param>
        /// <returns>An element definition instance <see cref="ElementDefinition"/></returns>
        public static ElementDefinition Create(string elementName, string elementShortName, Iteration iteration, DomainOfExpertise elementOwner)
        {
            var elementDefinition = new ElementDefinition
            {
                Iid = Guid.NewGuid(),
                Name = elementName,
                ShortName = elementShortName,
                Container = iteration,
                Owner = elementOwner
            };

            var parameters = CreateParameters(elementOwner);

            if (parameters != null)
            {
                elementDefinition.Parameter.AddRange(parameters);
            }

            return elementDefinition;
        }

        /// <summary>
        /// Create parameter list
        /// </summary>
        /// <param name="elementOwner">Element definition owner <see cref="DomainOfExpertise"/></param>
        /// <returns>Parameters list that has been created <see cref="List{Parameter}"/></returns>
        private static List<Parameter> CreateParameters(DomainOfExpertise elementOwner)
        {
            var configList = new List<Tuple<ParameterType, double>>();
            var parametersList = new List<Parameter>();
            var siteReferenceDataLibraries = (elementOwner.Container as SiteDirectory)?.SiteReferenceDataLibrary;
            var parameterTypes = siteReferenceDataLibraries?.FirstOrDefault()?.ParameterType.ToList();

            if (parameterTypes == null)
            {
                return parametersList;
            }

            foreach (var keyValue in StressGeneratorConfiguration.ParamValueConfig)
            {
                if (parameterTypes.All(x => x.ShortName != keyValue.Key))
                {
                    continue;
                }

                configList.Add(new Tuple<ParameterType, double>(parameterTypes.Single(x => x.ShortName == keyValue.Key),
                    keyValue.Value));
            }

            foreach (var config in configList)
            {
                var (paramType, _) = config;

                var parameter = ParameterGenerator.Create(paramType, elementOwner);
                parametersList.Add(parameter);
            }

            return parametersList;
        }
    }
}
