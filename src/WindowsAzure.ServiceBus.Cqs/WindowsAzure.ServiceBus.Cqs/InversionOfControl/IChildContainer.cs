using System;
using System.Collections.Generic;

namespace WindowsAzure.ServiceBus.Cqs.InversionOfControl
{
    /// <summary>
    /// A scoped container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All scoped <c>IDisposable</c> that has been resolved should be disposed when the child container is disposed.
    /// </para>
    /// </remarks>
    public interface IChildContainer : IDisposable
    {
        /// <summary>
        /// Resolve all implementations of the specified service.
        /// </summary>
        /// <typeparam name="T">Type of service</typeparam>
        /// <returns>A list of implementations (or an empty list if no implementations was found)</returns>
        IEnumerable<T> ResolveAll<T>() where T : class;
    }
}