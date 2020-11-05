// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Migration.cs" company="RHEA System S.A.">
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

namespace Migration.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using CDP4Common.EngineeringModelData;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4Dal.Operations;
    using CDP4JsonFileDal;
    using Common.ViewModels.PlainObjects;
    using NLog;

    /// <summary>
    /// Enumeration of the migration process steps
    /// </summary>
    public enum MigrationStep
    {
        ImportStart,
        PackStart,
        PackEnd,
        ImportEnd,
        ExportStart,
        ExportEnd
    };

    /// <summary>
    /// The purpose of this class is to implement migration specif operations such as: import, export, pack
    /// </summary>
    public sealed class Migration
    {
        /// <summary>
        /// The NLog logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Annex C3 Zip archive file name
        /// </summary>
        private static readonly string ArchiveFileName = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\Annex-C3.zip";

        /// <summary>
        /// Annex C3 Migration file name
        /// </summary>
        private static readonly string MigrationFileName = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\migration.json";

        /// <summary>
        /// Data Access Layer used during migration process
        /// </summary>
        private readonly JsonFileDal dal;

        /// <summary>
        ///  Gets or sets session of the migration source server <see cref="ISession"/>
        /// </summary>
        public ISession SourceSession { get; set; }

        /// <summary>
        /// Gets or sets session of the migration target server <see cref="ISession"/>
        /// </summary>
        public ISession TargetSession { get; set; }

        /// <summary>
        /// Delegate used for notifying current operation migration progress message
        /// </summary>
        /// <param name="message">Progress message</param>
        public delegate void MessageDelegate(string message);

        /// <summary>
        /// Delegate used for notifying current operation migration progress step
        /// </summary>
        public delegate void MigrationStepDelegate(MigrationStep step);

        /// <summary>
        /// Associated event with the <see cref="MessageDelegate"/>
        /// </summary>
        public event MessageDelegate OperationMessageEvent;

        /// <summary>
        /// Associated event with the <see cref="MigrationStepDelegate"/>
        /// </summary>
        public event MigrationStepDelegate OperationStepEvent;

        /// <summary>
        /// Invoke OperationMessageEvent
        /// </summary>
        /// <param name="message">progress message</param>
        private void NotifyMessage(string message)
        {
            OperationMessageEvent?.Invoke(message);
        }

        /// <summary>
        /// Invoke OperationStepEvent
        /// </summary>
        /// <param name="step">
        /// progress operation's step <see cref="MigrationStep"/>
        /// </param>
        private void NotifyStep(MigrationStep step)
        {
            OperationStepEvent?.Invoke(step);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Migration"/> class
        /// </summary>
        public Migration()
        {
            this.dal = new JsonFileDal(new Version("1.0.0"));

            if (Directory.Exists("Import"))
            {
                Directory.Delete("Import", true);
            }

            Directory.CreateDirectory("Import");
        }

        /// <summary>
        /// Implement import operation
        /// </summary>
        /// <param name="selectedModels">
        /// Selected engineering models from the source server <see cref="EngineeringModelRowViewModel"/>
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<bool> ImportData(List<EngineeringModelRowViewModel> selectedModels)
        {
            if (this.SourceSession is null)
            {
                this.NotifyMessage("Please select source session");
                return false;
            }

            if (selectedModels is null)
            {
                this.NotifyMessage("Please select model(s) to migrate");
                return false;
            }

            Logger.Info($"Retrieving SiteDirectory from {this.SourceSession.DataSourceUri}...");
            this.NotifyStep(MigrationStep.ImportStart);

            var siteDirectory = this.SourceSession.RetrieveSiteDirectory();

            if (siteDirectory == null)
            {
                await this.SourceSession.Open();
                siteDirectory = this.SourceSession.RetrieveSiteDirectory();
            }

            foreach (var modelSetup in siteDirectory.Model.OrderBy(m => m.Name))
            {
                if (!selectedModels.ToList().Any(em => em.Iid == modelSetup.Iid && em.IsSelected)) continue;

                var model = new EngineeringModel(
                    modelSetup.EngineeringModelIid,
                    this.SourceSession.Assembler.Cache,
                    this.SourceSession.Credentials.Uri)
                {
                    EngineeringModelSetup = modelSetup
                };

                var tasks = new List<Task>();

                // Read iterations
                foreach (var iterationSetup in modelSetup.IterationSetup)
                {
                    var iteration = new Iteration(
                        iterationSetup.IterationIid,
                        this.SourceSession.Assembler.Cache,
                        this.SourceSession.Credentials.Uri);

                    model.Iteration.Add(iteration);
                    tasks.Add(this.SourceSession.Read(iteration, this.SourceSession.ActivePerson.DefaultDomain)
                        .ContinueWith(t =>
                        {
                            var iterationDescription = $"'{modelSetup.Name}'.'{iterationSetup.IterationIid}'";

                            string message;

                            if (t.IsFaulted && t.Exception != null)
                            {
                                message =
                                    $"Reading iteration {iterationDescription} failed. Exception: {t.Exception.Message}.";
                                this.NotifyMessage(message);
                                Logger.Warn(message);
                                return;
                            }

                            message = $"Read iteration {iterationDescription} successfully.";
                            this.NotifyMessage(message);
                            Logger.Info(message);
                        }));
                }

                while (tasks.Count > 0)
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                }
            }

            Logger.Info("Finished pulling data");
            this.NotifyStep(MigrationStep.ImportEnd);

            return true;
        }

        /// <summary>
        /// Implement export operation
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<bool> ExportData()
        {
            var success = true;

            if (this.TargetSession is null)
            {
                this.NotifyMessage("Please select the target session");

                return false;
            }

            this.NotifyStep(MigrationStep.ExportStart);

            // TODO #34 Replace this in the near future, I cannot log into CDP WebService empty server
            var targetUrl = $"{this.TargetSession.DataSourceUri}Data/Exchange";

            Logger.Info($"Pushing data to {targetUrl}");

            try
            {
                using (var httpClient = this.CreateHttpClient(TargetSession.Credentials))
                {
                    using (var multipartContent = this.CreateMultipartContent())
                    {
                        await httpClient.PostAsync(targetUrl, multipartContent).ContinueWith( (t) =>
                        {
                            // TODO #35 add result interpretation
                            if (t.IsFaulted)
                            {
                                if (t.Exception?.InnerException != null)
                                {
                                    throw t.Exception?.InnerException;
                                }

                                throw new Exception("Unknown inner exception");
                            }

                            Logger.Info($"Server status response {t.Result.StatusCode}");
                            success = t.Result.IsSuccessStatusCode;

                            if (success)
                            {
                                Logger.Info("Finished pushing data");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not push data. Exception: {ex}");
                success = false;
                // TODO #36 add proper exception handling
            }
            finally
            {
                await this.TargetSession.Close();
            }

            this.NotifyStep(MigrationStep.ExportEnd);

            return success;
        }

        /// <summary>
        /// Implement pack(zip) data operation
        /// </summary>
        /// <param name="migrationFile">Migration file</param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<bool> PackData(string migrationFile)
        {
            List<string> extensionFiles = null;
            var zipCredentials = new Credentials("admin", "pass", new Uri(ArchiveFileName));
            var zipSession = new Session(this.dal, zipCredentials);
            var success = true;

            if (!string.IsNullOrEmpty(migrationFile))
            {
                if (!System.IO.File.Exists(migrationFile))
                {
                    this.NotifyMessage("Unable to find selected migration file");

                    return false;
                }

                try
                {
                    extensionFiles = new List<string> {MigrationFileName};
                    System.IO.File.Copy(migrationFile, MigrationFileName);
                }
                catch (Exception ex)
                {
                    // TODO #37 add proper exception handling
                    Logger.Error($"Could add migration.json file. Exception: {ex}");
                    this.NotifyMessage("Could add migration.json file");

                    return false;
                }
            }

            this.NotifyStep(MigrationStep.PackStart);

            var operationContainers = new List<OperationContainer>();
            var openIterations = this.SourceSession.OpenIterations.Select(i => i.Key);

            foreach (var iteration in openIterations)
            {
                var transactionContext = TransactionContextResolver.ResolveContext(iteration);
                var operationContainer = new OperationContainer(transactionContext.ContextRoute());
                var dto = iteration.ToDto();
                var operation = new Operation(null, dto, OperationKind.Create);
                operationContainer.AddOperation(operation);
                operationContainers.Add(operationContainer);

                try
                {
                    {
                        // TODO #26 add result interpretation
                        await this.dal.Write(operationContainers, extensionFiles);

                        if (System.IO.File.Exists(MigrationFileName))
                        {
                            System.IO.File.Delete(MigrationFileName);
                        }

                        this.NotifyStep(MigrationStep.PackEnd);
                    }
                }
                catch (Exception ex)
                {
                    // TODO #37 add proper exception handling
                    Logger.Error($"Could not pack data. Exception: {ex}");
                    success = false;
                }
                finally
                {
                    await this.SourceSession.Close();
                }
            }

            return success;
        }

        /// <summary>
        /// Create a new <see cref="HttpClient"/> instance
        /// </summary>
        /// <param name="credentials">
        /// The <see cref="Credentials"/> used to set the connection and authentication settings
        /// </param>
        /// <returns>
        /// An instance of <see cref="HttpClient"/>
        /// </returns>
        private HttpClient CreateHttpClient(Credentials credentials)
        {
            var client = new HttpClient
            {
                BaseAddress = credentials.Uri,
                // TODO #70 Add user manual for the migration process
                Timeout = Timeout.InfiniteTimeSpan
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.UserName}:{credentials.Password}")));
            client.DefaultRequestHeaders.Add("User-Agent", "SAT");

            return client;
        }

        /// <summary>
        /// Prepare request content as form data that will be send to the CDP4 server
        /// </summary>
        /// <returns>
        /// An instance of <see cref="MultipartContent"/>
        /// </returns>
        private MultipartContent CreateMultipartContent()
        {
            var fileName = Path.GetFileName(ArchiveFileName);
            var multipartContent = new MultipartFormDataContent();

            using (var fileStream = System.IO.File.OpenRead(ArchiveFileName))
            {
                var contentStream = new MemoryStream();
                fileStream.CopyTo(contentStream);
                contentStream.Position = 0;

                var streamContent = new StreamContent(contentStream);
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"file\"",
                    FileName = "\"" + fileName + "\""
                };
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                multipartContent.Add(streamContent, "file");
            }

            multipartContent.Add(new StringContent("pass", Encoding.UTF8), "password");

            return multipartContent;
        }
    }
}
