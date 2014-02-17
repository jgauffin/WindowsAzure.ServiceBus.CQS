using System;

namespace WindowsAzure.ServiceBus.Cqs
{
    public interface ITaskHandler
    {
        void SetResult(object result);
        void SetException(Exception exception);
    }
}