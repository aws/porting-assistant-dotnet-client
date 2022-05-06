using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace AwsEncoreService.Compatibility.Handler
{
    public static class ILMethodHelper
    {
        private const string OutParam = "&";
        private const string GetterMethod = "get";
        private const string SetterMethod = "set";
        private const string AddMethod = "add";
        private const string RemoveMethod = "remove";
        private const string OpExplicit = "op_Explicit";
        private const string OpImplicit = "op_Implicit";
        private const string Explicit = "explicit";
        private const string Implicit = "implicit";
        private const string ParamSeparator = ", ";
        private const string GenericArgSeparator = "`";
        private const string GetItem = "get_Item";
        private const string SetItem = "set_Item";
        private const string ParamsSeparator = "params ";
        private const string SystemValueTuple = "System.ValueTuple";
        private const string IsReadonlyAttribute = "IsReadOnlyAttribute";
        private const string ParamArrayAttribute = "ParamArrayAttribute";
        private const string Nullable = "Nullable";
        private const string RegExForCommaReplace = @",[\s]*";
        private const string RegExForCommaMatch = @",[^\s]";

        public static List<string> ConvertToCSharpMethod(MethodDefinition methodDefinition)
        {
            var resultMethods = new List<string>();
            
            string ns = GetNameSpace(methodDefinition.DeclaringType); 
            string name = methodDefinition.Name; 
            
            if (methodDefinition.IsVirtual)
            {
                name = GetFromVirtualMethod(methodDefinition);
            }

            try
            {
                if (methodDefinition.IsGetter ||
                    methodDefinition.IsSetter)
                {
                    name = GetPropertyMethod(methodDefinition);

                    resultMethods.Add(name);
                    return resultMethods;
                }
                else if (methodDefinition.IsConstructor)
                {
                    name = GetConstructorName(methodDefinition);
                }
                else if (methodDefinition.IsAddOn ||
                         methodDefinition.IsRemoveOn)
                {
                    string temp = GetAddRemoveMethod(methodDefinition);

                    resultMethods.Add(temp);

                    if (!methodDefinition.HasParameters)
                    {
                        return resultMethods;
                    }
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine($"Encountered exception when processing {methodDefinition}. Continuing with default values. Error: {e}");
            }
            

            string parameters = GetParametersAsString(methodDefinition);

            string result = "";
            if (IsOperatorMethod(methodDefinition))
            {
                result = GetOperatorMethod(methodDefinition, parameters);
            }
            else
            {
                var genericParamsList = new List<string>();
                if (methodDefinition.HasGenericParameters)
                {
                    genericParamsList = GetGenericParameters(methodDefinition.GenericParameters);
                    name = string.Format("{0}<{1}>", name, string.Join(ParamSeparator, genericParamsList));
                    
                    //Special case: SystemTupple should be changed to (args)
                    if (parameters.Contains(SystemValueTuple))
                    {
                        parameters = string.Format("({0})", string.Join(ParamSeparator, genericParamsList));
                    }
                }

                result = string.Format("{0}.{1}({2})", ns, name, parameters);
                
                //Special case: SystemTupple should be changed to (args)
                if (result.Contains(SystemValueTuple))
                {
                    resultMethods.Add(result);
                    ns = GetNameSpace(methodDefinition.DeclaringType, true); 
                    
                    if (methodDefinition.IsConstructor && 
                        ns.Contains(parameters))
                    {
                        result = string.Format("{0}.{1}()", ns, name);
                    }
                    else
                    {
                        result = string.Format("{0}.{1}({2})", ns, name, parameters);
                    }
                   
                }
            }

            resultMethods.Add(result);
            return resultMethods;
        }

        private static bool IsOperatorMethod(MethodDefinition methodDefinition)
        {
            if (ILOpMethodHelper.IsOperatorMethod(methodDefinition.Name))
            {
                if (!methodDefinition.HasGenericParameters) return true;
                
                if (OpExplicit.Equals(methodDefinition.Name) ||
                    OpImplicit.Equals(methodDefinition.Name)) return true;
            }

            return false;
        }

        private static string GetAddRemoveMethod(MethodDefinition methodDefinition)
        {
            string name = methodDefinition.Name;
            string ns = GetNameSpace(methodDefinition.DeclaringType); 
            
            var attrType = methodDefinition.IsAddOn ? AddMethod : RemoveMethod;

            var search = string.Format($"{attrType}_");
            var temp = string.Format("{0}.{1}.{2}", ns, name.Substring(search.Length), attrType);

            return temp;
        }

        private static string GetConstructorName(MethodDefinition methodDefinition)
        {
            string name = methodDefinition.Name;
            var temp = methodDefinition.DeclaringType.FullName;
            temp = NormalizeType(methodDefinition.DeclaringType, temp);
                
            if (temp.LastIndexOf('.') != -1)
            {
                name = GetName(temp.Substring(temp.LastIndexOf('.') + 1));
            }

            return name;
        }

        private static string GetPropertyMethod(MethodDefinition methodDefinition)
        {
            string ns = GetNameSpace(methodDefinition.DeclaringType); 
            string name = methodDefinition.Name; 
            
            var attrType = methodDefinition.IsGetter ? GetterMethod : SetterMethod;
            name = string.Format("{0}.{1}.{2}", ns, name.Substring("get_".Length), attrType);

            if (methodDefinition.HasParameters)
            {
                if (GetItem.Equals(methodDefinition.Name) ||
                    SetItem.Equals(methodDefinition.Name))
                {
                    var p = GetParametersAsString(methodDefinition);
                    if (attrType.Equals(SetterMethod))
                    { 
                        p = methodDefinition.Parameters.Select(p => GetParameterValue(p)).First();
                    }
                    name = string.Format("{0}.this[{1}].{2}", ns, p, attrType);
                }
            }

            return name;
        }

        private static string GetParametersAsString(MethodDefinition methodDefinition)
        {
            string parameters = "";
            if (methodDefinition.HasParameters)
            {
                var plist = new List<string>();
                for (int i = 0; i < methodDefinition.Parameters.Count; i++)
                {
                    var parameterDefinition = methodDefinition.Parameters[i];
                    var param = GetParameterValue(parameterDefinition);
                    plist.Add(param);
                }

                parameters = string.Join(ParamSeparator, plist);
            }

            return parameters;
        }

        private static string GetName(string name, string replace="")
        {
            var temp = name;
            if (temp.Contains(GenericArgSeparator))
            {
                temp = Regex.Replace(temp, "`[\\d]+", replace);
            }

            return temp;
        }

        private static string GetNameSpace(TypeReference typeDefinition, bool specialHandle = false)
        {
            var ns = typeDefinition.FullName;
            ns = NormalizeType(typeDefinition, ns);
            if (typeDefinition.HasGenericParameters)
            {
                var genericParams = new List<string>();
                genericParams = GetGenericParameters(typeDefinition.GenericParameters);
                
                if (genericParams.Count > 0)
                {
                    var paramString = string.Format("<{0}>", string.Join(ParamSeparator, genericParams));
                    ns = GetName(ns, paramString);
                }
                
                //Special case: SystemTupple should be changed to (args)
                if (specialHandle && ns.Contains(SystemValueTuple))
                {
                    ns = string.Format("({0})", string.Join(ParamSeparator, genericParams));
                }
            }
            
            ns = ILTypeMapper.GetType(ns);

            return ns;
        }


        private static string GetTypeFromTypeDef(TypeDefinition typeDefinition)
        {
            string type = typeDefinition.Namespace;
            string result = "";
            string name = typeDefinition.Name;
            
            if (typeDefinition.DeclaringType != null &&
                !string.IsNullOrEmpty(typeDefinition.Name))
            {
                type = ILTypeMapper.GetType(GetNameSpace(typeDefinition.DeclaringType));
                name = GetName(string.Format("{0}.{1}",type, name));
            }
            else if (typeDefinition.Namespace != null && 
                     !string.IsNullOrEmpty(typeDefinition.Name))
            {
                name = GetName(string.Format("{0}.{1}",type, name));
            }
            
            result = NormalizeType(typeDefinition, name);
            result = ILTypeMapper.GetType(result);

            return result;
        }

        private static List<string> GetGenericParameters(Collection<GenericParameter> genericParameters)
        {
            var genericParams = new List<string>();
            foreach (var parameter in genericParameters)
            {
                if (!genericParams.Contains(parameter.Name))
                {
                    genericParams.Add(ILTypeMapper.GetType(parameter.Name));
                }
            }

            return genericParams;
        }

        private static string GetOperatorMethod(MethodDefinition methodDefinition, string parameters)
        {
            string name = methodDefinition.Name;
            string ns = GetNameSpace(methodDefinition.DeclaringType); //.DeclaringType.FullName);

            if (OpExplicit.Equals(name) || 
                OpImplicit.Equals(name))
            {
                name = OpExplicit.Equals(name) ? Explicit : Implicit;
                var type = GetReturnType(methodDefinition.ReturnType);

                return string.Format("{0}.{1} operator {2}({3})", ns, name, type, parameters);
            }
            else
            {
                name = ILOpMethodHelper.GetOperatorMethod(name);
                return string.Format("{0}.operator {1}({2})", ns, name, parameters);
            }
        }

        private static string GetFromVirtualMethod(MethodDefinition methodDefinition)
        {
            string name = methodDefinition.Name;
            if (methodDefinition.HasOverrides)
            {
                var overrideDef = methodDefinition.Overrides.First();
                var oname = overrideDef.DeclaringType.FullName;
                if (name.Contains(oname))
                {
                    name = name.Substring(oname.Length + 1);
                }
            }

            return name;
        }

        private static string GetReturnType(TypeReference parameterDefinition)
        {
            string paramIn = parameterDefinition.FullName;

            if (parameterDefinition.IsGenericInstance)
            {
                paramIn = GetGenericParamType(parameterDefinition);
            }

            var param = ILTypeMapper.GetType(paramIn);

            param = NormalizeType(parameterDefinition, param);

            return param;
        }
        
        private static string GetParameterValue(ParameterDefinition parameterDefinition)
        {
            string paramIn = parameterDefinition.ParameterType.FullName;
            bool containsRef = paramIn.EndsWith(OutParam);
            bool pointer = paramIn.Contains(ILTypeMapper.PtrSuffix);
            bool array = paramIn.Contains("[]");
            
            if (parameterDefinition.ParameterType.IsGenericInstance)
            {
                paramIn = GetGenericParamType(parameterDefinition.ParameterType);
            } else if (parameterDefinition.ParameterType.ContainsGenericParameter)
            {
                paramIn = GetName(paramIn);
            }
            else 
            {
                if (parameterDefinition.ParameterType.IsByReference)
                {
                    try
                    {
                        var typeDefinition = parameterDefinition.ParameterType.Resolve();
                        paramIn = GetTypeFromTypeDef(typeDefinition);

                        if (array)
                        {
                            paramIn = paramIn + "[]";
                        }
                        
                        if (typeDefinition.HasGenericParameters)
                        {
                            var genericParamsList = GetGenericParametersFromString(parameterDefinition.ParameterType.FullName);
                            paramIn = string.Format("{0}<{1}>", paramIn, string.Join(ParamSeparator, genericParamsList));
                        }
                    }
                    catch (Exception e)
                    {
                        paramIn = GetName(paramIn);
                    }
                }
                else
                {
                    paramIn = GetName(paramIn);
                }
            }

            if (containsRef)
            {
                paramIn = paramIn.Replace("&", "");
                if (parameterDefinition.IsOut) // out param
                {
                    paramIn = ILTypeMapper.OutPrefix + paramIn;
                }
                else if (parameterDefinition.ParameterType.IsByReference) //ref parameter
                {
                    // ReadOnly parameter is in param
                    if (parameterDefinition.HasCustomAttributes && 
                        parameterDefinition.CustomAttributes.Any(a =>
                            a != null && a.AttributeType.Name.Contains(IsReadonlyAttribute)))
                    {
                        paramIn = ILTypeMapper.InPrefix + paramIn;
                    } 
                    else
                    {
                        paramIn = ILTypeMapper.RefPrefix + paramIn;
                    }
                }
            }

            var param = ILTypeMapper.GetType(paramIn);

            if (parameterDefinition.HasCustomAttributes)
            {
                foreach (var customAttribute in parameterDefinition.CustomAttributes)
                {
                    if (customAttribute.AttributeType == null || 
                        customAttribute.AttributeType.Name == null) continue;
                    
                    if (ParamArrayAttribute.Equals(customAttribute.AttributeType.Name))
                    {
                        param = ParamsSeparator + param;
                    }
                    else if (customAttribute.AttributeType.Name.Contains(Nullable))
                    {
                       // param = param + "?";
                    }
                } 
            }

            param = NormalizeType(parameterDefinition.ParameterType, param);

            if (pointer && !param.Contains(ILTypeMapper.PtrSuffix))
            {
                param = param + ILTypeMapper.PtrSuffix;
            }

            return param;
        }

        private static IEnumerable<string?> GetGenericParametersFromString(string genericParamsInput)
        {
            string RegExForGenericParamMatch = @"<(.*)>";

            var res = Regex.Match(genericParamsInput, RegExForGenericParamMatch);

            List<string> result = new List<string>();
            if (res.Success && res.Groups.Count > 0)
            {
                var values = res.Groups[1].Value.Split(",");
                foreach (var value in values)
                {
                    result.Add(ILTypeMapper.GetType(value.Trim()));
                }
            }

            return result;
        }

        private static string GetGenericParamType(TypeReference typeReference )
        {
            GenericInstanceType instanceType = (GenericInstanceType) typeReference;

            string args = "";
            if (instanceType.HasGenericArguments)
            {
                foreach (var genericArgument in instanceType.GenericArguments)
                {
                    string val = "";
                    if (genericArgument.IsGenericInstance)
                    {
                        val = GetGenericParamType(genericArgument);
                        val = NormalizeType(genericArgument, val);
                    }
                    else
                    {
                        val = ILTypeMapper.GetType(genericArgument.FullName);
                        val = NormalizeType(genericArgument, val);
                    }

                    if (args.Length != 0)
                    {
                        args += ParamSeparator;
                    }

                    args += val;
                }
            }

            string type = instanceType.Namespace;
            string result = "";
            
            /* System.Nullable are optional params */
            if (instanceType.FullName.StartsWith("System.Nullable") &&
                instanceType.HasGenericArguments &&
                instanceType.GenericArguments.Count == 1)
            {
                result = GetName(string.Format("{0}",args));
            }
            else if (instanceType.DeclaringType != null &&
                !string.IsNullOrEmpty(instanceType.Name))
            {
                type = ILTypeMapper.GetType(GetNameSpace(instanceType.DeclaringType));

                if (type.Contains(args))
                {
                    result = GetName(string.Format("{0}.{1}", type, instanceType.Name));
                }
                else
                {
                    result = GetName(string.Format("{0}.{1}<{2}>", type, instanceType.Name, args));
                }
            }
            else if (instanceType.Namespace != null && !string.IsNullOrEmpty(instanceType.Name))
            {
                type = ILTypeMapper.GetType(string.Format("{0}.{1}", instanceType.Namespace, instanceType.Name));
                result = GetName(string.Format("{0}<{1}>",type, args));
            }

            return result;
        }

        private static string NormalizeType(TypeReference typeReference, string val)
        {
            if (!string.IsNullOrEmpty(val))  
            {
                if (val.Contains("/"))
                {
                    val = val.Replace("/", ".");
                }
                else if (val.Contains("0..."))
                {
                    val = val.Replace("0...", "*");
                }
                else
                {
                    if (Regex.Match(val, RegExForCommaMatch).Success)
                    {
                        val = Regex.Replace(val, RegExForCommaReplace, ParamSeparator);
                    }
                }
            }

            return val;
        }

        public static string ConvertToCSharpConstructor(TypeDefinition klass)
        {
            string result = "";
            var name = GetName(klass.Name);
            string type = "";
            
            if (klass.DeclaringType != null &&
                !string.IsNullOrEmpty(klass.Name))
            {
                type = ILTypeMapper.GetType(GetNameSpace(klass.DeclaringType));
            }
            else if (klass.Namespace != null && !string.IsNullOrEmpty(klass.Name))
            {
                type = ILTypeMapper.GetType(string.Format("{0}.{1}", klass.Namespace, name));
            }

            if (klass.HasGenericParameters && 
                !(klass.DeclaringType != null && klass.DeclaringType.HasGenericParameters))
            {
                var genericParamsList = GetGenericParameters(klass.GenericParameters);
                if (klass.IsNested)
                {
                    name = string.Format("{0}.{0}", name);
                }
                result = name = string.Format("{0}<{2}>.{1}()", type, name, string.Join(ParamSeparator, genericParamsList));
            }
            else
            {
                if (klass.IsNested)
                {
                    type = string.Format("{0}.{1}", type, name);
                }
                result = string.Format("{0}.{1}()", type, name);
            }

            return result;
        }
    }
}
