using System;
using System.Runtime.Serialization;

namespace Novicell.Umbraco.OEmbed
{
    public class OEmbedException : Exception
    {
        public OEmbedException()
        {
            
        }

        public OEmbedException(string message)
            : base(message)
        {
        }

        public OEmbedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected OEmbedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}