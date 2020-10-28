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
    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4JsonFileDal;
    using CDP4ServicesDal;
    using CDP4WspDal;
    using Microsoft.Win32;
    using PlainObjects;
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reactive;
    using System.Threading.Tasks;

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
        /// Gets the server login command
        /// </summary>
        public ReactiveCommand<Unit> LoginCommand { get; private set; }

        /// <summary>
        /// Gets the AnnexC-3 zip file command which loads json file as data source <see cref="IReactiveCommand"/>
        /// </summary>
        public ReactiveCommand<object> LoadSourceFile { get; private set; }

        /// <summary>
        /// Gets or sets engineering models list
        /// </summary>
        public List<EngineeringModelRowViewModel> EngineeringModels { get; set; }

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
                    !string.IsNullOrEmpty(serverType.Value) &&
                    !string.IsNullOrEmpty(username) &&
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

            this.LoginSuccessfully = false;
            this.LoginFailed = false;
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
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);

                this.LoginFailed = true;
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
