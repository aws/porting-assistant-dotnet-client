using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortingAssistant.Compatibility.Common.Model
{
    public enum CodeEntityType
    {
        Namespace,
        Class,
        Method,
        InstanceAttribute,
        ClassAttribute,
        Annotation,
        Declaration,
        Using,
        Enum,
        Struct
    }
}
