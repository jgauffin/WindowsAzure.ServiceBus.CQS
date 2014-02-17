using WindowsAzure.ServiceBus.Cqs.Configuration;
using WindowsAzure.ServiceBus.Cqs.InversionOfControl;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    /// Class which makes it easier to create the different services that this library provides.
    /// </summary>
    public class AzureCqsFactory
    {
        private ISettingsProvider _appSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCqsFactory"/> class.
        /// </summary>
        public AzureCqsFactory()
        {
            _appSettings = new CloudConfigProvider();
        }

        /// <summary>
        /// Used to provide settings for this class.
        /// </summary>
        /// <value>Default is <see cref="CloudConfigProvider"/></value>
        public ISettingsProvider SettingsProvider { get { return _appSettings; } set { _appSettings = value; } }

        public AzureEventBus CreateEventBus()
        {
            var appQueueConnection = _appSettings.GetAppSetting("EventBus.ConnectionString");
            var appQueue = _appSettings.GetAppSetting("EventBus.QueueName");
            return new AzureEventBus(appQueueConnection, appQueue);
        }

        public AzureCommandBus CreateCommandBus()
        {
            var commandQueue = _appSettings.GetAppSetting("CommandBus.QueueName");
            var commandConnection = _appSettings.GetAppSetting("CommandBus.ConnectionString");
            var cmd= new AzureCommandBus(commandConnection, commandQueue);
            return cmd;
        }

        public AzureQueryBus CreateQueryBus()
        {
            var requestQueue = _appSettings.GetAppSetting("QueryBus.Request.QueueName");
            var requestConnectionString = _appSettings.GetAppSetting("QueryBus.Request.ConnectionString");
            var replyQueue = _appSettings.GetAppSetting("QueryBus.Response.QueueName");
            var replyConnectionString = _appSettings.GetAppSetting("QueryBus.Response.ConnectionString");
            var query= new AzureQueryBus(requestConnectionString, requestQueue, replyConnectionString,
                replyQueue);
            query.Start();
            return query;
        }

        public AzureRequestReplyBus CreateRequestReplyBus()
        {
            var requestQueue = _appSettings.GetAppSetting("RequestReplyBus.Request.QueueName");
            var requestConnectionString = _appSettings.GetAppSetting("RequestReplyBus.Request.ConnectionString");
            var replyQueue = _appSettings.GetAppSetting("RequestReplyBus.Reply.QueueName");
            var replyConnectionString = _appSettings.GetAppSetting("RequestReplyBus.Reply.ConnectionString");
            var req= new AzureRequestReplyBus(requestConnectionString, requestQueue, replyConnectionString,
                replyQueue);
            req.Start();

            return req;
        }

        public AzureCommandBusListener CreateCommandBusListener(IContainer container)
        {
            var commandQueue = _appSettings.GetAppSetting("CommandBus.QueueName");
            var commandConnection = _appSettings.GetAppSetting("CommandBus.ConnectionString");
            return new AzureCommandBusListener(commandConnection, commandQueue, container);
        }

        public AzureQueryBusListener CreateQueryBusListener(IContainer container)
        {
            var requestQueue = _appSettings.GetAppSetting("QueryBus.Request.QueueName");
            var requestConnectionString = _appSettings.GetAppSetting("QueryBus.Request.ConnectionString");
            var replyQueue = _appSettings.GetAppSetting("QueryBus.Response.QueueName");
            var replyConnectionString = _appSettings.GetAppSetting("QueryBus.Response.ConnectionString");
            return new AzureQueryBusListener(requestConnectionString, requestQueue, replyConnectionString, replyQueue,
                container);
        }

        public AzureEventBusListener CreateEventBusListener(IContainer container)
        {
            var appQueueConnection = _appSettings.GetAppSetting("EventBus.ConnectionString");
            var appQueue = _appSettings.GetAppSetting("EventBus.QueueName");
            return new AzureEventBusListener(appQueueConnection, appQueue, container);
        }

        public AzureRequestReplyBusListener CreateRequestReplyBusListener(IContainer container)
        {
            var requestQueue = _appSettings.GetAppSetting("RequestReplyBus.Request.QueueName");
            var requestConnectionString = _appSettings.GetAppSetting("RequestReplyBus.Request.ConnectionString");
            var replyQueue = _appSettings.GetAppSetting("RequestReplyBus.Reply.QueueName");
            var replyConnectionString = _appSettings.GetAppSetting("RequestReplyBus.Reply.ConnectionString");
            return new AzureRequestReplyBusListener(requestConnectionString, requestQueue, replyConnectionString,
                replyQueue, container);
        }
    }
}