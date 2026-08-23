using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Object = UnityEngine.Object;

namespace MagmaVRC.UdonVM.Tests
{
    public class SiteCacheTests
    {
        const string Receiver = "VRCUdonCommonInterfacesIUdonEventReceiver";
        const string IntAdd = "SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32";

        static readonly string Caller = $@"
.data_start
    recv: %{Receiver}, null
    sym: %SystemString, ""counter""
    evt: %SystemString, ""_start""
    val: %SystemInt32, 42
    got: %SystemObject, null
    got2: %SystemObject, null
.data_end
.code_start
    .export _start
    _start:
    PUSH, recv
    PUSH, sym
    PUSH, got
    EXTERN, ""{Receiver}.__GetProgramVariable__SystemString__SystemObject""
    PUSH, recv
    PUSH, sym
    PUSH, val
    EXTERN, ""{Receiver}.__SetProgramVariable__SystemString_SystemObject__SystemVoid""
    PUSH, recv
    PUSH, evt
    EXTERN, ""{Receiver}.__SendCustomEvent__SystemString__SystemVoid""
    PUSH, recv
    PUSH, sym
    PUSH, got2
    EXTERN, ""{Receiver}.__GetProgramVariable__SystemString__SystemObject""
    JUMP, 0xFFFFFFFC
.code_end
";

        static string Target(int initial, int step) => $@"
.data_start
    counter: %SystemInt32, {initial}
    step: %SystemInt32, {step}
.data_end
.code_start
    .export _start
    _start:
    PUSH, counter
    PUSH, step
    PUSH, counter
    EXTERN, ""{IntAdd}""
    JUMP, 0xFFFFFFFC
.code_end
";

        const BindingFlags Priv = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo ProgramField = typeof(UdonBehaviour).GetField("_program", Priv);
        static readonly FieldInfo VmField = typeof(UdonBehaviour).GetField("_udonVM", Priv);
        static readonly FieldInfo ManagerField = typeof(UdonBehaviour).GetField("_udonManager", Priv);
        static readonly FieldInfo ClientField = typeof(UdonManager).GetField("_udonClientInterface", Priv);

        UdonManager _manager;
        readonly List<UdonBehaviour> _receivers = new List<UdonBehaviour>();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            _manager = UdonManager.Instance;
            if (ClientField.GetValue(_manager) == null) _manager.Awake();
        }

        [TearDown]
        public void TearDown()
        {
            FastUdonVMStats.Enabled = false;
            foreach (var ub in _receivers)
            {
                ProgramField.SetValue(ub, null);
                VmField.SetValue(ub, null);
                Object.DestroyImmediate(ub.gameObject);
            }
            _receivers.Clear();
            LogAssert.ignoreFailingMessages = false;
        }

        // ponytail: no public edit-mode load path (InitializeUdonContent needs a play-mode UdonManager), so wire the private fields directly
        UdonBehaviour MakeReceiver(string source)
        {
            var ub = new GameObject("SiteCacheTests.receiver").AddComponent<UdonBehaviour>();
            _receivers.Add(ub);
            Load(ub, source);
            ManagerField.SetValue(ub, _manager);
            return ub;
        }

        static void Load(UdonBehaviour ub, string source)
        {
            var program = UAssemblyPrograms.Assemble(source);
            var vm = VMDifferential.Stock();
            Assert.IsTrue(vm.LoadProgram(program));
            ProgramField.SetValue(ub, program);
            VmField.SetValue(ub, vm);
        }

        static IUdonProgram CallerFor(UdonBehaviour receiver)
        {
            var program = UAssemblyPrograms.Assemble(Caller);
            program.Heap.SetHeapVariable(program.SymbolTable.GetAddressFromSymbol("recv"), receiver, typeof(IUdonEventReceiver));
            return program;
        }

