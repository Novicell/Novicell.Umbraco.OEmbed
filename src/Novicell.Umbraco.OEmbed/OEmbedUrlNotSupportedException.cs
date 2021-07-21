using System;
using System.Runtime.Serialization;

namespace Novicell.Umbraco.OEmbed
{
    public class OEmbedUrlNotSupportedException : OEmbedException
    {
        public OEmbedUrlNotSupportedException()
        {
            
        }

        public OEmbedUrlNotSupportedException(string message) 
            : base(message)
        {
        }

        public OEmbedUrlNotSupportedException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        protected OEmbedUrlNotSupportedException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}