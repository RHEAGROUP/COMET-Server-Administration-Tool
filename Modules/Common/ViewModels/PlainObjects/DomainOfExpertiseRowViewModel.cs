// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DomainOfExpertiseRowViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels.PlainObjects
{
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;
    using ReactiveUI;

    /// <summary>
    /// Row class representing a <see cref="DomainOfExpertise"/> or <see cref="DomainOfExpertiseGroup"/>
    /// as a plain object
    /// </summary>
    public class DomainOfExpertiseRowViewModel : DefinedThingRowViewModel<DefinedThing>
    {
        /// <summary>
        /// Backing field for <see cref="IsSelected"/>
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// Gets or sets the if object is selected
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.RaiseAndSetIfChanged(ref this.isSelected, value);
        }

        /// <summary>
        /// Backing field for <see cref="ClassKind"/>
        /// </summary>
        private ClassKind classKind;

        /// <summary>
        /// Gets or sets the ClassKind
        /// </summary>
        public ClassKind ClassKind
        {
            get => this.classKind;
            private set => this.RaiseAndSetIfChanged(ref this.classKind, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainOfExpertiseRowViewModel"/> class
        /// </summary>
        /// <param name="definedThing">
        /// The <see cref="DefinedThing"/> associated with this row
        /// </param>
        public DomainOfExpertiseRowViewModel(DefinedThing definedThing)
            : base(definedThing)
        {
            this.ClassKind = definedThing.ClassKind;
        }
    }
}
