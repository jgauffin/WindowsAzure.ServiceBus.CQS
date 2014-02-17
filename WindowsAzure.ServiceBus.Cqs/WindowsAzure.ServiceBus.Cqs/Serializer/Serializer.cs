using System;
using Microsoft.ServiceBus.Messaging;

namespace WindowsAzure.ServiceBus.Cqs.Serializer
{
    /// <summary>
    /// Serializer used to serialize messages before sending them over Azure queues.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default we let the <see cref="BrokeredMessage"/> serialize the message (which uses the DataContractSerializer internally). Switch serializer if you need a more flexible one.
    /// </para>
    /// </remarks>
    public class Serializer
    {
        /// <summary>
        /// Assign a new serializer by using this property.
        /// </summary>
        public static Serializer Instance = new Serializer();

        /// <summary>
        /// Serialize a new message.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Brokered message which the entity as it's body.</returns>
        /// <exception cref="System.ArgumentNullException">entity</exception>
        public virtual BrokeredMessage Serialize(object entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            return new BrokeredMessage(entity);
        }

        /// <summary>
        /// Deserialize a message from a brokered message
        /// </summary>
        /// <typeparam name="T">Type of entity to deserialize from the message body</typeparam>
        /// <param name="message">Message which contains a body to deserialize</param>
        /// <returns>
        /// Deserialized entity.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        /// <exception cref="NotSupportedException">If the body can not be deserialized.</exception>
        public virtual T Deserialize<T>(BrokeredMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");
            return message.GetBody<T>();
        }
    }
}