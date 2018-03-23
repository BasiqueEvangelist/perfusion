using System;
using System.Runtime.Serialization;

namespace Perfusion
{
    [System.Serializable]
    public class PerfusionException : Exception
    {
        public PerfusionException() { }
        public PerfusionException(string message) : base(message) { }
        public PerfusionException(string message, Exception inner) : base(message, inner) { }
        protected PerfusionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}