// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefinedThingRowViewModel.cs">
//    Copyright (c) 2020
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Common.ViewModels.PlainObjects
{
    using CDP4Common.CommonData;
    using ReactiveUI;
    using System;

    /// <summary>
    /// Row class representing a <see cref="DefinedThing"/> as a plain object
    /// </summary>
    public abstract class DefinedThingRowViewModel<T> : ReactiveObject where T : DefinedThing
    {
        /// <summary>
        /// Backing field for <see cref="Iid"/>
        /// </summary>
        private Guid iid;

        /// <summary>
        /// Backing field for <see cref="Name"/>
        /// </summary>
        private string name;

        /// <summary>
        /// Backing field for <see cref="ShortName"/>
        /// </summary>
        private string shortName;

        /// <summary>
        /// Gets or sets the iid
        /// </summary>
        public Guid Iid
        {
            get => this.iid;
            private set => this.RaiseAndSetIfChanged(ref this.iid, value);
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        public string Name
        {
            get => this.name;
            private set => this.RaiseAndSetIfChanged(ref this.name, value);
        }

        /// <summary>
        /// Gets or sets the shortName
        /// </summary>
        public string ShortName
        {
            get => this.shortName;
            private set => this.RaiseAndSetIfChanged(ref this.shortName, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefinedThingRowViewModel{T}"/> class
        /// </summary>
        /// <param name="thing">
        /// The <see cref="DefinedThing"/> associated with this row
        /// </param>
        protected DefinedThingRowViewModel(T thing)
        {
            this.Iid = thing.Iid;
            this.Name = thing.Name;
            this.ShortName = thing.ShortName;
        }
    }
}
