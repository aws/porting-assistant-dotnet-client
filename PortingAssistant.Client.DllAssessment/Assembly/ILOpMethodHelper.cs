using System;
using System.Collections.Generic;

namespace AwsEncoreService.Compatibility.Handler
{
     public enum BinaryOperator
    {
        None,
        Add,
        AddAssign,
        Subtract,
        SubtractAssign,
        Multiply,
        MultiplyAssign,
        Divide,
        DivideAssign,
        ValueEquality,
        ValueInequality,
        LogicalOr,
        LogicalAnd,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LeftShift,
        LeftShiftAssign,
        RightShift,
        RightShiftAssign,
        BitwiseOr,
        BitwiseAnd,
        BitwiseXor,
        Modulo,
        ModuloAssign,
        Assign,
        NullCoalesce,
        AndAssign,
        OrAssign,
        XorAssign
    }
    
    public enum UnaryOperator
    {
        Negate,
        LogicalNot,
        BitwiseNot,
        PostDecrement,
        PostIncrement,
        PreDecrement,
        PreIncrement,
        AddressReference,
        AddressDereference,
        AddressOf,
        UnaryPlus,
        None,
        True,
        False
    }

    public static class ILOpMethodHelper
    {
        static readonly Dictionary<string, BinaryOperator> binaryOperators
            = new Dictionary<string, BinaryOperator> () {

                { "op_Equality", BinaryOperator.ValueEquality },
                { "op_Inequality", BinaryOperator.ValueInequality },
                { "op_GreaterThan", BinaryOperator.GreaterThan },
                { "op_GreaterThanOrEqual", BinaryOperator.GreaterThanOrEqual },
                { "op_LessThan", BinaryOperator.LessThan },
                { "op_LessThanOrEqual", BinaryOperator.LessThanOrEqual },
                { "op_Addition", BinaryOperator.Add },
                { "op_Subtraction", BinaryOperator.Subtract },
                { "op_Division", BinaryOperator.Divide },
                { "op_Multiply", BinaryOperator.Multiply },
                { "op_Modulus", BinaryOperator.Modulo },
                { "op_BitwiseAnd", BinaryOperator.BitwiseAnd },
                { "op_BitwiseOr", BinaryOperator.BitwiseOr },
                { "op_ExclusiveOr", BinaryOperator.BitwiseXor },
                { "op_RightShift", BinaryOperator.RightShift },
                { "op_LeftShift", BinaryOperator.LeftShift },
                { "op_Explicit", BinaryOperator.None },
                { "op_Implicit", BinaryOperator.None }
            };
        
        static readonly Dictionary<string, UnaryOperator> unaryOperators
            = new Dictionary<string, UnaryOperator> () {

                { "op_UnaryNegation", UnaryOperator.Negate },
                { "op_LogicalNot", UnaryOperator.LogicalNot },
                { "op_OnesComplement", UnaryOperator.BitwiseNot },
                { "op_Decrement", UnaryOperator.PostDecrement },
                { "op_Increment", UnaryOperator.PostIncrement },
                { "op_UnaryPlus", UnaryOperator.UnaryPlus},
                { "op_True", UnaryOperator.True},
                { "op_False", UnaryOperator.False},
            };

        public static bool IsOperatorMethod(string methodName)
        {
            return binaryOperators.ContainsKey(methodName) ||
                   unaryOperators.ContainsKey(methodName);
        }
        
        public static string GetOperatorMethod(string methodName)
        {
            if (binaryOperators.ContainsKey(methodName))
                return getOperatorString(binaryOperators[methodName]);

            return getOperatorString(unaryOperators[methodName]);

        }
        
        private static string getOperatorString(BinaryOperator op)
        {
            switch (op)
            {
                case BinaryOperator.Add:
                    return "+";
                case BinaryOperator.BitwiseAnd:
                    return "&";
                case BinaryOperator.BitwiseOr:
                    return "|";
                case BinaryOperator.BitwiseXor:
                    return "^";
                case BinaryOperator.Divide:
                    return "/";
                case BinaryOperator.GreaterThan:
                    return ">";
                case BinaryOperator.GreaterThanOrEqual:
                    return ">=";
                case BinaryOperator.LeftShift:
                    return "<<";
                case BinaryOperator.LessThan:
                    return "<";
                case BinaryOperator.LessThanOrEqual:
                    return "<=";
                case BinaryOperator.LogicalAnd:
                    return "&&";
                case BinaryOperator.LogicalOr:
                    return "||";
                case BinaryOperator.Modulo:
                    return "%";
                case BinaryOperator.Multiply:
                    return "*";
                case BinaryOperator.RightShift:
                    return ">>";
                case BinaryOperator.Subtract:
                    return "-";
                case BinaryOperator.ValueEquality:
                    return "==";
                case BinaryOperator.ValueInequality:
                    return "!=";
                case BinaryOperator.Assign:
                    return "=";
                case BinaryOperator.AddAssign:
                    return "+=";
                case BinaryOperator.SubtractAssign:
                    return "-=";
                case BinaryOperator.AndAssign:
                    return "&=";
                case BinaryOperator.DivideAssign:
                    return "/=";
                case BinaryOperator.LeftShiftAssign:
                    return "<<=";
                case BinaryOperator.ModuloAssign:
                    return "%=";
                case BinaryOperator.MultiplyAssign:
                    return "*=";
                case BinaryOperator.OrAssign:
                    return "|=";
                case BinaryOperator.RightShiftAssign:
                    return ">>=";
                case BinaryOperator.XorAssign:
                    return "^=";
                case BinaryOperator.NullCoalesce:
                    return "??";
                default:
                    throw new ArgumentException();
            }
        }

        private static string getOperatorString(UnaryOperator op)
        {
            switch (op)
            {
                case UnaryOperator.BitwiseNot:
                    return "~";
                case UnaryOperator.LogicalNot:
                    return "!";
                case UnaryOperator.Negate:
                    return "-";
                case UnaryOperator.PostDecrement:
                case UnaryOperator.PreDecrement:
                    return "--";
                case UnaryOperator.PostIncrement:
                case UnaryOperator.PreIncrement:
                    return "++";
                case UnaryOperator.AddressDereference:
                    return "*";
                case UnaryOperator.AddressReference:
                case UnaryOperator.AddressOf:
                    return "&";
                case UnaryOperator.UnaryPlus:
                    return "+";
                case UnaryOperator.True:
                    return "true";
                case UnaryOperator.False:
                    return "false";
                case UnaryOperator.None:
                    return string.Empty;
                default:
                    throw new ArgumentException();
            }
        }

    }
}