using System;
using System.Runtime.Serialization;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    ///     A message which was not created by this library.
    /// </summary>
    /// <remarks>
    ///     <para>Did you manage to send a message from your own code to this queue?</para>
    /// </remarks>
    [Serializable]
    public class UnknownMessageException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownMessageException"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public UnknownMessageException(string errorMessage)
            : base(errorMessage)
        {
        }

            /// <summary>
        /// Initializes a new instance of the <see cref="UnknownMessageException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected UnknownMessageException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

    }


}