using System;
using System.Runtime.Serialization;

namespace WindowsAzure.ServiceBus.Cqs
{
    /// <summary>
    ///     No handler was found for the specified message type (command, application event, query or request/reply).
    /// </summary>
    [Serializable]
    public class NoHandlerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoHandlerException" /> class.
        /// </summary>
        /// <param name="messageType">Message that we did not find a handler for.</param>
        /// <exception cref="System.ArgumentNullException">MessageTypeName</exception>
        public NoHandlerException(Type messageType)
            : base(string.Format("Message '{0}' do not have a registered handler.", messageType.FullName))
        {
            if (messageType == null) throw new ArgumentNullException("messageType");
            MessageTypeName = messageType.FullName;
        }

         /// <summary>
        /// Initializes a new instance of the <see cref="NoHandlerException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected NoHandlerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            MessageTypeName = info.GetString("MessageTypeName");
        }


        /// <summary>
        ///     Type.FullName of the message that we did not find a handler for.
        /// </summary>
        public string MessageTypeName { get; private set; }

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
            info.AddValue("MessageTypeName", MessageTypeName);
        }
    }
}