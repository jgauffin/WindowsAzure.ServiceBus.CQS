using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WindowsAzure.ServiceBus.Cqs.Logging;
using DotNetCqs;
using Microsoft.ServiceBus.Messaging;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    /// Used to handle queries over azure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation uses queue dialogs to identify queries for specific clients. You must therefore have activated dialogs on the queues that you've configured for this bus.
    /// </para>
    /// </remarks>
    public class AzureQueryBus : IQueryBus, IDisposable
    {
        private QueueClient _sendQueueClient;
        private QueueClient _readQueueClient;
        private ConcurrentDictionary<Guid, ITaskHandler> _queue = new ConcurrentDictionary<Guid, ITaskHandler>();
        private string _sessionId;
        private MessageSession _session;
        private MethodInfo _serializerMethod;
        private volatile bool _shutdown = false;
        private ManualResetEventSlim _shutdownEvent = new ManualResetEventSlim(false);
        private ILogger _logger = LogManager.GetLogger<AzureQueryBus>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCommandBus" /> class.
        /// </summary>
        /// <param name="requestQueueConnectionString">The connection string as shown in the Azure management web</param>
        /// <param name="requestQueueName">Queue which queries are sent over. Name as you've configured it in the Azure management web.</param>
        /// <param name="resultQueueName">Queue which query results are sent back on. Name as you've configured it in the Azure management web. Dialogs must have been activated for this queue.</param>
        /// <remarks>
        /// <para>Dialogs must have been configured for the result queue. See class documentation.</para>
        /// <para>It's recommended that the queue timeouts are low (couple of seconds) if the clients are web users. Otherwise you'll get a lot of work done without anyone using the result (as web users can have gone to another page instead).</para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// requestQueueConnectionString
        /// or
        /// queueName
        /// </exception>
        public AzureQueryBus(string requestQueueConnectionString, string requestQueueName, string replyQueueConnectionString, string resultQueueName)
        {
            if (requestQueueConnectionString == null) throw new ArgumentNullException("requestQueueConnectionString");
            if (requestQueueName == null) throw new ArgumentNullException("requestQueueName");
            if (resultQueueName == null) throw new ArgumentNullException("resultQueueName");

            _sendQueueClient = QueueClient.CreateFromConnectionString(requestQueueConnectionString, requestQueueName);
            _readQueueClient = QueueClient.CreateFromConnectionString(replyQueueConnectionString, resultQueueName);
            _sessionId = Guid.NewGuid().ToString();
        }

        public void Start()
        {
            _readQueueClient.BeginAcceptMessageSession(_sessionId, OnMessageSession, null);
        }

        public void Stop()
        {
            _shutdown = true;
            _shutdownEvent.Wait();
        }

        private void OnMessageSession(IAsyncResult ar)
        {
            try
            {
                _logger.Write(LogLevel.Info, "Got message session " + _sessionId);
                _session = _readQueueClient.EndAcceptMessageSession(ar);
                _session.BeginReceive(OnMessage, null);
            }
            catch (Exception exception)
            {
                BusFailed(this, new ExceptionEventArgs(exception));
            }
        }

        private void OnMessage(IAsyncResult ar)
        {
            BrokeredMessage msg;
            try
            {
                msg = _session.EndReceive(ar);
                if (msg == null)
                {
                    _logger.Write(LogLevel.Info, "Got empty message.");
                    ReceiveMessage();
                    return;
                }
            }
            catch (Exception exception)
            {
                BusFailed(this, new ExceptionEventArgs(exception));
                return;
            }

            Guid queryId;
            try
            {
                var queryIdStr = (string)msg.ReplyTo;
                if (queryIdStr == null)
                {
                    BusFailed(this,
                        new ExceptionEventArgs(
                            new UnknownMessageException("Did not find the 'ReplyTo' in the broker message '" +
                                                        msg.ToString() +
                                                        "'. Does something else than this library use the configured queue?")));
                    ReceiveMessage();
                    return;
                }


                if (!Guid.TryParse(queryIdStr, out queryId))
                {
                    BusFailed(this,
                        new ExceptionEventArgs(
                            new UnknownMessageException("Failed to parse 'ReplyTo' as a Guid for msg '" +
                                                        msg.ToString() +
                                                        "'. Does something else than this library use the configured queue?")));
                    ReceiveMessage();
                    return;
                }

            }
            catch (Exception exception)
            {
                BusFailed(this, new ExceptionEventArgs(exception));
                ReceiveMessage();
                return;
            }

            ITaskHandler taskHandler = null;
            Type type;
            try
            {

                if (!_queue.TryRemove(queryId, out taskHandler))
                {
                    BusFailed(this,
                        new ExceptionEventArgs(
                            new UnknownMessageException("Failed to load task handler for query '" + queryId +
                                                        "'. Maybe it have been removed due to the timeout time?")));
                    ReceiveMessage();
                    return;
                }

                var typeName = (string) msg.Properties[MessageProperties.PayloadTypeName];
                if (typeName == null)
                {
                    taskHandler.SetException(
                        new UnknownMessageException(
                            "Did not find the 'PayloadTypeName' property in the broker message for query '" +
                            queryId + "'. Does something else than this library use the configured queue?"));

                    ReceiveMessage();
                    return;
                }

                type = Type.GetType(typeName, false);
                if (type == null)
                {
                    taskHandler.SetException(
                        new UnknownMessageException("Failed to load type '" + typeName +
                                                    "'. Have all assemblies been loaded?"));
                    ReceiveMessage();
                    return;
                }


                if (_serializerMethod == null)
                {
                    _serializerMethod = Serializer.Serializer.Instance.GetType().GetMethod("Deserialize");
                    if (_serializerMethod == null)
                    {
                        BusFailed(this,
                            new ExceptionEventArgs(
                                new UnknownMessageException(
                                    "Failed to identify the Deserialize method in Serializer.Instance. Strange.")));
                        ReceiveMessage();
                        return;
                    }
                }

                var genMethod = _serializerMethod.MakeGenericMethod(type);
                var queryResponse = genMethod.Invoke(Serializer.Serializer.Instance, new object[] {msg});
                if (queryResponse is Exception)
                    taskHandler.SetException((Exception) queryResponse);
                else
                    taskHandler.SetResult(queryResponse);
            }
            catch (Exception exception)
            {
                BusFailed(this, new ExceptionEventArgs(exception));
            }


            ReceiveMessage();
        }


        private void ReceiveMessage()
        {
            try
            {
                if (_shutdown)
                {
                    _shutdownEvent.Set();
                    return;
                }

                _session.BeginReceive(OnMessage, null);
            }
            catch (Exception exception)
            {
                BusFailed(this, new ExceptionEventArgs(exception));
            }
        }

        /// <summary>
        /// Invoke a query and wait for the result
        /// </summary>
        /// <typeparam name="TResult">Type of result that the query will return</typeparam>
        /// <param name="query">Query to execute.</param>
        /// <returns>Task which will complete once we've got the result (or something failed, like a query wait timeout).</returns>
        /// <exception cref="ArgumentNullException">query</exception>
        public async Task<TResult> QueryAsync<TResult>(Query<TResult> query)
        {
            if (query == null) throw new ArgumentNullException("query");

            var msg = Serializer.Serializer.Instance.Serialize(query);
            msg.MessageId = query.QueryId.ToString();
            msg.Properties[MessageProperties.PayloadTypeName] = query.GetType().AssemblyQualifiedName;
            msg.ReplyToSessionId = _sessionId;

            await _sendQueueClient.SendAsync(msg);

            var tcs = new TaskCompletionSource<TResult>(msg);
            _queue.TryAdd(query.QueryId, new TaskWrapper<TResult>(tcs));
            await tcs.Task;
            return tcs.Task.Result;
        }


        /// <summary>
        ///     Invoked every time an exception is thrown for the query bus which is not related to a specific query.
        /// </summary>
        /// <remarks>
        /// <para>this event typically means that the bus is in an inconsistent state. You probably have to recreate it.</para>
        /// </remarks>
        public event EventHandler<ExceptionEventArgs> BusFailed = delegate { };

/// <summary>
/// Closes both queues and closes the session.
/// </summary>
        public void Dispose()
        {
            if (_session != null)
            {
            _session.Close();
                _session = null;
            }

            if (_sendQueueClient != null)
            {
                _sendQueueClient.Close();
                _sendQueueClient = null;
            }

            if (_readQueueClient != null)
            {
                _readQueueClient.Close();
                _readQueueClient = null;
            }
            
        }
    }
}