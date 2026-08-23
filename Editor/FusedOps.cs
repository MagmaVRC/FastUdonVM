using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MagmaVRC.UdonVM
{
    /// <summary>
    /// Native replacement for one extern. Receives the heap slots and the popped parameter addresses in wrapper order.
    /// Returns false without side effects when the slots don't match the fast path, so the caller runs the wrapper delegate instead.
    /// </summary>
    public delegate bool FusedOp(IStrongBox[] heap, Span<uint> parameters);

    /// <summary>Registry of fused ops keyed by full extern signature.</summary>
    public static partial class FusedOps
    {
        static readonly Dictionary<string, FusedOp> Table = new Dictionary<string, FusedOp>();

        static FusedOps()
        {
            Register();
        }

        /// <summary>Returns the fused op for a signature, or null.</summary>
        public static FusedOp Get(string signature) => Table.TryGetValue(signature, out var op) ? op : null;

        /// <summary>All registered signatures.</summary>
        public static IEnumerable<string> Signatures => Table.Keys;

        static void Add(string signature, FusedOp op) => Table[signature] = op;

        static partial void Register();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Read<T>(IStrongBox[] h, uint address, out T value)
        {
            if (address < h.Length && h[address] is StrongBox<T> box)
            {
                value = box.Value;
                return true;
            }
            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Binary<TA, TB, TR>(IStrongBox[] h, Span<uint> p, Func<TA, TB, TR> f)
        {
            if (!Read(h, p[0], out TA a) || !Read(h, p[1], out TB b) || p[2] >= h.Length) return false;
            HeapAccess.Set(h, p[2], f(a, b));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Unary<TA, TR>(IStrongBox[] h, Span<uint> p, Func<TA, TR> f)
        {
            if (!Read(h, p[0], out TA a) || p[1] >= h.Length) return false;
            HeapAccess.Set(h, p[1], f(a));
            return true;
        }
    }
}
