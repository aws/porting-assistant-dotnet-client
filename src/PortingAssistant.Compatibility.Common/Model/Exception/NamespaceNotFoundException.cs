using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model.Exception
{
    public class NamespaceNotFoundException : System.Exception
    {
        public NamespaceNotFoundException(string message) :
            base(message)
        {
        }
    }
}
