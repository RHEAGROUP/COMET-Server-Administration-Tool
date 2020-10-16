// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PocoErrorRowViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels.PlainObjects
{
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.CommonData;
    using CDP4Common.Exceptions;
    using ReactiveUI;
    using System;

    /// <summary>
    /// Row class representing a <see cref="PocoErrorRowViewModel"/> as a plain object
    /// </summary>
    public class PocoErrorRowViewModel : ReactiveObject
    {
        /// <summary>
        /// Gets or sets the identifier or code of the Rule that may have been broken
        /// </summary>
        private string Id { get; set; }

        /// <summary>
        /// Gets the <see cref="ClassKind"/> of the <see cref="Thing"/> that contains the error.
        /// </summary>
        public string ContainerThingClassKind { get; private set; }

        /// <summary>
        /// Gets or sets the human readable content of the Error.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Gets or sets the human readable content of the Error.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets or sets top container name
        /// </summary>
        public string TopContainerName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PocoErrorRowViewModel"/> class
        /// <param name="thing">
        /// The thing <see cref="Thing" />.
        /// </param>
        /// <param name="error">
        /// The error content.
        /// </param>
        /// </summary>
        public PocoErrorRowViewModel(Thing thing, string error)
        {
            this.ContainerThingClassKind = thing.ClassKind.ToString();
            this.Error = error;
            this.Id = thing.Iid.ToString();
            this.TopContainerName = thing.TopContainer is SiteDirectory
                ? "SiteDirectory"
                : thing.TopContainer.UserFriendlyShortName;

            try
            {
                var dto = thing.ToDto();
                var dtoRoute = dto.Route;
                var uriBuilder = new UriBuilder(thing.IDalUri) { Path = dtoRoute };
                this.Path = uriBuilder.ToString();
            }
            catch (ContainmentException ex)
            {
                this.Path = ex.Message;
            }
        }

        /// <summary>
        /// Override ToString() method
        /// </summary>
        /// <returns>string object representation</returns>
        public override string ToString()
        {
            return
                $"{this.ContainerThingClassKind}({this.Id}) Top container: {this.TopContainerName}{Environment.NewLine}{this.Error}{Environment.NewLine}Path: {this.Path}";
        }
    }
}
