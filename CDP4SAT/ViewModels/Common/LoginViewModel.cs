// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SAT.ViewModels.Common
{
    using System;
    using System.Diagnostics;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using System.Threading.Tasks;
    using CDP4ServicesDal;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using ReactiveUI;
    using CDP4WspDal;
    using CDP4JsonFileDal;
    using Microsoft.Win32;
    using System.Linq;
    using CDP4SAT.ViewModels.Rows;
    using CDP4Common.SiteDirectoryData;

    /// <summary>
    /// The view-model for the Login that allows users to connect to different datasources
    /// </summary>
    public class LoginViewModel : ReactiveObject
    {
        /// <summary>
        /// Gets or sets datasource server type
        /// </summary>
        public static KeyValuePair<string, string>[] DataSourceList { get; } = {
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
        /// Backing field for the <see cref="Username"/> property
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
        /// Gets or sets login succesfully flag
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
        /// Gets or sets login succesfully flag
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

            set => this.RaiseAndSetIfChanged(ref this.output, value);
        }

        /// <summary>
        /// Gets the Ok Command
        /// </summary>
        public ReactiveCommand<Unit> LoginCommand { get; private set; }

        /// <summary>
        /// Gets the Annex-C-3 zip file <see cref="IReactiveCommand"/>
        /// </summary>
        public ReactiveCommand<object> LoadSourceFile { get; private set; }

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
                    !string.IsNullOrEmpty(serverType.Value) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) &&
                    !string.IsNullOrEmpty(uri));

            this.WhenAnyValue(vm => vm.LoginFailed).Subscribe((loginFailed) =>
            {
                if (!loginFailed)
                {
                    return;
                }
                LogMessage($"Cannot login to {this.Uri}({this.ServerType.Value}) data-source");
            });

            this.WhenAnyValue(vm => vm.LoginSuccessfully).Subscribe(loginSuccessfully =>
            {
                if (!loginSuccessfully)
                {
                    return;
                }
                LogMessage($"Succesfully logged to {this.Uri}({this.ServerType.Value}) data-source");
            });

            this.WhenAnyValue(vm => vm.ServerType).Subscribe(_ =>
            {
                this.JsonIsSelected = this.ServerType.Key != null && this.ServerType.Key.Equals("JSON");
            });

            this.LoginCommand = ReactiveCommand.CreateAsyncTask(canLogin, x => this.ExecuteLogin(), RxApp.MainThreadScheduler);
            this.LoadSourceFile = ReactiveCommand.Create();
            this.LoadSourceFile.Subscribe(_ => this.ExecuteLoadSourceFile());

            this.LoginSuccessfully = false;
            this.LoginFailed = false;
            this.EngineeringModels = new ReactiveList<EngineeringModelRowViewModel>();
            this.EngineeringModels.ChangeTrackingEnabled = true;
            this.SiteReferenceDataLibraries = new ReactiveList<SiteReferenceDataLibraryRowViewModel>();
            this.SiteReferenceDataLibraries.ChangeTrackingEnabled = true;
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
                var isParticipant = modelSetup.Participant.Any(x => x.Person == this.ServerSession.ActivePerson);

                if (isParticipant)
                {
                    this.EngineeringModels.Add(new EngineeringModelRowViewModel(modelSetup));
                }
            }
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
        /// Log message to console/output panel
        /// </summary>
        /// <param name="message"></param>
        private void LogMessage(string message)
        {
            Debug.WriteLine(message);
            this.Output = message;
        }

        /// <summary>
        /// Checki if a session is already open on the passed data source
        /// </summary>
        /// <param name="dataSourceUri">Data source</param>
        /// <param name="username">Logged username</param>
        /// <returns>true/false</returns>
        private bool IsSessionOpen(string dataSourceUri, string username, string password)
        {
            if (this.ServerSession is null)
            {
                return false;
            }

            return this.TrimUri(this.ServerSession.Credentials.Uri.ToString()).Equals(this.TrimUri(dataSourceUri)) && this.ServerSession.Credentials.UserName.Equals(username) && this.ServerSession.Credentials.Password.Equals(password);
        }

        /// <summary>
        /// Trims the final trailing forward slash of the URI
        /// </summary>
        /// <param name="input">The original Uri</param>
        /// <returns>The trimmed uri or the original if there is no slash.</returns>
        private string TrimUri(string input)
        {
            return input.EndsWith("/") ? input.Substring(0, input.Length - 1) : input;
        }
    }
}
