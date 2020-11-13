// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParameterGenerator.cs" company="RHEA System S.A.">
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
    using CDP4Dal;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    /// <summary>
    /// Helper class for creating parameters for stress test purposes
    /// </summary>
    internal static class ParameterGenerator
    {
        /// <summary>
        /// Create a new instance of <see cref="ParameterGenerator" />
        /// </summary>
        /// <param name="session">Server session <see cref="ISession"/></param>
        /// <param name="paramType">Parameter type <see cref="ParameterType"/></param>
        /// <param name="paramValue">Parameter value</param>
        /// <param name="paramOwner">Parameter owner <see cref="DomainOfExpertise"/></param>
        /// <param name="paramValueSwitch">Parameter value switch <see cref="ParameterSwitchKind"/></param>
        /// <returns>A parameter instance <see cref="Parameter"/></returns>
        public static Parameter Create(ISession session, ParameterType paramType,
            string paramValue, DomainOfExpertise paramOwner, ParameterSwitchKind paramValueSwitch)
        {
            var parameter = new Parameter(Guid.NewGuid(), session.Assembler.Cache,
                new Uri(session.DataSourceUri))
            {
                ParameterType = paramType as QuantityKind,
                Scale = (paramType as QuantityKind)?.DefaultScale,
                Owner = paramOwner
            };

            var parameterValueSet = new ParameterValueSet
            {
                ValueSwitch = paramValueSwitch,
                Manual = new ValueArray<string>(new List<string> {paramValue}),
                Computed = new ValueArray<string>(new List<string> {paramValue}),
                Reference = new ValueArray<string>(new List<string> {paramValue}),
                Formula = new ValueArray<string>(new List<string> {"=" + paramValue}),
                Published = new ValueArray<string>(new List<string> {paramValue})
            };

            parameter.ValueSet.Add(parameterValueSet);

            return parameter;
        }
    }
}
