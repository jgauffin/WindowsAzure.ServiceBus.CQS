using System;

namespace WindowsAzure.ServiceBus.Cqs.Logging
{
    public class NullLogger : ILogger
    {
        public void Write(LogLevel level, string message)
        {
        }

        public void Write(LogLevel level, string message, Exception exception)
        {
        }
    }
}