using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AutomationDrivers.Core.Exceptions
{
    public class AutomationExpectedFailureException : AutomationFailedException
    {

        public AutomationExpectedFailureException(string message, params object[] formatParams)
            : base(message, formatParams)
        {
        }

        public AutomationExpectedFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    public class AutomationFailedException : AutomationDriverException
    {
        public AutomationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AutomationFailedException(string message, params object[] formatParams)
            : base(string.Format(message, formatParams))
        {
        }
    }
}
