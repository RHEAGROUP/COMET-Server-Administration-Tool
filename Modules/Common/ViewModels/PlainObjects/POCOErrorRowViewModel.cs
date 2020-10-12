// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PocoErrorRowViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels.PlainObjects
{
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
        /// Gets the <see cref="ClassKind"/> of the <see cref="Thing"/> that contains the error.
        /// </summary>
        public string ContainerThingClassKind { get; private set; }

        /// <summary>
        /// Gets the human readable content of the Error.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Gets the human readable content of the Error.
        /// </summary>
        public string Path { get; private set; }

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
    }
}
