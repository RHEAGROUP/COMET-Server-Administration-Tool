// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainApp.xaml.cs" company="RHEA System S.A.">
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

namespace CDP4SAT
{
    using System;
    using System.Windows;
    using ExceptionReporting;
    using NLog;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// A NLog logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Called when the applications starts. Makes a distinction between debug and release mode
        /// </summary>
        /// <param name="e">
        /// the event argument
        /// </param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;

            Application.Current.MainWindow = new MainWindow();
            Application.Current.MainWindow.Show();
        }

        /// <summary>
        /// Event handler
        /// </summary>
        /// <param name="sender">T the sender of the exception</param>
        /// <param name="e">An instance of <see cref="UnhandledExceptionEventArgs"/> that carries the <see cref="Exception"/></param>
        private static void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// Handles the provided exception by showing it to the end-user
        /// </summary>
        /// <param name="ex">The exception that is being handled</param>
        private static void HandleException(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            logger.Error(ex);

            var exceptionReporter = new ExceptionReporter();
            exceptionReporter.Show(ex);

            Environment.Exit(1);
        }
    }
}
