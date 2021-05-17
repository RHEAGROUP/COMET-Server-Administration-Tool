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
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
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
    using Common.Events;
    using Common.ViewModels.PlainObjects;

    /// <summary>
    /// The purpose of this class is to implement migration specif operations such as: import, export, pack
    /// </summary>
    public sealed class Migration
    {
        /// <summary>
        /// Annex C3 Zip archive file name
        /// </summary>
        private static readonly string ArchiveFileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\Import\\Annex-C3.zip";

        /// <summary>
        /// Annex C3 Migration file name
        /// </summary>
        private static readonly string MigrationFileName = $"{AppDomain.CurrentDomain.BaseDirectory}\\Import\\migration.json";

        /// <summary>
        ///  Gets or sets session of the migration source server <see cref="ISession"/>
        /// </summary>
        public ISession SourceSession { get; set; }

        /// <summary>
        /// Gets or sets session of the migration target server <see cref="ISession"/>
        /// </summary>
        public ISession TargetSession { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Migration"/> class
        /// </summary>
        public Migration()
        {
            if (Directory.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\Import"))
            {
                Directory.Delete($"{AppDomain.CurrentDomain.BaseDirectory}\\Import", true);
            }

            Directory.CreateDirectory($"{AppDomain.CurrentDomain.BaseDirectory}\\Import");
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
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Please select source session."
                });

                return false;
            }

            if (selectedModels is null || selectedModels.Count == 0)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Please select model(s) to migrate."
                });

                return false;
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = "Import operation start"
            });

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = $"Retrieving SiteDirectory from {this.SourceSession.DataSourceUri}..."
            });

            var siteDirectory = this.SourceSession.RetrieveSiteDirectory();

            if (siteDirectory == null)
            {
                await this.SourceSession.Open();
                siteDirectory = this.SourceSession.RetrieveSiteDirectory();
            }

            var totalIterationSetups = siteDirectory.Model
                .Where(ems => selectedModels.Select(m => m.Iid).Contains(ems.Iid))
                .Sum(ems => ems.IterationSetup.Count(its => !its.IsDeleted));
            var finishedIterationSetups = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var modelSetup in siteDirectory.Model.OrderBy(m => m.Name))
            {
                if (!selectedModels.Any(em => em.Iid == modelSetup.Iid && em.IsSelected)) continue;

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
                    if (iterationSetup.IsDeleted)
                    {
                        continue;
                    }

                    var iteration = new Iteration(
                        iterationSetup.IterationIid,
                        this.SourceSession.Assembler.Cache,
                        this.SourceSession.Credentials.Uri);

                    model.Iteration.Add(iteration);
                    tasks.Add(this.SourceSession.Read(iteration, this.SourceSession.ActivePerson.DefaultDomain)
                        .ContinueWith(t =>
                        {
                            finishedIterationSetups++;

                            var iterationCount = $"{finishedIterationSetups}/{totalIterationSetups}";
                            var iterationDescription = $"'{modelSetup.Name}'.'{iterationSetup.IterationIid}'";

                            if (t.IsFaulted && t.Exception != null)
                            {
                                CDPMessageBus.Current.SendMessage(new LogEvent
                                {
                                    Message = $"Read iteration {iterationCount} failed: {iterationDescription}",
                                    Exception = t.Exception,
                                    Verbosity = LogVerbosity.Error
                                });
                            }
                            else
                            {
                                CDPMessageBus.Current.SendMessage(new LogEvent
                                {
                                    Message = $"Read iteration {iterationCount} success: {iterationDescription}"
                                });
                            }

                            var elapsed = stopwatch.Elapsed;
                            CDPMessageBus.Current.SendMessage(new LogEvent
                            {
                                Message = $"    Read {finishedIterationSetups} iterations in: {elapsed}"
                            });

                            var remainingIterationSetups = totalIterationSetups - finishedIterationSetups;
                            var remaining = new TimeSpan(elapsed.Ticks / finishedIterationSetups * remainingIterationSetups);
                            CDPMessageBus.Current.SendMessage(new LogEvent
                            {
                                Message = $"    Remaining {remainingIterationSetups} iterations read estimate: {remaining}"
                            });
                        }));
                }

                while (tasks.Count > 0)
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                }
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = "Import operation end"
            });

            return true;
        }

        /// <summary>
        /// Implement export operation
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [ExcludeFromCodeCoverage]
        public async Task<bool> ExportData()
        {
            var success = true;

            if (this.TargetSession is null)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Please select the target session."
                });

                return false;
            }

            if (this.TargetSession.RetrieveSiteDirectory() is null)
            {
                await this.TargetSession.Open();
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = "Export operation start"
            });

            var targetUrl = $"{this.TargetSession.DataSourceUri}Data/Exchange";

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = $"Pushing data to {targetUrl}.{Environment.NewLine}" +
                          $"    Please note that this operation takes a long time and there is no progress user feedback.",
                Verbosity = LogVerbosity.Info
            });

            try
            {
                using (var httpClient = this.CreateHttpClient(TargetSession.Credentials))
                {
                    using (var multipartContent = this.CreateMultipartContent())
                    {
                        await httpClient.PostAsync(targetUrl, multipartContent).ContinueWith((t) =>
                        {
                            success = this.ProcessPost(t);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Please select the target session.",
                    Exception = ex,
                    Verbosity = LogVerbosity.Error
                });
                success = false;
            }
            finally
            {
                CDPMessageBus.Current.SendMessage(new LogoutAndLoginEvent
                {
                    CurrentSession = this.TargetSession,
                    NewPassword = System.IO.File.Exists(MigrationFileName) ? this.SourceSession.Credentials.Password : string.Empty
                });
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = "Export operation end"
            });

            return success;
        }

        /// <summary>
        /// Cleanup Annex C3 files that have been sent to the target server
        /// </summary>
        public void Cleanup()
        {
            if (System.IO.File.Exists(MigrationFileName))
            {
                System.IO.File.Delete(MigrationFileName);
            }

            if (System.IO.File.Exists(ArchiveFileName))
            {
                System.IO.File.Delete(ArchiveFileName);
            }

        }

        /// <summary>
        /// Process the success POST message
        /// </summary>
        /// <param name="task">
        /// The <see cref="Task"/> containing the <see cref="HttpResponseMessage"/>
        /// </param>
        /// <returns>
        /// True if processed successfully
        /// </returns>
        [ExcludeFromCodeCoverage]
        private bool ProcessPost(Task<HttpResponseMessage> task)
        {
            if (task.IsFaulted)
            {
                if (task.Exception?.InnerException != null)
                {
                    throw task.Exception?.InnerException;
                }

                throw new Exception("Unknown inner exception");
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = $"Server status response {task.Result.StatusCode}"
            });

            var success = task.Result.IsSuccessStatusCode;

            if (success)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Finished pushing data"
                });
            }
            else
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Unable to push data. Server returned error. Please check server logs."
                });
            }

            return success;
        }

        /// <summary>
        /// Implement pack(zip) data operation
        /// </summary>
        /// <param name="migrationFile">
        /// Migration file
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>
        /// </returns>
        [ExcludeFromCodeCoverage]
        public async Task<bool> PackData(string migrationFile)
        {
            var success = true;
            List<string> extensionFiles = null;

            if (!string.IsNullOrEmpty(migrationFile))
            {
                if (!System.IO.File.Exists(migrationFile))
                {
                    CDPMessageBus.Current.SendMessage(new LogEvent
                    {
                        Message = "Unable to find selected migration file.",
                        Verbosity = LogVerbosity.Warn
                    });

                    return false;
                }

                try
                {
                    extensionFiles = new List<string> { MigrationFileName };
                    if (System.IO.File.Exists(MigrationFileName))
                    {
                        System.IO.File.Delete(MigrationFileName);
                    }
                    System.IO.File.Copy(migrationFile, MigrationFileName);
                }
                catch (Exception ex)
                {
                    CDPMessageBus.Current.SendMessage(new LogEvent
                    {
                        Message = "Could not add migration.json file.",
                        Exception = ex,
                        Verbosity = LogVerbosity.Error
                    });

                    return false;
                }
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = "Pack operation start"
            });

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
            }

            var zipDal = new JsonFileDal(new Version("1.0.0"));
            var zipCredentials = new Credentials(this.SourceSession.Credentials.UserName, this.TargetSession.Credentials.Password, new Uri(ArchiveFileName));
            // NOTE zipSession needed because JsonFileDal.Write uses the credentials from the Session
            var zipSession = new Session(zipDal, zipCredentials);

            try
            {
                await zipDal.Write(operationContainers, extensionFiles);
            }
            catch (Exception ex)
            {
                CDPMessageBus.Current.SendMessage(new LogEvent
                {
                    Message = "Could not pack data.",
                    Exception = ex,
                    Verbosity = LogVerbosity.Error
                });
                success = false;
            }
            finally
            {
                CDPMessageBus.Current.SendMessage(new LogoutAndLoginEvent { CurrentSession = this.SourceSession });
            }

            CDPMessageBus.Current.SendMessage(new LogEvent
            {
                Message = "Pack operation end"
            });

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
        [ExcludeFromCodeCoverage]
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
        [ExcludeFromCodeCoverage]
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

            multipartContent.Add(new StringContent(this.TargetSession.Credentials.Password, Encoding.UTF8), "password");

            return multipartContent;
        }
    }
}
