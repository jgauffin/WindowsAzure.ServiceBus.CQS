using System;

namespace WindowsAzure.ServiceBus.Cqs.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly Type _type;

        public ConsoleLogger(Type type)
        {
            _type = type;
        }

        public void Write(LogLevel level, string message)
        {
            Write(level, message, null);
        }

        public void Write(LogLevel level, string message, Exception exception)
        {
            switch (level)
            {
                    case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;

                    case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + _type.Name.PadLeft(20) + " " + message);
            if (exception != null)
            {
                Console.WriteLine("\t\t" + exception.ToString().Replace("\r\n", "\t\t\r\n"));
            }
        }
    }
}
