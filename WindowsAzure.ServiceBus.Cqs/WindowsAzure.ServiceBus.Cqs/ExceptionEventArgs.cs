using System;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    ///     Something has thrown an exception, typically in the background.
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BusMessageErrorEventArgs" /> class.
        /// </summary>
        /// <param name="exception">Exception that was thrown during the message processing.</param>
        /// <exception cref="System.ArgumentNullException">exception</exception>
        public ExceptionEventArgs(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            Exception = exception;
        }

        /// <summary>
        ///     Exception that was thrown during the message processing.
        /// </summary>
        public Exception Exception { get; set; }
    }
}