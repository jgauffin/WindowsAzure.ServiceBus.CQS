using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WindowsAzure.ServiceBus.Cqs.InversionOfControl;
using WindowsAzure.ServiceBus.Cqs.Logging;
using DotNetCqs;
using Microsoft.ServiceBus.Messaging;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    ///     Used to receive command bus messages.
    /// </summary>
    /// <remarks>
    /// <para>Creates a new scoped container for every received command.</para>
    /// </remarks>
    public class AzureEventBusListener
    {
        private readonly IContainer _container;
        private readonly MethodInfo _genericMethod;
        private readonly QueueClient _queueClient;
        private bool _isStarted = false;
        private ILogger _logger = LogManager.GetLogger<AzureCommandBusListener>();
        private Action<IChildContainer> _successTask;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureEventBusListener" /> class.
        /// </summary>
        /// <param name="connectionString">Connection string as returned from the Azure management web.</param>
        /// <param name="queueName">Name of the queue (name from the Azure management web).</param>
        /// <param name="container">Inversion of control container to use to identify command handlers.</param>
        public AzureEventBusListener(string connectionString, string queueName, IContainer container)
        {
            _container = container;
            _queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName, ReceiveMode.PeekLock);
            _genericMethod = GetType()
                .GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] {typeof (BrokeredMessage)}, null);

            SuccessTask = childContainer => { };
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


        /// <summary>
        ///     Begin listening for messages.
        /// </summary>
        public void Start()
        {
            if (_isStarted)
                throw new InvalidOperationException("AzureEventBusListener have already been started.");
            _isStarted = true;

            _logger.Write(LogLevel.Info, "Starting");
            _queueClient.BeginReceive(OnMessage, null);
        }

        /// <summary>
        ///     Stop listening for messages.
        /// </summary>
        public void Stop()
        {
            if (!_isStarted)
                throw new InvalidOperationException("AzureEventBusListener is not running.");

            _logger.Write(LogLevel.Info, "Stopping");
            _isStarted = false;
            _queueClient.Close();
        }

        private void OnMessage(IAsyncResult ar)
        {
            BrokeredMessage brokeredMessage = null;
            try
            {
                brokeredMessage = _queueClient.EndReceive(ar);
                if (brokeredMessage == null)
                {
                    ReceiveMessage(brokeredMessage);
                    return;
                }

                _logger.Write(LogLevel.Info, "Got message: " + brokeredMessage);
                var typeName = (string)brokeredMessage.Properties[MessageProperties.PayloadTypeName];
                if (!CheckTypeName(typeName, brokeredMessage))
                    return;


                var type = Type.GetType(typeName, false);
                if (!CheckMessageType(type, brokeredMessage, typeName))
                    return;

                var method = _genericMethod.MakeGenericMethod(type);
                method.Invoke(this, new object[] {brokeredMessage});

                brokeredMessage.Complete();
            }
            catch (Exception exception)
            {
                _logger.Write(LogLevel.Error, "Failed to process message " + brokeredMessage, exception);

                var targetInvoke = exception as TargetInvocationException;
                if (targetInvoke != null)
                {
                    exception = exception.InnerException;
                }

                var e = new BusMessageErrorEventArgs(brokeredMessage, exception);
                BusFailed(this, e);
                if (brokeredMessage != null)
                {
                    if (e.MessageTask == MessageHandling.RemoveMessage)
                    {
                        brokeredMessage.Complete();
                    }
                    else
                    {
                        brokeredMessage.Abandon();
                    }
                }
            }

            ReceiveMessage(brokeredMessage);
        }

        private void ReceiveMessage(BrokeredMessage brokeredMessage)
        {
            try
            {
                _queueClient.BeginReceive(OnMessage, null);
            }
            catch (Exception exception)
            {
                _logger.Write(LogLevel.Error, "Failed to start BeginReceive again.", exception);
                var e = new BusMessageErrorEventArgs(brokeredMessage, new FatalBusException(exception));
                BusFailed(this, e);

            }
        }

        private bool CheckMessageType(Type type, BrokeredMessage msg, string typeName)
        {
            if (type != null)
                return true;

            var e = new BusMessageErrorEventArgs(msg,
                new UnknownMessageException(
                    "Failed to load the Type object for '" + typeName + "'."));

            BusFailed(this, e);
            if (e.MessageTask == MessageHandling.PutMessageBackInQueue)
                msg.Abandon();
            else
                msg.Complete();

            return false;
        }

        private bool CheckTypeName(string typeName, BrokeredMessage msg)
        {
            if (typeName != null)
                return true;


            _logger.Write(LogLevel.Error, "Received message is not a AzureCommandBus message ('PayloadTypeName' property is missing).");

            var e = new BusMessageErrorEventArgs(msg,
                new UnknownMessageException(
                    "Received message is not a AzureCommandBus message ('PayloadTypeName' property is missing)."));

            BusFailed(this, e);
            if (e.MessageTask == MessageHandling.PutMessageBackInQueue)
                msg.Abandon();
            else
                msg.Complete();

            return false;
        }

        private async Task Execute<T>(BrokeredMessage msg) where T : ApplicationEvent
        {
            var appEvent = Serializer.Serializer.Instance.Deserialize<T>(msg);
            _logger.Write(LogLevel.Debug, "Received event: " + DebugSerializer.Serialize(appEvent));


            using (var scope = _container.CreateChildContainer())
            {
                var handlers = (
                    from handler in scope.ResolveAll<IApplicationEventSubscriber<T>>()
                    select handler.HandleAsync(appEvent)
                    ).ToArray();

                await Task.WhenAll(handlers);

                SuccessTask(scope);
            }
        }

        /// <summary>
        ///     Invoked every time an exception is thrown for the command bus.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Make sure that you specify <see cref="BusMessageErrorEventArgs.MessageTask" />.
        ///     </para>
        ///     <para>
        ///         Use the event to decide what to do with the message. You can put it back in the queue or mark it as completed
        ///         to remove it. What that actually means is
        ///         something you have to read about in the Azure documentation.
        ///     </para>
        ///     <para>
        ///         The exception <see cref="FatalBusException" /> should be treated differently from all other exceptions
        ///         since it means that the command bus
        ///         is broken. Typically because of failure of the underlying queue. Check it's inner exception for more
        ///         information.
        ///     </para>
        /// </remarks>
        public event EventHandler<BusMessageErrorEventArgs> BusFailed = delegate { };
    }
}