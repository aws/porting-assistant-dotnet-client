using Mono.Cecil;

namespace PortingAssistant.Client.DllAssessment.AssemblyCompatibility.Extensions;

public static class MethodDefinitionExtensions
{
    private readonly ISet<string> UnsupportedPlatformExceptions = new HashSet<string>
    {
        "System.Void System.PlatformNotSupportedException::.ctor()",
        "System.Void System.NotSupportedException::.ctor(System.String)"
    };
    
    public static bool DoesThrowPlatformNotSupportedException(this MethodDefinition method)
    {
        foreach (var op in GetOperationsPreceedingThrow(method))
        {
            // Check if throws new PlatformNotSupportedExeption(...)
            if (op.OpCode.Code == Code.Newobj 
                && op.Operand is MethodReference m 
                && UnsupportedPlatformExceptions.Contains(op.Operand.ToString()))
            {
                return true;
            }

            // Check if throws SomeFactoryForPlatformNotSupportedExeption(...)
            if (op.Operand is MethodReference r 
                && IsFactoryForPlatformNotSupported(r))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsLinuxCompatibleBasedOnAttributes(this MethodDefinition method)
    {
        // Check if it throws
        // Check attributes
        const string supportedOsPlatformAttribute = "SupportedOSPlatformAttribute";
        const string unsupportedOsPlatformAttribute = "UnsupportedOSPlatformAttribute";
        const string linuxOsPlatform = "linux";
        if (!method.HasCustomAttributes)
        {
            return true;
        }

        var attrs = method.CustomAttributes;
        return attrs.Any(a =>
        {
            // If there is a platform inclusion list and linux is not included, return false
            if (a.AttributeType.Name == supportedOsPlatformAttribute
                && a.ConstructorArguments.All(arg => arg.Value?.ToString() != linuxOsPlatform))
            {
                return false;
            }

            // If there is a platform exclusion list and linux is included, return false
            if (a.AttributeType.Name == unsupportedOsPlatformAttribute
                && a.ConstructorArguments.Any(arg => arg.Value?.ToString() == linuxOsPlatform))
            {
                return false;
            }

            // By default, return true
            return true;
        });
    }

    private static IEnumerable<Instruction> GetOperationsPreceedingThrow(MethodDefinition method)
    {
       Instruction previous = null;

       if (method.Body != null 
          && method.Body.Instructions != null)
       {
           foreach (var op in method.Body.Instructions)
           {
               if (op.OpCode.Code == Code.Nop)
                   continue;

               if (op.OpCode.Code == Code.Throw && previous != null)
                   yield return previous;

               previous = op;
           }
       }
    }

    private bool IsFactoryForPlatformNotSupported(MethodReference reference)
    {
       var methodInfo = reference.Resolve();

       if (methodInfo == null || methodInfo.IsAbstract)
       {
           return false;
       }

       Instruction constructorReference = null;
       foreach (var op in methodInfo.Body.Instructions)
       {
           switch (op.OpCode.Code)
           {
               case Code.Newobj:
                   constructorReference = op;
                   break;
               case Code.Ret:
                   if (constructorReference != null 
                      && IsPlatformNotSupported(constructorReference.Operand.ToString()))
                      return true;
                   break;
           }
       }
       return false;
    }
}