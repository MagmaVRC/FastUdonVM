using System;
using System.Collections.Generic;
using NUnit.Framework;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.VM.Common;

namespace MagmaVRC.UdonVM.Tests
{
    public class ProbeTests
    {
        const string IntLess = "SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean";

        Func<IUdonProgram, byte[]> _classify;
        Func<IUdonProgram, uint[]> _heapCallSlots;
        Action<uint, uint> _call, _tail, _return;
        Func<string, int> _registerExtern;
        Action<int> _extern;
        int _generation;
        bool _statsEnabled;

        readonly List<(char kind, uint pc, uint target)> _events = new List<(char, uint, uint)>();

        [SetUp]
        public void SetUp()
        {
            _classify = FastUdonVMProbe.Classify;
            _heapCallSlots = FastUdonVMProbe.HeapCallSlots;
            _call = FastUdonVMProbe.Call;
            _tail = FastUdonVMProbe.Tail;
            _return = FastUdonVMProbe.Return;
            _registerExtern = FastUdonVMProbe.RegisterExtern;
            _extern = FastUdonVMProbe.Extern;
            _generation = FastUdonVMProbe.Generation;
            _statsEnabled = FastUdonVMStats.Enabled;
            _events.Clear();
            Unset();
        }

        [TearDown]
        public void TearDown()
        {
            FastUdonVMProbe.Classify = _classify;
            FastUdonVMProbe.HeapCallSlots = _heapCallSlots;
            FastUdonVMProbe.Call = _call;
            FastUdonVMProbe.Tail = _tail;
            FastUdonVMProbe.Return = _return;
            FastUdonVMProbe.RegisterExtern = _registerExtern;
            FastUdonVMProbe.Extern = _extern;
            FastUdonVMProbe.Generation = Math.Max(FastUdonVMProbe.Generation, _generation) + 1;
            FastUdonVMStats.Enabled = _statsEnabled;
        }

        static void Unset()
        {
            FastUdonVMProbe.Classify = null;
            FastUdonVMProbe.HeapCallSlots = null;
            FastUdonVMProbe.Call = null;
            FastUdonVMProbe.Tail = null;
            FastUdonVMProbe.Return = null;
            FastUdonVMProbe.RegisterExtern = null;
            FastUdonVMProbe.Extern = null;
            FastUdonVMProbe.Generation++;
        }

        void Record()
        {
            FastUdonVMProbe.Call = (pc, t) => _events.Add(('c', pc, t));
            FastUdonVMProbe.Tail = (pc, t) => _events.Add(('t', pc, t));
            FastUdonVMProbe.Return = (pc, t) => _events.Add(('r', pc, t));
        }

        static byte[] Flags(IUdonProgram p, params (uint address, byte flag)[] marks)
        {
            var flags = new byte[p.ByteCode.Length / 4];
            foreach (var (address, flag) in marks) flags[address >> 2] = flag;
            return flags;
        }

        static uint Word(byte[] b, int i) => (uint)((b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3]);

        static byte[] FlagAllJumps(IUdonProgram p)
        {
            var bytes = p.ByteCode;
            var flags = new byte[bytes.Length / 4];
            int i = 0;
            while (i + 3 < bytes.Length)
            {
                var op = (OpCode)Word(bytes, i);
                switch (op)
                {
                    case OpCode.JUMP:
                    case OpCode.JUMP_IF_FALSE:
                        flags[i / 4] = FastUdonVMProbe.CallFlag | FastUdonVMProbe.TailFlag;
                        break;
                    case OpCode.JUMP_INDIRECT:
                        flags[i / 4] = FastUdonVMProbe.ReturnFlag;
                        break;
                }
                i += op == OpCode.NOP || op == OpCode.POP || op == OpCode.COPY ? 4 : 8;
            }
            return flags;
        }

        static IUdonVM Load(IUdonProgram program, bool fused)
        {
            var vm = VMDifferential.Fast(fused);
            Assert.IsTrue(vm.LoadProgram(program), "LoadProgram failed");
            return vm;
        }

