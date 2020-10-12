using System;

namespace Marten.Exceptions
{
    public class AmbiguousDocumentTypeAliasesException: Exception
    {
        public AmbiguousDocumentTypeAliasesException(string message) : base(message)
        {
        }

#if SERIALIZE
        protected AmbiguousDocumentTypeAliasesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
