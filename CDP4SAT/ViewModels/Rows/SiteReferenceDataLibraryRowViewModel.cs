// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteReferenceDataLibraryRowViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SAT.ViewModels.Rows
{
    using CDP4Common.SiteDirectoryData;

    /// <summary>
    /// Row class representing a <see cref="SiteReferenceDataLibrary"/> as a plain object
    /// </summary>
    public class SiteReferenceDataLibraryRowViewModel : DefinedThingRowViewModel<SiteReferenceDataLibrary>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SiteReferenceDataLibraryRowViewModel"/> class
        /// </summary>
        /// <param name="modelSetup">The <see cref="SiteReferenceDataLibrary"/> associated with this row</param>
        public SiteReferenceDataLibraryRowViewModel(SiteReferenceDataLibrary siteReferenceDataLibrary) : base(siteReferenceDataLibrary)
        {
        }
    }
}
