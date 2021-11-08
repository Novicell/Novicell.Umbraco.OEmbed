using System;
using System.Runtime.Serialization;

namespace Novicell.Umbraco.OEmbed.Core
{
    public class OEmbedException : Exception
    {
        protected OEmbedException()
        {
        }

        protected OEmbedException(string message)
            : base(message)
        {
        }

        protected OEmbedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected OEmbedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}