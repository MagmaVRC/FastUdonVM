using System.Diagnostics;
using System.Text;
using UnityEditor;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.EditorBindings;

namespace MagmaVRC.UdonVM
{
    public static class FastUdonVMBenchmark
    {
        const int Runs = 20;

        const string IntAdd = "SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32";
        const string IntLess = "SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean";
        const string IntToStr = "SystemInt32.__ToString__SystemString";
        const string StrConcat = "SystemString.__Concat__SystemString_SystemString__SystemString";
        const string IntArrCtor = "SystemInt32Array.__ctor__SystemInt32__SystemInt32Array";
        const string IntArrSet = "SystemInt32Array.__Set__SystemInt32_SystemInt32__SystemVoid";
        const string IntArrGet = "SystemInt32Array.__Get__SystemInt32__SystemInt32";

        static readonly (string name, string source)[] Workloads =
        {
            ("arithmetic loop", $@"
.data_start
    i: %SystemInt32, 0
    one: %SystemInt32, 1
    n: %SystemInt32, 100000
    sum: %SystemInt32, 0
    cond: %SystemBoolean, null
.data_end
.code_start
    .export _start
    _start:
    NOP
    loop:
    PUSH, i
    PUSH, n
    PUSH, cond
    EXTERN, ""{IntLess}""
    PUSH, cond
    JUMP_IF_FALSE, done
    PUSH, sum
    PUSH, i
    PUSH, sum
    EXTERN, ""{IntAdd}""
    PUSH, i
    PUSH, one
    PUSH, i
    EXTERN, ""{IntAdd}""
    JUMP, loop
    done:
    JUMP, 0xFFFFFFFC
.code_end
"),
            ("string concat", $@"
.data_start
    i: %SystemInt32, 0
    one: %SystemInt32, 1
    n: %SystemInt32, 2000
    s: %SystemString, """"
    tmp: %SystemString, null
    cond: %SystemBoolean, null
.data_end
.code_start
    .export _start
    _start:
    NOP
    loop:
    PUSH, i
    PUSH, n
    PUSH, cond
    EXTERN, ""{IntLess}""
    PUSH, cond
    JUMP_IF_FALSE, done
    PUSH, i
    PUSH, tmp
    EXTERN, ""{IntToStr}""
    PUSH, s
    PUSH, tmp
    PUSH, s
    EXTERN, ""{StrConcat}""
    PUSH, i
    PUSH, one
    PUSH, i
    EXTERN, ""{IntAdd}""
    JUMP, loop
    done:
    JUMP, 0xFFFFFFFC
.code_end
"),
            ("array sum", $@"
.data_start
    arr: %SystemInt32Array, null
    len: %SystemInt32, 20000
    i: %SystemInt32, 0
    one: %SystemInt32, 1
    zero: %SystemInt32, 0
    sum: %SystemInt32, 0
    tmp: %SystemInt32, 0
    cond: %SystemBoolean, null
.data_end
.code_start
    .export _start
    _start:
    PUSH, len
    PUSH, arr
    EXTERN, ""{IntArrCtor}""
    fill:
    PUSH, i
    PUSH, len
    PUSH, cond
    EXTERN, ""{IntLess}""
    PUSH, cond
    JUMP_IF_FALSE, fillDone
    PUSH, arr
    PUSH, i
    PUSH, i
    EXTERN, ""{IntArrSet}""
    PUSH, i
    PUSH, one
    PUSH, i
    EXTERN, ""{IntAdd}""
    JUMP, fill
    fillDone:
    PUSH, zero
    PUSH, i
    COPY
    read:
    PUSH, i
    PUSH, len
    PUSH, cond
    EXTERN, ""{IntLess}""
    PUSH, cond
    JUMP_IF_FALSE, readDone
    PUSH, arr
    PUSH, i
    PUSH, tmp
    EXTERN, ""{IntArrGet}""
    PUSH, sum
    PUSH, tmp
    PUSH, sum
    EXTERN, ""{IntAdd}""
    PUSH, i
    PUSH, one
    PUSH, i
    EXTERN, ""{IntAdd}""
    JUMP, read
    readDone:
    JUMP, 0xFFFFFFFC
.code_end
"),
        };

        [MenuItem("VRChat SDK/Udon VM/Benchmark")]
        public static void Run()
        {
            var editor = new UdonEditorInterface();
            var table = new StringBuilder("FastUdonVM benchmark (" + Runs + " runs each, ms)\n");
            table.AppendLine($"{"workload",-18}{"stock",10}{"fast",10}{"fast+fused",12}{"ratio",8}{"ratio+fused",13}");
            foreach (var (name, source) in Workloads)
            {
                double stock = Time(editor, source, () => editor.ConstructUdonVM());
                double fast = Time(editor, source, () => new FastUdonVM(editor.GetWrapper(), new VRC.Udon.ClientBindings.UdonVMTimeSource(), false));
                double fused = Time(editor, source, () => new FastUdonVM(editor.GetWrapper(), new VRC.Udon.ClientBindings.UdonVMTimeSource(), true));
                table.AppendLine($"{name,-18}{stock,10:F1}{fast,10:F1}{fused,12:F1}{stock / fast,8:F2}x{stock / fused,12:F2}x");
            }
            UnityEngine.Debug.Log(table.ToString());
        }

        static double Time(UdonEditorInterface editor, string source, System.Func<IUdonVM> make)
        {
            var sw = new Stopwatch();
            for (int i = 0; i < Runs; i++)
            {
                var program = editor.Assemble(source);
                var vm = make();
                vm.LoadProgram(program);
                vm.SetProgramCounter(program.EntryPoints.GetAddressFromSymbol("_start"));
                sw.Start();
                vm.Interpret();
                sw.Stop();
            }
            return sw.Elapsed.TotalMilliseconds;
        }
    }
}
