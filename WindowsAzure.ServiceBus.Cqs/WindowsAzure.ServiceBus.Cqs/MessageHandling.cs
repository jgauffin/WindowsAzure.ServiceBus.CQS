namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    /// Task to take if we failed to handle a message on one of the bus'es.
    /// </summary>
    public enum MessageHandling
    {
        /// <summary>
        /// Put the message back in the queue (as the first message to be processed)
        /// </summary>
        PutMessageBackInQueue,

        /// <summary>
        /// Remove the message from the queue.
        /// </summary>
        RemoveMessage
    }
}