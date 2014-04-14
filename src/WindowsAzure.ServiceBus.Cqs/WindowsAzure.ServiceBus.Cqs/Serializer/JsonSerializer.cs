using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace WindowsAzure.ServiceBus.Cqs.Serializer
{
    /// <summary>
    ///     Uses JSON.NET to deserialize bodies.
    /// </summary>
    public class JsonSerializer : Serializer
    {
        /// <summary>
        ///     Settings used during serialization and deserialization. Feel free to reconfigure.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only change from the default configuration is that we set
        ///         <c>ConstructorHandling.AllowNonPublicDefaultConstructor</c>.
        ///     </para>
        /// </remarks>
        public static JsonSerializerSettings Settings = new JsonSerializerSettings()
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        /// <summary>
        ///     Serialize a new message.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///     Brokered message which the entity as it's body.
        /// </returns>
        public override BrokeredMessage Serialize(object entity)
        {
            var str = JsonConvert.SerializeObject(entity, Settings);
            return new BrokeredMessage(str);
        }

        /// <summary>
        ///     Deserialize a message from a brokered message
        /// </summary>
        /// <typeparam name="T">Type of entity to deserialize from the message body</typeparam>
        /// <param name="message">Message which contains a body to deserialize</param>
        /// <returns>
        ///     Deserialized entity.
        /// </returns>
        public override T Deserialize<T>(BrokeredMessage message)
        {
            var body = message.GetBody<string>();

            return JsonConvert.DeserializeObject<T>(body, Settings);
        }
    }
}