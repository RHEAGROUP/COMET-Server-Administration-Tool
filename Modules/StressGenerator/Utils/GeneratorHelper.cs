
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratorHelper.cs" company="RHEA System S.A.">
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

namespace StressGenerator.Utils
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using CDP4Dal;
    using CDP4Dal.Operations;
    using Common.Events;
    using Polly;
    using ViewModels;

    internal static class GeneratorHelper
    {
        /// <summary>
        /// The maximum retry count for a write operation.
        /// </summary>
        private const int MaxRetryCount = 3;

        /// <summary>
        /// Write the given <paramref name="operationContainer"/> to the server, retrying on failure.
        /// </summary>
        /// <param name="session">
        /// The <see cref="ISession"/>.
        /// </param>
        /// <param name="operationContainer">
        /// The given <see cref="OperationContainer"/>.
        /// </param>
        /// <param name="actionDescription">
        /// The description of the action.
        /// </param>
        public static async Task WriteWithRetries(
            ISession session,
            OperationContainer operationContainer,
            string actionDescription)
        {
            try
            {
                await Policy
                    .Handle<Exception>()
                    .RetryAsync(MaxRetryCount, (ex, retryCount) =>
                    {
                        LogOperationResult(false, actionDescription, ex, retryCount);
                    })
                    .ExecuteAsync(async () => await session.Dal.Write(operationContainer));

                LogOperationResult(true, actionDescription);
            }
            catch (Exception ex)
            {
                LogOperationResult(false, actionDescription, ex);
            }
        }

        /// <summary>
        /// Log the result of on operation.
        /// </summary>
        /// <param name="success">
        /// The operation success.
        /// </param>
        /// <param name="actionDescription">
        /// The description of the action.
        /// </param>
        /// <param name="exception">
        /// Optionally, the <see cref="Exception"/> which caused the operation to fail.
        /// </param>
        /// <param name="retryCount">
        /// Optionally, the retry count of the failed operation.
        /// </param>
        private static void LogOperationResult(
            bool success,
            string actionDescription,
            Exception exception = null,
            int? retryCount = null)
        {
            var sb = new StringBuilder();

            sb.Append(success ? "Succeeded" : "Failed");
            sb.Append(" ");

            if (retryCount != null)
            {
                sb.Append($"(retry count {retryCount})");
                sb.Append(" ");
            }

            sb.Append(actionDescription);
            sb.Append(" ");

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = sb.ToString(),
                Exception = exception,
                Verbosity = success ? LogVerbosity.Info : LogVerbosity.Error,
                Type = typeof(StressGeneratorViewModel)
            });
        }
    }
}