        static List<(uint, IStrongBox, Type)> Dump(IUdonHeap heap)
        {
            var list = new List<(uint, IStrongBox, Type)>();
            heap.DumpHeapObjects(list);
            return list;
        }

        static void AssertSameReceiver(UdonBehaviour stock, UdonBehaviour fast) =>
            VMDifferential.AssertSameHeap(Dump(((IUdonProgram)ProgramField.GetValue(stock)).Heap), Dump(((IUdonProgram)ProgramField.GetValue(fast)).Heap));

        static Outcome Run(IUdonVM vm, UdonBehaviour receiver, int runs = 1) =>
            VMDifferential.Run(vm, CallerFor(receiver), new[] { "_start" }, runs: runs);

        [Test]
        public void StockAndFastAgree()
        {
            var rs = MakeReceiver(Target(5, 1));
            var rf = MakeReceiver(Target(5, 1));
            var stock = Run(VMDifferential.Stock(), rs);
            var fast = Run(VMDifferential.Fast(true), rf);
            VMDifferential.AssertSame(stock, fast);
            AssertSameReceiver(rs, rf);
            Assert.AreEqual(43, rf.GetProgramVariable("counter"));
        }

        [Test]
        public void WarmCacheHits()
        {
            var rs = MakeReceiver(Target(5, 1));
            var rf = MakeReceiver(Target(5, 1));
            FastUdonVMStats.Enabled = true;
            FastUdonVMStats.Reset();
            var stock = Run(VMDifferential.Stock(), rs, runs: 3);
            var fast = Run(VMDifferential.Fast(true), rf, runs: 3);
            VMDifferential.AssertSame(stock, fast);
            AssertSameReceiver(rs, rf);
            Assert.AreEqual(4, FastUdonVMStats.SiteCacheMisses, "first run misses every site");
            Assert.AreEqual(8, FastUdonVMStats.SiteCacheHits, "later runs hit every site");
            Assert.AreEqual(43, rf.GetProgramVariable("counter"));
        }

        [Test]
        public void ProgramSwapInvalidates()
        {
            var rs = MakeReceiver(Target(5, 1));
            var rf = MakeReceiver(Target(5, 1));
            var program = CallerFor(rf);
            uint entry = program.EntryPoints.GetAddressFromSymbol("_start");
            var fast = VMDifferential.Fast(true);
            Assert.IsTrue(fast.LoadProgram(program));
            fast.SetProgramCounter(entry);
            Assert.AreEqual(0u, fast.Interpret());
            Assert.AreEqual(43, rf.GetProgramVariable("counter"));

            Load(rf, Target(100, 10));
            Load(rs, Target(100, 10));
            FastUdonVMStats.Enabled = true;
            FastUdonVMStats.Reset();
            fast.SetProgramCounter(entry);
            Assert.AreEqual(0u, fast.Interpret());
            Assert.AreEqual(4, FastUdonVMStats.SiteCacheMisses, "swapped program misses every site");
            Assert.AreEqual(0, FastUdonVMStats.SiteCacheHits);
            Assert.AreEqual(52, rf.GetProgramVariable("counter"));

            var stock = Run(VMDifferential.Stock(), rs);
            Assert.IsNull(stock.Error);
            VMDifferential.AssertSameHeap(stock.Heap, Dump(program.Heap));
            AssertSameReceiver(rs, rf);

            fast.SetProgramCounter(entry);
            Assert.AreEqual(0u, fast.Interpret());
            Assert.AreEqual(4, FastUdonVMStats.SiteCacheHits, "re-warmed sites hit");
        }

        [Test]
        public void NullReceiverMatchesStock()
        {
            var stock = VMDifferential.Run(VMDifferential.Stock(), UAssemblyPrograms.Assemble(Caller), new[] { "_start" });
            var fast = VMDifferential.Run(VMDifferential.Fast(true), UAssemblyPrograms.Assemble(Caller), new[] { "_start" });
            VMDifferential.AssertSame(stock, fast);
        }
    }
}
