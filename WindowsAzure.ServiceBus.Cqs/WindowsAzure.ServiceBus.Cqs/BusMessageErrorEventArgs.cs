using System;
using Microsoft.ServiceBus.Messaging;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    ///     Arguments for the command bus error event.
    /// </summary>
    /// <remarks>
    /// <para>It's recommended that you set the <see cref="MessageTask"/> property when receiving this event argument.</para>
    /// </remarks>
    public class BusMessageErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BusMessageErrorEventArgs" /> class.
        /// </summary>
        /// <param name="message">Message that we failed to handle.</param>
        /// <param name="exception">Exception that was thrown during the message processing.</param>
        /// <exception cref="System.ArgumentNullException">message</exception>
        public BusMessageErrorEventArgs(BrokeredMessage message, Exception exception)
        {
            Message = message;
            Exception = exception;
            MessageTask = MessageHandling.PutMessageBackInQueue;
        }

        /// <summary>
        ///     Message that we failed to handle
        /// </summary>
        /// <remarks>
        /// <para>
        /// Might be null if it's the actual message receiving that failed.
        /// </para>
        /// </remarks>
        public BrokeredMessage Message { get; set; }

        /// <summary>
        ///     Exception that was thrown during the message processing.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        ///     Action to take.
        /// </summary>
        /// <value>
        /// Default is <see cref="MessageHandling.PutMessageBackInQueue"/>.
        /// </value>
        public MessageHandling MessageTask { get; set; }
    }
}