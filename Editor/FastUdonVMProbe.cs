using System;
using VRC.Udon.Common.Interfaces;

namespace MagmaVRC.UdonVM
{
    /// <summary>Optional instrumentation hooks read by <see cref="FastUdonVM"/>. Assign the fields, then increment <see cref="Generation"/>; each VM re-reads them on its next Interpret.</summary>
    public static class FastUdonVMProbe
    {
        /// <summary>Word flag: the JUMP (or JUMP_INDIRECT) is a call whose return address, pushed on the VM stack, is the jump address + 8.</summary>
        public const byte CallFlag = 1;
        /// <summary>Word flag: the JUMP, or the taken JUMP_IF_FALSE, enters a method without pushing a return address.</summary>
        public const byte TailFlag = 2;
        /// <summary>Word flag: the JUMP_INDIRECT may return to a caller.</summary>
        public const byte ReturnFlag = 4;
        /// <summary>Word flag: the JUMP is a call whose return address (jump address + 8) was copied into a heap slot by the preceding COPY; the slot comes from <see cref="HeapCallSlots"/>.</summary>
        public const byte HeapCallFlag = 8;

        /// <summary>Returns one flag byte per bytecode word of a program, or null to leave the program uninstrumented. Re-run only when the delegate instance changes or a program loads.</summary>
        public static Func<IUdonProgram, byte[]> Classify;
        /// <summary>Returns, per bytecode word, the heap slot that receives the return address of a <see cref="HeapCallFlag"/> jump, or null when the program has none. Read together with <see cref="Classify"/>.</summary>
        public static Func<IUdonProgram, uint[]> HeapCallSlots;
        /// <summary>Called with (jump address, target) before a verified call jump.</summary>
        public static Action<uint, uint> Call;
        /// <summary>Called with (jump address, target) before a tail jump.</summary>
        public static Action<uint, uint> Tail;
        /// <summary>Called with (jump address, target) before a flagged JUMP_INDIRECT that is not a call.</summary>
        public static Action<uint, uint> Return;
        /// <summary>Returns an id for one extern site, given its signature; called once per site per delegate instance. The id is later passed to <see cref="Extern"/>.</summary>
        public static Func<string, int> RegisterExtern;
        /// <summary>Called with the site id from <see cref="RegisterExtern"/> for each extern that ran without its wrapper delegate (fused op or site-cache hit).</summary>
        public static Action<int> Extern;
        /// <summary>Incremented after any field changes.</summary>
        public static int Generation;
    }
}
