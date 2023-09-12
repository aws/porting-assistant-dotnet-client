using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model.Exception
{
    public class PortingAssistantClientException : System.Exception
    {
        public PortingAssistantClientException(string message, System.Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
