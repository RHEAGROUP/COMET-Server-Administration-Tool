// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DomainOfExpertiseViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Common.ViewModels
{
    using CDP4Dal;
    using DevExpress.Mvvm.Native;
    using PlainObjects;
    using ReactiveUI;
    using System;
    using System.Linq;

    /// <summary>
    /// The viewmodel that is responsible for domains of expertise
    /// </summary>
    public class DomainOfExpertiseViewModel : ReactiveObject
    {
        /// <summary>
        /// Backing field for the <see cref="ISession"/> property
        /// </summary>
        private ISession serverSession;

        /// <summary>
        /// Gets or sets server session
        /// </summary>
        public ISession ServerSession
        {
            private get => this.serverSession;
            set => this.RaiseAndSetIfChanged(ref this.serverSession, value);
        }

        /// <summary>
        /// Backing field for the <see cref="DomainsOfExpertise"/> property
        /// </summary>
        private ReactiveList<DomainOfExpertiseRowViewModel> domainsOfExpertise;

        /// <summary>
        /// Gets or sets domains of expertise
        /// </summary>
        public ReactiveList<DomainOfExpertiseRowViewModel> DomainsOfExpertise
        {
            get => this.domainsOfExpertise;
            private set => this.RaiseAndSetIfChanged(ref this.domainsOfExpertise, value);
        }

        /// <summary>
        /// Gets or sets the command to select/unselect thing
        /// </summary>
        public ReactiveCommand<object> CheckUncheckThing { get; set; }

        /// <summary>
        /// Gets or sets the command to select/unselect all things
        /// </summary>
        public ReactiveCommand<object> CheckUncheckAllThings { get; set; }

        /// <summary>
        /// Out property for the <see cref="SelectAllThings"/> property
        /// </summary>
        private bool selectAllThings;

        /// <summary>
        /// Gets a value indicating whether all things are selected
        /// </summary>
        public bool SelectAllThings
        {
            get => this.selectAllThings;
            set => this.RaiseAndSetIfChanged(ref this.selectAllThings, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainOfExpertiseViewModel"/> class
        /// </summary>
        public DomainOfExpertiseViewModel(ISession serverSession)
        {
            this.serverSession = serverSession;

            this.CheckUncheckThing = ReactiveCommand.Create();
            this.CheckUncheckThing.Subscribe(_ =>
                this.SelectAllThings = !(this.DomainsOfExpertise.Any(d => !d.IsSelected)));

            this.CheckUncheckAllThings = ReactiveCommand.Create();
            this.CheckUncheckAllThings.Subscribe(_ =>
                this.DomainsOfExpertise.ForEach(d => d.IsSelected = this.SelectAllThings));

            this.DomainsOfExpertise = new ReactiveList<DomainOfExpertiseRowViewModel>
            {
                ChangeTrackingEnabled = true
            };

            foreach (var domain in this.serverSession.RetrieveSiteDirectory().Domain)
            {
                this.DomainsOfExpertise.Add(new DomainOfExpertiseRowViewModel(domain));
            }

            foreach (var domainGroup in this.serverSession.RetrieveSiteDirectory().DomainGroup)
            {
                this.DomainsOfExpertise.Add(new DomainOfExpertiseRowViewModel(domainGroup));
            }
        }
    }
}
