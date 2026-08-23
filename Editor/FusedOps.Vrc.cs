using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace MagmaVRC.UdonVM
{
    public static partial class FusedOps
    {
        static bool IsValid(IStrongBox[] h, Span<uint> p)
        {
            if (p[0] >= h.Length || p[1] >= h.Length || h[p[0]] == null) return false;
            if (_filter == null) return false;
            object o = h[p[0]].Value;
            _filter.ApplyFilter(ref o);
            HeapAccess.Set(h, p[1], Utilities.IsValid(o));
            return true;
        }

        /// <summary>Registers fused ops for pure VRC SDK externs.</summary>
        public static void RegisterVrcOps()
        {
            Add("VRCSDKBaseUtilities.__IsValid__SystemObject__SystemBoolean", new[] { typeof(object), typeof(bool) }, IsValid);
            Add("VRCSDKBaseNetworking.__CalculateServerDeltaTime__SystemDouble_SystemDouble__SystemDouble", new[] { typeof(double), typeof(double), typeof(double) },
                (h, p) => Binary<double, double, double>(h, p, (a, b) => Networking.CalculateServerDeltaTime(a, b)));
        }
    }

    [InitializeOnLoad]
    static class FusedOpsVrcInit
    {
        static FusedOpsVrcInit() => FusedOps.RegisterVrcOps();
    }
}
