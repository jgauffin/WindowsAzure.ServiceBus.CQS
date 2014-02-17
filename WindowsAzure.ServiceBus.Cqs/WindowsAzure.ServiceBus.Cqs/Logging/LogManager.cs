using System;

namespace WindowsAzure.ServiceBus.Cqs.Logging
{
    public class LogManager
    {
        private static Func<Type, ILogger> _factory = x => new NullLogger();

        public static void Assign(Func<Type, ILogger> logFactory)
        {
            _factory = logFactory;
        }

        public static ILogger GetLogger(Type type)
        {
            return _factory(type);
        }

        public static ILogger GetLogger<T>()
        {
            return _factory(typeof(T));
        }
    }
}
