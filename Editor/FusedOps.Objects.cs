using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Object = UnityEngine.Object;

namespace MagmaVRC.UdonVM
{
    public static partial class FusedOps
    {
        // ponytail: one process-wide filter (one live wrapper in play mode); bind per VM if wrappers ever diverge
        static IUdonSecurityFilter<Object> _boundFilter;
        static bool _objectsRegistered;
        static bool _resolveTried;

        static IUdonSecurityFilter<Object> _filter
        {
            get
            {
                if (_boundFilter != null || _resolveTried || !UnityEngine.Application.isPlaying) return _boundFilter;
                _resolveTried = true;
                return _boundFilter = Resolve(UdonManager.Instance.GetWrapper());
            }
        }

        /// <summary>Binds the security filter used by the object fused ops to the one inside the given wrapper's "UnityEngineObject" module. Unbound ops resolve lazily from UdonManager.Instance and fall back to the wrapper until a filter is available.</summary>
        public static void BindFilter(IUdonWrapper wrapper)
        {
            _boundFilter = Resolve(wrapper);
            _resolveTried = false;
        }

        static IUdonSecurityFilter<Object> Resolve(IUdonWrapper wrapper)
        {
            var module = wrapper?.GetWrapperModuleByName("UnityEngineObject");
            return module?.GetType().GetField("_filter", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(module) as IUdonSecurityFilter<Object>;
        }

        /// <summary>Registers the UnityEngineObject / SystemObject equality fused ops. Idempotent.</summary>
        public static void RegisterObjectOps()
        {
            if (_objectsRegistered) return;
            _objectsRegistered = true;
            var uuo = new[] { typeof(Object), typeof(Object), typeof(bool) };
            var ooo = new[] { typeof(object), typeof(object), typeof(bool) };
            Add("UnityEngineObject.__op_Equality__UnityEngineObject_UnityEngineObject__SystemBoolean", uuo, (h, p) => UnityPair(h, p, (a, b) => a == b));
            Add("UnityEngineObject.__op_Inequality__UnityEngineObject_UnityEngineObject__SystemBoolean", uuo, (h, p) => UnityPair(h, p, (a, b) => a != b));
            Add("UnityEngineObject.__op_Implicit__UnityEngineObject__SystemBoolean", new[] { typeof(Object), typeof(bool) }, UnityImplicit);
            Add("UnityEngineObject.__Equals__SystemObject__SystemBoolean", new[] { typeof(Object), typeof(object), typeof(bool) }, UnityEquals);
            Add("SystemObject.__op_Equality__SystemObject_SystemObject__SystemBoolean", ooo, (h, p) => ObjectPair(h, p, (a, b) => a == b));
            Add("SystemObject.__op_Inequality__SystemObject_SystemObject__SystemBoolean", ooo, (h, p) => ObjectPair(h, p, (a, b) => a != b));
            Add("SystemObject.__ReferenceEquals__SystemObject_SystemObject__SystemBoolean", ooo, (h, p) => ObjectPair(h, p, (a, b) => a == b));
            Add("SystemObject.__Equals__SystemObject_SystemObject__SystemBoolean", ooo, (h, p) => ObjectPair(h, p, (a, b) => object.Equals(a, b)));
            Add("SystemObject.__Equals__SystemObject__SystemBoolean", ooo, ObjectEquals);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ReadObject(IStrongBox[] h, uint address, out object value)
        {
            value = null;
            if (address >= h.Length || h[address] == null) return false;
            value = h[address].Value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ReadUnity(IStrongBox[] h, uint address, out Object value)
        {
            value = null;
            if (!ReadObject(h, address, out var o)) return false;
            if (o == null) return true;
            value = o as Object;
            return !(value is null);
        }

        static bool UnityPair(IStrongBox[] h, Span<uint> p, Func<Object, Object, bool> f)
        {
            if (_filter == null || p[2] >= h.Length || !ReadUnity(h, p[0], out var a) || !ReadUnity(h, p[1], out var b)) return false;
            _filter.ApplyFilter(ref a);
            _filter.ApplyFilter(ref b);
            HeapAccess.Set(h, p[2], f(a, b));
            return true;
        }

        static bool UnityImplicit(IStrongBox[] h, Span<uint> p)
        {
            if (_filter == null || p[1] >= h.Length || !ReadUnity(h, p[0], out var a)) return false;
            _filter.ApplyFilter(ref a);
            HeapAccess.Set(h, p[1], (bool)a);
            return true;
        }

        static bool UnityEquals(IStrongBox[] h, Span<uint> p)
        {
            if (_filter == null || p[2] >= h.Length || !ReadUnity(h, p[0], out var a) || !ReadObject(h, p[1], out var b)) return false;
            _filter.ApplyFilter(ref a);
            // ponytail: null receiver falls back so the wrapper throws its own NRE; filter runs twice on that path, harmless
            if (a is null) return false;
            _filter.ApplyFilter(ref b);
            HeapAccess.Set(h, p[2], ((object)a).Equals(b));
            return true;
        }

        static bool ObjectPair(IStrongBox[] h, Span<uint> p, Func<object, object, bool> f)
        {
            if (_filter == null || p[2] >= h.Length || !ReadObject(h, p[0], out var a) || !ReadObject(h, p[1], out var b)) return false;
            _filter.ApplyFilter(ref a);
            _filter.ApplyFilter(ref b);
            HeapAccess.Set(h, p[2], f(a, b));
            return true;
        }

        static bool ObjectEquals(IStrongBox[] h, Span<uint> p)
        {
            if (_filter == null || p[2] >= h.Length || !ReadObject(h, p[0], out var a) || !ReadObject(h, p[1], out var b)) return false;
            _filter.ApplyFilter(ref a);
            if (a == null) return false;
            _filter.ApplyFilter(ref b);
            HeapAccess.Set(h, p[2], a.Equals(b));
            return true;
        }
    }

    [UnityEditor.InitializeOnLoad]
    static class FusedOpsObjectsRegistrar
    {
        static FusedOpsObjectsRegistrar() => FusedOps.RegisterObjectOps();
    }
}
