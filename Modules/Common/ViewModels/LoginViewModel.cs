// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs" company="RHEA System S.A.">
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

namespace Common.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;

    using CDP4Dal;
    using CDP4Dal.DAL;
    
    using CDP4JsonFileDal;
    
    using CDP4ServicesDal;
    
    using CDP4WspDal;
    
    using Common.Utils;
    
    using DynamicData;
    
    using Events;
    
    using Microsoft.Win32;
    
    using PlainObjects;
    
    using ReactiveUI;
    
    using Settings;


    /// <summary>
    /// Enum describing the possible server types
    /// </summary>
    public enum DataSource
    {
        CDP4,
        WSP,
        JSON
    }

    /// <summary>
    /// The view-model for the Login that allows users to connect to different data sources
    /// </summary>
    public class LoginViewModel : ReactiveObject, ILoginViewModel
    {
        /// <summary>
        /// Gets data source server type
        /// </summary>
        public static Dictionary<DataSource, string> ServerTypes { get; } =
            new Dictionary<DataSource, string>
            {
                { DataSource.CDP4, "CDP4 WebServices" },
                { DataSource.WSP, "OCDT WSP Server" },
                { DataSource.JSON, "JSON" }
            };

        /// <summary>
        /// Backing field for the <see cref="DataSource"/> property
        /// </summary>
        private DataSource selectedDataSource;

        /// <summary>
        /// Gets or sets server serverType value
        /// </summary>
        public DataSource SelectedDataSource
        {
            get => this.selectedDataSource;
            set => this.RaiseAndSetIfChanged(ref this.selectedDataSource, value);
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
        /// Gets or sets dal
        /// </summary>
        public IDal Dal
        {
            get => this.dal;
            set => this.RaiseAndSetIfChanged(ref this.dal, value);
        }

        /// <summary>
        /// Backing field for the <see cref="ISession"/> property
        /// </summary>
        private ISession session;

        /// <summary>
        /// Gets or sets server session
        /// </summary>
        public ISession ServerSession
        {
            get => this.session;
            set => this.RaiseAndSetIfChanged(ref this.session, value);
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
        /// Gets or sets whether the currently entered uri can be saved
        /// </summary>
        public bool CanSaveUri
        {
            get => this.canSaveUri;
            private set => this.RaiseAndSetIfChanged(ref this.canSaveUri, value);
        }

        /// <summary>
        /// Backing field for the <see cref="Output"/> property
        /// </summary>
        private string output;

        /// <summary>
        /// Backing field for the <see cref="CanSaveUri"/> property
        /// </summary>
        private bool canSaveUri;

        /// <summary>
        /// Gets or sets output panel log messages
        /// </summary>
        public string Output
        {
            get => this.output;
            private set => this.RaiseAndSetIfChanged(ref this.output, value);
        }

        /// <summary>
        /// Gets the server login command
        /// </summary>
        public ReactiveCommand<Unit, Unit> LoginCommand { get; private set; }

        /// <summary>
        /// Gets the AnnexC-3 zip file command which loads json file as data source <see cref="IReactiveCommand"/>
        /// </summary>
        public ReactiveCommand<Unit, Unit> LoadSourceFile { get; private set; }

        /// <summary>
        /// Gets the command to save the current URI
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveCurrentUri { get; private set; }

        /// <summary>
        /// Gets or sets engineering models list
        /// </summary>
        public List<EngineeringModelRowViewModel> EngineeringModels { get; set; }

        /// <summary>
        /// Gets or sets the list of saved uris
        /// </summary>
        public SourceList<string> SavedUris { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
        /// </summary>
        public LoginViewModel()
        {
            var canLogin = this.WhenAnyValue(
                vm => vm.UserName,
                vm => vm.Password,
                vm => vm.Uri,
                (username, password, uri) =>
                    !string.IsNullOrWhiteSpace(username) &&
                    !string.IsNullOrWhiteSpace(password) &&
                    !string.IsNullOrWhiteSpace(uri));

            this.SavedUris = new SourceList<string>();

            this.WhenAnyValue(vm => vm.LoginFailed).Subscribe((loginFailed) =>
            {
                if (!loginFailed) return;

                this.Output = $"Cannot log in to {this.Uri} ({ServerTypes[SelectedDataSource]}) data-source";
            });

            this.WhenAnyValue(vm => vm.LoginSuccessfully).Subscribe(loginSuccessfully =>
            {
                if (!loginSuccessfully) return;

                this.Output = $"Successfully logged in to {this.Uri} ({ServerTypes[SelectedDataSource]}) data-source";
            });

            this.WhenAnyValue(vm => vm.SelectedDataSource).Subscribe(_ =>
            {
                this.JsonIsSelected = this.SelectedDataSource == DataSource.JSON;
            });

            this.WhenAnyValue(vm => vm.Uri).Subscribe(_ => { this.ComputeCanSaveUri(); });

            this.SavedUris.Connect().Subscribe(_ => this.ComputeCanSaveUri());

            this.WhenAnyValue(vm => vm.SelectedDataSource).Subscribe(_ =>
            {
                switch (this.SelectedDataSource)
                {
                    case DataSource.CDP4:
                        this.Dal = new CdpServicesDal();
                        break;
                    case DataSource.WSP:
                        this.Dal = new WspDal();
                        break;
                    case DataSource.JSON:
                        this.Dal = new JsonFileDal(new Version("1.0.0"));
                        break;
                }
            });
            this.GetSavedUris();

            CDPMessageBus.Current.Listen<SettingsReloadedEvent>().Subscribe(_ => this.GetSavedUris());
            CDPMessageBus.Current.Listen<LogoutAndLoginEvent>().Subscribe(async (serverEvent) => {
                if (serverEvent.CurrentSession != this.ServerSession)
                {
                    return;
                }

                await ExecuteLogout(serverEvent.CurrentSession);

                if (!string.IsNullOrEmpty(serverEvent.NewPassword))
                {
                    this.Password = serverEvent.NewPassword;
                }

                await ExecuteLogin();
            });

            this.LoginCommand = ReactiveCommand.CreateFromTask(x => this.ExecuteLogin(), canLogin, RxApp.MainThreadScheduler);
            this.LoadSourceFile = ReactiveCommandCreator.Create();
            this.LoadSourceFile.Subscribe(_ => this.ExecuteLoadSourceFile());

            this.SaveCurrentUri = ReactiveCommandCreator.Create();
            this.SaveCurrentUri.Subscribe(_ => this.ExecuteSaveCurrentUri());

            this.LoginSuccessfully = false;
            this.LoginFailed = false;
        }

        /// <summary>
        /// Gets the saved Uris
        /// </summary>
        private void GetSavedUris()
        {
            this.SavedUris.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(AppSettingsHandler.Settings.SavedUris);
            });
        }

        /// <summary>
        /// Saves the current Uri to the list
        /// </summary>
        [ExcludeFromCodeCoverage]
        private void ExecuteSaveCurrentUri()
        {
            AppSettingsHandler.Settings.SavedUris.Add(this.Uri);
            AppSettingsHandler.Save();
        }

        /// <summary>
        /// Computes whether the uri can be saves
        /// </summary>
        private void ComputeCanSaveUri()
        {
            this.CanSaveUri = !string.IsNullOrWhiteSpace(this.Uri) && !this.SavedUris.Items.Contains(this.Uri);
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
                    this.Output = "The user is already logged on this server. Closing the session.";

                    await this.ServerSession.Close();
                }

                // when no trailing slash is provided it can lead to loss of nested paths
                // see https://stackoverflow.com/questions/22543723/create-new-uri-from-base-uri-and-relative-path-slash-makes-a-difference
                // for consistency, all URIs are now appended, cannot rely on user getting it right.
                if (this.SelectedDataSource != DataSource.JSON && !this.Uri.EndsWith("/"))
                {
                    this.Uri += "/";
                }

                var credentials = new Credentials(this.UserName, this.Password, new Uri(this.Uri));

                this.ServerSession = new Session(this.Dal, credentials);

                await this.ServerSession.Open();

                this.LoginSuccessfully = true;
            }
            catch (Exception ex)
            {
                this.Output = $"Cannot execute login. Exception: {ex.Message} {ex.StackTrace}";

                this.LoginFailed = true;
            }
        }

        /// <summary>
        /// Executes login command
        /// </summary>
        /// <param name="currentSession">Current user <see cref="ISession"/></param>
        /// <returns>The <see cref="Task"/></returns>
        [ExcludeFromCodeCoverage]
        private async Task ExecuteLogout(ISession currentSession)
        {
            this.Output = $"Successfully logged out from {currentSession.DataSourceUri} data-source";

            await currentSession.Close();

            this.LoginSuccessfully = false;
            this.LoginFailed = false;
        }

        /// <summary>
        /// Executes loading of Annex-C-3 file
        /// </summary>
        [ExcludeFromCodeCoverage]
        private void ExecuteLoadSourceFile()
        {
            var openFileDialog = new OpenFileDialog()
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
                   this.ServerSession.Credentials.Password.Equals(dataSourcePassword) &&
                   this.ServerSession.ActivePerson != null;
        }

        /// <summary>
        /// Trims the final trailing forward slash of the URI
        /// </summary>
        /// <param name="input">The original Uri</param>
        /// <returns>The trimmed uri or the original if there is no slash.</returns>
        private static string TrimUri(string input)
        {
            return input.EndsWith("/")
                ? input.Substring(0, input.Length - 1)
                : input;
        }
    }
}
