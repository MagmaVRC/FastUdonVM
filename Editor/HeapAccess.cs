using System.Reflection;
using System.Runtime.CompilerServices;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace MagmaVRC.UdonVM
{
    /// <summary>Direct access to the slot array of a <see cref="UdonHeap"/>.</summary>
    public static class HeapAccess
    {
        static readonly FieldInfo HeapField = typeof(UdonHeap).GetField("_heap", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Returns the heap's slot array, or null when the heap is not a <see cref="UdonHeap"/>.</summary>
        public static IStrongBox[] Boxes(IUdonHeap heap)
        {
            return heap is UdonHeap h && HeapField != null ? (IStrongBox[])HeapField.GetValue(h) : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(IStrongBox[] h, uint address, T value)
        {
            if (h[address] is StrongBox<T> box) box.Value = value;
            else h[address] = new StrongBox<T>(value);
        }
    }
}
