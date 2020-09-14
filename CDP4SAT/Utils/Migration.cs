// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Migration.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SAT.Utils
{
    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4JsonFileDal;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System;
    using CDP4Dal.Operations;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using CDP4Common.EngineeringModelData;
    using ReactiveUI;
    using CDP4SAT.ViewModels.Rows;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;

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
        /// Annex C3 Zip archive file name
        /// </summary>
        private static readonly string archiveName = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\Annex-C3.zip";

        /// <summary>
        /// Data Access Layer used during migration process
        /// </summary>
        private JsonFileDal dal;

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
        /// <param name="step">progress operation's step <see cref="MigrationStep"></param>
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
        /// <param name="selectedModels">Selected engineering models from the source server <see cref="EngineeringModelRowViewModel"/></param>
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
            this.NotifyStep(MigrationStep.ImportStart);

            var siteDirectory = this.SourceSession.RetrieveSiteDirectory();
            var creds = new Credentials("admin", "pass", new Uri(archiveName));
            var exportSession = new Session(this.dal, creds);

            foreach (var modelSetup in siteDirectory.Model.OrderBy(m => m.Name))
            {
                if (!selectedModels.ToList().Any(em => em.Iid == modelSetup.Iid && em.IsSelected))
                {
                    continue;
                }
                var model = new EngineeringModel(modelSetup.EngineeringModelIid, this.SourceSession.Assembler.Cache, this.SourceSession.Credentials.Uri)
                { EngineeringModelSetup = modelSetup };
                var tasks = new List<Task>();

                // Read iterations
                foreach (var iterationSetup in modelSetup.IterationSetup)
                {
                    var iteration = new Iteration(iterationSetup.IterationIid, this.SourceSession.Assembler.Cache, this.SourceSession.Credentials.Uri);

                    model.Iteration.Add(iteration);
                    tasks.Add(this.SourceSession.Read(iteration, this.SourceSession.ActivePerson.DefaultDomain).ContinueWith(t =>
                    {
                        string message = (!t.IsFaulted) ? $"Read iteration '{modelSetup.Name}'.'{iterationSetup.IterationIid}' succesfully." : $"Read iteration '{modelSetup.Name}'.'{iterationSetup.IterationIid}' failed. Exception: {t.Exception.Message}.";
                        this.NotifyMessage(message);
                    }));
                }

                while (tasks.Count > 0)
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                }
            }

            await this.PackData();

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

            //TODO Replace this in the near future, I cannot logged in into CDP WebService empty server
            //var serverUrl = $"{this.TargetSession.DataSourceUri}Data/Exchange";
            var serverUrl = $"http://localhost:5000/Data/Exchange";
            try
            {
                using (var httpClient = this.CreateHttpClient(TargetSession.Credentials))
                {
                    using (var multipartContent = this.CreateMultipartContent())
                    {
                        using (var message = await httpClient.PostAsync(serverUrl, multipartContent))
                        {
                            var input = await message.Content.ReadAsStringAsync();
                            //TODO add result interpretation
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO add proper exception handling and logging here
            }
        }

        /// <summary>
        /// Implement pack(zip) data operation
        /// </summary>
        /// <returns></returns>
        /// <param name="iterations">Model iterations list <see cref="Iteration" /></param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private async Task PackData(IEnumerable<Iteration> iterations = null)
        {
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
                //TODO #26 add result interpretation
                await this.dal.Write(operationContainers);
            }
            catch (Exception ex)
            {
                //TODO #27 add proper exception handling and logging here
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                //TODO Inovoke: this.dal.Close(), or maybe we will close/reopen the session again
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
            HttpClient result = new HttpClient();

            result.BaseAddress = credentials.Uri;
            result.DefaultRequestHeaders.Accept.Clear();
            result.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            result.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{credentials.UserName}:{credentials.Password}")));
            result.DefaultRequestHeaders.Add("User-Agent", "SAT");

            return result;
        }

        /// <summary>
        /// Prepare request content as form data that will be send to the CDP4 server
        /// </summary>
        /// <returns>
        /// An instance of <see cref="MultipartContent"/>
        /// </returns>
        private MultipartContent CreateMultipartContent()
        {
            var fileName = Path.GetFileName(archiveName);
            var multipartContent = new MultipartFormDataContent();

            using (var filestream = System.IO.File.OpenRead(archiveName))
            {
                var contentStream = new MemoryStream();
                filestream.CopyTo(contentStream);
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
