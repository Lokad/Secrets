using System;
using System.Runtime.Serialization;

namespace Lokad.Secrets
{
    /// <summary> Exception thrown when a secret cannot be retrieved. </summary>
    public class SecretNotResolvedException : Exception
    {
        public SecretNotResolvedException()
        {
        }

        public SecretNotResolvedException(string message) : base(message)
        {
        }

        public SecretNotResolvedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SecretNotResolvedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
