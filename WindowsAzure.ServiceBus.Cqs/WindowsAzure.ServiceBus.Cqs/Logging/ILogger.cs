using System;

namespace WindowsAzure.ServiceBus.Cqs.Logging
{
    public interface ILogger
    {
        void Write(LogLevel level, string message);
        void Write(LogLevel level, string message, Exception exception);
    }
}