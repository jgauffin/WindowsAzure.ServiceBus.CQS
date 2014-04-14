using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WindowsAzure.ServiceBus.Cqs.InversionOfControl;
using WindowsAzure.ServiceBus.Cqs.Logging;
using DotNetCqs;
using Microsoft.ServiceBus.Messaging;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    /// The server side part of the query bus.
    /// </summary>
    public class AzureQueryBusListener
    {
        private readonly IContainer _container;
        private QueueClient _responseQueue;
        private QueueClient _requestQueue;
        private MethodInfo _serializerMethod;
        private volatile bool _shutdown = false;
        private ManualResetEventSlim _shutdownEvent = new ManualResetEventSlim(false);
        private MethodInfo _queryHandlerMethod;
        private ILogger _logger = LogManager.GetLogger<AzureQueryBusListener>();
        private Action<IChildContainer> _successTask;


        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCommandBus" /> class.
        /// </summary>
        /// <param name="requestQueueConnectionString">The connection string as shown in the Azure management web</param>
        /// <param name="requestQueueName">Queue which queries are sent over. i.e. the queue that this class is going to read from. Name as specified in the Azure management web.</param>
        /// <param name="replyConnectionString"></param>
        /// <param name="replyQueueName">Queue which query results are sent back on. i.e. the queue that this class is going to send responses on. Name as specified in the Azure management web. Dialogs must have been activated for this queue.</param>
        /// <param name="container"></param>
        /// <remarks>
        /// <para>Dialogs must have been configured for the result queue. See class documentation.</para>
        /// <para>It's recommended that the queue timeouts are low (couple of seconds) if the clients are web users. Otherwise you'll get a lot of work done without anyone using the result (as web users can have gone to another page instead).</para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// requestQueueConnectionString
        /// or
        /// queueName
        /// </exception>
        public AzureQueryBusListener(string requestQueueConnectionString, string requestQueueName, string replyConnectionString, string replyQueueName, IContainer container)
        {
            _container = container;
            if (requestQueueConnectionString == null) throw new ArgumentNullException("requestQueueConnectionString");
            if (requestQueueName == null) throw new ArgumentNullException("requestQueueName");
            if (replyQueueName == null) throw new ArgumentNullException("replyQueueName");
            _responseQueue = QueueClient.CreateFromConnectionString(replyConnectionString, replyQueueName);
            _requestQueue = QueueClient.CreateFromConnectionString(requestQueueConnectionString, requestQueueName);
            _queryHandlerMethod = GetType().GetMethod("InvokeHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            SuccessTask = childContainer => { };
        }

        public void Start()
        {
            _requestQueue.BeginReceive(OnMessage, null);
        }

        public void Stop()
        {
            _shutdown = true;
            _shutdownEvent.Wait();
        }

        /// <summary>
        /// Task to invoke when a query have been handled successfully (like resolve and commit an unit of work).
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// bus.SuccessTask = container => { container.Resolve<IUnitOfWork>().SaveChanges(); };
        /// ]]>
        /// </code>
        /// </example>
        public Action<IChildContainer> SuccessTask
        {
            get { return _successTask; }
            set
            {
                if (value == null)
                    _successTask = x => { };
                else
                    _successTask = value;
            }
        }

        private void OnMessage(IAsyncResult ar)
        {
            BrokeredMessage msg;
            try
            {
                msg = _requestQueue.EndReceive(ar);
                if (msg == null)
                {
                    _logger.Write(LogLevel.Info, "Received empty message.");
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

            Guid queryId;
            try
            {
                var queryIdStr = msg.MessageId;
                if (queryIdStr == null)
                {
                    throw new UnknownMessageException("Did not find the 'MessageId' for the broker message '" +
                                                        msg.ToString() +
                                                        "'. Does something else than this library use the configured queue?");
                }

                if (!Guid.TryParse(queryIdStr, out queryId))
                {
                    throw new UnknownMessageException("Failed to parse 'MessageId' as a Guid for msg '" +
                                                        msg.ToString() +
                                                        "'. Does something else than this library use the configured queue?");
                }

            }
            catch (Exception exception)
            {
                _logger.Write(LogLevel.Error, "Failed to find QueryId for " + msg, exception);
                BusFailed(this, new ExceptionEventArgs(exception));
                ReceiveMessage();
                return;
            }

            try
            {
                var typeName = (string) msg.Properties[MessageProperties.PayloadTypeName];
                if (typeName == null)
                {
                    throw new UnknownMessageException(
                        "Did not find the 'PayloadTypeName' property in the broker message for query '" +
                        queryId + "'. Does something else than this library use the configured queue?");
                }

                var type = Type.GetType(typeName, false);
                if (type == null)
                {
                    throw new UnknownMessageException("Failed to load type '" + typeName +
                                                    "'. Have all assemblies been loaded?");
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
                var query = genMethod.Invoke(Serializer.Serializer.Instance, new object[] { msg });


                var method = _queryHandlerMethod.MakeGenericMethod(type, type.BaseType.GenericTypeArguments[0]);
                var response = method.Invoke(this, new object[] {query});
                Reply(msg.ReplyToSessionId, queryId, response);
                msg.Complete();
                _logger.Write(LogLevel.Info, "Replied to " + msg.MessageId);
            }
            catch (Exception exception)
            {
                _logger.Write(LogLevel.Error, "Failed to process " + msg, exception);
                Reply(msg.ReplyToSessionId, queryId, exception);
            }


            ReceiveMessage();
        }

        private void Reply(string sessionId, Guid queryId, object reply)
        {
            var msg = Serializer.Serializer.Instance.Serialize(reply);
            _logger.Write(LogLevel.Info, "Sending reply to query " + queryId + " on session " + sessionId + ": " + msg);
            msg.ReplyTo = queryId.ToString();
            msg.SessionId = sessionId;
            msg.Properties[MessageProperties.PayloadTypeName] = reply.GetType().AssemblyQualifiedName;
            _responseQueue.Send(msg);
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

                _requestQueue.BeginReceive(OnMessage, null);
            }
            catch (Exception exception)
            {
                _logger.Write(LogLevel.Error, "Failed to invoke BeginReceive.", exception);
                BusFailed(this, new ExceptionEventArgs(exception));
            }
        }

        protected TResult InvokeHandler<T, TResult>(T query) where T : Query<TResult>
        {
            Task<TResult> resultTask;
            using (var scope = _container.CreateChildContainer())
            {
                var handlers = scope.ResolveAll<IQueryHandler<T, TResult>>().ToList();
                if (handlers.Count == 0)
                {
                    throw new NoHandlerException(typeof(T));
                }
                if (handlers.Count > 1)
                {
                    throw new MultipleHandlersException(typeof(T), handlers.Select(x => x.GetType()));
                }

                resultTask = handlers[0].ExecuteAsync(query);
                resultTask.Wait();

                SuccessTask(scope);
            }

            return resultTask.Result;
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
            if (_responseQueue != null)
            {
                _responseQueue.Close();
                _responseQueue = null;
            }

            if (_requestQueue != null)
            {
                _requestQueue.Close();
                _requestQueue = null;
            }
            
        }
    }
}