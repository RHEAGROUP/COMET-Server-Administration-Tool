// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels
{
    using CDP4Dal;
    using System;
    using System.Linq;
    using PlainObjects;
    using ReactiveUI;

    /// <summary>
    /// The view-model for the Source server errors that will be displayed before migration
    /// </summary>
    public class ErrorViewModel : ReactiveObject
    {
        /// <summary>
        /// Backing field for the <see cref="ISession"/> property
        /// </summary>
        private ISession session;

        /// <summary>
        /// Gets or sets login successfully flag
        /// </summary>
        public ISession ServerSession
        {
            private get => this.session;
            set => this.RaiseAndSetIfChanged(ref this.session, value);
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
        ///
        /// </summary>
        public ErrorViewModel()
        {
            this.PocoErrors = new ReactiveList<PocoErrorRowViewModel>
            {
                ChangeTrackingEnabled = true
            };
            this.WhenAnyValue(vm => vm.ServerSession).Subscribe(BindPocoErrors);
        }

        /// <summary>
        /// Apply PocoCardinality & PocoProperties to the E10-25 data set and bind errors to the reactive list
        /// </summary>
        private void BindPocoErrors(ISession currentSession)
        {
            if (currentSession is null)
            {
                return;
            }

            this.PocoErrors.Clear();

            foreach (var thing in currentSession.Assembler.Cache.Select(item => item.Value.Value)
                .Where(t => t.ValidationErrors.Any()))
            {
                foreach (var error in thing.ValidationErrors)
                {
                    this.PocoErrors.Add(new PocoErrorRowViewModel(thing, error));
                }
            }
        }
    }
}