        static uint Run(IUdonVM vm, IUdonProgram program, string entry = "_start")
        {
            vm.SetProgramCounter(program.EntryPoints.GetAddressFromSymbol(entry));
            return vm.Interpret();
        }

        static uint Address(IUdonProgram program, string symbol) => program.SymbolTable.GetAddressFromSymbol(symbol);

        static string HeapCall(uint back) => $@"
.data_start
    back: %SystemUInt32, 0x{back:X}
    slot: %SystemUInt32, null
    one: %SystemInt32, 1
    x: %SystemInt32, 0
.data_end
.code_start
    .export _start
    _start:
    PUSH, back
    PUSH, slot
    COPY
    JUMP, f
    NOP
    NOP
    JUMP, 0xFFFFFFFC
    f:
    PUSH, one
    PUSH, x
    COPY
    JUMP_INDIRECT, slot
.code_end
";

        const uint HeapCallJump = 20;
        const uint HeapCallBack = 28;
        const uint HeapCallTarget = 44;

        const string JifBranch = @"
.data_start
    c: %SystemBoolean, null
    one: %SystemInt32, 1
    x: %SystemInt32, 0
.data_end
.code_start
    .export _start
    _start:
    PUSH, c
    JUMP_IF_FALSE, t
    JUMP, 0xFFFFFFFC
    t:
    PUSH, one
    PUSH, x
    COPY
    JUMP, 0xFFFFFFFC
.code_end
";

        static readonly string JifChained = $@"
.data_start
    a: %SystemInt32, 0
    b: %SystemInt32, 3
    c: %SystemBoolean, null
    one: %SystemInt32, 1
    x: %SystemInt32, 0
.data_end
.code_start
    .export _start
    _start:
    PUSH, a
    PUSH, b
    PUSH, c
    EXTERN, ""{IntLess}""
    PUSH, c
    JUMP_IF_FALSE, t
    JUMP, 0xFFFFFFFC
    t:
    PUSH, one
    PUSH, x
    COPY
    JUMP, 0xFFFFFFFC
.code_end
";

        [TestCase(true), TestCase(false)]
        public void CallAndReturnReported(bool fused)
        {
            var program = UAssemblyPrograms.Assemble(UAssemblyPrograms.CallReturn);
            var flags = Flags(program, (8, FastUdonVMProbe.CallFlag), (24, FastUdonVMProbe.CallFlag), (84, FastUdonVMProbe.ReturnFlag));
            FastUdonVMProbe.Classify = p => flags;
            Record();
            FastUdonVMProbe.Generation++;
            var vm = Load(program, fused);
            Assert.AreEqual(0u, Run(vm, program));
            CollectionAssert.AreEqual(new[] { ('c', 8u, 40u), ('r', 84u, 16u), ('c', 24u, 40u), ('r', 84u, 32u) }, _events);
        }

        [Test]
        public void GuardRejectsNonCall()
        {
            var program = UAssemblyPrograms.Assemble(UAssemblyPrograms.CallReturn);
            var flags = Flags(program, (32, FastUdonVMProbe.CallFlag));
            FastUdonVMProbe.Classify = p => flags;
            Record();
            FastUdonVMProbe.Generation++;
            var vm = Load(program, true);
            Run(vm, program);
            CollectionAssert.IsEmpty(_events);

            var tailFlags = Flags(program, (32, FastUdonVMProbe.CallFlag | FastUdonVMProbe.TailFlag));
            FastUdonVMProbe.Classify = p => tailFlags;
            FastUdonVMProbe.Generation++;
            Run(vm, program);
            CollectionAssert.AreEqual(new[] { ('t', 32u, 0xFFFFFFFCu) }, _events);
        }

