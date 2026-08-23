using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.VM;

namespace MagmaVRC.UdonVM.Tests
{
    public sealed class FakeTimeSource : IUdonVMTimeSource
    {
        public long Step;
        long _now;
        public long CurrentTime => _now += Step;
    }

    public sealed class Outcome
    {
        public readonly List<uint> Results = new List<uint>();
        public readonly List<uint> ProgramCounters = new List<uint>();
        public Exception Error;
        public List<(uint address, IStrongBox box, Type type)> Heap;
    }

    public static class VMDifferential
    {
        public static IUdonVM Stock(IUdonVMTimeSource time = null) =>
            time == null ? UAssemblyPrograms.Editor.ConstructUdonVM() : new VRC.Udon.VM.UdonVM(UAssemblyPrograms.Editor.GetWrapper(), time);

        public static IUdonVM Fast(bool fused, IUdonVMTimeSource time = null) =>
            new FastUdonVM(UAssemblyPrograms.Editor.GetWrapper(), time ?? new VRC.Udon.ClientBindings.UdonVMTimeSource(), fused);

        public static Outcome Run(IUdonVM vm, IUdonProgram program, string[] entries, bool debug = false, int runs = 1)
        {
            var o = new Outcome();
            vm.DebugLogging = debug;
            Assert.IsTrue(vm.LoadProgram(program), "LoadProgram failed");
            try
            {
                foreach (var entry in entries)
                {
                    vm.SetProgramCounter(program.EntryPoints.GetAddressFromSymbol(entry));
                    for (int i = 0; i < runs; i++)
                    {
                        o.Results.Add(vm.Interpret());
                        o.ProgramCounters.Add(vm.GetProgramCounter());
                    }
                }
            }
            catch (Exception e)
            {
                o.Error = e;
            }
            o.Heap = new List<(uint, IStrongBox, Type)>();
            vm.InspectHeap().DumpHeapObjects(o.Heap);
            return o;
        }

        public static (Outcome stock, Outcome fast) Compare(string source, bool fused, string[] entries = null, bool debug = false, int runs = 1, Func<IUdonVMTimeSource> time = null)
        {
            entries ??= new[] { "_start" };
            var stock = Run(Stock(time?.Invoke()), UAssemblyPrograms.Assemble(source), entries, debug, runs);
            var fast = Run(Fast(fused, time?.Invoke()), UAssemblyPrograms.Assemble(source), entries, debug, runs);
            AssertSame(stock, fast);
            return (stock, fast);
        }

        public static void AssertSame(Outcome stock, Outcome fast)
        {
            CollectionAssert.AreEqual(stock.Results, fast.Results, "Interpret results");
            CollectionAssert.AreEqual(stock.ProgramCounters, fast.ProgramCounters, "program counters");
            AssertSameError(stock.Error, fast.Error);
            AssertSameHeap(stock.Heap, fast.Heap);
        }

        public static void AssertSameError(Exception stock, Exception fast)
        {
            if (stock == null && fast == null) return;
            Assert.IsNotNull(stock, "stock VM did not throw but fast VM did: " + fast);
            Assert.IsNotNull(fast, "fast VM did not throw but stock VM did: " + stock);
            Assert.AreEqual(stock.GetType(), fast.GetType(), "exception type");
            Assert.AreEqual(stock.Message, fast.Message, "exception message");
            AssertSameError(stock.InnerException, fast.InnerException);
        }

        public static void AssertSameHeap(List<(uint address, IStrongBox box, Type type)> stock, List<(uint address, IStrongBox box, Type type)> fast)
        {
            Assert.AreEqual(stock.Count, fast.Count, "heap slot count");
            for (int i = 0; i < stock.Count; i++)
            {
                var (sa, sb, _) = stock[i];
                var (fa, fb, _) = fast[i];
                Assert.AreEqual(sa, fa, "heap address order");
                if (sb.GetType().Name.StartsWith("StrongBox`1") && sb.Value != null && sb.Value.GetType().Name == "CachedUdonExternDelegate")
                {
                    Assert.AreEqual(sb.Value.ToString(), fb.Value, $"extern signature at 0x{sa:X8}");
                    continue;
                }
                Assert.AreEqual(sb.GetType(), fb.GetType(), $"box type at 0x{sa:X8}");
                Assert.IsTrue(ValueEquals(sb.Value, fb.Value), $"value at 0x{sa:X8}: stock={Show(sb.Value)} fast={Show(fb.Value)}");
            }
        }

        static bool ValueEquals(object a, object b)
        {
            if (a == null || b == null) return a == b;
            if (a is Array aa && b is Array ba)
            {
                if (aa.Length != ba.Length) return false;
                var ea = aa.GetEnumerator();
                var eb = ba.GetEnumerator();
                while (ea.MoveNext() && eb.MoveNext()) if (!ValueEquals(ea.Current, eb.Current)) return false;
                return true;
            }
            if (a.GetType() != b.GetType()) return false;
            // Reference-typed heap objects (components, heap references) compare by type only.
            if (!a.GetType().IsPrimitive && !(a is string) && !(a is IStructuralEquatable) && !a.GetType().IsValueType && !a.GetType().IsEnum) return true;
            return a.Equals(b);
        }

        static string Show(object v) => v == null ? "null" : v is Array arr ? $"{v.GetType().Name}[{arr.Length}]" : v.ToString();
    }
}
