// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibraryRowViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels.PlainObjects
{
    using CDP4Common.SiteDirectoryData;
    using ReactiveUI;

    /// <summary>
    /// Row class representing a <see cref="SiteReferenceDataLibrary"/> as a plain object
    /// </summary>
    public class SiteReferenceDataLibraryRowViewModel : DefinedThingRowViewModel<SiteReferenceDataLibrary>
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
        /// Initializes a new instance of the <see cref="SiteReferenceDataLibraryRowViewModel"/> class
        /// </summary>
        /// <param name="siteReferenceDataLibrary">
        /// The <see cref="SiteReferenceDataLibrary"/> associated with this row
        /// </param>
        public SiteReferenceDataLibraryRowViewModel(SiteReferenceDataLibrary siteReferenceDataLibrary)
            : base(siteReferenceDataLibrary)
        {
        }
    }
}
