// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Migration.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Utils
{
    using CDP4Common.EngineeringModelData;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4Dal.Operations;
    using CDP4JsonFileDal;
    using ReactiveUI;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Rows;

    /// <summary>
    /// Enumeration of the migration process steps
    /// </summary>
    public enum MigrationStep { ImportStart, PackStart, PackEnd, ImportEnd, ExportStart, ExportEnd };

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
        private static readonly string ArchiveName = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\Annex-C3.zip";

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
        public async Task ImportData(ReactiveList<EngineeringModelRowViewModel> selectedModels)
        {
            if (this.SourceSession is null)
            {
                this.NotifyMessage("Please select source session");
                return;
            }

            Logger.Info($"Retrieving SiteDirectory from {this.SourceSession.DataSourceUri}...");
            this.NotifyStep(MigrationStep.ImportStart);

            var siteDirectory = this.SourceSession.RetrieveSiteDirectory();

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
                    tasks.Add(this.SourceSession.Read(iteration, this.SourceSession.ActivePerson.DefaultDomain).ContinueWith(t =>
                    {
                        var iterationDescription = $"'{modelSetup.Name}'.'{iterationSetup.IterationIid}'";

                        string message;

                        if (t.IsFaulted)
                        {
                            message = $"Reading iteration {iterationDescription} failed. Exception: {t.Exception.Message}.";
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

            await this.PackData();

            Logger.Info("Finished pulling data");
            this.NotifyStep(MigrationStep.ImportEnd);
        }

        /// <summary>
        /// Implement export operation
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task ExportData()
        {
            if (this.TargetSession is null)
            {
                this.NotifyMessage("Please select the target session");
                return;
            }

            // TODO #34 Replace this in the near future, I cannot log into CDP WebService empty server
            // var targetUrl = $"{this.TargetSession.DataSourceUri}Data/Exchange";
            var targetUrl = $"http://localhost:5000/Data/Exchange";

            Logger.Info($"Pushing data to {targetUrl}");

            try
            {
                using (var httpClient = this.CreateHttpClient(TargetSession.Credentials))
                {
                    using (var multipartContent = this.CreateMultipartContent())
                    {
                        using (var message = await httpClient.PostAsync(targetUrl, multipartContent))
                        {
                            var input = await message.Content.ReadAsStringAsync();
                            // TODO #35 add result interpretation

                            Logger.Info($"Finished pushing data");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not push data. Exception: {ex}");
                // TODO #36 add proper exception handling
            }
            finally
            {
                await this.TargetSession.Close();
            }
        }

        /// <summary>
        /// Implement pack(zip) data operation
        /// </summary>
        /// <returns></returns>
        /// <param name="iterations">
        /// Model iterations list <see cref="Iteration"/>
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task PackData(IEnumerable<Iteration> iterations = null)
        {
            var zipCredentials = new Credentials("admin", "pass", new Uri(ArchiveName));
            var zipSession = new Session(this.dal, zipCredentials);

            var operationContainers = new List<OperationContainer>();
            var openIterations = iterations != null ? this.SourceSession.OpenIterations.Select(i => i.Key).Where(oi => iterations.Any(i => i.Iid == oi.Iid)) : this.SourceSession.OpenIterations.Select(i => i.Key);

            foreach (var iteration in openIterations)
            {
                var transactionContext = TransactionContextResolver.ResolveContext(iteration);
                var operationContainer = new OperationContainer(transactionContext.ContextRoute());
                var dto = iteration.ToDto();
                var operation = new Operation(null, dto, OperationKind.Create);
                operationContainer.AddOperation(operation);
                operationContainers.Add(operationContainer);
            }

            try
            {
                // TODO #26 add result interpretation
                await this.dal.Write(operationContainers);
            }
            catch (Exception ex)
            {
                // TODO #37 add proper exception handling
                Logger.Error($"Could not pack data. Exception: {ex}");
            }
            finally
            {
                await this.SourceSession.Close();
            }
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
                BaseAddress = credentials.Uri
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{credentials.UserName}:{credentials.Password}")));
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
            var fileName = Path.GetFileName(ArchiveName);
            var multipartContent = new MultipartFormDataContent();

            using (var fileStream = System.IO.File.OpenRead(ArchiveName))
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
                multipartContent.Add(new StringContent("pass", Encoding.UTF8), "password");
            }

            return multipartContent;
        }
    }
}
