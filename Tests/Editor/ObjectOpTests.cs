using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.Udon.ClientBindings;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using Object = UnityEngine.Object;

namespace MagmaVRC.UdonVM.Tests
{
    public class ObjectOpTests
    {
        const uint HeapSize = 12;
        const int Seeds = 60;

        static readonly IUdonWrapper Wrapper = new UdonClientInterface().GetWrapper();

        GameObject _live;
        GameObject _dead;

        public static IEnumerable<string> Signatures => Registered()
            .Where(s => s.StartsWith("UnityEngineObject.") || s.StartsWith("SystemObject.") || s.StartsWith("VRCSDKBaseUtilities."))
            .OrderBy(s => s, StringComparer.Ordinal);

        static IEnumerable<string> Registered()
        {
            FusedOps.RegisterObjectOps();
            FusedOps.RegisterVrcOps();
            return FusedOps.Signatures;
        }

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            FusedOps.BindFilter(Wrapper);
            _live = new GameObject("ObjectOpTests.live");
            _dead = new GameObject("ObjectOpTests.dead");
            Object.DestroyImmediate(_dead);
        }

        [TearDown]
        public void TearDown()
        {
            if (_live != null) Object.DestroyImmediate(_live);
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void ObjectSignaturesAreRegistered()
        {
            CollectionAssert.IsNotEmpty(Signatures.ToList());
        }

        [Test, TestCaseSource(nameof(Signatures))]
        public void MatchesWrapper(string signature)
        {
            var del = Wrapper.GetExternFunctionDelegate(signature);
            var fused = FusedOps.Get(signature);
            var types = FusedOps.SlotTypes(signature);
            int count = Wrapper.GetExternFunctionParameterCount(signature);
            Assert.AreEqual(count, types.Length, signature + " parameter count");
            int hits = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                int s = seed * 7919 + signature.Length;
                var a = Build(s, types, out var pa);
                var b = Build(s, types, out var pb);
                Type exA = null, exB = null;
                try { del(a, pa); } catch (Exception e) { exA = e.GetType(); }
                bool hit = true;
                try { hit = fused(HeapAccess.Boxes(b), pb); } catch (Exception e) { exB = e.GetType(); }
                if (hit) hits++;
                else
                {
                    try { del(b, pb); } catch (Exception e) { exB = e.GetType(); }
                }
                string where = signature + " seed " + seed + " addresses [" + string.Join(",", pa) + "]";
                Assert.AreEqual(exA, exB, where + " exception");
                var da = new List<(uint, IStrongBox, Type)>();
                var db = new List<(uint, IStrongBox, Type)>();
                a.DumpHeapObjects(da);
                b.DumpHeapObjects(db);
                Assert.AreEqual(da.Count, db.Count, where + " slot count");
                for (int i = 0; i < da.Count; i++)
                {
                    Assert.AreEqual(da[i].Item1, db[i].Item1, where + " slot address");
                    Assert.AreEqual(da[i].Item2.GetType(), db[i].Item2.GetType(), where + " slot " + da[i].Item1 + " box type");
                    Assert.IsTrue(Same(da[i].Item2.Value, db[i].Item2.Value), where + " slot " + da[i].Item1 + " value: " + Show(da[i].Item2.Value) + " vs " + Show(db[i].Item2.Value));
                }
            }
            Assert.Greater(hits, 0, signature + " never took the fused path");
        }

        UdonHeap Build(int seed, Type[] types, out uint[] p)
        {
            var rng = new System.Random(seed);
            var heap = new UdonHeap(HeapSize);
            p = new uint[types.Length];
            for (int i = 0; i < p.Length; i++)
            {
                int roll = rng.Next(100);
                if (roll < 4) p[i] = HeapSize + (uint)rng.Next(3);
                else if (roll < 20 && i > 0) p[i] = p[rng.Next(i)];
                else p[i] = (uint)rng.Next((int)HeapSize);
            }
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i] >= HeapSize) continue;
                int roll = rng.Next(100);
                if (roll < 4) continue;
                if (roll < 8) { heap.InitializeHeapVariable(p[i], types[i]); continue; }
                var (value, type) = Slot(rng, types[i]);
                heap.SetHeapVariable(p[i], value, type);
            }
            return heap;
        }

        (object value, Type type) Slot(System.Random rng, Type declared)
        {
            if (declared == typeof(bool)) return (rng.Next(2) == 0, typeof(bool));
            switch (rng.Next(12))
            {
                case 0: return (null, declared);
                case 1: return (_live, typeof(GameObject));
                case 2: return (_live, typeof(Object));
                case 3: return (_live, typeof(object));
                case 4: return (_dead, typeof(GameObject));
                case 5: return (_dead, typeof(Object));
                case 6: return (_dead, typeof(object));
                case 7: return (rng.Next(2) == 0 ? "abc" : "", typeof(string));
                case 8: return (rng.Next(-1, 3), typeof(int));
                case 9: return (rng.Next(-1, 3), typeof(object));
                case 10: return (_live.transform, typeof(Transform));
                default: return ("abc", typeof(object));
            }
        }

        static bool Same(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (a is Object || b is Object) return ReferenceEquals(a, b);
            return a.GetType() == b.GetType() && a.Equals(b);
        }

        static string Show(object o) => o == null ? "null" : o.GetType().Name + ":" + o;
    }
}
