using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    ///     We've find multiple handlers for a message type where only one handler is expected (like commands and queries).
    /// </summary>
    /// <remarks>
    ///     <para>Thrown when we've found multiple handlers for the same message type.</para>
    /// </remarks>
    public class MultipleHandlersException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MultipleHandlersException" /> class.
        /// </summary>
        /// <param name="messageType">Message being handled.</param>
        /// <param name="handlers">All found handlers.</param>
        public MultipleHandlersException(Type messageType, IEnumerable<Type> handlers)
            : base(
                messageType.FullName + " has the following handlers: " +
                string.Join(",", handlers.Select(x => x.FullName)))
        {
            MessageType = messageType.FullName;
            Handlers = handlers.Select(x=>x.FullName).ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MyException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected MultipleHandlersException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            MessageType = info.GetString("MessageType");
            Handlers = (string[]) info.GetValue("Handlers", typeof (string[]));
        }

        /// <summary>
        ///     Message being handled
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        ///     All found handlers.
        /// </summary>
        public string[] Handlers { get; set; }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter" />
        ///   </PermissionSet>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("MessageType", MessageType);
            info.AddValue("Handlers", Handlers);
        }
    }

}