        [Test]
        public void HeapCallReported()
        {
            IUdonProgram Setup(uint back, Func<IUdonProgram, uint[]> slots)
            {
                var program = UAssemblyPrograms.Assemble(HeapCall(back));
                var flags = Flags(program, (HeapCallJump, FastUdonVMProbe.HeapCallFlag));
                FastUdonVMProbe.Classify = p => flags;
                FastUdonVMProbe.HeapCallSlots = slots;
                FastUdonVMProbe.Generation++;
                _events.Clear();
                return program;
            }

            Func<IUdonProgram, uint[]> Slots(int extra) => p =>
            {
                var s = new uint[p.ByteCode.Length / 4 + extra];
                if (extra == 0) s[HeapCallJump >> 2] = Address(p, "slot");
                return s;
            };
            Record();

            var good = Setup(HeapCallBack, Slots(0));
            Assert.AreEqual(0u, Run(Load(good, true), good));
            CollectionAssert.AreEqual(new[] { ('c', HeapCallJump, HeapCallTarget) }, _events);

            var wrong = Setup(HeapCallBack + 4, Slots(0));
            Assert.AreEqual(0u, Run(Load(wrong, true), wrong));
            CollectionAssert.IsEmpty(_events);

            var noSlots = Setup(HeapCallBack, null);
            Assert.AreEqual(0u, Run(Load(noSlots, true), noSlots));
            CollectionAssert.IsEmpty(_events);

            var badLength = Setup(HeapCallBack, Slots(1));
            Assert.AreEqual(0u, Run(Load(badLength, true), badLength));
            CollectionAssert.IsEmpty(_events);
        }

        [TestCase(true), TestCase(false)]
        public void JifTailReported(bool fused)
        {
            Record();

            var branch = UAssemblyPrograms.Assemble(JifBranch);
            var branchFlags = Flags(branch, (8, FastUdonVMProbe.TailFlag));
            FastUdonVMProbe.Classify = p => branchFlags;
            FastUdonVMProbe.Generation++;
            branch.Heap.SetHeapVariable(Address(branch, "c"), false);
            var vm = Load(branch, fused);
            Assert.AreEqual(0u, Run(vm, branch));
            CollectionAssert.AreEqual(new[] { ('t', 8u, 24u) }, _events);

            _events.Clear();
            branch.Heap.SetHeapVariable(Address(branch, "c"), true);
            Assert.AreEqual(0u, Run(vm, branch));
            CollectionAssert.IsEmpty(_events);

            var chained = UAssemblyPrograms.Assemble(JifChained);
            var chainedFlags = Flags(chained, (40, FastUdonVMProbe.TailFlag));
            FastUdonVMProbe.Classify = p => chainedFlags;
            FastUdonVMProbe.Generation++;
            _events.Clear();
            chained.Heap.SetHeapVariable(Address(chained, "a"), 5);
            vm = Load(chained, fused);
            Assert.AreEqual(0u, Run(vm, chained));
            CollectionAssert.AreEqual(new[] { ('t', 40u, 56u) }, _events);

            _events.Clear();
            chained.Heap.SetHeapVariable(Address(chained, "a"), 1);
            Assert.AreEqual(0u, Run(vm, chained));
            CollectionAssert.IsEmpty(_events);
        }

        [Test]
        public void FusedExternsCounted()
        {
            int count = 0;
            FastUdonVMProbe.RegisterExtern = s => 7;
            FastUdonVMProbe.Extern = id => { if (id == 7) count++; };
            FastUdonVMProbe.Generation++;
            FastUdonVMStats.Enabled = true;
            FastUdonVMStats.Reset();
            var program = UAssemblyPrograms.Assemble(UAssemblyPrograms.ArithmeticLoop);
            Assert.AreEqual(0u, Run(Load(program, true), program));
            Assert.Greater(FastUdonVMStats.FusedHits, 0);
            Assert.AreEqual(FastUdonVMStats.FusedHits, (long)count);
        }

