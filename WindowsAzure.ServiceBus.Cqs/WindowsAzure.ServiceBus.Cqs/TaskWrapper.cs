using System;
using System.Threading.Tasks;

namespace WindowsAzure.ServiceBus.Cqs
{
    internal class TaskWrapper<TResult> : ITaskHandler
    {
        private readonly TaskCompletionSource<TResult> _task;
        private DateTime _addedAt;

        public TaskWrapper(TaskCompletionSource<TResult> task)
        {
            _task = task;
            _addedAt = DateTime.UtcNow;
        }

        public bool IsExpired
        {
            get { return DateTime.UtcNow.Subtract(_addedAt) > TimeSpan.FromSeconds(60); }
        }

        public void SetResult(object result)
        {
            _task.SetResult((TResult) result);
        }

        public void SetException(Exception exception)
        {
            _task.SetException(exception);
        }
    }
}