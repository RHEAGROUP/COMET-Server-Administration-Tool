// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SyncerBehaviors.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Syncer.Behaviors
{
    using Common.ViewModels;
    using ViewModels;
    using Views;
    using DevExpress.Mvvm.UI.Interactivity;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The purpose of this class is to implement a behavior that allows the login and source sections
    /// to share the same view model
    /// </summary>
    public class SyncerBehavior : Behavior<Layout>
    {
        /// <summary>
        /// The on attached event handler
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override void OnAttached()
        {
            base.OnAttached();

            if (!(AssociatedObject.DataContext is SyncerViewModel currentDataContext)) return;

            currentDataContext.SourceViewModel = AssociatedObject.LoginSource.DataContext as LoginViewModel;
            currentDataContext.TargetViewModel = AssociatedObject.LoginTarget.DataContext as LoginViewModel;
            currentDataContext.AddSubscriptions();
        }

        /// <summary>
        /// The on detached event handler
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override void OnDetaching()
        {
            if (AssociatedObject.DataContext is SyncerViewModel currentDataContext)
            {
                currentDataContext.SourceViewModel = null;
                currentDataContext.TargetViewModel = null;
            }

            base.OnDetaching();
        }
    }
}