        [Test]
        public void LazyProbeIds()
        {
            int registered = 0, classified = 0;
            Func<string, int> Register() => s => { registered++; return 1; };
            Func<IUdonProgram, byte[]> Classifier() => p => { classified++; return null; };
            FastUdonVMProbe.RegisterExtern = Register();
            FastUdonVMProbe.Extern = id => { };
            FastUdonVMProbe.Classify = Classifier();
            FastUdonVMProbe.Generation++;

            var program = UAssemblyPrograms.Assemble(UAssemblyPrograms.ArithmeticLoop);
            uint i = Address(program, "i"), sum = Address(program, "sum");
            var vm = Load(program, true);
            void Again()
            {
                program.Heap.SetHeapVariable(i, 0);
                program.Heap.SetHeapVariable(sum, 0);
                Assert.AreEqual(0u, Run(vm, program));
            }

            Again();
            Assert.AreEqual(3, registered, "one registration per fused site");
            Assert.AreEqual(1, classified);

            Again();
            Assert.AreEqual(3, registered, "second run registers nothing");

            FastUdonVMProbe.Generation++;
            Again();
            Assert.AreEqual(3, registered, "generation bump with the same delegates registers nothing");
            Assert.AreEqual(1, classified, "same classifier instance is not re-run");

            FastUdonVMProbe.RegisterExtern = Register();
            FastUdonVMProbe.Generation++;
            Again();
            Assert.AreEqual(6, registered, "new delegate instance registers every site again");

            Assert.IsTrue(vm.LoadProgram(program));
            Again();
            Assert.AreEqual(2, classified, "program load re-runs the classifier");
            Assert.AreEqual(9, registered, "program load re-registers lazily");

            FastUdonVMProbe.Classify = Classifier();
            FastUdonVMProbe.Generation++;
            Again();
            Assert.AreEqual(3, classified, "new classifier instance is run");
        }

        [Test]
        public void GenerationRefresh()
        {
            var program = UAssemblyPrograms.Assemble(UAssemblyPrograms.CallReturn);
            var vm = Load(program, true);
            Run(vm, program);
            CollectionAssert.IsEmpty(_events);

            var flags = Flags(program, (8, FastUdonVMProbe.CallFlag), (24, FastUdonVMProbe.CallFlag));
            FastUdonVMProbe.Classify = p => flags;
            Record();
            FastUdonVMProbe.Generation++;
            Run(vm, program);
            CollectionAssert.AreEqual(new[] { ('c', 8u, 40u), ('c', 24u, 40u) }, _events);
        }

        static readonly (string source, string[] entries, int runs)[] Sources =
        {
            (UAssemblyPrograms.ArithmeticLoop, null, 1),
            (UAssemblyPrograms.StringBuilding, null, 1),
            (UAssemblyPrograms.IntArraySum, null, 1),
            (UAssemblyPrograms.MixedObjectArray, null, 1),
            (UAssemblyPrograms.BooleanLogic, null, 1),
            (UAssemblyPrograms.CallReturn, null, 1),
            (UAssemblyPrograms.PopUnderflow, null, 2),
            (UAssemblyPrograms.CopyUnderflow, null, 2),
            (UAssemblyPrograms.UnalignedJump, null, 1),
            (UAssemblyPrograms.UnsupportedExtern, null, 1),
            (UAssemblyPrograms.DivideByZero, null, 1),
            (UAssemblyPrograms.ExternOperandNotString, null, 1),
            (UAssemblyPrograms.UninitializedCopy, null, 1),
            (UAssemblyPrograms.TwoEntries, new[] { "_a", "_b" }, 1),
            (UAssemblyPrograms.NopAnnotation, null, 1),
            (UAssemblyPrograms.JumpIntoCompareBranch, null, 1),
        };

        [TestCase(true), TestCase(false)]
        public void ProbeDoesNotChangeSemantics(bool fused)
        {
            FastUdonVMProbe.Classify = FlagAllJumps;
            FastUdonVMProbe.Call = (pc, t) => { };
            FastUdonVMProbe.Tail = (pc, t) => { };
            FastUdonVMProbe.Return = (pc, t) => { };
            FastUdonVMProbe.RegisterExtern = s => 0;
            FastUdonVMProbe.Extern = id => { };
            FastUdonVMProbe.Generation++;
            foreach (var (source, entries, runs) in Sources) VMDifferential.Compare(source, fused, entries, runs: runs);
        }

        [TestCase(true), TestCase(false)]
        public void ThrowingCallbackDisablesProbe(bool fused)
        {
            FastUdonVMProbe.Classify = FlagAllJumps;
            FastUdonVMProbe.Call = (_, _) => throw new InvalidOperationException("probe");
            FastUdonVMProbe.Generation++;
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.CallReturn, fused);
            Assert.IsNull(stock.Error);
        }
    }
}
