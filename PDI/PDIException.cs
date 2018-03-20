using System;
using System.Runtime.Serialization;

namespace PDI
{
    [System.Serializable]
    public class PDIException : Exception
    {
        public PDIException() { }
        public PDIException(string message) : base(message) { }
        public PDIException(string message, Exception inner) : base(message, inner) { }
        protected PDIException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}