using System;
using System.Diagnostics;
using NUnit.Framework;
using VRC.Udon.VM;

namespace MagmaVRC.UdonVM.Tests
{
    public class DifferentialVMTests
    {
        [TestCase(true), TestCase(false)]
        public void ArithmeticLoop(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.ArithmeticLoop, fused);
            Assert.IsNull(stock.Error);
            Assert.AreEqual(0u, stock.Results[0]);
        }

        [TestCase(true), TestCase(false)]
        public void StringBuilding(bool fused) => VMDifferential.Compare(UAssemblyPrograms.StringBuilding, fused);

        [TestCase(true), TestCase(false)]
        public void IntArraySum(bool fused) => VMDifferential.Compare(UAssemblyPrograms.IntArraySum, fused);

        [TestCase(true), TestCase(false)]
        public void MixedObjectArray(bool fused) => VMDifferential.Compare(UAssemblyPrograms.MixedObjectArray, fused);

        [TestCase(true), TestCase(false)]
        public void BooleanLogic(bool fused) => VMDifferential.Compare(UAssemblyPrograms.BooleanLogic, fused);

        [TestCase(true), TestCase(false)]
        public void CallReturn(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.CallReturn, fused);
            Assert.IsNull(stock.Error);
            Assert.AreEqual(uint.MaxValue - 3, stock.ProgramCounters[0]);
        }

        [TestCase(true), TestCase(false)]
        public void PopUnderflowHalts(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.PopUnderflow, fused, runs: 2);
            CollectionAssert.AreEqual(new[] { 1u, 1u }, stock.Results);
        }

        [TestCase(true), TestCase(false)]
        public void CopyUnderflowHalts(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.CopyUnderflow, fused, runs: 2);
            CollectionAssert.AreEqual(new[] { 1u, 1u }, stock.Results);
        }

        [TestCase(true), TestCase(false)]
        public void UnalignedJump(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.UnalignedJump, fused);
            Assert.IsInstanceOf<UdonVMException>(stock.Error);
            StringAssert.Contains("Unaligned jump address", stock.Error.InnerException.Message);
        }

        [TestCase(true), TestCase(false)]
        public void SetProgramCounterUnaligned(bool fused)
        {
            var s = Assert.Throws<UdonVMException>(() => VMDifferential.Stock().SetProgramCounter(6));
            var f = Assert.Throws<UdonVMException>(() => VMDifferential.Fast(fused).SetProgramCounter(6));
            Assert.AreEqual(s.Message, f.Message);
        }

        [TestCase(true), TestCase(false)]
        public void UnsupportedExtern(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.UnsupportedExtern, fused);
            Assert.IsInstanceOf<UdonVMException>(stock.Error);
        }

        [TestCase(true), TestCase(false)]
        public void DivideByZero(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.DivideByZero, fused);
            Assert.IsInstanceOf<UdonVMException>(stock.Error);
            Assert.IsInstanceOf<UdonVMException>(stock.Error.InnerException);
            var root = stock.Error;
            while (root.InnerException != null) root = root.InnerException;
            Assert.IsInstanceOf<DivideByZeroException>(root);
        }

        [TestCase(true), TestCase(false)]
        public void ExternOperandNotString(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.ExternOperandNotString, fused);
            StringAssert.Contains("EXTERN operand must be of type 'System.String'", stock.Error.InnerException.Message);
        }

        [TestCase(true), TestCase(false)]
        public void UninitializedCopy(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.UninitializedCopy, fused);
            Assert.IsInstanceOf<UdonVMException>(stock.Error);
        }

        [TestCase(true), TestCase(false)]
        public void ReentrantProgramCounter(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.TwoEntries, fused, new[] { "_a", "_b" });
            Assert.IsNull(stock.Error);
            Assert.AreEqual(uint.MaxValue - 3, stock.ProgramCounters[0]);
            Assert.AreEqual((uint)UAssemblyPrograms.Assemble(UAssemblyPrograms.TwoEntries).ByteCode.Length, stock.ProgramCounters[1]);
        }

        [TestCase(true), TestCase(false)]
        public void DebugLoggingExternFailure(bool fused)
        {
            var stock = VMDifferential.Run(VMDifferential.Stock(), UAssemblyPrograms.Assemble(UAssemblyPrograms.DivideByZero), new[] { "_start" }, debug: true);
            var fast = VMDifferential.Run(VMDifferential.Fast(fused), UAssemblyPrograms.Assemble(UAssemblyPrograms.DivideByZero), new[] { "_start" }, debug: true);
            foreach (var o in new[] { stock, fast })
            {
                Assert.IsInstanceOf<UdonVMException>(o.Error);
                StringAssert.StartsWith("The VM encountered an error!", o.Error.Message);
                StringAssert.Contains("Parameter Addresses", o.Error.InnerException.Message);
            }
            VMDifferential.AssertSameError(stock.Error.InnerException, fast.Error.InnerException);
            VMDifferential.AssertSameHeap(stock.Heap, fast.Heap);
        }

        [TestCase(true), TestCase(false)]
        public void NopAndAnnotation(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.NopAnnotation, fused);
            Assert.IsNull(stock.Error);
        }

        [TestCase(true), TestCase(false)]
        public void JumpIntoCompareBranch(bool fused)
        {
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.JumpIntoCompareBranch, fused);
            Assert.IsNull(stock.Error);
        }

        [TestCase(true), TestCase(false)]
        public void Watchdog(bool fused)
        {
            if (Debugger.IsAttached) Assert.Ignore("watchdog is disabled while a debugger is attached");
            var (stock, _) = VMDifferential.Compare(UAssemblyPrograms.InfiniteLoop, fused, time: () => new FakeTimeSource { Step = 6000 });
            Assert.IsInstanceOf<UdonVMException>(stock.Error);
            StringAssert.Contains("exceeded max VM time", stock.Error.InnerException.Message);
        }
    }
}
