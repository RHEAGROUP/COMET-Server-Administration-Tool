// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels
{
    using CDP4Common.SiteDirectoryData;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4JsonFileDal;
    using CDP4Rules;
    using CDP4ServicesDal;
    using CDP4WspDal;
    using Microsoft.Win32;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;
    using PlainObjects;

    /// <summary>
    /// The view-model for the Login that allows users to connect to different data sources
    /// </summary>
    public class LoginViewModel : ReactiveObject
    {
        /// <summary>
        /// Gets or sets data source server type
        /// </summary>
        public static KeyValuePair<string, string>[] DataSourceList { get; } =
        {
            new KeyValuePair<string, string>("CDP", "CDP4 WebServices"),
            new KeyValuePair<string, string>("OCDT", "OCDT WSP Server"),
            new KeyValuePair<string, string>("JSON", "JSON")
        };

        /// <summary>
        /// Backing field for the <see cref="ServerType"/> property
        /// </summary>
        private KeyValuePair<string, string> serverType;

        /// <summary>
        /// Gets or sets server serverType value
        /// </summary>
        public KeyValuePair<string, string> ServerType
        {
            get => this.serverType;
            set => this.RaiseAndSetIfChanged(ref this.serverType, value);
        }

        /// <summary>
        /// Backing field for the <see cref="UserName"/> property
        /// </summary>
        private string username;

        /// <summary>
        /// Gets or sets server username value
        /// </summary>
        public string UserName
        {
            get => this.username;
            set => this.RaiseAndSetIfChanged(ref this.username, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Password"/> property
        /// </summary>
        private string password;

        /// <summary>
        /// Gets or sets server password value
        /// </summary>
        public string Password
        {
            get => this.password;
            set => this.RaiseAndSetIfChanged(ref this.password, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Uri"/> property
        /// </summary>
        private string uri;

        /// <summary>
        /// Gets or sets server uri
        /// </summary>
        public string Uri
        {
            get => this.uri;
            set => this.RaiseAndSetIfChanged(ref this.uri, value);
        }

        /// <summary>
        /// The backing field for available data access layer <see cref="IDal"/>
        /// </summary>
        private IDal dal;

        /// <summary>
        /// Backing field for the <see cref="ISession"/> property
        /// </summary>
        private ISession session;

        /// <summary>
        /// Gets or sets login successfully flag
        /// </summary>
        public ISession ServerSession
        {
            get => this.session;
            private set => this.RaiseAndSetIfChanged(ref this.session, value);
        }

        /// <summary>
        /// Backing field for the <see cref="LoginSuccessfully"/> property
        /// </summary>
        private bool loginSuccessfully;

        /// <summary>
        /// Gets or sets login successfully flag
        /// </summary>
        public bool LoginSuccessfully
        {
            get => this.loginSuccessfully;
            private set => this.RaiseAndSetIfChanged(ref this.loginSuccessfully, value);
        }

        /// <summary>
        /// Backing field for the <see cref="LoginFailed"/> property
        /// </summary>
        private bool loginFailed;

        /// <summary>
        /// Gets or sets login failed flag
        /// </summary>
        public bool LoginFailed
        {
            get => this.loginFailed;
            private set => this.RaiseAndSetIfChanged(ref this.loginFailed, value);
        }

        /// <summary>
        /// Backing field for the <see cref="JsonIsSelected"/> property
        /// </summary>
        private bool jsonIsSelected;

        /// <summary>
        /// Gets or sets json selected flag
        /// </summary>
        public bool JsonIsSelected
        {
            get => this.jsonIsSelected;
            private set => this.RaiseAndSetIfChanged(ref this.jsonIsSelected, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Output"/> property
        /// </summary>
        private string output;

        /// <summary>
        /// Gets or sets output panel log messages
        /// </summary>
        public string Output
        {
            get => this.output;
            private set => this.RaiseAndSetIfChanged(ref this.output, value);
        }

        /// <summary>
        /// Out property for the <see cref="SelectAllModels"/> property
        /// </summary>
        private bool selectAllModels;

        /// <summary>
        /// Gets a value indicating whether all models are selected
        /// </summary>
        private bool SelectAllModels
        {
            get => this.selectAllModels;
            set => this.RaiseAndSetIfChanged(ref this.selectAllModels, value);
        }

        /// <summary>
        /// Backing field for the <see cref="EngineeringModels"/> property
        /// </summary>
        private ReactiveList<EngineeringModelRowViewModel> engineeringModels;

        /// <summary>
        /// Gets or sets engineering models list
        /// </summary>
        public ReactiveList<EngineeringModelRowViewModel> EngineeringModels
        {
            get => this.engineeringModels;
            private set => this.RaiseAndSetIfChanged(ref this.engineeringModels, value);
        }

        /// <summary>
        /// Backing field for the <see cref="SiteReferenceDataLibraries"/> property
        /// </summary>
        private ReactiveList<SiteReferenceDataLibraryRowViewModel> siteReferenceDataLibraries;

        /// <summary>
        /// Gets or sets site reference data libraries
        /// </summary>
        public ReactiveList<SiteReferenceDataLibraryRowViewModel> SiteReferenceDataLibraries
        {
            get => this.siteReferenceDataLibraries;
            private set => this.RaiseAndSetIfChanged(ref this.siteReferenceDataLibraries, value);
        }

        /// <summary>
        /// Backing field for the <see cref="PocoErrors"/> property
        /// </summary>
        private ReactiveList<PocoErrorRowViewModel> pocoErrors;

        /// <summary>
        /// Gets or sets poco errors list
        /// </summary>
        public ReactiveList<PocoErrorRowViewModel> PocoErrors
        {
            get => this.pocoErrors;
            private set => this.RaiseAndSetIfChanged(ref this.pocoErrors, value);
        }

        /// <summary>
        /// Backing field for the <see cref="RuleCheckerErrors"/> property
        /// </summary>
        private ReactiveList<RuleCheckerErrorRowViewModel> ruleCheckerErrors;

        /// <summary>
        /// Gets or sets rule checker errors list
        /// </summary>
        public ReactiveList<RuleCheckerErrorRowViewModel> RuleCheckerErrors
        {
            get => this.ruleCheckerErrors;
            private set => this.RaiseAndSetIfChanged(ref this.ruleCheckerErrors, value);
        }

        /// <summary>
        /// Gets the server login command
        /// </summary>
        public ReactiveCommand<Unit> LoginCommand { get; private set; }

        /// <summary>
        /// Gets the AnnexC-3 zip file command which loads json file as data source <see cref="IReactiveCommand"/>
        /// </summary>
        public ReactiveCommand<object> LoadSourceFile { get; private set; }

        /// <summary>
        /// Gets the command to select/unselect all models for import
        /// </summary>
        public ReactiveCommand<object> CheckUncheckModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
        /// </summary>
        public LoginViewModel()
        {
            var canLogin = this.WhenAnyValue(
                vm => vm.ServerType,
                vm => vm.UserName,
                vm => vm.Password,
                vm => vm.Uri,
                (serverType, username, password, uri) =>
                    !string.IsNullOrEmpty(serverType.Value) && !string.IsNullOrEmpty(username) &&
                    !string.IsNullOrEmpty(password) &&
                    !string.IsNullOrEmpty(uri));

            this.WhenAnyValue(vm => vm.LoginFailed).Subscribe((loginFailed) =>
            {
                if (!loginFailed) return;

                LogMessage($"Cannot login to {this.Uri}({this.ServerType.Value}) data-source");
            });

            this.WhenAnyValue(vm => vm.LoginSuccessfully).Subscribe(loginSuccessfully =>
            {
                if (!loginSuccessfully) return;

                LogMessage($"Successfully logged to {this.Uri}({this.ServerType.Value}) data-source");
            });

            this.WhenAnyValue(vm => vm.ServerType).Subscribe(_ =>
            {
                this.JsonIsSelected = this.ServerType.Key != null && this.ServerType.Key.Equals("JSON");
            });

            this.LoginCommand =
                ReactiveCommand.CreateAsyncTask(canLogin, x => this.ExecuteLogin(), RxApp.MainThreadScheduler);
            this.LoadSourceFile = ReactiveCommand.Create();
            this.LoadSourceFile.Subscribe(_ => this.ExecuteLoadSourceFile());
            this.CheckUncheckModel = ReactiveCommand.Create();
            this.CheckUncheckModel.Subscribe(_ => this.ExecuteCheckUncheckModel());

            this.LoginSuccessfully = false;
            this.LoginFailed = false;

            this.EngineeringModels = new ReactiveList<EngineeringModelRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            this.SiteReferenceDataLibraries = new ReactiveList<SiteReferenceDataLibraryRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            this.PocoErrors = new ReactiveList<PocoErrorRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            this.RuleCheckerErrors = new ReactiveList<RuleCheckerErrorRowViewModel>
            {
                ChangeTrackingEnabled = true
            };
        }

        /// <summary>
        /// Executes login command
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        private async Task ExecuteLogin()
        {
            this.LoginSuccessfully = false;
            this.LoginFailed = false;

            try
            {
                if (this.IsSessionOpen(this.Uri, this.UserName, this.Password))
                {
                    LogMessage("The user is already logged on this server. Closing the session.");
                    await this.ServerSession.Close();
                }

                var credentials = new Credentials(this.UserName, this.Password, new Uri(this.Uri));

                switch (this.ServerType.Key)
                {
                    case "CDP":
                        this.dal = new CdpServicesDal();
                        break;
                    case "OCDT":
                        this.dal = new WspDal();
                        break;
                    case "JSON":
                        this.dal = new JsonFileDal(new Version("1.0.0"));
                        break;
                }

                this.ServerSession = new Session(this.dal, credentials);

                await this.ServerSession.Open();

                this.LoginSuccessfully = true;

                var siteDirectory = this.ServerSession.RetrieveSiteDirectory();

                this.BindEngineeringModels(siteDirectory);
                this.BindSiteReferenceDataLibraries(siteDirectory);
                this.BindPocoErrors();
                this.BindEngineeringModelErrors();
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);

                this.LoginFailed = true;
            }
        }

        /// <summary>
        /// Bind engineering models to the reactive list
        /// </summary>
        /// <param name="siteDirectory">The <see cref="SiteDirectory"/> top container</param>
        private void BindEngineeringModels(SiteDirectory siteDirectory)
        {
            this.EngineeringModels.Clear();

            foreach (var modelSetup in siteDirectory.Model.OrderBy(m => m.Name))
            {
                this.EngineeringModels.Add(new EngineeringModelRowViewModel(modelSetup));
            }

            this.SelectAllModels = true;
        }

        /// <summary>
        /// Bind site reference data libraries to the reactive list
        /// </summary>
        /// <param name="siteDirectory">The <see cref="SiteDirectory"/> top container</param>
        private void BindSiteReferenceDataLibraries(SiteDirectory siteDirectory)
        {
            this.SiteReferenceDataLibraries.Clear();

            foreach (var rdl in siteDirectory.SiteReferenceDataLibrary.OrderBy(m => m.Name))
            {
                this.SiteReferenceDataLibraries.Add(new SiteReferenceDataLibraryRowViewModel(rdl));
            }
        }

        /// <summary>
        /// Apply PocoCardinality & PocoProperties to the E10-25 data set and bind errors to the reactive list
        /// </summary>
        private void BindPocoErrors()
        {
            this.PocoErrors.Clear();

            foreach (var thing in this.ServerSession.Assembler.Cache.Select(item => item.Value.Value)
                .Where(t => t.ValidationErrors.Any()))
            {
                foreach (var error in thing.ValidationErrors)
                {
                    this.PocoErrors.Add(new PocoErrorRowViewModel(thing, error));
                }
            }
        }

        /// <summary>
        /// Apply RuleCheckerEngine to the E10-25 data set and bind errors to the reactive list
        /// </summary>
        private void BindEngineeringModelErrors()
        {
            var ruleCheckerEngine = new RuleCheckerEngine();
            var resultList = ruleCheckerEngine.Run(this.ServerSession.Assembler.Cache.Select(item => item.Value.Value));

            this.RuleCheckerErrors.Clear();

            foreach (var result in resultList)
            {
                this.RuleCheckerErrors.Add(new RuleCheckerErrorRowViewModel(result.Thing, result.Id, result.Description,
                    result.Severity));
            }
        }

        /// <summary>
        /// Executes loading of Annex-C-3 file
        /// </summary>
        private void ExecuteLoadSourceFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Import\\",
                Filter = "Zip files (*.zip)|*.zip"
            };

            var dialogResult = openFileDialog.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value && openFileDialog.FileNames.Length == 1)
            {
                this.Uri = openFileDialog.FileNames[0];
            }
        }

        /// <summary>
        /// Select model for the migration procedure
        /// </summary>
        private void ExecuteCheckUncheckModel()
        {
            this.SelectAllModels = !(this.EngineeringModels.Where(em => !em.IsSelected).Count() > 0);
        }

        /// <summary>
        /// Log message to console/output panel
        /// </summary>
        /// <param name="message"></param>
        private void LogMessage(string message)
        {
            Debug.WriteLine(message);
            this.Output = message;
        }

        /// <summary>
        /// Check if a session is already open on the passed data source
        /// </summary>
        /// <param name="dataSourceUri">Data source</param>
        /// <param name="dataSourceUsername">Data source username</param>
        /// <param name="dataSourcePassword">Data source password</param>
        /// <returns>true/false</returns>
        private bool IsSessionOpen(string dataSourceUri, string dataSourceUsername, string dataSourcePassword)
        {
            if (this.ServerSession is null) return false;

            return TrimUri(this.ServerSession.Credentials.Uri.ToString()).Equals(TrimUri(dataSourceUri)) &&
                   this.ServerSession.Credentials.UserName.Equals(dataSourceUsername) &&
                   this.ServerSession.Credentials.Password.Equals(dataSourcePassword);
        }

        /// <summary>
        /// Trims the final trailing forward slash of the URI
        /// </summary>
        /// <param name="input">The original Uri</param>
        /// <returns>The trimmed uri or the original if there is no slash.</returns>
        private static string TrimUri(string input)
        {
            return input.EndsWith("/") ? input.Substring(0, input.Length - 1) : input;
        }
    }
}
