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
    ///     Used to handle queries over azure.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This implementation uses queue dialogs to identify queries for specific clients. You must therefore have
    ///         activated dialogs on the queues that you've configured for this bus.
    ///     </para>
    /// </remarks>
    public class AzureRequestReplyBus : IRequestReplyBus, IDisposable
    {
        private ILogger _logger = LogManager.GetLogger<AzureRequestReplyBus>();
        private ConcurrentDictionary<Guid, ITaskHandler> _queue = new ConcurrentDictionary<Guid, ITaskHandler>();
        private QueueClient _readQueueClient;
        private QueueClient _sendQueueClient;
        private MethodInfo _serializerMethod;
        private MessageSession _session;
        private string _sessionId;
        private volatile bool _shutdown = false;
        private ManualResetEventSlim _shutdownEvent = new ManualResetEventSlim(false);

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureCommandBus" /> class.
        /// </summary>
        /// <param name="requestQueueConnectionString">The connection string as shown in the Azure management web</param>
        /// <param name="requestQueueName">
        ///     Queue which queries are sent over. Name as you've configured it in the Azure management
        ///     web.
        /// </param>
        /// <param name="replyQueueName">
        ///     Queue which query results are sent back on. Name as you've configured it in the Azure
        ///     management web. Dialogs must have been activated for this queue.
        /// </param>
        /// <remarks>
        ///     <para>Dialogs must have been configured for the result queue. See class documentation.</para>
        ///     <para>
        ///         It's recommended that the queue timeouts are low (couple of seconds) if the clients are web users. Otherwise
        ///         you'll get a lot of work done without anyone using the result (as web users can have gone to another page
        ///         instead).
        ///     </para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        ///     requestQueueConnectionString
        ///     or
        ///     queueName
        /// </exception>
        public AzureRequestReplyBus(string requestQueueConnectionString, string requestQueueName,
            string replyQueueConnectionString, string replyQueueName)
        {
            if (requestQueueConnectionString == null) throw new ArgumentNullException("requestQueueConnectionString");
            if (requestQueueName == null) throw new ArgumentNullException("requestQueueName");
            if (replyQueueName == null) throw new ArgumentNullException("replyQueueName");

            _sendQueueClient = QueueClient.CreateFromConnectionString(requestQueueConnectionString, requestQueueName);
            _readQueueClient = QueueClient.CreateFromConnectionString(replyQueueConnectionString, replyQueueName);
            _sessionId = Guid.NewGuid().ToString();
            TimeToLive = TimeSpan.FromDays(1);
        }

        /// <summary>
        ///     How long messages can live on the queue
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        /// <summary>
        ///     Closes both queues and closes the session.
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

        /// <summary>
        ///     Invoke a query and wait for the result
        /// </summary>
        /// <typeparam name="TReply">Type of result that the query will return</typeparam>
        /// <param name="request">Query to execute.</param>
        /// <returns>Task which will complete once we've got the result (or something failed, like a query wait timeout).</returns>
        /// <exception cref="ArgumentNullException">query</exception>
        public async Task<TReply> ExecuteAsync<TReply>(Request<TReply> request)
        {
            if (request == null) throw new ArgumentNullException("request");

            var msg = Serializer.Serializer.Instance.Serialize(request);
            msg.MessageId = request.RequestId.ToString();
            msg.Properties[MessageProperties.PayloadTypeName] = request.GetType().AssemblyQualifiedName;
            msg.ReplyToSessionId = _sessionId;
            if (TimeToLive != TimeSpan.Zero)
                msg.TimeToLive = TimeToLive;
            await _sendQueueClient.SendAsync(msg);

            var tcs = new TaskCompletionSource<TReply>(msg);

            _queue.TryAdd(request.RequestId, new TaskWrapper<TReply>(tcs));
            await tcs.Task;
            return tcs.Task.Result;
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
                _session = _readQueueClient.EndAcceptMessageSession(ar);
                _logger.Write(LogLevel.Info, "Are not listening on session " + _sessionId);
                _session.BeginReceive(OnMessage, null);
            }
            catch (Exception exception)
            {
                _logger.Write(LogLevel.Error, "Failed to EndAcceptMessageSession " + _sessionId, exception);
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
                    _logger.Write(LogLevel.Debug, "Received null message (i.e. the Azure ServiceBus library completed the async op without receiving a message)");
                    ReceiveMessage();
                    return;
                }
            }
            catch (Exception exception)
            {
                _logger.Write(LogLevel.Error, "Failed to EndReceive in " + _sessionId, exception);
                BusFailed(this, new ExceptionEventArgs(exception));
                return;
            }

            Guid requestId;
            ITaskHandler taskHandler;
            try
            {
                var requestIdStr = (string) msg.ReplyTo;
                if (requestIdStr == null)
                {
                    throw new UnknownMessageException(
                        "Did not find the 'ReplyTo' property in the broker message '" +
                        msg.ToString() +
                        "'. Does something else than this library use the configured queue?");
                }

                if (!Guid.TryParse(requestIdStr, out requestId))
                {
                    throw new UnknownMessageException("Failed to parse 'ReplyTo' as a Guid for msg '" +
                                                      msg.ToString() +
                                                      "'. Does something else than this library use the configured queue?");
                }

                if (!_queue.TryRemove(requestId, out taskHandler))
                {
                    throw new UnknownMessageException("Failed to load task handler for request '" + requestId +
                                                      "'. Maybe it have been removed due to the timeout time?");
                }
            }
            catch (Exception exception)
            {
                BusFailed(this, new ExceptionEventArgs(exception));
                ReceiveMessage();
                return;
            }


            try
            {
                var typeName = (string) msg.Properties[MessageProperties.PayloadTypeName];
                if (typeName == null)
                {
                    taskHandler.SetException(
                        new UnknownMessageException(
                            "Did not find the 'PayloadTypeName' property in the broker message for request '" +
                            requestId + "'. Does something else than this library use the configured queue?"));

                    ReceiveMessage();
                    return;
                }

                var type = Type.GetType(typeName, false);
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
                        throw new UnknownMessageException(
                            "Failed to identify the Deserialize method in Serializer.Instance. Strange.");
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
                _logger.Write(LogLevel.Error, "Failed to receive message on session " + _sessionId, exception);
                BusFailed(this, new ExceptionEventArgs(exception));
            }
        }


        /// <summary>
        ///     Invoked every time an exception is thrown for the query bus which is not related to a specific query.
        /// </summary>
        /// <remarks>
        ///     <para>this event typically means that the bus is in an inconsistent state. You probably have to recreate it.</para>
        /// </remarks>
        public event EventHandler<ExceptionEventArgs> BusFailed = delegate { };
    }
}