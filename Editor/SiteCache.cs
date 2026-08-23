using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace MagmaVRC.UdonVM
{
    /// <summary>
    /// Per-EXTERN-site inline cache for <c>IUdonEventReceiver</c> GetProgramVariable / SetProgramVariable / SendCustomEvent.
    /// Caches the resolved heap or entry address of the last <see cref="UdonBehaviour"/> target and program instance.
    /// </summary>
    public sealed class SiteCache
    {
        const string ModuleName = "VRCUdonCommonInterfacesIUdonEventReceiver";
        const string GetSignature = ModuleName + ".__GetProgramVariable__SystemString__SystemObject";
        const string SetSignature = ModuleName + ".__SetProgramVariable__SystemString_SystemObject__SystemVoid";
        const string SendSignature = ModuleName + ".__SendCustomEvent__SystemString__SystemVoid";

        enum Kind { Get, Set, Send }

        static readonly AccessTools.FieldRef<UdonBehaviour, IUdonProgram> ProgramOf;
        static readonly AccessTools.FieldRef<UdonBehaviour, SortedDictionary<uint, (uint, uint)>> ChangeEventsOf;
        static readonly Action<UdonBehaviour, uint> RunProgram;

        static SiteCache()
        {
            try
            {
                ProgramOf = AccessTools.FieldRefAccess<UdonBehaviour, IUdonProgram>("_program");
                ChangeEventsOf = AccessTools.FieldRefAccess<UdonBehaviour, SortedDictionary<uint, (uint, uint)>>("_variableToChangeEvent");
                RunProgram = AccessTools.MethodDelegate<Action<UdonBehaviour, uint>>(AccessTools.Method(typeof(UdonBehaviour), "RunProgram", new[] { typeof(uint) }));
            }
            catch (Exception)
            {
                ProgramOf = null;
                ChangeEventsOf = null;
                RunProgram = null;
            }
        }

        readonly Kind _kind;
        readonly IUdonSecurityFilter _filter;
        UdonBehaviour _target;
        IUdonProgram _program;
        SortedDictionary<uint, (uint, uint)> _changeEvents;
        string _symbol;
        uint _address;

        SiteCache(Kind kind, IUdonSecurityFilter filter)
        {
            _kind = kind;
            _filter = filter;
        }

        /// <summary>Returns a cache for a supported extern signature, or null.</summary>
        public static SiteCache Create(IUdonWrapper wrapper, string signature)
        {
            Kind kind;
            switch (signature)
            {
                case GetSignature: kind = Kind.Get; break;
                case SetSignature: kind = Kind.Set; break;
                case SendSignature: kind = Kind.Send; break;
                default: return null;
            }
            if (ProgramOf == null || ChangeEventsOf == null || RunProgram == null) return null;
            var module = wrapper.GetWrapperModuleByName(ModuleName);
            var filter = module == null ? null : AccessTools.Field(module.GetType(), "_filter")?.GetValue(module) as IUdonSecurityFilter;
            if (filter == null && kind != Kind.Send) return null;
            return new SiteCache(kind, filter);
        }

        /// <summary>Runs the extern from the cache. Returns false without side effects when the site misses.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRun(IStrongBox[] boxes, IUdonHeap heap, Span<uint> p)
        {
            if (!Match(boxes, p)) return false;
            switch (_kind)
            {
                case Kind.Get:
                {
                    object value = _program.Heap.GetHeapVariable(_address);
                    _filter.ApplyFilter(ref value);
                    heap.SetHeapVariable(p[2], value);
                    return true;
                }
                case Kind.Set:
                {
                    if (_changeEvents.Count != 0 && _changeEvents.ContainsKey(_address)) return false;
                    object value = heap.GetHeapVariable<object>(p[2]);
                    _filter.ApplyFilter(ref value);
                    _program.Heap.SetHeapVariable(_address, value);
                    return true;
                }
                default:
                    RunProgram(_target, _address);
                    return true;
            }
        }

        /// <summary>Resolves and stores the site's target after the wrapper delegate ran.</summary>
        public void Warm(IStrongBox[] boxes, Span<uint> p)
        {
            _program = null;
            if (!Receiver(boxes, p, out var ub, out var symbol)) return;
            var program = ProgramOf(ub);
            if (program == null) return;
            bool found = _kind == Kind.Send ? program.EntryPoints.HasExportedSymbol(symbol) : program.SymbolTable.TryGetAddressFromSymbol(symbol, out _address);
            if (!found) return;
            if (_kind == Kind.Send) _address = program.EntryPoints.GetAddressFromSymbol(symbol);
            _target = ub;
            _program = program;
            _changeEvents = ChangeEventsOf(ub);
            _symbol = symbol;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Match(IStrongBox[] boxes, Span<uint> p)
        {
            return _program != null
                && Receiver(boxes, p, out var ub, out var symbol)
                && ReferenceEquals(ub, _target)
                && (ReferenceEquals(symbol, _symbol) || symbol == _symbol)
                && ReferenceEquals(ProgramOf(ub), _program);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Receiver(IStrongBox[] boxes, Span<uint> p, out UdonBehaviour ub, out string symbol)
        {
            ub = null;
            symbol = null;
            if (p[0] >= boxes.Length || p[1] >= boxes.Length) return false;
            if (!(boxes[p[0]]?.Value is UdonBehaviour b) || !(boxes[p[1]] is StrongBox<string> s)) return false;
            ub = b;
            symbol = s.Value;
            return symbol != null;
        }
    }
}
