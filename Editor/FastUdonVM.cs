using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using VRC.Udon.Common.Delegates;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.VM;
using VRC.Udon.VM.Common;

namespace MagmaVRC.UdonVM
{
    /// <summary>
    /// Drop-in <see cref="IUdonVM"/> with the same bytecode, heap and extern semantics as <see cref="VRC.Udon.VM.UdonVM"/>.
    /// Extern delegates are resolved once at load time and pure primitive externs may run as fused native operations.
    /// </summary>
    public sealed class FastUdonVM : IUdonVM
    {
        sealed class Extern
        {
            public string Signature;
            public UdonExternDelegate Delegate;
            public int ParameterCount;
            public FusedOp Fused;
            public Exception ResolveError;
        }

        const string InstructionSetIdentifier = "UDON";
        const int InstructionSetVersion = 1;
        const uint ResultSuccess = 0u;
        const uint ResultFailure = 1u;
        const long MaxVmTimeMs = 10000L;

        readonly IUdonWrapper _wrapper;
        readonly IUdonVMTimeSource _timeSource;
        readonly bool _useFused;

        sealed class Super
        {
            public Extern Extern;
            public FusedOp Fused;
            public uint[] Addresses;
            public uint Length;
        }

        uint[] _words;
        Extern[] _externs;
        Super[] _supers;
        IUdonProgram _program;
        IUdonHeap _heap;
        IStrongBox[] _boxes;
        uint[] _stack = new uint[1024];
        int _sp;
        uint _pc;
        bool _halted;

        public bool DebugLogging { get; set; }

        public FastUdonVM(IUdonWrapper wrapper, IUdonVMTimeSource timeSource, bool useFusedOps = true)
        {
            _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
            _timeSource = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
            _useFused = useFusedOps;
        }

        public bool LoadProgram(IUdonProgram program)
        {
            if (program.InstructionSetIdentifier != InstructionSetIdentifier) return false;
            if (program.InstructionSetVersion > InstructionSetVersion) return false;
            if (!Decode(program.ByteCode, out var words)) return false;
            _words = words;
            _program = program;
            _heap = program.Heap;
            _boxes = HeapAccess.Boxes(_heap);
            _externs = ResolveExterns(words);
            _supers = _useFused && _boxes != null ? BuildSupers(words, _externs) : null;
            return true;
        }

        static Super[] BuildSupers(uint[] words, Extern[] externs)
        {
            Super[] supers = null;
            uint i = 0;
            while (i < words.Length)
            {
                var op = (OpCode)words[i];
                if (op == OpCode.PUSH)
                {
                    uint j = i;
                    int pushes = 0;
                    while (j < words.Length && (OpCode)words[j] == OpCode.PUSH) { j += 2; pushes++; }
                    if (j < words.Length && (OpCode)words[j] == OpCode.EXTERN)
                    {
                        var e = externs[j];
                        if (e?.Fused != null && e.ParameterCount <= pushes)
                        {
                            uint first = j - (uint)e.ParameterCount * 2;
                            var addresses = new uint[e.ParameterCount];
                            for (int k = 0; k < e.ParameterCount; k++) addresses[k] = words[first + (uint)k * 2 + 1];
                            supers ??= new Super[words.Length];
                            supers[first] = new Super { Extern = e, Fused = e.Fused, Addresses = addresses, Length = (j + 2 - first) * 4 };
                        }
                    }
                    i = j;
                    continue;
                }
                i += op == OpCode.NOP || op == OpCode.POP || op == OpCode.COPY ? 1u : 2u;
            }
            return supers;
        }

