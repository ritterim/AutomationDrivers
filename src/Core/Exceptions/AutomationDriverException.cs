using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AutomationDrivers.Core.Exceptions
{
    public class AutomationDriverException : System.Exception, ISerializable
    {
        public AutomationDriverException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AutomationDriverException(string message, params object[] formatParams)
            : base(string.Format(message, formatParams))
        {
            PreserveStackTrace(this);
        }

        public AutomationDriverException(string message, Exception innerException, params object[] formatParams)
            : base(string.Format(message, formatParams), innerException)
        {
            PreserveStackTrace(this);
        }

        public override string StackTrace
        {
            get
            {
                var stackTraceLines = base.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(s => !s.TrimStart(' ').StartsWith("at " + this.GetType().Namespace));
                return string.Join(Environment.NewLine, stackTraceLines);
            }
        }

        public string ScreenShotPath { get; set; }

        private static void PreserveStackTrace(Exception e)
        {
            var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
            var mgr = new ObjectManager(null, ctx);
            var si = new SerializationInfo(e.GetType(), new FormatterConverter());

            e.GetObjectData(si, ctx);
            mgr.RegisterObject(e, 1, si);
            mgr.DoFixups();
        }
    }
}
