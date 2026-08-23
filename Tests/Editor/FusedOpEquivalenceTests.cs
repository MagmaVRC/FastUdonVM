using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon.ClientBindings;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace MagmaVRC.UdonVM.Tests
{
    public class FusedOpEquivalenceTests
    {
        [SetUp]
        public void IgnoreUnityAsserts() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void RestoreUnityAsserts() => LogAssert.ignoreFailingMessages = false;

        const uint HeapSize = 16;
        const int Seeds = 50;

        static readonly IUdonWrapper Wrapper = new UdonClientInterface().GetWrapper();
        static readonly Type[] Pool = { typeof(int), typeof(float), typeof(string), typeof(bool), typeof(object), typeof(Vector3), typeof(int[]), typeof(char), typeof(long) };

        public static IEnumerable<string> Signatures => FusedOps.Signatures.OrderBy(s => s, StringComparer.Ordinal);

        [Test, TestCaseSource(nameof(Signatures))]
        public void ParameterCountMatchesWrapper(string signature)
        {
            Assert.AreEqual(Wrapper.GetExternFunctionParameterCount(signature), FusedOps.SlotTypes(signature).Length, signature);
        }

        [Test, TestCaseSource(nameof(Signatures))]
        public void MatchesWrapper(string signature)
        {
            LogAssert.ignoreFailingMessages = true;
            var del = Wrapper.GetExternFunctionDelegate(signature);
            var fused = FusedOps.Get(signature);
            var types = FusedOps.SlotTypes(signature);
            int count = Wrapper.GetExternFunctionParameterCount(signature);
            int hits = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                int s = seed * 7919 + Fnv(signature);
                var a = Build(s, types, count, out var pa);
                var b = Build(s, types, count, out var pb);
                Type exA = null, exB = null;
                try { del(a, pa); } catch (Exception e) { exA = e.GetType(); }
                bool hit = true;
                try { hit = fused(HeapAccess.Boxes(b), pb); }
                catch (Exception e) { exB = e.GetType(); }
                if (hit) hits++;
                else
                {
                    try { del(b, pb); } catch (Exception e) { exB = e.GetType(); }
                }
                string where = signature + " seed " + seed + " addresses [" + string.Join(",", pa) + "]";
                Assert.AreEqual(exA, exB, where + " exception");
                var da = new List<(uint, IStrongBox, Type)>();
                var db = new List<(uint, IStrongBox, Type)>();
                a.DumpHeapObjects(da);
                b.DumpHeapObjects(db);
                Assert.AreEqual(da.Count, db.Count, where + " slot count");
                for (int i = 0; i < da.Count; i++)
                {
                    Assert.AreEqual(da[i].Item1, db[i].Item1, where + " slot address");
                    Assert.AreEqual(da[i].Item2.GetType(), db[i].Item2.GetType(), where + " slot " + da[i].Item1 + " box type");
                    Assert.IsTrue(Same(da[i].Item2.Value, db[i].Item2.Value), where + " slot " + da[i].Item1 + " value: " + Show(da[i].Item2.Value) + " vs " + Show(db[i].Item2.Value));
                }
            }
            Assert.Greater(hits, 0, signature + " never took the fused path");
        }

        static UdonHeap Build(int seed, Type[] types, int count, out uint[] p)
        {
            var rng = new System.Random(seed);
            var heap = new UdonHeap(HeapSize);
            p = new uint[count];
            for (int i = 0; i < count; i++)
            {
                int roll = rng.Next(100);
                if (roll < 4) p[i] = HeapSize + (uint)rng.Next(3);
                else if (roll < 14 && i > 0) p[i] = p[rng.Next(i)];
                else p[i] = (uint)rng.Next((int)HeapSize);
            }
            for (int i = 0; i < count; i++)
            {
                if (p[i] >= HeapSize) continue;
                int roll = rng.Next(100);
                var t = types[i];
                if (roll < 4) continue;
                if (roll < 8) { heap.InitializeHeapVariable(p[i], t); continue; }
                if (roll < 16) t = Pool[rng.Next(Pool.Length)];
                heap.SetHeapVariable(p[i], Value(rng, t), t);
            }
            return heap;
        }

        static object Value(System.Random rng, Type t)
        {
            if (t == typeof(int)) return rng.Next(10) < 7 ? rng.Next(-2, 12) : rng.Next(3) == 0 ? int.MinValue : (int)(Bits(rng) % 4096) * (rng.Next(2) == 0 ? 1 : -1);
            if (t == typeof(uint)) return rng.Next(10) < 7 ? (uint)rng.Next(0, 12) : Bits(rng);
            if (t == typeof(long)) return rng.Next(10) < 7 ? (long)rng.Next(-2, 12) : (long)(((ulong)Bits(rng) << 32) | Bits(rng));
            if (t == typeof(ulong)) return rng.Next(10) < 7 ? (ulong)rng.Next(0, 12) : ((ulong)Bits(rng) << 32) | Bits(rng);
            if (t == typeof(short)) return (short)Bits(rng);
            if (t == typeof(ushort)) return (ushort)Bits(rng);
            if (t == typeof(byte)) return (byte)Bits(rng);
            if (t == typeof(sbyte)) return (sbyte)Bits(rng);
            if (t == typeof(bool)) return rng.Next(2) == 0;
            if (t == typeof(char)) return rng.Next(10) < 7 ? (char)rng.Next(32, 127) : (char)Bits(rng);
            if (t == typeof(float)) return Float(rng);
            if (t == typeof(double)) return Double(rng);
            if (t == typeof(string)) return Str(rng);
            if (t == typeof(object))
            {
                switch (rng.Next(6))
                {
                    case 0: return null;
                    case 1: return rng.Next(-2, 12);
                    case 2: return Str(rng);
                    case 3: return Float(rng);
                    case 4: return new List<int> { rng.Next(5) };
                    default: return Vec3(rng);
                }
            }
            if (t == typeof(Vector2)) return new Vector2(Float(rng), Float(rng));
            if (t == typeof(Vector3)) return Vec3(rng);
            if (t == typeof(Vector4)) return new Vector4(Float(rng), Float(rng), Float(rng), Float(rng));
            if (t == typeof(Quaternion)) return rng.Next(3) == 0 ? new Quaternion(Float(rng), Float(rng), Float(rng), Float(rng)) : Quaternion.Euler(Vec3(rng));
            if (t == typeof(Color)) return new Color(Float(rng), Float(rng), Float(rng), Float(rng));
            if (t == typeof(Array)) return Value(rng, new[] { typeof(int[]), typeof(string[]), typeof(Vector3[]), typeof(object[]) }[rng.Next(4)]);
            if (t.IsArray)
            {
                if (rng.Next(10) == 0) return null;
                var e = t.GetElementType();
                var arr = Array.CreateInstance(e, rng.Next(0, 6));
                for (int i = 0; i < arr.Length; i++) arr.SetValue(Value(rng, e), i);
                return arr;
            }
            if (t.IsEnum)
            {
                var values = Enum.GetValues(t);
                return values.GetValue(rng.Next(values.Length));
            }
            throw new NotSupportedException(t.FullName);
        }

        static uint Bits(System.Random rng) => (uint)rng.Next(0, 1 << 16) | ((uint)rng.Next(0, 1 << 16) << 16);

        static float Float(System.Random rng)
        {
            switch (rng.Next(12))
            {
                case 0: return 0f;
                case 1: return -0f;
                case 2: return float.NaN;
                case 3: return float.PositiveInfinity;
                case 4: return float.NegativeInfinity;
                case 5: return rng.Next(-3, 4) + 0.5f;
                case 6: return BitConverter.ToSingle(BitConverter.GetBytes(Bits(rng)), 0);
                default: return (float)(rng.NextDouble() * 20 - 10);
            }
        }

        static double Double(System.Random rng)
        {
            switch (rng.Next(10))
            {
                case 0: return 0.0;
                case 1: return -0.0;
                case 2: return double.NaN;
                case 3: return double.PositiveInfinity;
                case 4: return rng.Next(-3, 4) + 0.5;
                case 5: return (double)Float(rng);
                default: return rng.NextDouble() * 20 - 10;
            }
        }

        static Vector3 Vec3(System.Random rng) => new Vector3(Float(rng), Float(rng), Float(rng));

        static string Str(System.Random rng)
        {
            if (rng.Next(10) == 0) return null;
            const string alphabet = "abcAB xyZ09-_";
            var chars = new char[rng.Next(0, 8)];
            for (int i = 0; i < chars.Length; i++) chars[i] = alphabet[rng.Next(alphabet.Length)];
            return new string(chars);
        }

        static bool Same(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (a.GetType() != b.GetType()) return false;
            switch (a)
            {
                case float f: return BitConverter.SingleToInt32Bits(f) == BitConverter.SingleToInt32Bits((float)b);
                case double d: return BitConverter.DoubleToInt64Bits(d) == BitConverter.DoubleToInt64Bits((double)b);
                case Vector2 v: { var o = (Vector2)b; return Same(v.x, o.x) && Same(v.y, o.y); }
                case Vector3 v: { var o = (Vector3)b; return Same(v.x, o.x) && Same(v.y, o.y) && Same(v.z, o.z); }
                case Vector4 v: { var o = (Vector4)b; return Same(v.x, o.x) && Same(v.y, o.y) && Same(v.z, o.z) && Same(v.w, o.w); }
                case Quaternion v: { var o = (Quaternion)b; return Same(v.x, o.x) && Same(v.y, o.y) && Same(v.z, o.z) && Same(v.w, o.w); }
                case Color v: { var o = (Color)b; return Same(v.r, o.r) && Same(v.g, o.g) && Same(v.b, o.b) && Same(v.a, o.a); }
                case Array arr:
                {
                    var other = (Array)b;
                    if (arr.Length != other.Length) return false;
                    for (int i = 0; i < arr.Length; i++) if (!Same(arr.GetValue(i), other.GetValue(i))) return false;
                    return true;
                }
                case List<int> l: return l.Count == ((List<int>)b).Count;
                default: return a.Equals(b);
            }
        }

        static string Show(object o)
        {
            if (o == null) return "null";
            if (o is Array arr) return o.GetType().Name + "{" + string.Join(",", arr.Cast<object>().Select(Show)) + "}";
            return o.GetType().Name + ":" + o;
        }

        static int Fnv(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s) h = (h ^ c) * 16777619;
                return (int)(h & 0x7fffffff);
            }
        }
    }
}
