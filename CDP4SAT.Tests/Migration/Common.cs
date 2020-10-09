// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommonTest.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace App.Tests.Migration
{
    /// <summary>
    /// Common class that will be used by all suite tests
    /// </summary>
    public abstract class CommonTest
    {
        protected const string SourceServerUri = "https://cdp4services-public.cdp4.org";
        protected const string SourceUsername = "admin";
        protected const string SourcePassword = "pass";
        protected const string TargetServerUri = "http://localhost:5000";
        protected const string TargetUsername = "admin";
        protected const string TargetPassword = "pass";
    }
}