        static bool Decode(byte[] bytes, out uint[] words)
        {
            words = new uint[bytes.Length / 4];
            uint i = 0;
            while (i < bytes.Length)
            {
                var op = (OpCode)ReadUInt(bytes, i);
                switch (op)
                {
                    case OpCode.NOP:
                    case OpCode.POP:
                    case OpCode.COPY:
                        words[i / 4] = (uint)op;
                        i += 4;
                        break;
                    case OpCode.PUSH:
                    case OpCode.JUMP_IF_FALSE:
                    case OpCode.JUMP:
                    case OpCode.EXTERN:
                    case OpCode.ANNOTATION:
                    case OpCode.JUMP_INDIRECT:
                        words[i / 4] = (uint)op;
                        words[i / 4 + 1] = ReadUInt(bytes, i + 4);
                        i += 8;
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        Extern[] ResolveExterns(uint[] words)
        {
            var externs = new Extern[words.Length];
            uint i = 0;
            while (i < words.Length)
            {
                var op = (OpCode)words[i];
                if (op == OpCode.EXTERN) externs[i] = Resolve(words[i + 1]);
                i += op == OpCode.NOP || op == OpCode.POP || op == OpCode.COPY ? 1u : 2u;
            }
            return externs;
        }

        Extern Resolve(uint address)
        {
            var e = new Extern();
            if (!_heap.TryGetHeapVariable(address, out string signature))
            {
                e.ResolveError = new UdonVMException("EXTERN operand must be of type 'System.String' but was of type '" + _heap.GetHeapVariableType(address).FullName + "'");
                return e;
            }
            e.Signature = signature;
            try
            {
                e.Delegate = _wrapper.GetExternFunctionDelegate(signature);
                e.ParameterCount = _wrapper.GetExternFunctionParameterCount(signature);
            }
            catch (Exception ex)
            {
                e.ResolveError = ex;
                return e;
            }
            if (_useFused && _boxes != null) e.Fused = FusedOps.Get(signature);
            return e;
        }

        public IUdonProgram RetrieveProgram() => _program;

        public uint Interpret()
        {
            if (_words == null) return ResultFailure;
            if (_halted) return ResultFailure;
            long start = _timeSource.CurrentTime;
            var words = _words;
            var externs = _externs;
            var supers = _supers;
            var heap = _heap;
            var boxes = _boxes;
            bool stats = FastUdonVMStats.Enabled;
            long limit = (long)words.Length * 4;
            try
            {
                bool debuggerAttached = Debugger.IsAttached;
                while (_pc < uint.MaxValue && (long)_pc + 3 < limit)
                {
                    if (stats) FastUdonVMStats.Instructions++;
                    uint w = _pc / 4;
                    switch ((OpCode)words[w])
                    {
                        case OpCode.NOP:
                            _pc += 4;
                            break;
                        case OpCode.PUSH:
                        {
                            var s = supers?[w];
                            if (s != null)
                            {
                                CheckExecutionTimeLimit(start, debuggerAttached);
                                bool done;
                                try
                                {
                                    done = s.Fused(boxes, s.Addresses);
                                }
                                catch (Exception inner)
                                {
                                    throw ExternException(s.Extern, s.Addresses, inner);
                                }
                                if (done)
                                {
                                    if (stats) { FastUdonVMStats.Externs++; FastUdonVMStats.FusedHits++; }
                                    _pc += s.Length;
                                    break;
                                }
                            }
                            Push(words[w + 1]);
                            _pc += 8;
                            break;
                        }
                        case OpCode.POP:
                            if (_sp == 0) { _halted = true; return ResultFailure; }
                            _sp--;
                            _pc += 4;
                            break;
                        case OpCode.JUMP_IF_FALSE:
                        {
                            CheckExecutionTimeLimit(start, debuggerAttached);
                            if (_sp == 0) { _halted = true; return ResultFailure; }
                            uint addr = _stack[--_sp];
                            bool value = boxes != null && addr < boxes.Length && boxes[addr] is StrongBox<bool> b ? b.Value : heap.GetHeapVariable<bool>(addr);
                            if (!value)
                            {
                                uint target = words[w + 1];
                                CheckJumpTarget(target);
                                _pc = target;
                            }
                            else _pc += 8;
                            break;
                        }
                        case OpCode.JUMP:
                        {
                            CheckExecutionTimeLimit(start, debuggerAttached);
                            uint target = words[w + 1];
                            CheckJumpTarget(target);
                            _pc = target;
                            break;
                        }
                        case OpCode.JUMP_INDIRECT:
                        {
                            CheckExecutionTimeLimit(start, debuggerAttached);
                            uint addr = words[w + 1];
                            uint target = boxes != null && addr < boxes.Length && boxes[addr] is StrongBox<uint> b ? b.Value : heap.GetHeapVariable<uint>(addr);
                            CheckJumpTarget(target);
                            _pc = target;
                            break;
                        }
                        case OpCode.EXTERN:
                        {
                            CheckExecutionTimeLimit(start, debuggerAttached);
                            var e = externs[w];
                            if (e.ResolveError != null) throw e.ResolveError;
                            if (stats) FastUdonVMStats.Externs++;
                            Span<uint> parameters = PopSlice(e.ParameterCount);
                            try
                            {
                                if (e.Fused != null && e.Fused(boxes, parameters))
                                {
                                    if (stats) FastUdonVMStats.FusedHits++;
                                    _pc += 8;
                                    break;
                                }
                                if (stats && e.Fused != null) FastUdonVMStats.FusedFallbacks++;
                                e.Delegate(heap, parameters);
                            }
                            catch (Exception inner)
                            {
                                throw ExternException(e, parameters, inner);
                            }
                            _pc += 8;
                            break;
                        }
                        case OpCode.ANNOTATION:
                            _pc += 8;
                            break;
                        case OpCode.COPY:
                        {
                            if (_sp == 0) { _halted = true; return ResultFailure; }
                            uint dest = _stack[--_sp];
                            if (_sp == 0) { _halted = true; return ResultFailure; }
                            uint src = _stack[--_sp];
                            if (boxes == null || !FastCopy(boxes, src, dest)) heap.CopyHeapVariable(src, dest);
                            _pc += 4;
                            break;
                        }
                        default:
                            return ResultFailure;
                    }
                }
            }
            catch (Exception inner)
            {
                _halted = true;
                if (!DebugLogging) throw new UdonVMException("An exception occurred in an UdonVM, execution will be halted.", inner);
                var dump = new List<(uint, IStrongBox, Type)>();
                heap.DumpHeapObjects(dump);
                var stackDump = new uint[_sp];
                Array.Copy(_stack, stackDump, _sp);
                throw new UdonVMException(_pc, dump, stackDump, inner);
            }
            return ResultSuccess;
        }

        UdonVMException ExternException(Extern e, Span<uint> parameters, Exception inner)
        {
            if (!DebugLogging) return new UdonVMException("An exception occurred during EXTERN to '" + e.Signature + "'.", inner);
            var sb = new StringBuilder();
            sb.AppendFormat("An exception occurred during EXTERN to '{0}'.", e.Signature);
            sb.AppendLine();
            sb.Append("    Parameter Addresses: ");
            for (int i = 0; i < e.ParameterCount; i++)
            {
                sb.AppendFormat("0x{0:X8}", parameters[i]);
                if (i < e.ParameterCount - 1) sb.Append(", ");
            }
            sb.AppendLine();
            return new UdonVMException(sb.ToString(), inner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Push(uint value)
        {
            if (_sp == _stack.Length) Array.Resize(ref _stack, _stack.Length == 0 ? 4 : _stack.Length * 2);
            _stack[_sp++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<uint> PopSlice(int count)
        {
            if (_sp == 0) return default;
            var slice = new Span<uint>(_stack, _sp - count, count);
            _sp -= count;
            return slice;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool FastCopy(IStrongBox[] h, uint src, uint dest)
        {
            if (src >= h.Length || dest >= h.Length) return false;
            switch (h[src])
            {
                case StrongBox<float> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<double> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<int> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<uint> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<short> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<ushort> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<byte> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<sbyte> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<char> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<long> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<ulong> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<bool> b: HeapAccess.Set(h, dest, b.Value); return true;
                case StrongBox<string> b: HeapAccess.Set(h, dest, b.Value); return true;
                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckExecutionTimeLimit(long start, bool debuggerAttached)
        {
            if (debuggerAttached || _timeSource.CurrentTime - start <= MaxVmTimeMs) return;
            _halted = true;
            throw new UdonVMException($"The VM encountered an error!\nProgram execution time exceeded max VM time of {10f:N1} seconds. You may want to check for infinite loops/recursion.\nProgram Counter was at {_pc}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckJumpTarget(uint target)
        {
            if (target == uint.MaxValue || target % 4 == 0) return;
            throw new UdonVMException($"Unaligned jump address '0x{target:x8}'. Addresses must be aligned to {4}-byte boundaries.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint ReadUInt(byte[] bytes, uint i)
        {
            return (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]);
        }

        public void SetProgramCounter(uint value)
        {
            CheckJumpTarget(value);
            _pc = value;
        }

        public uint GetProgramCounter() => _pc;

        public IUdonHeap InspectHeap() => _heap;
    }
}
