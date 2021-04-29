﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogEvent.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2021 RHEA System S.A.
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

namespace Common.Events
{
    using System;

    /// <summary>
    /// Log verbosity
    /// </summary>
    public enum LogVerbosity
    {
        Info,
        Warn,
        Debug,
        Error
    };
    /// <summary>
    /// A message bus event to signify that message should be logged
    /// </summary>
    public class LogEvent
    {
        /// <summary>
        /// Message that will be logged
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// NLog verbosity <see cref="LogVerbosity"/>
        /// </summary>
        public LogVerbosity Verbosity { get; set; }

        /// <summary>
        /// Exception that will be logged <see cref="Exception"/>
        /// </summary>
        public Exception Exception { get; set; }
    }
}
