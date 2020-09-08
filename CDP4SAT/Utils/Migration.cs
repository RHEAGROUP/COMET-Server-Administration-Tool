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
        /// Data Access Layer used during migration process
        /// </summary>
        private JsonFileDal dal;

        /// <summary>
        /// This flag specify that the output of the grabing data process is one single zip file or mulltiple zip file(one per model)
        /// </summary>
        private bool singleArchive;

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
        /// <param name="message"></param>
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
        /// <param name="singleArchive"></param>
        public Migration(bool singleArchive = true)
        {
            this.dal = new JsonFileDal(new Version("1.0.0"));
            this.singleArchive = singleArchive;

            if (Directory.Exists("Import"))
            {
                Directory.Delete("Import", true);
            }

            Directory.CreateDirectory("Import");
        }

        /// <summary>
        /// Implement import operation
        /// </summary>
        /// <returns></returns>
        public async Task ImportData()
        {
            if (this.SourceSession is null)
            {
                NotifyMessage("Please select source session");
                return;
            }
            NotifyStep(MigrationStep.ImportStart);

            var siteDirectory = this.SourceSession.RetrieveSiteDirectory();
            var archiveName = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\Annex-C3.zip";
            var creds = new Credentials("admin", "pass", new Uri(archiveName));
            var exportSession = new Session(this.dal, creds);

            foreach (var modelSetup in siteDirectory.Model.OrderBy(m => m.Name))
            {
                var isParticipant = modelSetup.Participant.Any(x => x.Person == this.SourceSession.ActivePerson);

                if (isParticipant)
                {
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
                            NotifyMessage(message);
                        }));
                    }

                    while (tasks.Count > 0)
                    {
                        var task = await Task.WhenAny(tasks);
                        tasks.Remove(task);
                    }

                    if (!this.singleArchive)
                    {
                        // Create export session for each model
                        archiveName = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\{modelSetup.Name}.zip";
                        creds = new Credentials("admin", "pass", new Uri(archiveName));
                        exportSession = new Session(this.dal, creds);
                        await PackData(model.Iteration.ToList());
                    }
                }
            }

            if (this.singleArchive)
            {
                await PackData();
            }
            NotifyStep(MigrationStep.ImportEnd);
        }

        /// <summary>
        /// Implement export operation
        /// </summary>
        /// <returns></returns>
        public async Task ExportData()
        {
            if (this.TargetSession is null)
            {
                NotifyMessage("Please select the target session");
                return;
            }

            await Task.Delay(1000);
        }

        /// <summary>
        /// Implement pack(zip) data operation
        /// </summary>
        /// <returns></returns>
        /// <param name="iterations">Model iterations list <see cref="Iteration" /></param>
        /// <returns></returns>
        private async Task PackData(IEnumerable<Iteration> iterations = null)
        {
            var operationContainers = new List<OperationContainer>();
            var openIterations = iterations != null ? this.SourceSession.OpenIterations.Select(i => i.Key).Where(oi => iterations.Any(i => i.Iid == oi.Iid)) : this.SourceSession.OpenIterations.Select(i => i.Key);
            //this.dal.UpdateExchangeFileHeader(this.SourceSession.ActivePerson, "TEST Copyright information", "TEST Header Remark");

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
        }
    }
}
