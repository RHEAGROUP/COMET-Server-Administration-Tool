// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EngineeringModelRowViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SAT.ViewModels.Rows
{
    using CDP4Common.SiteDirectoryData;
    using ReactiveUI;

    /// <summary>
    /// Row class representing a <see cref="EngineeringModelSetup"/> as a plain object
    /// </summary>
    public class EngineeringModelRowViewModel : DefinedThingRowViewModel<EngineeringModelSetup>
    {
        /// <summary>
        /// Backing field for <see cref="Kind"/>
        /// </summary>
        private EngineeringModelKind kind;

        /// <summary>
        /// Gets or sets the kind
        /// </summary>
        public EngineeringModelKind Kind
        {
            get => this.kind;
            set => this.RaiseAndSetIfChanged(ref this.kind, value);
        }

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
        /// Initializes a new instance of the <see cref="EngineeringModelRowViewModel"/> class
        /// </summary>
        /// <param name="modelSetup">The <see cref="EngineeringModelSetup"/> associated with this row</param>
        public EngineeringModelRowViewModel(EngineeringModelSetup modelSetup) : base(modelSetup)
        {
            this.Kind = modelSetup.Kind;
            this.IsSelected = true;
        }
    }
}
