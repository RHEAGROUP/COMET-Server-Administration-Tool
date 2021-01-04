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
    using System.Linq;
    using CDP4Dal;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    /// <summary>
    /// Helper class for creating parameters for stress test purposes
    /// </summary>
    public static class ParameterGenerator
    {
        /// <summary>
        /// Create a new instance of <see cref="ParameterGenerator" />
        /// </summary>
        /// <param name="parameterType">Parameter type <see cref="ParameterType"/></param>
        /// <param name="parameterOwner">Parameter owner <see cref="DomainOfExpertise"/></param>
        /// <returns>A parameter instance <see cref="Parameter"/></returns>
        public static Parameter Create(ParameterType parameterType, DomainOfExpertise parameterOwner)
        {
            var parameter = new Parameter
            {
                Iid = Guid.NewGuid(),
                ParameterType = parameterType as QuantityKind,
                Scale = (parameterType as QuantityKind)?.DefaultScale,
                Owner = parameterOwner
            };

            return parameter;
        }

        /// <summary>
        /// Clone existing value sets and update its values
        /// </summary>
        /// <param name="parameterValueSet">New value set value <see cref="IEnumerable{IValueSet}"/></param>
        /// <param name="parameterValueSwitch">New value switch value <see cref="ParameterSwitchKind"/></param>
        /// <param name="parameterValue">New value represented as string</param>
        /// <returns>The value set that will be cloned <see cref="ParameterValueSetBase" /></returns>
        public static ParameterValueSetBase UpdateValueSets(IEnumerable<IValueSet> parameterValueSet, ParameterSwitchKind parameterValueSwitch, string parameterValue)
        {
            var valueSetClone = ((ParameterValueSet)parameterValueSet.FirstOrDefault())?.Clone(false);

            if (valueSetClone == null) return null;

            valueSetClone.ValueSwitch = parameterValueSwitch;
            valueSetClone.Manual = new ValueArray<string>(new List<string> {parameterValue});
            valueSetClone.Computed = new ValueArray<string>(new List<string> {parameterValue});
            valueSetClone.Reference = new ValueArray<string>(new List<string> {parameterValue});
            valueSetClone.Formula = new ValueArray<string>(new List<string> {parameterValue});
            valueSetClone.Published = new ValueArray<string>(new List<string> {parameterValue});

            return valueSetClone;
        }
    }
}
