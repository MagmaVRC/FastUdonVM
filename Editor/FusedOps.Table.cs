using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MagmaVRC.UdonVM
{
    public static partial class FusedOps
    {
        static readonly Dictionary<string, Type[]> Types = new Dictionary<string, Type[]>();

        /// <summary>Expected slot types for a registered signature in parameter order (result slot last), or null.</summary>
        public static Type[] SlotTypes(string signature) => Types.TryGetValue(signature, out var t) ? t : null;

        static void Add(string signature, Type[] types, FusedOp op)
        {
            Types[signature] = types;
            Add(signature, op);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Const<TR>(IStrongBox[] h, Span<uint> p, TR value)
        {
            if (p[0] >= h.Length) return false;
            HeapAccess.Set(h, p[0], value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Ternary<TA, TB, TC, TR>(IStrongBox[] h, Span<uint> p, Func<TA, TB, TC, TR> f)
        {
            if (!Read(h, p[0], out TA a) || !Read(h, p[1], out TB b) || !Read(h, p[2], out TC c) || p[3] >= h.Length) return false;
            HeapAccess.Set(h, p[3], f(a, b, c));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Quaternary<TA, TB, TC, TD, TR>(IStrongBox[] h, Span<uint> p, Func<TA, TB, TC, TD, TR> f)
        {
            if (!Read(h, p[0], out TA a) || !Read(h, p[1], out TB b) || !Read(h, p[2], out TC c) || !Read(h, p[3], out TD d) || p[4] >= h.Length) return false;
            HeapAccess.Set(h, p[4], f(a, b, c, d));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool SetField<TS, TV>(IStrongBox[] h, Span<uint> p, Func<TS, TV, TS> f)
        {
            if (!Read(h, p[0], out TS s) || !Read(h, p[1], out TV v)) return false;
            HeapAccess.Set(h, p[0], f(s, v));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool NoOp<TS>(IStrongBox[] h, Span<uint> p)
        {
            return p[0] < h.Length && h[p[0]] is StrongBox<TS>;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ArrayGet<T>(IStrongBox[] h, Span<uint> p)
        {
            if (!Read(h, p[0], out T[] arr) || !Read(h, p[1], out int i) || p[2] >= h.Length) return false;
            HeapAccess.Set(h, p[2], arr[i]);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ArraySet<T>(IStrongBox[] h, Span<uint> p)
        {
            if (!Read(h, p[0], out T[] arr) || !Read(h, p[1], out int i) || !Read(h, p[2], out T v)) return false;
            arr[i] = v;
            return true;
        }

        static bool ArrayLength(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] >= h.Length || p[1] >= h.Length) return false;
            var box = h[p[0]];
            if (box == null || !(box.Value is Array arr)) return false;
            HeapAccess.Set(h, p[1], arr.Length);
            return true;
        }

        static bool Plain(object o) => o == null || o is string || o.GetType().IsValueType;

        static bool ObjectArrayGet(IStrongBox[] h, Span<uint> p)
        {
            if (!Read(h, p[0], out object[] arr) || !Read(h, p[1], out int i) || p[2] >= h.Length) return false;
            var o = arr[i];
            if (!Plain(o)) return false;
            HeapAccess.Set(h, p[2], o);
            return true;
        }

        static bool ObjectArraySet(IStrongBox[] h, Span<uint> p)
        {
            if (!Read(h, p[0], out object[] arr) || !Read(h, p[1], out int i) || !Read(h, p[2], out object v) || !Plain(v)) return false;
            arr[i] = v;
            return true;
        }

        static partial void Register()
        {
            Add("SystemBoolean.__CompareTo__SystemBoolean__SystemInt32", new[] { typeof(bool), typeof(bool), typeof(int) }, (h, p) => Binary<bool, bool, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemBoolean.__Equals__SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, (h, p) => Binary<bool, bool, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemBoolean.__GetHashCode__SystemInt32", new[] { typeof(bool), typeof(int) }, (h, p) => Unary<bool, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemBoolean.__ToString__SystemString", new[] { typeof(bool), typeof(string) }, (h, p) => Unary<bool, string>(h, p, (a) => a.ToString()));
            Add("SystemBoolean.__get_FalseString__SystemString", new[] { typeof(string) }, (h, p) => Const<string>(h, p, bool.FalseString));
            Add("SystemBoolean.__get_TrueString__SystemString", new[] { typeof(string) }, (h, p) => Const<string>(h, p, bool.TrueString));
            Add("SystemBoolean.__op_ConditionalAnd__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_ConditionalAnd__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_ConditionalOr__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_ConditionalOr__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_ConditionalXor__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_ConditionalXor__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_Equality__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_Equality__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_Inequality__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_Inequality__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_LogicalAnd__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_LogicalAnd__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_LogicalOr__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_LogicalOr__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_LogicalXor__SystemBoolean_SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool), typeof(bool) }, F_SystemBoolean___op_LogicalXor__SystemBoolean_SystemBoolean__SystemBoolean);
            Add("SystemBoolean.__op_UnaryNegation__SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool) }, F_SystemBoolean___op_UnaryNegation__SystemBoolean__SystemBoolean);
            Add("SystemBooleanArray.__Get__SystemInt32__SystemBoolean", new[] { typeof(bool[]), typeof(int), typeof(bool) }, ArrayGet<bool>);
            Add("SystemBooleanArray.__Set__SystemInt32_SystemBoolean__SystemVoid", new[] { typeof(bool[]), typeof(int), typeof(bool) }, ArraySet<bool>);
            Add("SystemBooleanArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemByte.__CompareTo__SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemByte.__Equals__SystemByte__SystemBoolean", new[] { typeof(byte), typeof(byte), typeof(bool) }, (h, p) => Binary<byte, byte, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemByte.__GetHashCode__SystemInt32", new[] { typeof(byte), typeof(int) }, (h, p) => Unary<byte, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemByte.__ToString__SystemString", new[] { typeof(byte), typeof(string) }, (h, p) => Unary<byte, string>(h, p, (a) => a.ToString()));
            Add("SystemByte.__get_MaxValue__SystemByte", new[] { typeof(byte) }, (h, p) => Const<byte>(h, p, byte.MaxValue));
            Add("SystemByte.__get_MinValue__SystemByte", new[] { typeof(byte) }, (h, p) => Const<byte>(h, p, 0));
            Add("SystemByte.__op_Addition__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a + b));
            Add("SystemByte.__op_Division__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a / b));
            Add("SystemByte.__op_Equality__SystemByte_SystemByte__SystemBoolean", new[] { typeof(byte), typeof(byte), typeof(bool) }, (h, p) => Binary<byte, byte, bool>(h, p, (a, b) => a == b));
            Add("SystemByte.__op_GreaterThanOrEqual__SystemByte_SystemByte__SystemBoolean", new[] { typeof(byte), typeof(byte), typeof(bool) }, (h, p) => Binary<byte, byte, bool>(h, p, (a, b) => a >= b));
            Add("SystemByte.__op_GreaterThan__SystemByte_SystemByte__SystemBoolean", new[] { typeof(byte), typeof(byte), typeof(bool) }, (h, p) => Binary<byte, byte, bool>(h, p, (a, b) => a > b));
            Add("SystemByte.__op_Inequality__SystemByte_SystemByte__SystemBoolean", new[] { typeof(byte), typeof(byte), typeof(bool) }, (h, p) => Binary<byte, byte, bool>(h, p, (a, b) => a != b));
            Add("SystemByte.__op_LeftShift__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a << (int)b));
            Add("SystemByte.__op_LessThanOrEqual__SystemByte_SystemByte__SystemBoolean", new[] { typeof(byte), typeof(byte), typeof(bool) }, (h, p) => Binary<byte, byte, bool>(h, p, (a, b) => a <= b));
            Add("SystemByte.__op_LessThan__SystemByte_SystemByte__SystemBoolean", new[] { typeof(byte), typeof(byte), typeof(bool) }, (h, p) => Binary<byte, byte, bool>(h, p, (a, b) => a < b));
            Add("SystemByte.__op_LogicalAnd__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a & b));
            Add("SystemByte.__op_LogicalOr__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a | b));
            Add("SystemByte.__op_LogicalXor__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a ^ b));
            Add("SystemByte.__op_Multiplication__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a * b));
            Add("SystemByte.__op_RightShift__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a >> (int)b));
            Add("SystemByte.__op_Subtraction__SystemByte_SystemByte__SystemInt32", new[] { typeof(byte), typeof(byte), typeof(int) }, (h, p) => Binary<byte, byte, int>(h, p, (a, b) => a - b));
            Add("SystemByte.__op_UnaryMinus__SystemByte__SystemInt32", new[] { typeof(byte), typeof(int) }, (h, p) => Unary<byte, int>(h, p, (a) => -a));
            Add("SystemByteArray.__Get__SystemInt32__SystemByte", new[] { typeof(byte[]), typeof(int), typeof(byte) }, ArrayGet<byte>);
            Add("SystemByteArray.__Set__SystemInt32_SystemByte__SystemVoid", new[] { typeof(byte[]), typeof(int), typeof(byte) }, ArraySet<byte>);
            Add("SystemByteArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemChar.__CompareTo__SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, (h, p) => Binary<char, char, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemChar.__ConvertFromUtf32__SystemInt32__SystemString", new[] { typeof(int), typeof(string) }, (h, p) => Unary<int, string>(h, p, (a) => char.ConvertFromUtf32(a)));
            Add("SystemChar.__ConvertToUtf32__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, (h, p) => Binary<char, char, int>(h, p, (a, b) => char.ConvertToUtf32(a, b)));
            Add("SystemChar.__ConvertToUtf32__SystemString_SystemInt32__SystemInt32", new[] { typeof(string), typeof(int), typeof(int) }, (h, p) => Binary<string, int, int>(h, p, (a, b) => char.ConvertToUtf32(a, b)));
            Add("SystemChar.__Equals__SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, (h, p) => Binary<char, char, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemChar.__GetHashCode__SystemInt32", new[] { typeof(char), typeof(int) }, (h, p) => Unary<char, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemChar.__GetNumericValue__SystemChar__SystemDouble", new[] { typeof(char), typeof(double) }, (h, p) => Unary<char, double>(h, p, (a) => char.GetNumericValue(a)));
            Add("SystemChar.__GetNumericValue__SystemString_SystemInt32__SystemDouble", new[] { typeof(string), typeof(int), typeof(double) }, (h, p) => Binary<string, int, double>(h, p, (a, b) => char.GetNumericValue(a, b)));
            Add("SystemChar.__IsControl__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsControl(a)));
            Add("SystemChar.__IsControl__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsControl(a, b)));
            Add("SystemChar.__IsDigit__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsDigit(a)));
            Add("SystemChar.__IsDigit__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsDigit(a, b)));
            Add("SystemChar.__IsHighSurrogate__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsHighSurrogate(a)));
            Add("SystemChar.__IsHighSurrogate__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsHighSurrogate(a, b)));
            Add("SystemChar.__IsLetterOrDigit__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsLetterOrDigit(a)));
            Add("SystemChar.__IsLetterOrDigit__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsLetterOrDigit(a, b)));
            Add("SystemChar.__IsLetter__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsLetter(a)));
            Add("SystemChar.__IsLetter__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsLetter(a, b)));
            Add("SystemChar.__IsLowSurrogate__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsLowSurrogate(a)));
            Add("SystemChar.__IsLowSurrogate__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsLowSurrogate(a, b)));
            Add("SystemChar.__IsLower__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsLower(a)));
            Add("SystemChar.__IsLower__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsLower(a, b)));
            Add("SystemChar.__IsNumber__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsNumber(a)));
            Add("SystemChar.__IsNumber__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsNumber(a, b)));
            Add("SystemChar.__IsPunctuation__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsPunctuation(a)));
            Add("SystemChar.__IsPunctuation__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsPunctuation(a, b)));
            Add("SystemChar.__IsSeparator__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsSeparator(a)));
            Add("SystemChar.__IsSeparator__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsSeparator(a, b)));
            Add("SystemChar.__IsSurrogatePair__SystemChar_SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, (h, p) => Binary<char, char, bool>(h, p, (a, b) => char.IsSurrogatePair(a, b)));
            Add("SystemChar.__IsSurrogatePair__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsSurrogatePair(a, b)));
            Add("SystemChar.__IsSurrogate__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsSurrogate(a)));
            Add("SystemChar.__IsSurrogate__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsSurrogate(a, b)));
            Add("SystemChar.__IsSymbol__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsSymbol(a)));
            Add("SystemChar.__IsSymbol__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsSymbol(a, b)));
            Add("SystemChar.__IsUpper__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsUpper(a)));
            Add("SystemChar.__IsUpper__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsUpper(a, b)));
            Add("SystemChar.__IsWhiteSpace__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => char.IsWhiteSpace(a)));
            Add("SystemChar.__IsWhiteSpace__SystemString_SystemInt32__SystemBoolean", new[] { typeof(string), typeof(int), typeof(bool) }, (h, p) => Binary<string, int, bool>(h, p, (a, b) => char.IsWhiteSpace(a, b)));
            Add("SystemChar.__ToLowerInvariant__SystemChar__SystemChar", new[] { typeof(char), typeof(char) }, (h, p) => Unary<char, char>(h, p, (a) => char.ToLowerInvariant(a)));
            Add("SystemChar.__ToLower__SystemChar__SystemChar", new[] { typeof(char), typeof(char) }, (h, p) => Unary<char, char>(h, p, (a) => char.ToLower(a)));
            Add("SystemChar.__ToString__SystemChar__SystemString", new[] { typeof(char), typeof(string) }, (h, p) => Unary<char, string>(h, p, (a) => char.ToString(a)));
            Add("SystemChar.__ToString__SystemString", new[] { typeof(char), typeof(string) }, (h, p) => Unary<char, string>(h, p, (a) => a.ToString()));
            Add("SystemChar.__ToUpperInvariant__SystemChar__SystemChar", new[] { typeof(char), typeof(char) }, (h, p) => Unary<char, char>(h, p, (a) => char.ToUpperInvariant(a)));
            Add("SystemChar.__ToUpper__SystemChar__SystemChar", new[] { typeof(char), typeof(char) }, (h, p) => Unary<char, char>(h, p, (a) => char.ToUpper(a)));
            Add("SystemChar.__get_MaxValue__SystemChar", new[] { typeof(char) }, (h, p) => Const<char>(h, p, '\uffff'));
            Add("SystemChar.__get_MinValue__SystemChar", new[] { typeof(char) }, (h, p) => Const<char>(h, p, '\0'));
            Add("SystemChar.__op_Addition__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_Addition__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_Division__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_Division__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_Equality__SystemChar_SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, F_SystemChar___op_Equality__SystemChar_SystemChar__SystemBoolean);
            Add("SystemChar.__op_GreaterThanOrEqual__SystemChar_SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, F_SystemChar___op_GreaterThanOrEqual__SystemChar_SystemChar__SystemBoolean);
            Add("SystemChar.__op_GreaterThan__SystemChar_SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, F_SystemChar___op_GreaterThan__SystemChar_SystemChar__SystemBoolean);
            Add("SystemChar.__op_Inequality__SystemChar_SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, F_SystemChar___op_Inequality__SystemChar_SystemChar__SystemBoolean);
            Add("SystemChar.__op_LeftShift__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_LeftShift__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_LessThanOrEqual__SystemChar_SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, F_SystemChar___op_LessThanOrEqual__SystemChar_SystemChar__SystemBoolean);
            Add("SystemChar.__op_LessThan__SystemChar_SystemChar__SystemBoolean", new[] { typeof(char), typeof(char), typeof(bool) }, F_SystemChar___op_LessThan__SystemChar_SystemChar__SystemBoolean);
            Add("SystemChar.__op_LogicalAnd__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_LogicalAnd__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_LogicalOr__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_LogicalOr__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_LogicalXor__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_LogicalXor__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_Multiplication__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_Multiplication__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_RightShift__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_RightShift__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_Subtraction__SystemChar_SystemChar__SystemInt32", new[] { typeof(char), typeof(char), typeof(int) }, F_SystemChar___op_Subtraction__SystemChar_SystemChar__SystemInt32);
            Add("SystemChar.__op_UnaryMinus__SystemChar__SystemInt32", new[] { typeof(char), typeof(int) }, F_SystemChar___op_UnaryMinus__SystemChar__SystemInt32);
            Add("SystemCharArray.__Get__SystemInt32__SystemChar", new[] { typeof(char[]), typeof(int), typeof(char) }, ArrayGet<char>);
            Add("SystemCharArray.__Set__SystemInt32_SystemChar__SystemVoid", new[] { typeof(char[]), typeof(int), typeof(char) }, ArraySet<char>);
            Add("SystemCharArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemConvert.__ToBoolean__SystemBoolean__SystemBoolean", new[] { typeof(bool), typeof(bool) }, (h, p) => Unary<bool, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemByte__SystemBoolean", new[] { typeof(byte), typeof(bool) }, (h, p) => Unary<byte, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemChar__SystemBoolean", new[] { typeof(char), typeof(bool) }, (h, p) => Unary<char, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemDouble__SystemBoolean", new[] { typeof(double), typeof(bool) }, (h, p) => Unary<double, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemInt16__SystemBoolean", new[] { typeof(short), typeof(bool) }, (h, p) => Unary<short, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemInt32__SystemBoolean", new[] { typeof(int), typeof(bool) }, (h, p) => Unary<int, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemInt64__SystemBoolean", new[] { typeof(long), typeof(bool) }, (h, p) => Unary<long, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemSByte__SystemBoolean", new[] { typeof(sbyte), typeof(bool) }, (h, p) => Unary<sbyte, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemSingle__SystemBoolean", new[] { typeof(float), typeof(bool) }, (h, p) => Unary<float, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemUInt16__SystemBoolean", new[] { typeof(ushort), typeof(bool) }, (h, p) => Unary<ushort, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(bool) }, (h, p) => Unary<uint, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToBoolean__SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(bool) }, (h, p) => Unary<ulong, bool>(h, p, (a) => Convert.ToBoolean(a)));
            Add("SystemConvert.__ToByte__SystemBoolean__SystemByte", new[] { typeof(bool), typeof(byte) }, (h, p) => Unary<bool, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemByte__SystemByte", new[] { typeof(byte), typeof(byte) }, (h, p) => Unary<byte, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemChar__SystemByte", new[] { typeof(char), typeof(byte) }, (h, p) => Unary<char, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemDouble__SystemByte", new[] { typeof(double), typeof(byte) }, (h, p) => Unary<double, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemInt16__SystemByte", new[] { typeof(short), typeof(byte) }, (h, p) => Unary<short, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemInt32__SystemByte", new[] { typeof(int), typeof(byte) }, (h, p) => Unary<int, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemInt64__SystemByte", new[] { typeof(long), typeof(byte) }, (h, p) => Unary<long, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemSByte__SystemByte", new[] { typeof(sbyte), typeof(byte) }, (h, p) => Unary<sbyte, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemSingle__SystemByte", new[] { typeof(float), typeof(byte) }, (h, p) => Unary<float, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemUInt16__SystemByte", new[] { typeof(ushort), typeof(byte) }, (h, p) => Unary<ushort, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemUInt32__SystemByte", new[] { typeof(uint), typeof(byte) }, (h, p) => Unary<uint, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToByte__SystemUInt64__SystemByte", new[] { typeof(ulong), typeof(byte) }, (h, p) => Unary<ulong, byte>(h, p, (a) => Convert.ToByte(a)));
            Add("SystemConvert.__ToChar__SystemBoolean__SystemChar", new[] { typeof(bool), typeof(char) }, (h, p) => Unary<bool, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemByte__SystemChar", new[] { typeof(byte), typeof(char) }, (h, p) => Unary<byte, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemChar__SystemChar", new[] { typeof(char), typeof(char) }, (h, p) => Unary<char, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemDouble__SystemChar", new[] { typeof(double), typeof(char) }, (h, p) => Unary<double, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemInt16__SystemChar", new[] { typeof(short), typeof(char) }, (h, p) => Unary<short, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemInt32__SystemChar", new[] { typeof(int), typeof(char) }, (h, p) => Unary<int, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemInt64__SystemChar", new[] { typeof(long), typeof(char) }, (h, p) => Unary<long, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemSByte__SystemChar", new[] { typeof(sbyte), typeof(char) }, (h, p) => Unary<sbyte, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemSingle__SystemChar", new[] { typeof(float), typeof(char) }, (h, p) => Unary<float, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemUInt16__SystemChar", new[] { typeof(ushort), typeof(char) }, (h, p) => Unary<ushort, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemUInt32__SystemChar", new[] { typeof(uint), typeof(char) }, (h, p) => Unary<uint, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToChar__SystemUInt64__SystemChar", new[] { typeof(ulong), typeof(char) }, (h, p) => Unary<ulong, char>(h, p, (a) => Convert.ToChar(a)));
            Add("SystemConvert.__ToDouble__SystemBoolean__SystemDouble", new[] { typeof(bool), typeof(double) }, (h, p) => Unary<bool, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemByte__SystemDouble", new[] { typeof(byte), typeof(double) }, (h, p) => Unary<byte, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemChar__SystemDouble", new[] { typeof(char), typeof(double) }, (h, p) => Unary<char, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemInt16__SystemDouble", new[] { typeof(short), typeof(double) }, (h, p) => Unary<short, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemInt32__SystemDouble", new[] { typeof(int), typeof(double) }, (h, p) => Unary<int, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemInt64__SystemDouble", new[] { typeof(long), typeof(double) }, (h, p) => Unary<long, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemSByte__SystemDouble", new[] { typeof(sbyte), typeof(double) }, (h, p) => Unary<sbyte, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemSingle__SystemDouble", new[] { typeof(float), typeof(double) }, (h, p) => Unary<float, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemUInt16__SystemDouble", new[] { typeof(ushort), typeof(double) }, (h, p) => Unary<ushort, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemUInt32__SystemDouble", new[] { typeof(uint), typeof(double) }, (h, p) => Unary<uint, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToDouble__SystemUInt64__SystemDouble", new[] { typeof(ulong), typeof(double) }, (h, p) => Unary<ulong, double>(h, p, (a) => Convert.ToDouble(a)));
            Add("SystemConvert.__ToInt16__SystemBoolean__SystemInt16", new[] { typeof(bool), typeof(short) }, (h, p) => Unary<bool, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemByte__SystemInt16", new[] { typeof(byte), typeof(short) }, (h, p) => Unary<byte, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemChar__SystemInt16", new[] { typeof(char), typeof(short) }, (h, p) => Unary<char, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemDouble__SystemInt16", new[] { typeof(double), typeof(short) }, (h, p) => Unary<double, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemInt16__SystemInt16", new[] { typeof(short), typeof(short) }, (h, p) => Unary<short, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemInt32__SystemInt16", new[] { typeof(int), typeof(short) }, (h, p) => Unary<int, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemInt64__SystemInt16", new[] { typeof(long), typeof(short) }, (h, p) => Unary<long, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemSByte__SystemInt16", new[] { typeof(sbyte), typeof(short) }, (h, p) => Unary<sbyte, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemSingle__SystemInt16", new[] { typeof(float), typeof(short) }, (h, p) => Unary<float, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemUInt16__SystemInt16", new[] { typeof(ushort), typeof(short) }, (h, p) => Unary<ushort, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemUInt32__SystemInt16", new[] { typeof(uint), typeof(short) }, (h, p) => Unary<uint, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt16__SystemUInt64__SystemInt16", new[] { typeof(ulong), typeof(short) }, (h, p) => Unary<ulong, short>(h, p, (a) => Convert.ToInt16(a)));
            Add("SystemConvert.__ToInt32__SystemBoolean__SystemInt32", new[] { typeof(bool), typeof(int) }, (h, p) => Unary<bool, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemByte__SystemInt32", new[] { typeof(byte), typeof(int) }, (h, p) => Unary<byte, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemChar__SystemInt32", new[] { typeof(char), typeof(int) }, F_SystemConvert___ToInt32__SystemChar__SystemInt32);
            Add("SystemConvert.__ToInt32__SystemDouble__SystemInt32", new[] { typeof(double), typeof(int) }, (h, p) => Unary<double, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemInt16__SystemInt32", new[] { typeof(short), typeof(int) }, (h, p) => Unary<short, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int) }, (h, p) => Unary<int, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemInt64__SystemInt32", new[] { typeof(long), typeof(int) }, (h, p) => Unary<long, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemSByte__SystemInt32", new[] { typeof(sbyte), typeof(int) }, (h, p) => Unary<sbyte, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemSingle__SystemInt32", new[] { typeof(float), typeof(int) }, (h, p) => Unary<float, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemUInt16__SystemInt32", new[] { typeof(ushort), typeof(int) }, (h, p) => Unary<ushort, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemUInt32__SystemInt32", new[] { typeof(uint), typeof(int) }, (h, p) => Unary<uint, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt32__SystemUInt64__SystemInt32", new[] { typeof(ulong), typeof(int) }, (h, p) => Unary<ulong, int>(h, p, (a) => Convert.ToInt32(a)));
            Add("SystemConvert.__ToInt64__SystemBoolean__SystemInt64", new[] { typeof(bool), typeof(long) }, (h, p) => Unary<bool, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemByte__SystemInt64", new[] { typeof(byte), typeof(long) }, (h, p) => Unary<byte, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemChar__SystemInt64", new[] { typeof(char), typeof(long) }, (h, p) => Unary<char, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemDouble__SystemInt64", new[] { typeof(double), typeof(long) }, (h, p) => Unary<double, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemInt16__SystemInt64", new[] { typeof(short), typeof(long) }, (h, p) => Unary<short, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemInt32__SystemInt64", new[] { typeof(int), typeof(long) }, (h, p) => Unary<int, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemInt64__SystemInt64", new[] { typeof(long), typeof(long) }, (h, p) => Unary<long, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemSByte__SystemInt64", new[] { typeof(sbyte), typeof(long) }, (h, p) => Unary<sbyte, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemSingle__SystemInt64", new[] { typeof(float), typeof(long) }, (h, p) => Unary<float, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemUInt16__SystemInt64", new[] { typeof(ushort), typeof(long) }, (h, p) => Unary<ushort, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemUInt32__SystemInt64", new[] { typeof(uint), typeof(long) }, (h, p) => Unary<uint, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToInt64__SystemUInt64__SystemInt64", new[] { typeof(ulong), typeof(long) }, (h, p) => Unary<ulong, long>(h, p, (a) => Convert.ToInt64(a)));
            Add("SystemConvert.__ToSByte__SystemBoolean__SystemSByte", new[] { typeof(bool), typeof(sbyte) }, (h, p) => Unary<bool, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemByte__SystemSByte", new[] { typeof(byte), typeof(sbyte) }, (h, p) => Unary<byte, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemChar__SystemSByte", new[] { typeof(char), typeof(sbyte) }, (h, p) => Unary<char, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemDouble__SystemSByte", new[] { typeof(double), typeof(sbyte) }, (h, p) => Unary<double, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemInt16__SystemSByte", new[] { typeof(short), typeof(sbyte) }, (h, p) => Unary<short, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemInt32__SystemSByte", new[] { typeof(int), typeof(sbyte) }, (h, p) => Unary<int, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemInt64__SystemSByte", new[] { typeof(long), typeof(sbyte) }, (h, p) => Unary<long, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemSByte__SystemSByte", new[] { typeof(sbyte), typeof(sbyte) }, (h, p) => Unary<sbyte, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemSingle__SystemSByte", new[] { typeof(float), typeof(sbyte) }, (h, p) => Unary<float, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemUInt16__SystemSByte", new[] { typeof(ushort), typeof(sbyte) }, (h, p) => Unary<ushort, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemUInt32__SystemSByte", new[] { typeof(uint), typeof(sbyte) }, (h, p) => Unary<uint, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSByte__SystemUInt64__SystemSByte", new[] { typeof(ulong), typeof(sbyte) }, (h, p) => Unary<ulong, sbyte>(h, p, (a) => Convert.ToSByte(a)));
            Add("SystemConvert.__ToSingle__SystemBoolean__SystemSingle", new[] { typeof(bool), typeof(float) }, (h, p) => Unary<bool, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemByte__SystemSingle", new[] { typeof(byte), typeof(float) }, (h, p) => Unary<byte, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemChar__SystemSingle", new[] { typeof(char), typeof(float) }, (h, p) => Unary<char, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemDouble__SystemSingle", new[] { typeof(double), typeof(float) }, (h, p) => Unary<double, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemInt16__SystemSingle", new[] { typeof(short), typeof(float) }, (h, p) => Unary<short, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemInt32__SystemSingle", new[] { typeof(int), typeof(float) }, (h, p) => Unary<int, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemInt64__SystemSingle", new[] { typeof(long), typeof(float) }, (h, p) => Unary<long, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemSByte__SystemSingle", new[] { typeof(sbyte), typeof(float) }, (h, p) => Unary<sbyte, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemUInt16__SystemSingle", new[] { typeof(ushort), typeof(float) }, (h, p) => Unary<ushort, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemUInt32__SystemSingle", new[] { typeof(uint), typeof(float) }, (h, p) => Unary<uint, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToSingle__SystemUInt64__SystemSingle", new[] { typeof(ulong), typeof(float) }, (h, p) => Unary<ulong, float>(h, p, (a) => Convert.ToSingle(a)));
            Add("SystemConvert.__ToUInt16__SystemBoolean__SystemUInt16", new[] { typeof(bool), typeof(ushort) }, (h, p) => Unary<bool, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemByte__SystemUInt16", new[] { typeof(byte), typeof(ushort) }, (h, p) => Unary<byte, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemChar__SystemUInt16", new[] { typeof(char), typeof(ushort) }, (h, p) => Unary<char, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemDouble__SystemUInt16", new[] { typeof(double), typeof(ushort) }, (h, p) => Unary<double, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemInt16__SystemUInt16", new[] { typeof(short), typeof(ushort) }, (h, p) => Unary<short, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemInt32__SystemUInt16", new[] { typeof(int), typeof(ushort) }, (h, p) => Unary<int, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemInt64__SystemUInt16", new[] { typeof(long), typeof(ushort) }, (h, p) => Unary<long, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemSByte__SystemUInt16", new[] { typeof(sbyte), typeof(ushort) }, (h, p) => Unary<sbyte, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemSingle__SystemUInt16", new[] { typeof(float), typeof(ushort) }, (h, p) => Unary<float, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemUInt16__SystemUInt16", new[] { typeof(ushort), typeof(ushort) }, (h, p) => Unary<ushort, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemUInt32__SystemUInt16", new[] { typeof(uint), typeof(ushort) }, (h, p) => Unary<uint, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt16__SystemUInt64__SystemUInt16", new[] { typeof(ulong), typeof(ushort) }, (h, p) => Unary<ulong, ushort>(h, p, (a) => Convert.ToUInt16(a)));
            Add("SystemConvert.__ToUInt32__SystemBoolean__SystemUInt32", new[] { typeof(bool), typeof(uint) }, (h, p) => Unary<bool, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemByte__SystemUInt32", new[] { typeof(byte), typeof(uint) }, (h, p) => Unary<byte, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemChar__SystemUInt32", new[] { typeof(char), typeof(uint) }, (h, p) => Unary<char, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemDouble__SystemUInt32", new[] { typeof(double), typeof(uint) }, (h, p) => Unary<double, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemInt16__SystemUInt32", new[] { typeof(short), typeof(uint) }, (h, p) => Unary<short, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemInt32__SystemUInt32", new[] { typeof(int), typeof(uint) }, (h, p) => Unary<int, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemInt64__SystemUInt32", new[] { typeof(long), typeof(uint) }, (h, p) => Unary<long, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemSByte__SystemUInt32", new[] { typeof(sbyte), typeof(uint) }, (h, p) => Unary<sbyte, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemSingle__SystemUInt32", new[] { typeof(float), typeof(uint) }, (h, p) => Unary<float, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemUInt16__SystemUInt32", new[] { typeof(ushort), typeof(uint) }, (h, p) => Unary<ushort, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint) }, (h, p) => Unary<uint, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt32__SystemUInt64__SystemUInt32", new[] { typeof(ulong), typeof(uint) }, (h, p) => Unary<ulong, uint>(h, p, (a) => Convert.ToUInt32(a)));
            Add("SystemConvert.__ToUInt64__SystemBoolean__SystemUInt64", new[] { typeof(bool), typeof(ulong) }, (h, p) => Unary<bool, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemByte__SystemUInt64", new[] { typeof(byte), typeof(ulong) }, (h, p) => Unary<byte, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemChar__SystemUInt64", new[] { typeof(char), typeof(ulong) }, (h, p) => Unary<char, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemDouble__SystemUInt64", new[] { typeof(double), typeof(ulong) }, (h, p) => Unary<double, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemInt16__SystemUInt64", new[] { typeof(short), typeof(ulong) }, (h, p) => Unary<short, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemInt32__SystemUInt64", new[] { typeof(int), typeof(ulong) }, (h, p) => Unary<int, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemInt64__SystemUInt64", new[] { typeof(long), typeof(ulong) }, (h, p) => Unary<long, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemSByte__SystemUInt64", new[] { typeof(sbyte), typeof(ulong) }, (h, p) => Unary<sbyte, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemSingle__SystemUInt64", new[] { typeof(float), typeof(ulong) }, (h, p) => Unary<float, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemUInt16__SystemUInt64", new[] { typeof(ushort), typeof(ulong) }, (h, p) => Unary<ushort, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemUInt32__SystemUInt64", new[] { typeof(uint), typeof(ulong) }, (h, p) => Unary<uint, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemConvert.__ToUInt64__SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong) }, (h, p) => Unary<ulong, ulong>(h, p, (a) => Convert.ToUInt64(a)));
            Add("SystemDouble.__CompareTo__SystemDouble__SystemInt32", new[] { typeof(double), typeof(double), typeof(int) }, (h, p) => Binary<double, double, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemDouble.__Equals__SystemDouble__SystemBoolean", new[] { typeof(double), typeof(double), typeof(bool) }, (h, p) => Binary<double, double, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemDouble.__GetHashCode__SystemInt32", new[] { typeof(double), typeof(int) }, (h, p) => Unary<double, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemDouble.__IsInfinity__SystemDouble__SystemBoolean", new[] { typeof(double), typeof(bool) }, (h, p) => Unary<double, bool>(h, p, (a) => double.IsInfinity(a)));
            Add("SystemDouble.__IsNaN__SystemDouble__SystemBoolean", new[] { typeof(double), typeof(bool) }, (h, p) => Unary<double, bool>(h, p, (a) => double.IsNaN(a)));
            Add("SystemDouble.__IsNegativeInfinity__SystemDouble__SystemBoolean", new[] { typeof(double), typeof(bool) }, (h, p) => Unary<double, bool>(h, p, (a) => double.IsNegativeInfinity(a)));
            Add("SystemDouble.__IsPositiveInfinity__SystemDouble__SystemBoolean", new[] { typeof(double), typeof(bool) }, (h, p) => Unary<double, bool>(h, p, (a) => double.IsPositiveInfinity(a)));
            Add("SystemDouble.__ToString__SystemString", new[] { typeof(double), typeof(string) }, (h, p) => Unary<double, string>(h, p, (a) => a.ToString()));
            Add("SystemDouble.__get_Epsilon__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, double.Epsilon));
            Add("SystemDouble.__get_MaxValue__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, double.MaxValue));
            Add("SystemDouble.__get_MinValue__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, double.MinValue));
            Add("SystemDouble.__get_NaN__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, double.NaN));
            Add("SystemDouble.__get_NegativeInfinity__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, double.NegativeInfinity));
            Add("SystemDouble.__get_PositiveInfinity__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, double.PositiveInfinity));
            Add("SystemDouble.__op_Addition__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => a + b));
            Add("SystemDouble.__op_Division__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => a / b));
            Add("SystemDouble.__op_Equality__SystemDouble_SystemDouble__SystemBoolean", new[] { typeof(double), typeof(double), typeof(bool) }, (h, p) => Binary<double, double, bool>(h, p, (a, b) => a == b));
            Add("SystemDouble.__op_GreaterThanOrEqual__SystemDouble_SystemDouble__SystemBoolean", new[] { typeof(double), typeof(double), typeof(bool) }, (h, p) => Binary<double, double, bool>(h, p, (a, b) => a >= b));
            Add("SystemDouble.__op_GreaterThan__SystemDouble_SystemDouble__SystemBoolean", new[] { typeof(double), typeof(double), typeof(bool) }, (h, p) => Binary<double, double, bool>(h, p, (a, b) => a > b));
            Add("SystemDouble.__op_Inequality__SystemDouble_SystemDouble__SystemBoolean", new[] { typeof(double), typeof(double), typeof(bool) }, (h, p) => Binary<double, double, bool>(h, p, (a, b) => a != b));
            Add("SystemDouble.__op_LessThanOrEqual__SystemDouble_SystemDouble__SystemBoolean", new[] { typeof(double), typeof(double), typeof(bool) }, (h, p) => Binary<double, double, bool>(h, p, (a, b) => a <= b));
            Add("SystemDouble.__op_LessThan__SystemDouble_SystemDouble__SystemBoolean", new[] { typeof(double), typeof(double), typeof(bool) }, (h, p) => Binary<double, double, bool>(h, p, (a, b) => a < b));
            Add("SystemDouble.__op_Multiplication__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => a * b));
            Add("SystemDouble.__op_Remainder__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => a % b));
            Add("SystemDouble.__op_Subtraction__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => a - b));
            Add("SystemDouble.__op_UnaryMinus__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => 0.0 - a));
            Add("SystemDoubleArray.__Get__SystemInt32__SystemDouble", new[] { typeof(double[]), typeof(int), typeof(double) }, ArrayGet<double>);
            Add("SystemDoubleArray.__Set__SystemInt32_SystemDouble__SystemVoid", new[] { typeof(double[]), typeof(int), typeof(double) }, ArraySet<double>);
            Add("SystemDoubleArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemInt16.__CompareTo__SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemInt16.__Equals__SystemInt16__SystemBoolean", new[] { typeof(short), typeof(short), typeof(bool) }, (h, p) => Binary<short, short, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemInt16.__GetHashCode__SystemInt32", new[] { typeof(short), typeof(int) }, (h, p) => Unary<short, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemInt16.__ToString__SystemString", new[] { typeof(short), typeof(string) }, (h, p) => Unary<short, string>(h, p, (a) => a.ToString()));
            Add("SystemInt16.__get_MaxValue__SystemInt16", new[] { typeof(short) }, (h, p) => Const<short>(h, p, short.MaxValue));
            Add("SystemInt16.__get_MinValue__SystemInt16", new[] { typeof(short) }, (h, p) => Const<short>(h, p, short.MinValue));
            Add("SystemInt16.__op_Addition__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a + b));
            Add("SystemInt16.__op_Division__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a / b));
            Add("SystemInt16.__op_Equality__SystemInt16_SystemInt16__SystemBoolean", new[] { typeof(short), typeof(short), typeof(bool) }, (h, p) => Binary<short, short, bool>(h, p, (a, b) => a == b));
            Add("SystemInt16.__op_GreaterThanOrEqual__SystemInt16_SystemInt16__SystemBoolean", new[] { typeof(short), typeof(short), typeof(bool) }, (h, p) => Binary<short, short, bool>(h, p, (a, b) => a >= b));
            Add("SystemInt16.__op_GreaterThan__SystemInt16_SystemInt16__SystemBoolean", new[] { typeof(short), typeof(short), typeof(bool) }, (h, p) => Binary<short, short, bool>(h, p, (a, b) => a > b));
            Add("SystemInt16.__op_Inequality__SystemInt16_SystemInt16__SystemBoolean", new[] { typeof(short), typeof(short), typeof(bool) }, (h, p) => Binary<short, short, bool>(h, p, (a, b) => a != b));
            Add("SystemInt16.__op_LeftShift__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a << (int)b));
            Add("SystemInt16.__op_LessThanOrEqual__SystemInt16_SystemInt16__SystemBoolean", new[] { typeof(short), typeof(short), typeof(bool) }, (h, p) => Binary<short, short, bool>(h, p, (a, b) => a <= b));
            Add("SystemInt16.__op_LessThan__SystemInt16_SystemInt16__SystemBoolean", new[] { typeof(short), typeof(short), typeof(bool) }, (h, p) => Binary<short, short, bool>(h, p, (a, b) => a < b));
            Add("SystemInt16.__op_LogicalAnd__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a & b));
            Add("SystemInt16.__op_LogicalOr__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a | b));
            Add("SystemInt16.__op_LogicalXor__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a ^ b));
            Add("SystemInt16.__op_Multiplication__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a * b));
            Add("SystemInt16.__op_RightShift__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a >> (int)b));
            Add("SystemInt16.__op_Subtraction__SystemInt16_SystemInt16__SystemInt32", new[] { typeof(short), typeof(short), typeof(int) }, (h, p) => Binary<short, short, int>(h, p, (a, b) => a - b));
            Add("SystemInt16.__op_UnaryMinus__SystemInt16__SystemInt32", new[] { typeof(short), typeof(int) }, (h, p) => Unary<short, int>(h, p, (a) => -a));
            Add("SystemInt32.__CompareTo__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, (h, p) => Binary<int, int, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemInt32.__Equals__SystemInt32__SystemBoolean", new[] { typeof(int), typeof(int), typeof(bool) }, (h, p) => Binary<int, int, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemInt32.__GetHashCode__SystemInt32", new[] { typeof(int), typeof(int) }, (h, p) => Unary<int, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemInt32.__ToString__SystemString", new[] { typeof(int), typeof(string) }, (h, p) => Unary<int, string>(h, p, (a) => a.ToString()));
            Add("SystemInt32.__get_MaxValue__SystemInt32", new[] { typeof(int) }, (h, p) => Const<int>(h, p, int.MaxValue));
            Add("SystemInt32.__get_MinValue__SystemInt32", new[] { typeof(int) }, (h, p) => Const<int>(h, p, int.MinValue));
            Add("SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_Addition__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_Division__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_Division__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_Equality__SystemInt32_SystemInt32__SystemBoolean", new[] { typeof(int), typeof(int), typeof(bool) }, F_SystemInt32___op_Equality__SystemInt32_SystemInt32__SystemBoolean);
            Add("SystemInt32.__op_GreaterThanOrEqual__SystemInt32_SystemInt32__SystemBoolean", new[] { typeof(int), typeof(int), typeof(bool) }, F_SystemInt32___op_GreaterThanOrEqual__SystemInt32_SystemInt32__SystemBoolean);
            Add("SystemInt32.__op_GreaterThan__SystemInt32_SystemInt32__SystemBoolean", new[] { typeof(int), typeof(int), typeof(bool) }, F_SystemInt32___op_GreaterThan__SystemInt32_SystemInt32__SystemBoolean);
            Add("SystemInt32.__op_Inequality__SystemInt32_SystemInt32__SystemBoolean", new[] { typeof(int), typeof(int), typeof(bool) }, F_SystemInt32___op_Inequality__SystemInt32_SystemInt32__SystemBoolean);
            Add("SystemInt32.__op_LeftShift__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_LeftShift__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_LessThanOrEqual__SystemInt32_SystemInt32__SystemBoolean", new[] { typeof(int), typeof(int), typeof(bool) }, F_SystemInt32___op_LessThanOrEqual__SystemInt32_SystemInt32__SystemBoolean);
            Add("SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean", new[] { typeof(int), typeof(int), typeof(bool) }, F_SystemInt32___op_LessThan__SystemInt32_SystemInt32__SystemBoolean);
            Add("SystemInt32.__op_LogicalAnd__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_LogicalAnd__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_LogicalOr__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_LogicalOr__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_LogicalXor__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_LogicalXor__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_Multiplication__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_Multiplication__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_Remainder__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_Remainder__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_RightShift__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_RightShift__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_Subtraction__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, F_SystemInt32___op_Subtraction__SystemInt32_SystemInt32__SystemInt32);
            Add("SystemInt32.__op_UnaryMinus__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int) }, F_SystemInt32___op_UnaryMinus__SystemInt32__SystemInt32);
            Add("SystemInt32Array.__Get__SystemInt32__SystemInt32", new[] { typeof(int[]), typeof(int), typeof(int) }, F_SystemInt32Array___Get__SystemInt32__SystemInt32);
            Add("SystemInt32Array.__Set__SystemInt32_SystemInt32__SystemVoid", new[] { typeof(int[]), typeof(int), typeof(int) }, F_SystemInt32Array___Set__SystemInt32_SystemInt32__SystemVoid);
            Add("SystemInt32Array.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemInt64.__CompareTo__SystemInt64__SystemInt32", new[] { typeof(long), typeof(long), typeof(int) }, (h, p) => Binary<long, long, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemInt64.__Equals__SystemInt64__SystemBoolean", new[] { typeof(long), typeof(long), typeof(bool) }, (h, p) => Binary<long, long, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemInt64.__GetHashCode__SystemInt32", new[] { typeof(long), typeof(int) }, (h, p) => Unary<long, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemInt64.__ToString__SystemString", new[] { typeof(long), typeof(string) }, (h, p) => Unary<long, string>(h, p, (a) => a.ToString()));
            Add("SystemInt64.__get_MaxValue__SystemInt64", new[] { typeof(long) }, (h, p) => Const<long>(h, p, long.MaxValue));
            Add("SystemInt64.__get_MinValue__SystemInt64", new[] { typeof(long) }, (h, p) => Const<long>(h, p, long.MinValue));
            Add("SystemInt64.__op_Addition__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => a + b));
            Add("SystemInt64.__op_Division__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => a / b));
            Add("SystemInt64.__op_Equality__SystemInt64_SystemInt64__SystemBoolean", new[] { typeof(long), typeof(long), typeof(bool) }, (h, p) => Binary<long, long, bool>(h, p, (a, b) => a == b));
            Add("SystemInt64.__op_GreaterThanOrEqual__SystemInt64_SystemInt64__SystemBoolean", new[] { typeof(long), typeof(long), typeof(bool) }, (h, p) => Binary<long, long, bool>(h, p, (a, b) => a >= b));
            Add("SystemInt64.__op_GreaterThan__SystemInt64_SystemInt64__SystemBoolean", new[] { typeof(long), typeof(long), typeof(bool) }, (h, p) => Binary<long, long, bool>(h, p, (a, b) => a > b));
            Add("SystemInt64.__op_Inequality__SystemInt64_SystemInt64__SystemBoolean", new[] { typeof(long), typeof(long), typeof(bool) }, (h, p) => Binary<long, long, bool>(h, p, (a, b) => a != b));
            Add("SystemInt64.__op_LeftShift__SystemInt64_SystemInt32__SystemInt64", new[] { typeof(long), typeof(int), typeof(long) }, (h, p) => Binary<long, int, long>(h, p, (a, b) => a << b));
            Add("SystemInt64.__op_LessThanOrEqual__SystemInt64_SystemInt64__SystemBoolean", new[] { typeof(long), typeof(long), typeof(bool) }, (h, p) => Binary<long, long, bool>(h, p, (a, b) => a <= b));
            Add("SystemInt64.__op_LessThan__SystemInt64_SystemInt64__SystemBoolean", new[] { typeof(long), typeof(long), typeof(bool) }, (h, p) => Binary<long, long, bool>(h, p, (a, b) => a < b));
            Add("SystemInt64.__op_LogicalAnd__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => a & b));
            Add("SystemInt64.__op_LogicalOr__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => a | b));
            Add("SystemInt64.__op_LogicalXor__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => a ^ b));
            Add("SystemInt64.__op_Multiplication__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => a * b));
            Add("SystemInt64.__op_RightShift__SystemInt64_SystemInt32__SystemInt64", new[] { typeof(long), typeof(int), typeof(long) }, (h, p) => Binary<long, int, long>(h, p, (a, b) => a >> b));
            Add("SystemInt64.__op_Subtraction__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => a - b));
            Add("SystemInt64.__op_UnaryMinus__SystemInt64__SystemInt64", new[] { typeof(long), typeof(long) }, (h, p) => Unary<long, long>(h, p, (a) => -a));
            Add("SystemInt64Array.__Get__SystemInt32__SystemInt64", new[] { typeof(long[]), typeof(int), typeof(long) }, ArrayGet<long>);
            Add("SystemInt64Array.__Set__SystemInt32_SystemInt64__SystemVoid", new[] { typeof(long[]), typeof(int), typeof(long) }, ArraySet<long>);
            Add("SystemInt64Array.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemMath.__Abs__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Abs(a)));
            Add("SystemMath.__Abs__SystemInt16__SystemInt16", new[] { typeof(short), typeof(short) }, (h, p) => Unary<short, short>(h, p, (a) => Math.Abs(a)));
            Add("SystemMath.__Abs__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int) }, (h, p) => Unary<int, int>(h, p, (a) => Math.Abs(a)));
            Add("SystemMath.__Abs__SystemInt64__SystemInt64", new[] { typeof(long), typeof(long) }, (h, p) => Unary<long, long>(h, p, (a) => Math.Abs(a)));
            Add("SystemMath.__Abs__SystemSByte__SystemSByte", new[] { typeof(sbyte), typeof(sbyte) }, (h, p) => Unary<sbyte, sbyte>(h, p, (a) => Math.Abs(a)));
            Add("SystemMath.__Abs__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Math.Abs(a)));
            Add("SystemMath.__Acos__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Acos(a)));
            Add("SystemMath.__Asin__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Asin(a)));
            Add("SystemMath.__Atan2__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => Math.Atan2(a, b)));
            Add("SystemMath.__Atan__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Atan(a)));
            Add("SystemMath.__BigMul__SystemInt32_SystemInt32__SystemInt64", new[] { typeof(int), typeof(int), typeof(long) }, (h, p) => Binary<int, int, long>(h, p, (a, b) => Math.BigMul(a, b)));
            Add("SystemMath.__Ceiling__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Ceiling(a)));
            Add("SystemMath.__Cos__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Cos(a)));
            Add("SystemMath.__Cosh__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Cosh(a)));
            Add("SystemMath.__Exp__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Exp(a)));
            Add("SystemMath.__Floor__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Floor(a)));
            Add("SystemMath.__IEEERemainder__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => Math.IEEERemainder(a, b)));
            Add("SystemMath.__Log10__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Log10(a)));
            Add("SystemMath.__Log__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => Math.Log(a, b)));
            Add("SystemMath.__Log__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Log(a)));
            Add("SystemMath.__Max__SystemByte_SystemByte__SystemByte", new[] { typeof(byte), typeof(byte), typeof(byte) }, (h, p) => Binary<byte, byte, byte>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemInt16_SystemInt16__SystemInt16", new[] { typeof(short), typeof(short), typeof(short) }, (h, p) => Binary<short, short, short>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, (h, p) => Binary<int, int, int>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemSByte_SystemSByte__SystemSByte", new[] { typeof(sbyte), typeof(sbyte), typeof(sbyte) }, (h, p) => Binary<sbyte, sbyte, sbyte>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemUInt16_SystemUInt16__SystemUInt16", new[] { typeof(ushort), typeof(ushort), typeof(ushort) }, (h, p) => Binary<ushort, ushort, ushort>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Max__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, (h, p) => Binary<ulong, ulong, ulong>(h, p, (a, b) => Math.Max(a, b)));
            Add("SystemMath.__Min__SystemByte_SystemByte__SystemByte", new[] { typeof(byte), typeof(byte), typeof(byte) }, (h, p) => Binary<byte, byte, byte>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemInt16_SystemInt16__SystemInt16", new[] { typeof(short), typeof(short), typeof(short) }, (h, p) => Binary<short, short, short>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, (h, p) => Binary<int, int, int>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemInt64_SystemInt64__SystemInt64", new[] { typeof(long), typeof(long), typeof(long) }, (h, p) => Binary<long, long, long>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemSByte_SystemSByte__SystemSByte", new[] { typeof(sbyte), typeof(sbyte), typeof(sbyte) }, (h, p) => Binary<sbyte, sbyte, sbyte>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemUInt16_SystemUInt16__SystemUInt16", new[] { typeof(ushort), typeof(ushort), typeof(ushort) }, (h, p) => Binary<ushort, ushort, ushort>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Min__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, (h, p) => Binary<ulong, ulong, ulong>(h, p, (a, b) => Math.Min(a, b)));
            Add("SystemMath.__Pow__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) }, (h, p) => Binary<double, double, double>(h, p, (a, b) => Math.Pow(a, b)));
            Add("SystemMath.__Round__SystemDouble_SystemInt32_SystemMidpointRounding__SystemDouble", new[] { typeof(double), typeof(int), typeof(MidpointRounding), typeof(double) }, (h, p) => Ternary<double, int, MidpointRounding, double>(h, p, (a, b, c) => Math.Round(a, b, c)));
            Add("SystemMath.__Round__SystemDouble_SystemInt32__SystemDouble", new[] { typeof(double), typeof(int), typeof(double) }, (h, p) => Binary<double, int, double>(h, p, (a, b) => Math.Round(a, b)));
            Add("SystemMath.__Round__SystemDouble_SystemMidpointRounding__SystemDouble", new[] { typeof(double), typeof(MidpointRounding), typeof(double) }, (h, p) => Binary<double, MidpointRounding, double>(h, p, (a, b) => Math.Round(a, b)));
            Add("SystemMath.__Round__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Round(a)));
            Add("SystemMath.__Sign__SystemDouble__SystemInt32", new[] { typeof(double), typeof(int) }, (h, p) => Unary<double, int>(h, p, (a) => Math.Sign(a)));
            Add("SystemMath.__Sign__SystemInt16__SystemInt32", new[] { typeof(short), typeof(int) }, (h, p) => Unary<short, int>(h, p, (a) => Math.Sign(a)));
            Add("SystemMath.__Sign__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int) }, (h, p) => Unary<int, int>(h, p, (a) => Math.Sign(a)));
            Add("SystemMath.__Sign__SystemInt64__SystemInt32", new[] { typeof(long), typeof(int) }, (h, p) => Unary<long, int>(h, p, (a) => Math.Sign(a)));
            Add("SystemMath.__Sign__SystemSByte__SystemInt32", new[] { typeof(sbyte), typeof(int) }, (h, p) => Unary<sbyte, int>(h, p, (a) => Math.Sign(a)));
            Add("SystemMath.__Sign__SystemSingle__SystemInt32", new[] { typeof(float), typeof(int) }, (h, p) => Unary<float, int>(h, p, (a) => Math.Sign(a)));
            Add("SystemMath.__Sin__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Sin(a)));
            Add("SystemMath.__Sinh__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Sinh(a)));
            Add("SystemMath.__Sqrt__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Sqrt(a)));
            Add("SystemMath.__Tan__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Tan(a)));
            Add("SystemMath.__Tanh__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Tanh(a)));
            Add("SystemMath.__Truncate__SystemDouble__SystemDouble", new[] { typeof(double), typeof(double) }, (h, p) => Unary<double, double>(h, p, (a) => Math.Truncate(a)));
            Add("SystemMath.__get_E__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, Math.E));
            Add("SystemMath.__get_PI__SystemDouble", new[] { typeof(double) }, (h, p) => Const<double>(h, p, Math.PI));
            Add("SystemObjectArray.__Get__SystemInt32__SystemObject", new[] { typeof(object[]), typeof(int), typeof(object) }, ObjectArrayGet);
            Add("SystemObjectArray.__Set__SystemInt32_SystemObject__SystemVoid", new[] { typeof(object[]), typeof(int), typeof(object) }, ObjectArraySet);
            Add("SystemObjectArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemSingle.__CompareTo__SystemSingle__SystemInt32", new[] { typeof(float), typeof(float), typeof(int) }, (h, p) => Binary<float, float, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemSingle.__Equals__SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, (h, p) => Binary<float, float, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemSingle.__GetHashCode__SystemInt32", new[] { typeof(float), typeof(int) }, (h, p) => Unary<float, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemSingle.__IsInfinity__SystemSingle__SystemBoolean", new[] { typeof(float), typeof(bool) }, (h, p) => Unary<float, bool>(h, p, (a) => float.IsInfinity(a)));
            Add("SystemSingle.__IsNaN__SystemSingle__SystemBoolean", new[] { typeof(float), typeof(bool) }, (h, p) => Unary<float, bool>(h, p, (a) => float.IsNaN(a)));
            Add("SystemSingle.__IsNegativeInfinity__SystemSingle__SystemBoolean", new[] { typeof(float), typeof(bool) }, (h, p) => Unary<float, bool>(h, p, (a) => float.IsNegativeInfinity(a)));
            Add("SystemSingle.__IsPositiveInfinity__SystemSingle__SystemBoolean", new[] { typeof(float), typeof(bool) }, (h, p) => Unary<float, bool>(h, p, (a) => float.IsPositiveInfinity(a)));
            Add("SystemSingle.__ToString__SystemString", new[] { typeof(float), typeof(string) }, (h, p) => Unary<float, string>(h, p, (a) => a.ToString()));
            Add("SystemSingle.__get_Epsilon__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.Epsilon));
            Add("SystemSingle.__get_MaxValue__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.MaxValue));
            Add("SystemSingle.__get_MinValue__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.MinValue));
            Add("SystemSingle.__get_NaN__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.NaN));
            Add("SystemSingle.__get_NegativeInfinity__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.NegativeInfinity));
            Add("SystemSingle.__get_PositiveInfinity__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.PositiveInfinity));
            Add("SystemSingle.__op_Addition__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, F_SystemSingle___op_Addition__SystemSingle_SystemSingle__SystemSingle);
            Add("SystemSingle.__op_Division__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, F_SystemSingle___op_Division__SystemSingle_SystemSingle__SystemSingle);
            Add("SystemSingle.__op_Equality__SystemSingle_SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, F_SystemSingle___op_Equality__SystemSingle_SystemSingle__SystemBoolean);
            Add("SystemSingle.__op_GreaterThanOrEqual__SystemSingle_SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, F_SystemSingle___op_GreaterThanOrEqual__SystemSingle_SystemSingle__SystemBoolean);
            Add("SystemSingle.__op_GreaterThan__SystemSingle_SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, F_SystemSingle___op_GreaterThan__SystemSingle_SystemSingle__SystemBoolean);
            Add("SystemSingle.__op_Inequality__SystemSingle_SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, F_SystemSingle___op_Inequality__SystemSingle_SystemSingle__SystemBoolean);
            Add("SystemSingle.__op_LessThanOrEqual__SystemSingle_SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, F_SystemSingle___op_LessThanOrEqual__SystemSingle_SystemSingle__SystemBoolean);
            Add("SystemSingle.__op_LessThan__SystemSingle_SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, F_SystemSingle___op_LessThan__SystemSingle_SystemSingle__SystemBoolean);
            Add("SystemSingle.__op_Multiplication__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, F_SystemSingle___op_Multiplication__SystemSingle_SystemSingle__SystemSingle);
            Add("SystemSingle.__op_Remainder__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, F_SystemSingle___op_Remainder__SystemSingle_SystemSingle__SystemSingle);
            Add("SystemSingle.__op_Subtraction__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, F_SystemSingle___op_Subtraction__SystemSingle_SystemSingle__SystemSingle);
            Add("SystemSingle.__op_UnaryMinus__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, F_SystemSingle___op_UnaryMinus__SystemSingle__SystemSingle);
            Add("SystemSingleArray.__Get__SystemInt32__SystemSingle", new[] { typeof(float[]), typeof(int), typeof(float) }, ArrayGet<float>);
            Add("SystemSingleArray.__Set__SystemInt32_SystemSingle__SystemVoid", new[] { typeof(float[]), typeof(int), typeof(float) }, ArraySet<float>);
            Add("SystemSingleArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemString.__CompareTo__SystemString__SystemInt32", new[] { typeof(string), typeof(string), typeof(int) }, (h, p) => Binary<string, string, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemString.__Compare__SystemString_SystemString__SystemInt32", new[] { typeof(string), typeof(string), typeof(int) }, (h, p) => Binary<string, string, int>(h, p, (a, b) => string.Compare(a, b)));
            Add("SystemString.__Concat__SystemString_SystemString_SystemString_SystemString__SystemString", new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) }, (h, p) => Quaternary<string, string, string, string, string>(h, p, (a, b, c, d) => a + b + c + d));
            Add("SystemString.__Concat__SystemString_SystemString_SystemString__SystemString", new[] { typeof(string), typeof(string), typeof(string), typeof(string) }, (h, p) => Ternary<string, string, string, string>(h, p, (a, b, c) => a + b + c));
            Add("SystemString.__Concat__SystemString_SystemString__SystemString", new[] { typeof(string), typeof(string), typeof(string) }, (h, p) => Binary<string, string, string>(h, p, (a, b) => a + b));
            Add("SystemString.__Contains__SystemString__SystemBoolean", new[] { typeof(string), typeof(string), typeof(bool) }, (h, p) => Binary<string, string, bool>(h, p, (a, b) => a.Contains(b)));
            Add("SystemString.__EndsWith__SystemString__SystemBoolean", new[] { typeof(string), typeof(string), typeof(bool) }, (h, p) => Binary<string, string, bool>(h, p, (a, b) => a.EndsWith(b)));
            Add("SystemString.__Equals__SystemString_SystemString__SystemBoolean", new[] { typeof(string), typeof(string), typeof(bool) }, (h, p) => Binary<string, string, bool>(h, p, (a, b) => string.Equals(a, b)));
            Add("SystemString.__Equals__SystemString__SystemBoolean", new[] { typeof(string), typeof(string), typeof(bool) }, (h, p) => Binary<string, string, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemString.__GetHashCode__SystemInt32", new[] { typeof(string), typeof(int) }, (h, p) => Unary<string, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemString.__IndexOf__SystemChar_SystemInt32__SystemInt32", new[] { typeof(string), typeof(char), typeof(int), typeof(int) }, (h, p) => Ternary<string, char, int, int>(h, p, (a, b, c) => a.IndexOf(b, c)));
            Add("SystemString.__IndexOf__SystemChar__SystemInt32", new[] { typeof(string), typeof(char), typeof(int) }, (h, p) => Binary<string, char, int>(h, p, (a, b) => a.IndexOf(b)));
            Add("SystemString.__IndexOf__SystemString__SystemInt32", new[] { typeof(string), typeof(string), typeof(int) }, (h, p) => Binary<string, string, int>(h, p, (a, b) => a.IndexOf(b)));
            Add("SystemString.__Insert__SystemInt32_SystemString__SystemString", new[] { typeof(string), typeof(int), typeof(string), typeof(string) }, (h, p) => Ternary<string, int, string, string>(h, p, (a, b, c) => a.Insert(b, c)));
            Add("SystemString.__IsNullOrEmpty__SystemString__SystemBoolean", new[] { typeof(string), typeof(bool) }, (h, p) => Unary<string, bool>(h, p, (a) => string.IsNullOrEmpty(a)));
            Add("SystemString.__IsNullOrWhiteSpace__SystemString__SystemBoolean", new[] { typeof(string), typeof(bool) }, (h, p) => Unary<string, bool>(h, p, (a) => string.IsNullOrWhiteSpace(a)));
            Add("SystemString.__LastIndexOf__SystemChar_SystemInt32__SystemInt32", new[] { typeof(string), typeof(char), typeof(int), typeof(int) }, (h, p) => Ternary<string, char, int, int>(h, p, (a, b, c) => a.LastIndexOf(b, c)));
            Add("SystemString.__LastIndexOf__SystemChar__SystemInt32", new[] { typeof(string), typeof(char), typeof(int) }, (h, p) => Binary<string, char, int>(h, p, (a, b) => a.LastIndexOf(b)));
            Add("SystemString.__PadLeft__SystemInt32__SystemString", new[] { typeof(string), typeof(int), typeof(string) }, (h, p) => Binary<string, int, string>(h, p, (a, b) => a.PadLeft(b)));
            Add("SystemString.__PadRight__SystemInt32__SystemString", new[] { typeof(string), typeof(int), typeof(string) }, (h, p) => Binary<string, int, string>(h, p, (a, b) => a.PadRight(b)));
            Add("SystemString.__Remove__SystemInt32_SystemInt32__SystemString", new[] { typeof(string), typeof(int), typeof(int), typeof(string) }, (h, p) => Ternary<string, int, int, string>(h, p, (a, b, c) => a.Remove(b, c)));
            Add("SystemString.__Remove__SystemInt32__SystemString", new[] { typeof(string), typeof(int), typeof(string) }, (h, p) => Binary<string, int, string>(h, p, (a, b) => a.Remove(b)));
            Add("SystemString.__Replace__SystemChar_SystemChar__SystemString", new[] { typeof(string), typeof(char), typeof(char), typeof(string) }, (h, p) => Ternary<string, char, char, string>(h, p, (a, b, c) => a.Replace(b, c)));
            Add("SystemString.__Replace__SystemString_SystemString__SystemString", new[] { typeof(string), typeof(string), typeof(string), typeof(string) }, (h, p) => Ternary<string, string, string, string>(h, p, (a, b, c) => a.Replace(b, c)));
            Add("SystemString.__StartsWith__SystemString__SystemBoolean", new[] { typeof(string), typeof(string), typeof(bool) }, (h, p) => Binary<string, string, bool>(h, p, (a, b) => a.StartsWith(b)));
            Add("SystemString.__Substring__SystemInt32_SystemInt32__SystemString", new[] { typeof(string), typeof(int), typeof(int), typeof(string) }, (h, p) => Ternary<string, int, int, string>(h, p, (a, b, c) => a.Substring(b, c)));
            Add("SystemString.__Substring__SystemInt32__SystemString", new[] { typeof(string), typeof(int), typeof(string) }, (h, p) => Binary<string, int, string>(h, p, (a, b) => a.Substring(b)));
            Add("SystemString.__ToCharArray__SystemCharArray", new[] { typeof(string), typeof(char[]) }, (h, p) => Unary<string, char[]>(h, p, (a) => a.ToCharArray()));
            Add("SystemString.__ToLowerInvariant__SystemString", new[] { typeof(string), typeof(string) }, (h, p) => Unary<string, string>(h, p, (a) => a.ToLowerInvariant()));
            Add("SystemString.__ToLower__SystemString", new[] { typeof(string), typeof(string) }, (h, p) => Unary<string, string>(h, p, (a) => a.ToLower()));
            Add("SystemString.__ToUpperInvariant__SystemString", new[] { typeof(string), typeof(string) }, (h, p) => Unary<string, string>(h, p, (a) => a.ToUpperInvariant()));
            Add("SystemString.__ToUpper__SystemString", new[] { typeof(string), typeof(string) }, (h, p) => Unary<string, string>(h, p, (a) => a.ToUpper()));
            Add("SystemString.__TrimEnd__SystemCharArray__SystemString", new[] { typeof(string), typeof(char[]), typeof(string) }, (h, p) => Binary<string, char[], string>(h, p, (a, b) => a.TrimEnd(b)));
            Add("SystemString.__TrimEnd__SystemChar__SystemString", new[] { typeof(string), typeof(char), typeof(string) }, (h, p) => Binary<string, char, string>(h, p, (a, b) => a.TrimEnd(b)));
            Add("SystemString.__TrimEnd__SystemString", new[] { typeof(string), typeof(string) }, (h, p) => Unary<string, string>(h, p, (a) => a.TrimEnd()));
            Add("SystemString.__TrimStart__SystemCharArray__SystemString", new[] { typeof(string), typeof(char[]), typeof(string) }, (h, p) => Binary<string, char[], string>(h, p, (a, b) => a.TrimStart(b)));
            Add("SystemString.__TrimStart__SystemChar__SystemString", new[] { typeof(string), typeof(char), typeof(string) }, (h, p) => Binary<string, char, string>(h, p, (a, b) => a.TrimStart(b)));
            Add("SystemString.__TrimStart__SystemString", new[] { typeof(string), typeof(string) }, (h, p) => Unary<string, string>(h, p, (a) => a.TrimStart()));
            Add("SystemString.__Trim__SystemCharArray__SystemString", new[] { typeof(string), typeof(char[]), typeof(string) }, (h, p) => Binary<string, char[], string>(h, p, (a, b) => a.Trim(b)));
            Add("SystemString.__Trim__SystemChar__SystemString", new[] { typeof(string), typeof(char), typeof(string) }, (h, p) => Binary<string, char, string>(h, p, (a, b) => a.Trim(b)));
            Add("SystemString.__Trim__SystemString", new[] { typeof(string), typeof(string) }, (h, p) => Unary<string, string>(h, p, (a) => a.Trim()));
            Add("SystemString.__get_Empty__SystemString", new[] { typeof(string) }, (h, p) => Const<string>(h, p, string.Empty));
            Add("SystemString.__get_Length__SystemInt32", new[] { typeof(string), typeof(int) }, F_SystemString___get_Length__SystemInt32);
            Add("SystemString.__op_Equality__SystemString_SystemString__SystemBoolean", new[] { typeof(string), typeof(string), typeof(bool) }, F_SystemString___op_Equality__SystemString_SystemString__SystemBoolean);
            Add("SystemString.__op_Inequality__SystemString_SystemString__SystemBoolean", new[] { typeof(string), typeof(string), typeof(bool) }, (h, p) => Binary<string, string, bool>(h, p, (a, b) => a != b));
            Add("SystemStringArray.__Get__SystemInt32__SystemString", new[] { typeof(string[]), typeof(int), typeof(string) }, F_SystemStringArray___Get__SystemInt32__SystemString);
            Add("SystemStringArray.__Set__SystemInt32_SystemString__SystemVoid", new[] { typeof(string[]), typeof(int), typeof(string) }, F_SystemStringArray___Set__SystemInt32_SystemString__SystemVoid);
            Add("SystemStringArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemUInt32.__CompareTo__SystemUInt32__SystemInt32", new[] { typeof(uint), typeof(uint), typeof(int) }, (h, p) => Binary<uint, uint, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemUInt32.__Equals__SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(uint), typeof(bool) }, (h, p) => Binary<uint, uint, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemUInt32.__GetHashCode__SystemInt32", new[] { typeof(uint), typeof(int) }, (h, p) => Unary<uint, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemUInt32.__ToString__SystemString", new[] { typeof(uint), typeof(string) }, (h, p) => Unary<uint, string>(h, p, (a) => a.ToString()));
            Add("SystemUInt32.__get_MaxValue__SystemUInt32", new[] { typeof(uint) }, (h, p) => Const<uint>(h, p, uint.MaxValue));
            Add("SystemUInt32.__get_MinValue__SystemUInt32", new[] { typeof(uint) }, (h, p) => Const<uint>(h, p, 0u));
            Add("SystemUInt32.__op_Addition__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => a + b));
            Add("SystemUInt32.__op_Division__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => a / b));
            Add("SystemUInt32.__op_Equality__SystemUInt32_SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(uint), typeof(bool) }, (h, p) => Binary<uint, uint, bool>(h, p, (a, b) => a == b));
            Add("SystemUInt32.__op_GreaterThanOrEqual__SystemUInt32_SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(uint), typeof(bool) }, (h, p) => Binary<uint, uint, bool>(h, p, (a, b) => a >= b));
            Add("SystemUInt32.__op_GreaterThan__SystemUInt32_SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(uint), typeof(bool) }, (h, p) => Binary<uint, uint, bool>(h, p, (a, b) => a > b));
            Add("SystemUInt32.__op_Inequality__SystemUInt32_SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(uint), typeof(bool) }, (h, p) => Binary<uint, uint, bool>(h, p, (a, b) => a != b));
            Add("SystemUInt32.__op_LeftShift__SystemUInt32_SystemInt32__SystemUInt32", new[] { typeof(uint), typeof(int), typeof(uint) }, (h, p) => Binary<uint, int, uint>(h, p, (a, b) => a << b));
            Add("SystemUInt32.__op_LessThanOrEqual__SystemUInt32_SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(uint), typeof(bool) }, (h, p) => Binary<uint, uint, bool>(h, p, (a, b) => a <= b));
            Add("SystemUInt32.__op_LessThan__SystemUInt32_SystemUInt32__SystemBoolean", new[] { typeof(uint), typeof(uint), typeof(bool) }, (h, p) => Binary<uint, uint, bool>(h, p, (a, b) => a < b));
            Add("SystemUInt32.__op_LogicalAnd__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => a & b));
            Add("SystemUInt32.__op_LogicalOr__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => a | b));
            Add("SystemUInt32.__op_LogicalXor__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => a ^ b));
            Add("SystemUInt32.__op_Multiplication__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => a * b));
            Add("SystemUInt32.__op_RightShift__SystemUInt32_SystemInt32__SystemUInt32", new[] { typeof(uint), typeof(int), typeof(uint) }, (h, p) => Binary<uint, int, uint>(h, p, (a, b) => a >> b));
            Add("SystemUInt32.__op_Subtraction__SystemUInt32_SystemUInt32__SystemUInt32", new[] { typeof(uint), typeof(uint), typeof(uint) }, (h, p) => Binary<uint, uint, uint>(h, p, (a, b) => a - b));
            Add("SystemUInt32.__op_UnaryMinus__SystemUInt32__SystemInt64", new[] { typeof(uint), typeof(long) }, (h, p) => Unary<uint, long>(h, p, (a) => 0L - (long)a));
            Add("SystemUInt32Array.__Get__SystemInt32__SystemUInt32", new[] { typeof(uint[]), typeof(int), typeof(uint) }, ArrayGet<uint>);
            Add("SystemUInt32Array.__Set__SystemInt32_SystemUInt32__SystemVoid", new[] { typeof(uint[]), typeof(int), typeof(uint) }, ArraySet<uint>);
            Add("SystemUInt32Array.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("SystemUInt64.__CompareTo__SystemUInt64__SystemInt32", new[] { typeof(ulong), typeof(ulong), typeof(int) }, (h, p) => Binary<ulong, ulong, int>(h, p, (a, b) => a.CompareTo(b)));
            Add("SystemUInt64.__Equals__SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(ulong), typeof(bool) }, (h, p) => Binary<ulong, ulong, bool>(h, p, (a, b) => a.Equals(b)));
            Add("SystemUInt64.__GetHashCode__SystemInt32", new[] { typeof(ulong), typeof(int) }, (h, p) => Unary<ulong, int>(h, p, (a) => a.GetHashCode()));
            Add("SystemUInt64.__ToString__SystemString", new[] { typeof(ulong), typeof(string) }, (h, p) => Unary<ulong, string>(h, p, (a) => a.ToString()));
            Add("SystemUInt64.__get_MaxValue__SystemUInt64", new[] { typeof(ulong) }, (h, p) => Const<ulong>(h, p, ulong.MaxValue));
            Add("SystemUInt64.__get_MinValue__SystemUInt64", new[] { typeof(ulong) }, (h, p) => Const<ulong>(h, p, 0uL));
            Add("SystemUInt64.__op_Addition__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, F_SystemUInt64___op_Addition__SystemUInt64_SystemUInt64__SystemUInt64);
            Add("SystemUInt64.__op_Division__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, F_SystemUInt64___op_Division__SystemUInt64_SystemUInt64__SystemUInt64);
            Add("SystemUInt64.__op_Equality__SystemUInt64_SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(ulong), typeof(bool) }, F_SystemUInt64___op_Equality__SystemUInt64_SystemUInt64__SystemBoolean);
            Add("SystemUInt64.__op_GreaterThanOrEqual__SystemUInt64_SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(ulong), typeof(bool) }, F_SystemUInt64___op_GreaterThanOrEqual__SystemUInt64_SystemUInt64__SystemBoolean);
            Add("SystemUInt64.__op_GreaterThan__SystemUInt64_SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(ulong), typeof(bool) }, F_SystemUInt64___op_GreaterThan__SystemUInt64_SystemUInt64__SystemBoolean);
            Add("SystemUInt64.__op_Inequality__SystemUInt64_SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(ulong), typeof(bool) }, F_SystemUInt64___op_Inequality__SystemUInt64_SystemUInt64__SystemBoolean);
            Add("SystemUInt64.__op_LeftShift__SystemUInt64_SystemInt32__SystemUInt64", new[] { typeof(ulong), typeof(int), typeof(ulong) }, F_SystemUInt64___op_LeftShift__SystemUInt64_SystemInt32__SystemUInt64);
            Add("SystemUInt64.__op_LessThanOrEqual__SystemUInt64_SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(ulong), typeof(bool) }, F_SystemUInt64___op_LessThanOrEqual__SystemUInt64_SystemUInt64__SystemBoolean);
            Add("SystemUInt64.__op_LessThan__SystemUInt64_SystemUInt64__SystemBoolean", new[] { typeof(ulong), typeof(ulong), typeof(bool) }, F_SystemUInt64___op_LessThan__SystemUInt64_SystemUInt64__SystemBoolean);
            Add("SystemUInt64.__op_LogicalAnd__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, F_SystemUInt64___op_LogicalAnd__SystemUInt64_SystemUInt64__SystemUInt64);
            Add("SystemUInt64.__op_LogicalOr__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, F_SystemUInt64___op_LogicalOr__SystemUInt64_SystemUInt64__SystemUInt64);
            Add("SystemUInt64.__op_LogicalXor__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, F_SystemUInt64___op_LogicalXor__SystemUInt64_SystemUInt64__SystemUInt64);
            Add("SystemUInt64.__op_Multiplication__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, F_SystemUInt64___op_Multiplication__SystemUInt64_SystemUInt64__SystemUInt64);
            Add("SystemUInt64.__op_RightShift__SystemUInt64_SystemInt32__SystemUInt64", new[] { typeof(ulong), typeof(int), typeof(ulong) }, F_SystemUInt64___op_RightShift__SystemUInt64_SystemInt32__SystemUInt64);
            Add("SystemUInt64.__op_Subtraction__SystemUInt64_SystemUInt64__SystemUInt64", new[] { typeof(ulong), typeof(ulong), typeof(ulong) }, F_SystemUInt64___op_Subtraction__SystemUInt64_SystemUInt64__SystemUInt64);
            Add("SystemUInt64Array.__Get__SystemInt32__SystemUInt64", new[] { typeof(ulong[]), typeof(int), typeof(ulong) }, ArrayGet<ulong>);
            Add("SystemUInt64Array.__Set__SystemInt32_SystemUInt64__SystemVoid", new[] { typeof(ulong[]), typeof(int), typeof(ulong) }, ArraySet<ulong>);
            Add("SystemUInt64Array.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("UnityEngineColor.__Equals__UnityEngineColor__SystemBoolean", new[] { typeof(Color), typeof(Color), typeof(bool) }, (h, p) => Binary<Color, Color, bool>(h, p, (a, b) => a.Equals(b)));
            Add("UnityEngineColor.__LerpUnclamped__UnityEngineColor_UnityEngineColor_SystemSingle__UnityEngineColor", new[] { typeof(Color), typeof(Color), typeof(float), typeof(Color) }, (h, p) => Ternary<Color, Color, float, Color>(h, p, (a, b, c) => Color.LerpUnclamped(a, b, c)));
            Add("UnityEngineColor.__Lerp__UnityEngineColor_UnityEngineColor_SystemSingle__UnityEngineColor", new[] { typeof(Color), typeof(Color), typeof(float), typeof(Color) }, (h, p) => Ternary<Color, Color, float, Color>(h, p, (a, b, c) => Color.Lerp(a, b, c)));
            Add("UnityEngineColor.__ctor__SystemSingle_SystemSingle_SystemSingle_SystemSingle__UnityEngineColor", new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(Color) }, (h, p) => Quaternary<float, float, float, float, Color>(h, p, (a, b, c, d) => new Color(a, b, c, d)));
            Add("UnityEngineColor.__ctor__SystemSingle_SystemSingle_SystemSingle__UnityEngineColor", new[] { typeof(float), typeof(float), typeof(float), typeof(Color) }, (h, p) => Ternary<float, float, float, Color>(h, p, (a, b, c) => new Color(a, b, c)));
            Add("UnityEngineColor.__get_a__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => Unary<Color, float>(h, p, (a) => a.a));
            Add("UnityEngineColor.__get_b__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => Unary<Color, float>(h, p, (a) => a.b));
            Add("UnityEngineColor.__get_black__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.black));
            Add("UnityEngineColor.__get_blue__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.blue));
            Add("UnityEngineColor.__get_clear__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.clear));
            Add("UnityEngineColor.__get_cyan__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.cyan));
            Add("UnityEngineColor.__get_g__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => Unary<Color, float>(h, p, (a) => a.g));
            Add("UnityEngineColor.__get_gamma__UnityEngineColor", new[] { typeof(Color), typeof(Color) }, (h, p) => Unary<Color, Color>(h, p, (a) => a.gamma));
            Add("UnityEngineColor.__get_gray__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.gray));
            Add("UnityEngineColor.__get_grayscale__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => Unary<Color, float>(h, p, (a) => a.grayscale));
            Add("UnityEngineColor.__get_green__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.green));
            Add("UnityEngineColor.__get_grey__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.grey));
            Add("UnityEngineColor.__get_linear__UnityEngineColor", new[] { typeof(Color), typeof(Color) }, (h, p) => Unary<Color, Color>(h, p, (a) => a.linear));
            Add("UnityEngineColor.__get_magenta__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.magenta));
            Add("UnityEngineColor.__get_maxColorComponent__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => Unary<Color, float>(h, p, (a) => a.maxColorComponent));
            Add("UnityEngineColor.__get_r__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => Unary<Color, float>(h, p, (a) => a.r));
            Add("UnityEngineColor.__get_red__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.red));
            Add("UnityEngineColor.__get_white__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.white));
            Add("UnityEngineColor.__get_yellow__UnityEngineColor", new[] { typeof(Color) }, (h, p) => Const<Color>(h, p, Color.yellow));
            Add("UnityEngineColor.__op_Addition__UnityEngineColor_UnityEngineColor__UnityEngineColor", new[] { typeof(Color), typeof(Color), typeof(Color) }, (h, p) => Binary<Color, Color, Color>(h, p, (a, b) => a + b));
            Add("UnityEngineColor.__op_Division__UnityEngineColor_SystemSingle__UnityEngineColor", new[] { typeof(Color), typeof(float), typeof(Color) }, (h, p) => Binary<Color, float, Color>(h, p, (a, b) => a / b));
            Add("UnityEngineColor.__op_Equality__UnityEngineColor_UnityEngineColor__SystemBoolean", new[] { typeof(Color), typeof(Color), typeof(bool) }, (h, p) => Binary<Color, Color, bool>(h, p, (a, b) => a == b));
            Add("UnityEngineColor.__op_Implicit__UnityEngineColor__UnityEngineVector4", new[] { typeof(Color), typeof(Vector4) }, (h, p) => Unary<Color, Vector4>(h, p, (a) => (Vector4)a));
            Add("UnityEngineColor.__op_Implicit__UnityEngineVector4__UnityEngineColor", new[] { typeof(Vector4), typeof(Color) }, (h, p) => Unary<Vector4, Color>(h, p, (a) => (Color)a));
            Add("UnityEngineColor.__op_Inequality__UnityEngineColor_UnityEngineColor__SystemBoolean", new[] { typeof(Color), typeof(Color), typeof(bool) }, (h, p) => Binary<Color, Color, bool>(h, p, (a, b) => a != b));
            Add("UnityEngineColor.__op_Multiply__SystemSingle_UnityEngineColor__UnityEngineColor", new[] { typeof(float), typeof(Color), typeof(Color) }, (h, p) => Binary<float, Color, Color>(h, p, (a, b) => a * b));
            Add("UnityEngineColor.__op_Multiply__UnityEngineColor_SystemSingle__UnityEngineColor", new[] { typeof(Color), typeof(float), typeof(Color) }, (h, p) => Binary<Color, float, Color>(h, p, (a, b) => a * b));
            Add("UnityEngineColor.__op_Multiply__UnityEngineColor_UnityEngineColor__UnityEngineColor", new[] { typeof(Color), typeof(Color), typeof(Color) }, (h, p) => Binary<Color, Color, Color>(h, p, (a, b) => a * b));
            Add("UnityEngineColor.__op_Subtraction__UnityEngineColor_UnityEngineColor__UnityEngineColor", new[] { typeof(Color), typeof(Color), typeof(Color) }, (h, p) => Binary<Color, Color, Color>(h, p, (a, b) => a - b));
            Add("UnityEngineColor.__set_a__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => SetField<Color, float>(h, p, (s, v) => { s.a = v; return s; }));
            Add("UnityEngineColor.__set_b__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => SetField<Color, float>(h, p, (s, v) => { s.b = v; return s; }));
            Add("UnityEngineColor.__set_g__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => SetField<Color, float>(h, p, (s, v) => { s.g = v; return s; }));
            Add("UnityEngineColor.__set_r__SystemSingle", new[] { typeof(Color), typeof(float) }, (h, p) => SetField<Color, float>(h, p, (s, v) => { s.r = v; return s; }));
            Add("UnityEngineColorArray.__Get__SystemInt32__UnityEngineColor", new[] { typeof(Color[]), typeof(int), typeof(Color) }, ArrayGet<Color>);
            Add("UnityEngineColorArray.__Set__SystemInt32_UnityEngineColor__SystemVoid", new[] { typeof(Color[]), typeof(int), typeof(Color) }, ArraySet<Color>);
            Add("UnityEngineColorArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("UnityEngineMathf.__Abs__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int) }, (h, p) => Unary<int, int>(h, p, (a) => Mathf.Abs(a)));
            Add("UnityEngineMathf.__Abs__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Abs(a)));
            Add("UnityEngineMathf.__Acos__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Acos(a)));
            Add("UnityEngineMathf.__Approximately__SystemSingle_SystemSingle__SystemBoolean", new[] { typeof(float), typeof(float), typeof(bool) }, (h, p) => Binary<float, float, bool>(h, p, (a, b) => Mathf.Approximately(a, b)));
            Add("UnityEngineMathf.__Asin__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Asin(a)));
            Add("UnityEngineMathf.__Atan2__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.Atan2(a, b)));
            Add("UnityEngineMathf.__Atan__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Atan(a)));
            Add("UnityEngineMathf.__CeilToInt__SystemSingle__SystemInt32", new[] { typeof(float), typeof(int) }, (h, p) => Unary<float, int>(h, p, (a) => Mathf.CeilToInt(a)));
            Add("UnityEngineMathf.__Ceil__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Ceil(a)));
            Add("UnityEngineMathf.__Clamp01__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Clamp01(a)));
            Add("UnityEngineMathf.__Clamp__SystemInt32_SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int), typeof(int) }, (h, p) => Ternary<int, int, int, int>(h, p, (a, b, c) => Mathf.Clamp(a, b, c)));
            Add("UnityEngineMathf.__Clamp__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.Clamp(a, b, c)));
            Add("UnityEngineMathf.__ClosestPowerOfTwo__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int) }, (h, p) => Unary<int, int>(h, p, (a) => Mathf.ClosestPowerOfTwo(a)));
            Add("UnityEngineMathf.__Cos__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Cos(a)));
            Add("UnityEngineMathf.__DeltaAngle__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.DeltaAngle(a, b)));
            Add("UnityEngineMathf.__Exp__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Exp(a)));
            Add("UnityEngineMathf.__FloorToInt__SystemSingle__SystemInt32", new[] { typeof(float), typeof(int) }, (h, p) => Unary<float, int>(h, p, (a) => Mathf.FloorToInt(a)));
            Add("UnityEngineMathf.__Floor__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Floor(a)));
            Add("UnityEngineMathf.__InverseLerp__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.InverseLerp(a, b, c)));
            Add("UnityEngineMathf.__IsPowerOfTwo__SystemInt32__SystemBoolean", new[] { typeof(int), typeof(bool) }, (h, p) => Unary<int, bool>(h, p, (a) => Mathf.IsPowerOfTwo(a)));
            Add("UnityEngineMathf.__LerpAngle__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.LerpAngle(a, b, c)));
            Add("UnityEngineMathf.__LerpUnclamped__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.LerpUnclamped(a, b, c)));
            Add("UnityEngineMathf.__Lerp__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.Lerp(a, b, c)));
            Add("UnityEngineMathf.__Log10__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Log10(a)));
            Add("UnityEngineMathf.__Log__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.Log(a, b)));
            Add("UnityEngineMathf.__Log__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Log(a)));
            Add("UnityEngineMathf.__Max__SystemInt32Array__SystemInt32", new[] { typeof(int[]), typeof(int) }, (h, p) => Unary<int[], int>(h, p, (a) => Mathf.Max(a)));
            Add("UnityEngineMathf.__Max__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, (h, p) => Binary<int, int, int>(h, p, (a, b) => Mathf.Max(a, b)));
            Add("UnityEngineMathf.__Max__SystemSingleArray__SystemSingle", new[] { typeof(float[]), typeof(float) }, (h, p) => Unary<float[], float>(h, p, (a) => Mathf.Max(a)));
            Add("UnityEngineMathf.__Max__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.Max(a, b)));
            Add("UnityEngineMathf.__Min__SystemInt32Array__SystemInt32", new[] { typeof(int[]), typeof(int) }, (h, p) => Unary<int[], int>(h, p, (a) => Mathf.Min(a)));
            Add("UnityEngineMathf.__Min__SystemInt32_SystemInt32__SystemInt32", new[] { typeof(int), typeof(int), typeof(int) }, (h, p) => Binary<int, int, int>(h, p, (a, b) => Mathf.Min(a, b)));
            Add("UnityEngineMathf.__Min__SystemSingleArray__SystemSingle", new[] { typeof(float[]), typeof(float) }, (h, p) => Unary<float[], float>(h, p, (a) => Mathf.Min(a)));
            Add("UnityEngineMathf.__Min__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.Min(a, b)));
            Add("UnityEngineMathf.__MoveTowardsAngle__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.MoveTowardsAngle(a, b, c)));
            Add("UnityEngineMathf.__MoveTowards__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.MoveTowards(a, b, c)));
            Add("UnityEngineMathf.__NextPowerOfTwo__SystemInt32__SystemInt32", new[] { typeof(int), typeof(int) }, (h, p) => Unary<int, int>(h, p, (a) => Mathf.NextPowerOfTwo(a)));
            Add("UnityEngineMathf.__PingPong__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.PingPong(a, b)));
            Add("UnityEngineMathf.__Pow__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.Pow(a, b)));
            Add("UnityEngineMathf.__Repeat__SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float) }, (h, p) => Binary<float, float, float>(h, p, (a, b) => Mathf.Repeat(a, b)));
            Add("UnityEngineMathf.__RoundToInt__SystemSingle__SystemInt32", new[] { typeof(float), typeof(int) }, (h, p) => Unary<float, int>(h, p, (a) => Mathf.RoundToInt(a)));
            Add("UnityEngineMathf.__Round__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Round(a)));
            Add("UnityEngineMathf.__Sign__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Sign(a)));
            Add("UnityEngineMathf.__Sin__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Sin(a)));
            Add("UnityEngineMathf.__SmoothStep__SystemSingle_SystemSingle_SystemSingle__SystemSingle", new[] { typeof(float), typeof(float), typeof(float), typeof(float) }, (h, p) => Ternary<float, float, float, float>(h, p, (a, b, c) => Mathf.SmoothStep(a, b, c)));
            Add("UnityEngineMathf.__Sqrt__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Sqrt(a)));
            Add("UnityEngineMathf.__Tan__SystemSingle__SystemSingle", new[] { typeof(float), typeof(float) }, (h, p) => Unary<float, float>(h, p, (a) => Mathf.Tan(a)));
            Add("UnityEngineMathf.__get_Deg2Rad__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, MathF.PI / 180f));
            Add("UnityEngineMathf.__get_Epsilon__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, Mathf.Epsilon));
            Add("UnityEngineMathf.__get_Infinity__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.PositiveInfinity));
            Add("UnityEngineMathf.__get_NegativeInfinity__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, float.NegativeInfinity));
            Add("UnityEngineMathf.__get_PI__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, MathF.PI));
            Add("UnityEngineMathf.__get_Rad2Deg__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, 57.29578f));
            Add("UnityEngineQuaternion.__AngleAxis__SystemSingle_UnityEngineVector3__UnityEngineQuaternion", new[] { typeof(float), typeof(Vector3), typeof(Quaternion) }, (h, p) => Binary<float, Vector3, Quaternion>(h, p, (a, b) => Quaternion.AngleAxis(a, b)));
            Add("UnityEngineQuaternion.__Angle__UnityEngineQuaternion_UnityEngineQuaternion__SystemSingle", new[] { typeof(Quaternion), typeof(Quaternion), typeof(float) }, (h, p) => Binary<Quaternion, Quaternion, float>(h, p, (a, b) => Quaternion.Angle(a, b)));
            Add("UnityEngineQuaternion.__Dot__UnityEngineQuaternion_UnityEngineQuaternion__SystemSingle", new[] { typeof(Quaternion), typeof(Quaternion), typeof(float) }, (h, p) => Binary<Quaternion, Quaternion, float>(h, p, (a, b) => Quaternion.Dot(a, b)));
            Add("UnityEngineQuaternion.__Equals__UnityEngineQuaternion__SystemBoolean", new[] { typeof(Quaternion), typeof(Quaternion), typeof(bool) }, (h, p) => Binary<Quaternion, Quaternion, bool>(h, p, (a, b) => a.Equals(b)));
            Add("UnityEngineQuaternion.__Euler__SystemSingle_SystemSingle_SystemSingle__UnityEngineQuaternion", new[] { typeof(float), typeof(float), typeof(float), typeof(Quaternion) }, (h, p) => Ternary<float, float, float, Quaternion>(h, p, (a, b, c) => Quaternion.Euler(a, b, c)));
            Add("UnityEngineQuaternion.__Euler__UnityEngineVector3__UnityEngineQuaternion", new[] { typeof(Vector3), typeof(Quaternion) }, (h, p) => Unary<Vector3, Quaternion>(h, p, (a) => Quaternion.Euler(a)));
            Add("UnityEngineQuaternion.__FromToRotation__UnityEngineVector3_UnityEngineVector3__UnityEngineQuaternion", new[] { typeof(Vector3), typeof(Vector3), typeof(Quaternion) }, (h, p) => Binary<Vector3, Vector3, Quaternion>(h, p, (a, b) => Quaternion.FromToRotation(a, b)));
            Add("UnityEngineQuaternion.__Inverse__UnityEngineQuaternion__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion) }, (h, p) => Unary<Quaternion, Quaternion>(h, p, (a) => Quaternion.Inverse(a)));
            Add("UnityEngineQuaternion.__LerpUnclamped__UnityEngineQuaternion_UnityEngineQuaternion_SystemSingle__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion), typeof(float), typeof(Quaternion) }, (h, p) => Ternary<Quaternion, Quaternion, float, Quaternion>(h, p, (a, b, c) => Quaternion.LerpUnclamped(a, b, c)));
            Add("UnityEngineQuaternion.__Lerp__UnityEngineQuaternion_UnityEngineQuaternion_SystemSingle__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion), typeof(float), typeof(Quaternion) }, (h, p) => Ternary<Quaternion, Quaternion, float, Quaternion>(h, p, (a, b, c) => Quaternion.Lerp(a, b, c)));
            Add("UnityEngineQuaternion.__Normalize__SystemVoid", new[] { typeof(Quaternion) }, NoOp<Quaternion>);
            Add("UnityEngineQuaternion.__Normalize__UnityEngineQuaternion__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion) }, (h, p) => Unary<Quaternion, Quaternion>(h, p, (a) => Quaternion.Normalize(a)));
            Add("UnityEngineQuaternion.__RotateTowards__UnityEngineQuaternion_UnityEngineQuaternion_SystemSingle__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion), typeof(float), typeof(Quaternion) }, (h, p) => Ternary<Quaternion, Quaternion, float, Quaternion>(h, p, (a, b, c) => Quaternion.RotateTowards(a, b, c)));
            Add("UnityEngineQuaternion.__SlerpUnclamped__UnityEngineQuaternion_UnityEngineQuaternion_SystemSingle__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion), typeof(float), typeof(Quaternion) }, (h, p) => Ternary<Quaternion, Quaternion, float, Quaternion>(h, p, (a, b, c) => Quaternion.SlerpUnclamped(a, b, c)));
            Add("UnityEngineQuaternion.__Slerp__UnityEngineQuaternion_UnityEngineQuaternion_SystemSingle__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion), typeof(float), typeof(Quaternion) }, (h, p) => Ternary<Quaternion, Quaternion, float, Quaternion>(h, p, (a, b, c) => Quaternion.Slerp(a, b, c)));
            Add("UnityEngineQuaternion.__ctor__SystemSingle_SystemSingle_SystemSingle_SystemSingle__UnityEngineQuaternion", new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(Quaternion) }, (h, p) => Quaternary<float, float, float, float, Quaternion>(h, p, (a, b, c, d) => new Quaternion(a, b, c, d)));
            Add("UnityEngineQuaternion.__get_eulerAngles__UnityEngineVector3", new[] { typeof(Quaternion), typeof(Vector3) }, (h, p) => Unary<Quaternion, Vector3>(h, p, (a) => a.eulerAngles));
            Add("UnityEngineQuaternion.__get_identity__UnityEngineQuaternion", new[] { typeof(Quaternion) }, (h, p) => Const<Quaternion>(h, p, Quaternion.identity));
            Add("UnityEngineQuaternion.__get_kEpsilon__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, 1E-06f));
            Add("UnityEngineQuaternion.__get_normalized__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion) }, (h, p) => Unary<Quaternion, Quaternion>(h, p, (a) => a.normalized));
            Add("UnityEngineQuaternion.__get_w__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => Unary<Quaternion, float>(h, p, (a) => a.w));
            Add("UnityEngineQuaternion.__get_x__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => Unary<Quaternion, float>(h, p, (a) => a.x));
            Add("UnityEngineQuaternion.__get_y__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => Unary<Quaternion, float>(h, p, (a) => a.y));
            Add("UnityEngineQuaternion.__get_z__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => Unary<Quaternion, float>(h, p, (a) => a.z));
            Add("UnityEngineQuaternion.__op_Equality__UnityEngineQuaternion_UnityEngineQuaternion__SystemBoolean", new[] { typeof(Quaternion), typeof(Quaternion), typeof(bool) }, (h, p) => Binary<Quaternion, Quaternion, bool>(h, p, (a, b) => a == b));
            Add("UnityEngineQuaternion.__op_Inequality__UnityEngineQuaternion_UnityEngineQuaternion__SystemBoolean", new[] { typeof(Quaternion), typeof(Quaternion), typeof(bool) }, (h, p) => Binary<Quaternion, Quaternion, bool>(h, p, (a, b) => a != b));
            Add("UnityEngineQuaternion.__op_Multiply__UnityEngineQuaternion_UnityEngineQuaternion__UnityEngineQuaternion", new[] { typeof(Quaternion), typeof(Quaternion), typeof(Quaternion) }, (h, p) => Binary<Quaternion, Quaternion, Quaternion>(h, p, (a, b) => a * b));
            Add("UnityEngineQuaternion.__op_Multiply__UnityEngineQuaternion_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Quaternion), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Quaternion, Vector3, Vector3>(h, p, (a, b) => a * b));
            Add("UnityEngineQuaternion.__set_eulerAngles__UnityEngineVector3__SystemVoid", new[] { typeof(Quaternion), typeof(Vector3) }, (h, p) => SetField<Quaternion, Vector3>(h, p, (s, v) => { s.eulerAngles = v; return s; }));
            Add("UnityEngineQuaternion.__set_w__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => SetField<Quaternion, float>(h, p, (s, v) => { s.w = v; return s; }));
            Add("UnityEngineQuaternion.__set_x__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => SetField<Quaternion, float>(h, p, (s, v) => { s.x = v; return s; }));
            Add("UnityEngineQuaternion.__set_y__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => SetField<Quaternion, float>(h, p, (s, v) => { s.y = v; return s; }));
            Add("UnityEngineQuaternion.__set_z__SystemSingle", new[] { typeof(Quaternion), typeof(float) }, (h, p) => SetField<Quaternion, float>(h, p, (s, v) => { s.z = v; return s; }));
            Add("UnityEngineQuaternionArray.__Get__SystemInt32__UnityEngineQuaternion", new[] { typeof(Quaternion[]), typeof(int), typeof(Quaternion) }, ArrayGet<Quaternion>);
            Add("UnityEngineQuaternionArray.__Set__SystemInt32_UnityEngineQuaternion__SystemVoid", new[] { typeof(Quaternion[]), typeof(int), typeof(Quaternion) }, ArraySet<Quaternion>);
            Add("UnityEngineQuaternionArray.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("UnityEngineVector2.__Angle__UnityEngineVector2_UnityEngineVector2__SystemSingle", new[] { typeof(Vector2), typeof(Vector2), typeof(float) }, (h, p) => Binary<Vector2, Vector2, float>(h, p, (a, b) => Vector2.Angle(a, b)));
            Add("UnityEngineVector2.__ClampMagnitude__UnityEngineVector2_SystemSingle__UnityEngineVector2", new[] { typeof(Vector2), typeof(float), typeof(Vector2) }, (h, p) => Binary<Vector2, float, Vector2>(h, p, (a, b) => Vector2.ClampMagnitude(a, b)));
            Add("UnityEngineVector2.__Distance__UnityEngineVector2_UnityEngineVector2__SystemSingle", new[] { typeof(Vector2), typeof(Vector2), typeof(float) }, (h, p) => Binary<Vector2, Vector2, float>(h, p, (a, b) => Vector2.Distance(a, b)));
            Add("UnityEngineVector2.__Dot__UnityEngineVector2_UnityEngineVector2__SystemSingle", new[] { typeof(Vector2), typeof(Vector2), typeof(float) }, (h, p) => Binary<Vector2, Vector2, float>(h, p, (a, b) => Vector2.Dot(a, b)));
            Add("UnityEngineVector2.__Equals__UnityEngineVector2__SystemBoolean", new[] { typeof(Vector2), typeof(Vector2), typeof(bool) }, (h, p) => Binary<Vector2, Vector2, bool>(h, p, (a, b) => a.Equals(b)));
            Add("UnityEngineVector2.__LerpUnclamped__UnityEngineVector2_UnityEngineVector2_SystemSingle__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(float), typeof(Vector2) }, (h, p) => Ternary<Vector2, Vector2, float, Vector2>(h, p, (a, b, c) => Vector2.LerpUnclamped(a, b, c)));
            Add("UnityEngineVector2.__Lerp__UnityEngineVector2_UnityEngineVector2_SystemSingle__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(float), typeof(Vector2) }, (h, p) => Ternary<Vector2, Vector2, float, Vector2>(h, p, (a, b, c) => Vector2.Lerp(a, b, c)));
            Add("UnityEngineVector2.__Max__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => Vector2.Max(a, b)));
            Add("UnityEngineVector2.__Min__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => Vector2.Min(a, b)));
            Add("UnityEngineVector2.__MoveTowards__UnityEngineVector2_UnityEngineVector2_SystemSingle__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(float), typeof(Vector2) }, (h, p) => Ternary<Vector2, Vector2, float, Vector2>(h, p, (a, b, c) => Vector2.MoveTowards(a, b, c)));
            Add("UnityEngineVector2.__Normalize__SystemVoid", new[] { typeof(Vector2) }, NoOp<Vector2>);
            Add("UnityEngineVector2.__Perpendicular__UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2) }, (h, p) => Unary<Vector2, Vector2>(h, p, (a) => Vector2.Perpendicular(a)));
            Add("UnityEngineVector2.__Reflect__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => Vector2.Reflect(a, b)));
            Add("UnityEngineVector2.__Scale__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => Vector2.Scale(a, b)));
            Add("UnityEngineVector2.__Scale__UnityEngineVector2__SystemVoid", new[] { typeof(Vector2), typeof(Vector2) }, (h, p) => NoOp<Vector2>(h, p) && p[1] < h.Length && h[p[1]] is StrongBox<Vector2>);
            Add("UnityEngineVector2.__SignedAngle__UnityEngineVector2_UnityEngineVector2__SystemSingle", new[] { typeof(Vector2), typeof(Vector2), typeof(float) }, (h, p) => Binary<Vector2, Vector2, float>(h, p, (a, b) => Vector2.SignedAngle(a, b)));
            Add("UnityEngineVector2.__SqrMagnitude__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => Unary<Vector2, float>(h, p, (a) => a.SqrMagnitude()));
            Add("UnityEngineVector2.__SqrMagnitude__UnityEngineVector2__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => Unary<Vector2, float>(h, p, (a) => Vector2.SqrMagnitude(a)));
            Add("UnityEngineVector2.__ctor__SystemSingle_SystemSingle__UnityEngineVector2", new[] { typeof(float), typeof(float), typeof(Vector2) }, (h, p) => Binary<float, float, Vector2>(h, p, (a, b) => new Vector2(a, b)));
            Add("UnityEngineVector2.__get_down__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.down));
            Add("UnityEngineVector2.__get_kEpsilonNormalSqrt__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, 1E-15f));
            Add("UnityEngineVector2.__get_kEpsilon__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, 1E-05f));
            Add("UnityEngineVector2.__get_left__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.left));
            Add("UnityEngineVector2.__get_magnitude__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => Unary<Vector2, float>(h, p, (a) => a.magnitude));
            Add("UnityEngineVector2.__get_negativeInfinity__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.negativeInfinity));
            Add("UnityEngineVector2.__get_normalized__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2) }, (h, p) => Unary<Vector2, Vector2>(h, p, (a) => a.normalized));
            Add("UnityEngineVector2.__get_one__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.one));
            Add("UnityEngineVector2.__get_positiveInfinity__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.positiveInfinity));
            Add("UnityEngineVector2.__get_right__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.right));
            Add("UnityEngineVector2.__get_sqrMagnitude__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => Unary<Vector2, float>(h, p, (a) => a.sqrMagnitude));
            Add("UnityEngineVector2.__get_up__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.up));
            Add("UnityEngineVector2.__get_x__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => Unary<Vector2, float>(h, p, (a) => a.x));
            Add("UnityEngineVector2.__get_y__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => Unary<Vector2, float>(h, p, (a) => a.y));
            Add("UnityEngineVector2.__get_zero__UnityEngineVector2", new[] { typeof(Vector2) }, (h, p) => Const<Vector2>(h, p, Vector2.zero));
            Add("UnityEngineVector2.__op_Addition__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => a + b));
            Add("UnityEngineVector2.__op_Division__UnityEngineVector2_SystemSingle__UnityEngineVector2", new[] { typeof(Vector2), typeof(float), typeof(Vector2) }, (h, p) => Binary<Vector2, float, Vector2>(h, p, (a, b) => a / b));
            Add("UnityEngineVector2.__op_Division__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => a / b));
            Add("UnityEngineVector2.__op_Equality__UnityEngineVector2_UnityEngineVector2__SystemBoolean", new[] { typeof(Vector2), typeof(Vector2), typeof(bool) }, (h, p) => Binary<Vector2, Vector2, bool>(h, p, (a, b) => a == b));
            Add("UnityEngineVector2.__op_Implicit__UnityEngineVector2__UnityEngineVector3", new[] { typeof(Vector2), typeof(Vector3) }, (h, p) => Unary<Vector2, Vector3>(h, p, (a) => (Vector3)a));
            Add("UnityEngineVector2.__op_Implicit__UnityEngineVector3__UnityEngineVector2", new[] { typeof(Vector3), typeof(Vector2) }, (h, p) => Unary<Vector3, Vector2>(h, p, (a) => (Vector2)a));
            Add("UnityEngineVector2.__op_Inequality__UnityEngineVector2_UnityEngineVector2__SystemBoolean", new[] { typeof(Vector2), typeof(Vector2), typeof(bool) }, (h, p) => Binary<Vector2, Vector2, bool>(h, p, (a, b) => a != b));
            Add("UnityEngineVector2.__op_Multiply__SystemSingle_UnityEngineVector2__UnityEngineVector2", new[] { typeof(float), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<float, Vector2, Vector2>(h, p, (a, b) => a * b));
            Add("UnityEngineVector2.__op_Multiply__UnityEngineVector2_SystemSingle__UnityEngineVector2", new[] { typeof(Vector2), typeof(float), typeof(Vector2) }, (h, p) => Binary<Vector2, float, Vector2>(h, p, (a, b) => a * b));
            Add("UnityEngineVector2.__op_Multiply__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => a * b));
            Add("UnityEngineVector2.__op_Subtraction__UnityEngineVector2_UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, (h, p) => Binary<Vector2, Vector2, Vector2>(h, p, (a, b) => a - b));
            Add("UnityEngineVector2.__op_UnaryNegation__UnityEngineVector2__UnityEngineVector2", new[] { typeof(Vector2), typeof(Vector2) }, (h, p) => Unary<Vector2, Vector2>(h, p, (a) => -a));
            Add("UnityEngineVector2.__set_x__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => SetField<Vector2, float>(h, p, (s, v) => { s.x = v; return s; }));
            Add("UnityEngineVector2.__set_y__SystemSingle", new[] { typeof(Vector2), typeof(float) }, (h, p) => SetField<Vector2, float>(h, p, (s, v) => { s.y = v; return s; }));
            Add("UnityEngineVector2Array.__Get__SystemInt32__UnityEngineVector2", new[] { typeof(Vector2[]), typeof(int), typeof(Vector2) }, ArrayGet<Vector2>);
            Add("UnityEngineVector2Array.__Set__SystemInt32_UnityEngineVector2__SystemVoid", new[] { typeof(Vector2[]), typeof(int), typeof(Vector2) }, ArraySet<Vector2>);
            Add("UnityEngineVector2Array.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("UnityEngineVector3.__Angle__UnityEngineVector3_UnityEngineVector3__SystemSingle", new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, (h, p) => Binary<Vector3, Vector3, float>(h, p, (a, b) => Vector3.Angle(a, b)));
            Add("UnityEngineVector3.__ClampMagnitude__UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(float), typeof(Vector3) }, (h, p) => Binary<Vector3, float, Vector3>(h, p, (a, b) => Vector3.ClampMagnitude(a, b)));
            Add("UnityEngineVector3.__Cross__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Vector3, Vector3, Vector3>(h, p, (a, b) => Vector3.Cross(a, b)));
            Add("UnityEngineVector3.__Distance__UnityEngineVector3_UnityEngineVector3__SystemSingle", new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, (h, p) => Binary<Vector3, Vector3, float>(h, p, (a, b) => Vector3.Distance(a, b)));
            Add("UnityEngineVector3.__Dot__UnityEngineVector3_UnityEngineVector3__SystemSingle", new[] { typeof(Vector3), typeof(Vector3), typeof(float) }, F_UnityEngineVector3___Dot__UnityEngineVector3_UnityEngineVector3__SystemSingle);
            Add("UnityEngineVector3.__Equals__UnityEngineVector3__SystemBoolean", new[] { typeof(Vector3), typeof(Vector3), typeof(bool) }, (h, p) => Binary<Vector3, Vector3, bool>(h, p, (a, b) => a.Equals(b)));
            Add("UnityEngineVector3.__LerpUnclamped__UnityEngineVector3_UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(Vector3) }, (h, p) => Ternary<Vector3, Vector3, float, Vector3>(h, p, (a, b, c) => Vector3.LerpUnclamped(a, b, c)));
            Add("UnityEngineVector3.__Lerp__UnityEngineVector3_UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(Vector3) }, (h, p) => Ternary<Vector3, Vector3, float, Vector3>(h, p, (a, b, c) => Vector3.Lerp(a, b, c)));
            Add("UnityEngineVector3.__Magnitude__UnityEngineVector3__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => Unary<Vector3, float>(h, p, (a) => Vector3.Magnitude(a)));
            Add("UnityEngineVector3.__Max__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Vector3, Vector3, Vector3>(h, p, (a, b) => Vector3.Max(a, b)));
            Add("UnityEngineVector3.__Min__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Vector3, Vector3, Vector3>(h, p, (a, b) => Vector3.Min(a, b)));
            Add("UnityEngineVector3.__MoveTowards__UnityEngineVector3_UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(Vector3) }, (h, p) => Ternary<Vector3, Vector3, float, Vector3>(h, p, (a, b, c) => Vector3.MoveTowards(a, b, c)));
            Add("UnityEngineVector3.__Normalize__SystemVoid", new[] { typeof(Vector3) }, NoOp<Vector3>);
            Add("UnityEngineVector3.__Normalize__UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3) }, (h, p) => Unary<Vector3, Vector3>(h, p, (a) => Vector3.Normalize(a)));
            Add("UnityEngineVector3.__ProjectOnPlane__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Vector3, Vector3, Vector3>(h, p, (a, b) => Vector3.ProjectOnPlane(a, b)));
            Add("UnityEngineVector3.__Project__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Vector3, Vector3, Vector3>(h, p, (a, b) => Vector3.Project(a, b)));
            Add("UnityEngineVector3.__Reflect__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Vector3, Vector3, Vector3>(h, p, (a, b) => Vector3.Reflect(a, b)));
            Add("UnityEngineVector3.__RotateTowards__UnityEngineVector3_UnityEngineVector3_SystemSingle_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(float), typeof(Vector3) }, (h, p) => Quaternary<Vector3, Vector3, float, float, Vector3>(h, p, (a, b, c, d) => Vector3.RotateTowards(a, b, c, d)));
            Add("UnityEngineVector3.__Scale__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, (h, p) => Binary<Vector3, Vector3, Vector3>(h, p, (a, b) => Vector3.Scale(a, b)));
            Add("UnityEngineVector3.__Scale__UnityEngineVector3__SystemVoid", new[] { typeof(Vector3), typeof(Vector3) }, (h, p) => NoOp<Vector3>(h, p) && p[1] < h.Length && h[p[1]] is StrongBox<Vector3>);
            Add("UnityEngineVector3.__SignedAngle__UnityEngineVector3_UnityEngineVector3_UnityEngineVector3__SystemSingle", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(float) }, (h, p) => Ternary<Vector3, Vector3, Vector3, float>(h, p, (a, b, c) => Vector3.SignedAngle(a, b, c)));
            Add("UnityEngineVector3.__SlerpUnclamped__UnityEngineVector3_UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(Vector3) }, (h, p) => Ternary<Vector3, Vector3, float, Vector3>(h, p, (a, b, c) => Vector3.SlerpUnclamped(a, b, c)));
            Add("UnityEngineVector3.__Slerp__UnityEngineVector3_UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(float), typeof(Vector3) }, (h, p) => Ternary<Vector3, Vector3, float, Vector3>(h, p, (a, b, c) => Vector3.Slerp(a, b, c)));
            Add("UnityEngineVector3.__SqrMagnitude__UnityEngineVector3__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => Unary<Vector3, float>(h, p, (a) => Vector3.SqrMagnitude(a)));
            Add("UnityEngineVector3.__ctor__SystemSingle_SystemSingle_SystemSingle__UnityEngineVector3", new[] { typeof(float), typeof(float), typeof(float), typeof(Vector3) }, (h, p) => Ternary<float, float, float, Vector3>(h, p, (a, b, c) => new Vector3(a, b, c)));
            Add("UnityEngineVector3.__ctor__SystemSingle_SystemSingle__UnityEngineVector3", new[] { typeof(float), typeof(float), typeof(Vector3) }, (h, p) => Binary<float, float, Vector3>(h, p, (a, b) => new Vector3(a, b)));
            Add("UnityEngineVector3.__get_back__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.back));
            Add("UnityEngineVector3.__get_down__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.down));
            Add("UnityEngineVector3.__get_forward__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.forward));
            Add("UnityEngineVector3.__get_kEpsilonNormalSqrt__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, 1E-15f));
            Add("UnityEngineVector3.__get_kEpsilon__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, 1E-05f));
            Add("UnityEngineVector3.__get_left__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.left));
            Add("UnityEngineVector3.__get_magnitude__SystemSingle", new[] { typeof(Vector3), typeof(float) }, F_UnityEngineVector3___get_magnitude__SystemSingle);
            Add("UnityEngineVector3.__get_negativeInfinity__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.negativeInfinity));
            Add("UnityEngineVector3.__get_normalized__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3) }, (h, p) => Unary<Vector3, Vector3>(h, p, (a) => a.normalized));
            Add("UnityEngineVector3.__get_one__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.one));
            Add("UnityEngineVector3.__get_positiveInfinity__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.positiveInfinity));
            Add("UnityEngineVector3.__get_right__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.right));
            Add("UnityEngineVector3.__get_sqrMagnitude__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => Unary<Vector3, float>(h, p, (a) => a.sqrMagnitude));
            Add("UnityEngineVector3.__get_up__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.up));
            Add("UnityEngineVector3.__get_x__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => Unary<Vector3, float>(h, p, (a) => a.x));
            Add("UnityEngineVector3.__get_y__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => Unary<Vector3, float>(h, p, (a) => a.y));
            Add("UnityEngineVector3.__get_z__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => Unary<Vector3, float>(h, p, (a) => a.z));
            Add("UnityEngineVector3.__get_zero__UnityEngineVector3", new[] { typeof(Vector3) }, (h, p) => Const<Vector3>(h, p, Vector3.zero));
            Add("UnityEngineVector3.__op_Addition__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, F_UnityEngineVector3___op_Addition__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3);
            Add("UnityEngineVector3.__op_Division__UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(float), typeof(Vector3) }, (h, p) => Binary<Vector3, float, Vector3>(h, p, (a, b) => a / b));
            Add("UnityEngineVector3.__op_Equality__UnityEngineVector3_UnityEngineVector3__SystemBoolean", new[] { typeof(Vector3), typeof(Vector3), typeof(bool) }, (h, p) => Binary<Vector3, Vector3, bool>(h, p, (a, b) => a == b));
            Add("UnityEngineVector3.__op_Inequality__UnityEngineVector3_UnityEngineVector3__SystemBoolean", new[] { typeof(Vector3), typeof(Vector3), typeof(bool) }, (h, p) => Binary<Vector3, Vector3, bool>(h, p, (a, b) => a != b));
            Add("UnityEngineVector3.__op_Multiply__SystemSingle_UnityEngineVector3__UnityEngineVector3", new[] { typeof(float), typeof(Vector3), typeof(Vector3) }, F_UnityEngineVector3___op_Multiply__SystemSingle_UnityEngineVector3__UnityEngineVector3);
            Add("UnityEngineVector3.__op_Multiply__UnityEngineVector3_SystemSingle__UnityEngineVector3", new[] { typeof(Vector3), typeof(float), typeof(Vector3) }, F_UnityEngineVector3___op_Multiply__UnityEngineVector3_SystemSingle__UnityEngineVector3);
            Add("UnityEngineVector3.__op_Subtraction__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3), typeof(Vector3) }, F_UnityEngineVector3___op_Subtraction__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3);
            Add("UnityEngineVector3.__op_UnaryNegation__UnityEngineVector3__UnityEngineVector3", new[] { typeof(Vector3), typeof(Vector3) }, (h, p) => Unary<Vector3, Vector3>(h, p, (a) => -a));
            Add("UnityEngineVector3.__set_x__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => SetField<Vector3, float>(h, p, (s, v) => { s.x = v; return s; }));
            Add("UnityEngineVector3.__set_y__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => SetField<Vector3, float>(h, p, (s, v) => { s.y = v; return s; }));
            Add("UnityEngineVector3.__set_z__SystemSingle", new[] { typeof(Vector3), typeof(float) }, (h, p) => SetField<Vector3, float>(h, p, (s, v) => { s.z = v; return s; }));
            Add("UnityEngineVector3Array.__Get__SystemInt32__UnityEngineVector3", new[] { typeof(Vector3[]), typeof(int), typeof(Vector3) }, ArrayGet<Vector3>);
            Add("UnityEngineVector3Array.__Set__SystemInt32_UnityEngineVector3__SystemVoid", new[] { typeof(Vector3[]), typeof(int), typeof(Vector3) }, ArraySet<Vector3>);
            Add("UnityEngineVector3Array.__get_Length__SystemInt32", new[] { typeof(Array), typeof(int) }, ArrayLength);
            Add("UnityEngineVector4.__Distance__UnityEngineVector4_UnityEngineVector4__SystemSingle", new[] { typeof(Vector4), typeof(Vector4), typeof(float) }, (h, p) => Binary<Vector4, Vector4, float>(h, p, (a, b) => Vector4.Distance(a, b)));
            Add("UnityEngineVector4.__Dot__UnityEngineVector4_UnityEngineVector4__SystemSingle", new[] { typeof(Vector4), typeof(Vector4), typeof(float) }, (h, p) => Binary<Vector4, Vector4, float>(h, p, (a, b) => Vector4.Dot(a, b)));
            Add("UnityEngineVector4.__Equals__UnityEngineVector4__SystemBoolean", new[] { typeof(Vector4), typeof(Vector4), typeof(bool) }, (h, p) => Binary<Vector4, Vector4, bool>(h, p, (a, b) => a.Equals(b)));
            Add("UnityEngineVector4.__LerpUnclamped__UnityEngineVector4_UnityEngineVector4_SystemSingle__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(float), typeof(Vector4) }, (h, p) => Ternary<Vector4, Vector4, float, Vector4>(h, p, (a, b, c) => Vector4.LerpUnclamped(a, b, c)));
            Add("UnityEngineVector4.__Lerp__UnityEngineVector4_UnityEngineVector4_SystemSingle__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(float), typeof(Vector4) }, (h, p) => Ternary<Vector4, Vector4, float, Vector4>(h, p, (a, b, c) => Vector4.Lerp(a, b, c)));
            Add("UnityEngineVector4.__Magnitude__UnityEngineVector4__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => Vector4.Magnitude(a)));
            Add("UnityEngineVector4.__Max__UnityEngineVector4_UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(Vector4) }, (h, p) => Binary<Vector4, Vector4, Vector4>(h, p, (a, b) => Vector4.Max(a, b)));
            Add("UnityEngineVector4.__Min__UnityEngineVector4_UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(Vector4) }, (h, p) => Binary<Vector4, Vector4, Vector4>(h, p, (a, b) => Vector4.Min(a, b)));
            Add("UnityEngineVector4.__MoveTowards__UnityEngineVector4_UnityEngineVector4_SystemSingle__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(float), typeof(Vector4) }, (h, p) => Ternary<Vector4, Vector4, float, Vector4>(h, p, (a, b, c) => Vector4.MoveTowards(a, b, c)));
            Add("UnityEngineVector4.__Normalize__SystemVoid", new[] { typeof(Vector4) }, NoOp<Vector4>);
            Add("UnityEngineVector4.__Normalize__UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4) }, (h, p) => Unary<Vector4, Vector4>(h, p, (a) => Vector4.Normalize(a)));
            Add("UnityEngineVector4.__Project__UnityEngineVector4_UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(Vector4) }, (h, p) => Binary<Vector4, Vector4, Vector4>(h, p, (a, b) => Vector4.Project(a, b)));
            Add("UnityEngineVector4.__Scale__UnityEngineVector4_UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(Vector4) }, (h, p) => Binary<Vector4, Vector4, Vector4>(h, p, (a, b) => Vector4.Scale(a, b)));
            Add("UnityEngineVector4.__Scale__UnityEngineVector4__SystemVoid", new[] { typeof(Vector4), typeof(Vector4) }, (h, p) => NoOp<Vector4>(h, p) && p[1] < h.Length && h[p[1]] is StrongBox<Vector4>);
            Add("UnityEngineVector4.__SqrMagnitude__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => a.SqrMagnitude()));
            Add("UnityEngineVector4.__SqrMagnitude__UnityEngineVector4__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => Vector4.SqrMagnitude(a)));
            Add("UnityEngineVector4.__ctor__SystemSingle_SystemSingle_SystemSingle_SystemSingle__UnityEngineVector4", new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(Vector4) }, (h, p) => Quaternary<float, float, float, float, Vector4>(h, p, (a, b, c, d) => new Vector4(a, b, c, d)));
            Add("UnityEngineVector4.__ctor__SystemSingle_SystemSingle_SystemSingle__UnityEngineVector4", new[] { typeof(float), typeof(float), typeof(float), typeof(Vector4) }, (h, p) => Ternary<float, float, float, Vector4>(h, p, (a, b, c) => new Vector4(a, b, c)));
            Add("UnityEngineVector4.__ctor__SystemSingle_SystemSingle__UnityEngineVector4", new[] { typeof(float), typeof(float), typeof(Vector4) }, (h, p) => Binary<float, float, Vector4>(h, p, (a, b) => new Vector4(a, b)));
            Add("UnityEngineVector4.__get_kEpsilon__SystemSingle", new[] { typeof(float) }, (h, p) => Const<float>(h, p, 1E-05f));
            Add("UnityEngineVector4.__get_magnitude__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => a.magnitude));
            Add("UnityEngineVector4.__get_negativeInfinity__UnityEngineVector4", new[] { typeof(Vector4) }, (h, p) => Const<Vector4>(h, p, Vector4.negativeInfinity));
            Add("UnityEngineVector4.__get_normalized__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4) }, (h, p) => Unary<Vector4, Vector4>(h, p, (a) => a.normalized));
            Add("UnityEngineVector4.__get_one__UnityEngineVector4", new[] { typeof(Vector4) }, (h, p) => Const<Vector4>(h, p, Vector4.one));
            Add("UnityEngineVector4.__get_positiveInfinity__UnityEngineVector4", new[] { typeof(Vector4) }, (h, p) => Const<Vector4>(h, p, Vector4.positiveInfinity));
            Add("UnityEngineVector4.__get_sqrMagnitude__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => a.sqrMagnitude));
            Add("UnityEngineVector4.__get_w__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => a.w));
            Add("UnityEngineVector4.__get_x__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => a.x));
            Add("UnityEngineVector4.__get_y__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => a.y));
            Add("UnityEngineVector4.__get_z__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => Unary<Vector4, float>(h, p, (a) => a.z));
            Add("UnityEngineVector4.__get_zero__UnityEngineVector4", new[] { typeof(Vector4) }, (h, p) => Const<Vector4>(h, p, Vector4.zero));
            Add("UnityEngineVector4.__op_Addition__UnityEngineVector4_UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(Vector4) }, (h, p) => Binary<Vector4, Vector4, Vector4>(h, p, (a, b) => a + b));
            Add("UnityEngineVector4.__op_Division__UnityEngineVector4_SystemSingle__UnityEngineVector4", new[] { typeof(Vector4), typeof(float), typeof(Vector4) }, (h, p) => Binary<Vector4, float, Vector4>(h, p, (a, b) => a / b));
            Add("UnityEngineVector4.__op_Equality__UnityEngineVector4_UnityEngineVector4__SystemBoolean", new[] { typeof(Vector4), typeof(Vector4), typeof(bool) }, (h, p) => Binary<Vector4, Vector4, bool>(h, p, (a, b) => a == b));
            Add("UnityEngineVector4.__op_Implicit__UnityEngineVector2__UnityEngineVector4", new[] { typeof(Vector2), typeof(Vector4) }, (h, p) => Unary<Vector2, Vector4>(h, p, (a) => (Vector4)a));
            Add("UnityEngineVector4.__op_Implicit__UnityEngineVector3__UnityEngineVector4", new[] { typeof(Vector3), typeof(Vector4) }, (h, p) => Unary<Vector3, Vector4>(h, p, (a) => (Vector4)a));
            Add("UnityEngineVector4.__op_Implicit__UnityEngineVector4__UnityEngineVector2", new[] { typeof(Vector4), typeof(Vector2) }, (h, p) => Unary<Vector4, Vector2>(h, p, (a) => (Vector2)a));
            Add("UnityEngineVector4.__op_Implicit__UnityEngineVector4__UnityEngineVector3", new[] { typeof(Vector4), typeof(Vector3) }, (h, p) => Unary<Vector4, Vector3>(h, p, (a) => (Vector3)a));
            Add("UnityEngineVector4.__op_Inequality__UnityEngineVector4_UnityEngineVector4__SystemBoolean", new[] { typeof(Vector4), typeof(Vector4), typeof(bool) }, (h, p) => Binary<Vector4, Vector4, bool>(h, p, (a, b) => a != b));
            Add("UnityEngineVector4.__op_Multiply__SystemSingle_UnityEngineVector4__UnityEngineVector4", new[] { typeof(float), typeof(Vector4), typeof(Vector4) }, (h, p) => Binary<float, Vector4, Vector4>(h, p, (a, b) => a * b));
            Add("UnityEngineVector4.__op_Multiply__UnityEngineVector4_SystemSingle__UnityEngineVector4", new[] { typeof(Vector4), typeof(float), typeof(Vector4) }, (h, p) => Binary<Vector4, float, Vector4>(h, p, (a, b) => a * b));
            Add("UnityEngineVector4.__op_Subtraction__UnityEngineVector4_UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4), typeof(Vector4) }, (h, p) => Binary<Vector4, Vector4, Vector4>(h, p, (a, b) => a - b));
            Add("UnityEngineVector4.__op_UnaryNegation__UnityEngineVector4__UnityEngineVector4", new[] { typeof(Vector4), typeof(Vector4) }, (h, p) => Unary<Vector4, Vector4>(h, p, (a) => -a));
            Add("UnityEngineVector4.__set_w__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => SetField<Vector4, float>(h, p, (s, v) => { s.w = v; return s; }));
            Add("UnityEngineVector4.__set_x__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => SetField<Vector4, float>(h, p, (s, v) => { s.x = v; return s; }));
            Add("UnityEngineVector4.__set_y__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => SetField<Vector4, float>(h, p, (s, v) => { s.y = v; return s; }));
            Add("UnityEngineVector4.__set_z__SystemSingle", new[] { typeof(Vector4), typeof(float) }, (h, p) => SetField<Vector4, float>(h, p, (s, v) => { s.z = v; return s; }));
        }

        static bool F_SystemInt32___op_Addition__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value + b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_Division__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value / b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_Equality__SystemInt32_SystemInt32__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value == b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_GreaterThanOrEqual__SystemInt32_SystemInt32__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value >= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_GreaterThan__SystemInt32_SystemInt32__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value > b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_Inequality__SystemInt32_SystemInt32__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value != b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_LeftShift__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value << b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_LessThanOrEqual__SystemInt32_SystemInt32__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value <= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_LessThan__SystemInt32_SystemInt32__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value < b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_LogicalAnd__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value & b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_LogicalOr__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value | b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_LogicalXor__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value ^ b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_Multiplication__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value * b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_Remainder__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value % b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_RightShift__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value >> b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_Subtraction__SystemInt32_SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value - b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32___op_UnaryMinus__SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int> a_ && p[1] < h.Length)
            {
                HeapAccess.Set(h, p[1], -a_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_Addition__SystemUInt64_SystemUInt64__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value + b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_Division__SystemUInt64_SystemUInt64__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value / b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_Equality__SystemUInt64_SystemUInt64__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value == b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_GreaterThanOrEqual__SystemUInt64_SystemUInt64__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value >= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_GreaterThan__SystemUInt64_SystemUInt64__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value > b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_Inequality__SystemUInt64_SystemUInt64__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value != b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_LeftShift__SystemUInt64_SystemInt32__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value << b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_LessThanOrEqual__SystemUInt64_SystemUInt64__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value <= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_LessThan__SystemUInt64_SystemUInt64__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value < b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_LogicalAnd__SystemUInt64_SystemUInt64__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value & b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_LogicalOr__SystemUInt64_SystemUInt64__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value | b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_LogicalXor__SystemUInt64_SystemUInt64__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value ^ b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_Multiplication__SystemUInt64_SystemUInt64__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value * b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_RightShift__SystemUInt64_SystemInt32__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value >> b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemUInt64___op_Subtraction__SystemUInt64_SystemUInt64__SystemUInt64(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<ulong> a_ && p[1] < h.Length && h[p[1]] is StrongBox<ulong> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value - b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_Addition__SystemSingle_SystemSingle__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value + b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_Division__SystemSingle_SystemSingle__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value / b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_Equality__SystemSingle_SystemSingle__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value == b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_GreaterThanOrEqual__SystemSingle_SystemSingle__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value >= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_GreaterThan__SystemSingle_SystemSingle__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value > b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_Inequality__SystemSingle_SystemSingle__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value != b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_LessThanOrEqual__SystemSingle_SystemSingle__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value <= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_LessThan__SystemSingle_SystemSingle__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value < b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_Multiplication__SystemSingle_SystemSingle__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value * b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_Remainder__SystemSingle_SystemSingle__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value % b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_Subtraction__SystemSingle_SystemSingle__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value - b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemSingle___op_UnaryMinus__SystemSingle__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length)
            {
                HeapAccess.Set(h, p[1], 0f - a_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_ConditionalAnd__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value & b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_ConditionalOr__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value | b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_ConditionalXor__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value ^ b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_Equality__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value == b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_Inequality__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value != b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_LogicalAnd__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value & b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_LogicalOr__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value | b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_LogicalXor__SystemBoolean_SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length && h[p[1]] is StrongBox<bool> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value ^ b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemBoolean___op_UnaryNegation__SystemBoolean__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<bool> a_ && p[1] < h.Length)
            {
                HeapAccess.Set(h, p[1], !a_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_Addition__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value + b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_Division__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value / b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_Equality__SystemChar_SystemChar__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value == b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_GreaterThanOrEqual__SystemChar_SystemChar__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value >= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_GreaterThan__SystemChar_SystemChar__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value > b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_Inequality__SystemChar_SystemChar__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value != b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_LeftShift__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], (int)((uint)a_.Value << (int)b_.Value));
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_LessThanOrEqual__SystemChar_SystemChar__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value <= b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_LessThan__SystemChar_SystemChar__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value < b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_LogicalAnd__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value & b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_LogicalOr__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value | b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_LogicalXor__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value ^ b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_Multiplication__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value * b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_RightShift__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], (int)a_.Value >> (int)b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_Subtraction__SystemChar_SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length && h[p[1]] is StrongBox<char> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value - b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemChar___op_UnaryMinus__SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length)
            {
                HeapAccess.Set(h, p[1], 0 - a_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemConvert___ToInt32__SystemChar__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<char> a_ && p[1] < h.Length)
            {
                HeapAccess.Set(h, p[1], Convert.ToInt32(a_.Value));
                return true;
            }
            return false;
        }

        static bool F_SystemString___get_Length__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<string> a_ && p[1] < h.Length)
            {
                HeapAccess.Set(h, p[1], a_.Value.Length);
                return true;
            }
            return false;
        }

        static bool F_SystemString___op_Equality__SystemString_SystemString__SystemBoolean(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<string> a_ && p[1] < h.Length && h[p[1]] is StrongBox<string> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value == b_.Value);
                return true;
            }
            return false;
        }

        static bool F_UnityEngineVector3___Dot__UnityEngineVector3_UnityEngineVector3__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<Vector3> a_ && p[1] < h.Length && h[p[1]] is StrongBox<Vector3> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], Vector3.Dot(a_.Value, b_.Value));
                return true;
            }
            return false;
        }

        static bool F_UnityEngineVector3___get_magnitude__SystemSingle(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<Vector3> a_ && p[1] < h.Length)
            {
                HeapAccess.Set(h, p[1], a_.Value.magnitude);
                return true;
            }
            return false;
        }

        static bool F_UnityEngineVector3___op_Addition__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<Vector3> a_ && p[1] < h.Length && h[p[1]] is StrongBox<Vector3> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value + b_.Value);
                return true;
            }
            return false;
        }

        static bool F_UnityEngineVector3___op_Multiply__SystemSingle_UnityEngineVector3__UnityEngineVector3(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<float> a_ && p[1] < h.Length && h[p[1]] is StrongBox<Vector3> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value * b_.Value);
                return true;
            }
            return false;
        }

        static bool F_UnityEngineVector3___op_Multiply__UnityEngineVector3_SystemSingle__UnityEngineVector3(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<Vector3> a_ && p[1] < h.Length && h[p[1]] is StrongBox<float> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value * b_.Value);
                return true;
            }
            return false;
        }

        static bool F_UnityEngineVector3___op_Subtraction__UnityEngineVector3_UnityEngineVector3__UnityEngineVector3(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<Vector3> a_ && p[1] < h.Length && h[p[1]] is StrongBox<Vector3> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value - b_.Value);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32Array___Get__SystemInt32__SystemInt32(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int[]> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value[b_.Value]);
                return true;
            }
            return false;
        }

        static bool F_SystemInt32Array___Set__SystemInt32_SystemInt32__SystemVoid(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<int[]> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length && h[p[2]] is StrongBox<int> c_)
            {
                a_.Value[b_.Value] = c_.Value;
                return true;
            }
            return false;
        }

        static bool F_SystemStringArray___Get__SystemInt32__SystemString(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<string[]> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length)
            {
                HeapAccess.Set(h, p[2], a_.Value[b_.Value]);
                return true;
            }
            return false;
        }

        static bool F_SystemStringArray___Set__SystemInt32_SystemString__SystemVoid(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] < h.Length && h[p[0]] is StrongBox<string[]> a_ && p[1] < h.Length && h[p[1]] is StrongBox<int> b_ && p[2] < h.Length && h[p[2]] is StrongBox<string> c_)
            {
                a_.Value[b_.Value] = c_.Value;
                return true;
            }
            return false;
        }
    }
}
