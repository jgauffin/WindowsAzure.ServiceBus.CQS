using System;
using System.Threading.Tasks;
using DotNetCqs;
using Microsoft.ServiceBus.Messaging;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    /// Used to send messages over Azure queues.
    /// </summary>
    public class AzureEventBus : IEventBus
    {
        private readonly QueueClient _queueClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEventBus" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string as shown in the Azure management web</param>
        /// <param name="queueName">Queue name as you configured it in the Azure management web.</param>
        /// <exception cref="System.ArgumentNullException">
        /// connectionString
        /// or
        /// queueName
        /// </exception>
        public AzureEventBus(string connectionString, string queueName)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (queueName == null) throw new ArgumentNullException("queueName");

            _queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);
        }

        /// <summary>
        /// Request that a command should be executed.
        /// </summary>
        /// <typeparam name="T">Type of command to execute.</typeparam>
        /// <param name="command">Command to execute</param>
        /// <returns>
        /// Task which completes once the command has been delivered (and NOT when it has been executed).
        /// </returns>
        /// <exception cref="System.ArgumentNullException">command</exception>
        public async Task PublishAsync<T>(T command) where T : ApplicationEvent
        {
            if (command == null) throw new ArgumentNullException("command");

            var msg = Serializer.Serializer.Instance.Serialize(command);
            msg.Properties[MessageProperties.PayloadTypeName] = typeof (T).AssemblyQualifiedName;
            msg.MessageId = command.EventId.ToString();
            await _queueClient.SendAsync(msg);
        }
    }
}