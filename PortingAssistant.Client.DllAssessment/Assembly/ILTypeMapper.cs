using System;
using System.Collections.Generic;
using System.Linq;

namespace AwsEncoreService.Compatibility.Handler
{
    // Parameter type in IL to CSharp code
    public static class ILTypeMapper
    {
        private const string ArraySuffix = "[]";
        public const string PtrSuffix = "*";
        public const string OutPrefix = "out ";
        public const string InPrefix = "in ";
        public const string RefPrefix = "ref ";

        public const string SystemDecimal = "System.Decimal";
        public const string SystemDecimalShort = "decimal";
        
        static ILTypeMapper()
        {
            var map = new Dictionary<String, String>()
            {
                {"System.Boolean", "bool"},
                {"System.Byte", "byte"},
                {"System.SByte", "sbyte"},
                {"System.Char", "char"},
                {"System.Double", "double"},
                {"System.Single", "float"},
                {"System.Int32", "int"},
                {"System.UInt32", "uint"},
                {"System.Int64", "long"},
                {"System.UInt64", "ulong"},
                {"System.Int16", "short"},
                {"System.UInt16", "ushort"},
                {"System.Object", "object"},
                {"System.String", "string"},
                {"System.Void", "void"},
                {"System.Decimal", "decimal"}
            };

            TypeRefMap = new Dictionary<string, string>(map);
          
            foreach (var kv in map)
            {
                TypeRefMap[ kv.Key + ArraySuffix ] = kv.Value + ArraySuffix;
                TypeRefMap[ kv.Key + PtrSuffix ] = kv.Value + PtrSuffix;
                TypeRefMap[ OutPrefix + kv.Key ] = OutPrefix + kv.Value;
                TypeRefMap[ RefPrefix + kv.Key ] = RefPrefix + kv.Value;
            }
        }

        private static Dictionary<string, string> TypeRefMap;
        
        public static string GetType(string type)
        {
            if (TypeRefMap.ContainsKey(type))
            {
                return TypeRefMap[type];
            }

            return type;
        }
    }
}