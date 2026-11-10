using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.ProgramSources;
using VRC.Udon.VM.Common;

namespace MagmaVRC.UdonVM.Tests
{
    public class CorpusTests
    {
        const string Folder = "Assets/SerializedUdonPrograms";

        static readonly HashSet<string> Pure = new HashSet<string>(FusedOps.Signatures)
        {
            "SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32",
            "SystemInt32.__op_Subtraction__SystemInt32_SystemInt32__SystemInt32",
            "SystemInt32.__op_Multiplication__SystemInt32_SystemInt32__SystemInt32",
            "SystemInt32.__op_Division__SystemInt32_SystemInt32__SystemInt32",
            "SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean",
            "SystemInt32.__op_GreaterThan__SystemInt32_SystemInt32__SystemBoolean",
            "SystemInt32.__op_Equality__SystemInt32_SystemInt32__SystemBoolean",
            "SystemInt32.__op_Inequality__SystemInt32_SystemInt32__SystemBoolean",
            "SystemInt32.__ToString__SystemString",
            "SystemSingle.__op_Addition__SystemSingle_SystemSingle__SystemSingle",
            "SystemSingle.__op_Multiplication__SystemSingle_SystemSingle__SystemSingle",
            "SystemString.__Concat__SystemString_SystemString__SystemString",
            "SystemString.__get_Length__SystemInt32",
            "SystemBoolean.__op_ConditionalAnd__SystemBoolean_SystemBoolean__SystemBoolean",
            "SystemBoolean.__op_ConditionalOr__SystemBoolean_SystemBoolean__SystemBoolean",
            "SystemBoolean.__op_UnaryNegation__SystemBoolean__SystemBoolean",
            "SystemInt32Array.__Get__SystemInt32__SystemInt32",
            "SystemInt32Array.__Set__SystemInt32_SystemInt32__SystemVoid",
            "SystemInt32Array.__get_Length__SystemInt32",
        };

        [Test, Timeout(120000)]
        public void AllPureEntryPointsMatch()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) Assert.Ignore(Folder + " missing");
            var guids = AssetDatabase.FindAssets("t:SerializedUdonProgramAsset", new[] { Folder });
            if (guids.Length == 0) Assert.Ignore("no programs");
            int programs = 0, ran = 0, skipped = 0;
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SerializedUdonProgramAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var program = asset.RetrieveProgram();
                if (program == null) continue;
                programs++;
                foreach (var entry in program.EntryPoints.GetExportedSymbols())
                {
                    var impure = FirstImpure(program, program.EntryPoints.GetAddressFromSymbol(entry));
                    if (impure != null)
                    {
                        skipped++;
                        Debug.Log($"skip {asset.name}.{entry}: {impure}");
                        continue;
                    }
                    foreach (var fused in new[] { true, false })
                    {
                        var stock = VMDifferential.Run(VMDifferential.Stock(), asset.RetrieveProgram(), new[] { entry });
                        var fast = VMDifferential.Run(VMDifferential.Fast(fused), asset.RetrieveProgram(), new[] { entry });
                        try { VMDifferential.AssertSame(stock, fast); }
                        catch (AssertionException e) { throw new AssertionException($"{asset.name}.{entry} fused={fused}: {e.Message}", e); }
                    }
                    ran++;
                }
            }
            Debug.Log($"corpus: {programs} programs, {ran} entry points run, {skipped} skipped");
        }

        static string FirstImpure(IUdonProgram program, uint entry)
        {
            var code = program.ByteCode;
            var seen = new HashSet<uint>();
            var work = new Stack<uint>();
            work.Push(entry);
            while (work.Count > 0)
            {
                uint pc = work.Pop();
                while ((long)pc + 3 < code.Length && seen.Add(pc))
                {
                    var op = (OpCode)Read(code, pc);
                    uint operand = (long)pc + 7 < code.Length ? Read(code, pc + 4) : 0;
                    switch (op)
                    {
                        case OpCode.NOP: case OpCode.POP: case OpCode.COPY:
                            pc += 4;
                            continue;
                        case OpCode.EXTERN:
                            if (!program.Heap.TryGetHeapVariable(operand, out string sig)) return $"extern slot 0x{operand:X8} not a string";
                            if (!Pure.Contains(sig)) return sig;
                            pc += 8;
                            continue;
                        case OpCode.JUMP:
                            if (operand == uint.MaxValue - 3) goto next;
                            work.Push(operand);
                            pc += 8; // Also fall through past JUMP so call sites stay covered; over-approximates reachability.
                            continue;
                        case OpCode.JUMP_IF_FALSE:
                            work.Push(operand);
                            pc += 8;
                            continue;
                        case OpCode.JUMP_INDIRECT:
                            goto next;
                        case OpCode.PUSH: case OpCode.ANNOTATION:
                            pc += 8;
                            continue;
                        default:
                            return $"bad opcode {op} at 0x{pc:X8}";
                    }
                }
                next:;
            }
            return null;
        }

        static uint Read(byte[] b, uint i) => (uint)((b[i] << 24) + (b[i + 1] << 16) + (b[i + 2] << 8) + b[i + 3]);
    }
}
