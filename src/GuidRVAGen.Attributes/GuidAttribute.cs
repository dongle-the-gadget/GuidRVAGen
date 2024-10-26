using System;
using System.Diagnostics.CodeAnalysis;

namespace GuidRVAGen
{
    /// <summary>
    /// Specifies a <see cref="Guid"/> to use for RVA generation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class GuidAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GuidAttribute"/> class.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> that should be used as the value of the property.</param>
        public GuidAttribute(string guid)
        { }
    }
}
