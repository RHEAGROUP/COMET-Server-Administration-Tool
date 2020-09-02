// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MigrationBehavior.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SAT.Behaviors
{
    using CDP4SAT.ViewModels;
    using CDP4SAT.ViewModels.Common;
    using CDP4SAT.Views.Tabs;
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

            var currentDataContext = AssociatedObject.DataContext as MigrationViewModel;

            if (currentDataContext != null)
            {
                currentDataContext.SourceViewModel = AssociatedObject.LoginSource.DataContext as LoginViewModel;
                currentDataContext.TargetViewModel = AssociatedObject.LoginTarget.DataContext as LoginViewModel;
                currentDataContext.AddSubscriptions();
            }
        }

        /// <summary>
        /// The on dettached event handler
        /// </summary>
        [ExcludeFromCodeCoverage]
        protected override void OnDetaching()
        {
            var currentDataContext = AssociatedObject.DataContext as MigrationViewModel;

            if (currentDataContext != null)
            {
                currentDataContext.SourceViewModel = null;
                currentDataContext.TargetViewModel = null;
            }

            base.OnDetaching();
        }
    }
}
