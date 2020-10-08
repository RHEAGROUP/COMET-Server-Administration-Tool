// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationBehavior.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Migration.Behaviors
{
    using ViewModels;
    using ViewModels.Common;
    using Views.Tabs;
    using DevExpress.Mvvm.UI.Interactivity;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The purpose of this class is to implement a behavior that is specific to migration flow and allows
    /// the login and source sections(fo the migration tab) to share the same view model
    /// </summary>
    public class MigrationBehavior : Behavior<MigrationLayoutGroup>
    {
        /// <summary>
        /// The on attached event handler
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override void OnAttached()
        {
            base.OnAttached();

            if (!(AssociatedObject.DataContext is MigrationViewModel currentDataContext)) return;

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
            if (AssociatedObject.DataContext is MigrationViewModel currentDataContext)
            {
                currentDataContext.SourceViewModel = null;
                currentDataContext.TargetViewModel = null;
            }

            base.OnDetaching();
        }
    }
}
