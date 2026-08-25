using VRC.Udon.Common.Interfaces;
using VRC.Udon.EditorBindings;

namespace MagmaVRC.UdonVM.Tests
{
    public static class UAssemblyPrograms
    {
        public static readonly UdonEditorInterface Editor = new UdonEditorInterface();

        public static IUdonProgram Assemble(string source) => Editor.Assemble(source);

        const string IntAdd = "SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32";
        const string IntLess = "SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean";
        const string IntEq = "SystemInt32.__op_Equality__SystemInt32_SystemInt32__SystemBoolean";
        const string IntDiv = "SystemInt32.__op_Division__SystemInt32_SystemInt32__SystemInt32";
        const string IntToStr = "SystemInt32.__ToString__SystemString";
        const string StrConcat = "SystemString.__Concat__SystemString_SystemString__SystemString";
        const string BoolAnd = "SystemBoolean.__op_ConditionalAnd__SystemBoolean_SystemBoolean__SystemBoolean";
        const string BoolOr = "SystemBoolean.__op_ConditionalOr__SystemBoolean_SystemBoolean__SystemBoolean";
        const string BoolNot = "SystemBoolean.__op_UnaryNegation__SystemBoolean__SystemBoolean";
        const string IntArrCtor = "SystemInt32Array.__ctor__SystemInt32__SystemInt32Array";
        const string IntArrSet = "SystemInt32Array.__Set__SystemInt32_SystemInt32__SystemVoid";
        const string IntArrGet = "SystemInt32Array.__Get__SystemInt32__SystemInt32";
        const string ObjArrCtor = "SystemObjectArray.__ctor__SystemInt32__SystemObjectArray";
        const string ObjArrSet = "SystemObjectArray.__Set__SystemInt32_SystemObject__SystemVoid";

        public static readonly string ArithmeticLoop = $@"
.data_start
    i: %SystemInt32, 0
    one: %SystemInt32, 1
    n: %SystemInt32, 10000
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
";

        public static readonly string StringBuilding = $@"
.data_start
    i: %SystemInt32, 0
    one: %SystemInt32, 1
    n: %SystemInt32, 50
    s: %SystemString, """"
    sep: %SystemString, "",""
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
    PUSH, s
    PUSH, sep
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
";

        public static readonly string IntArraySum = $@"
.data_start
    arr: %SystemInt32Array, null
    len: %SystemInt32, 100
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
";

        public static readonly string MixedObjectArray = $@"
.data_start
    objs: %SystemObjectArray, null
    three: %SystemInt32, 3
    zero: %SystemInt32, 0
    one: %SystemInt32, 1
    two: %SystemInt32, 2
    num: %SystemInt32, 42
    str: %SystemString, ""hello""
    flag: %SystemBoolean, null
.data_end
.code_start
    .export _start
    _start:
    PUSH, three
    PUSH, objs
    EXTERN, ""{ObjArrCtor}""
    PUSH, one
    PUSH, one
    PUSH, flag
    EXTERN, ""{IntEq}""
    PUSH, objs
    PUSH, zero
    PUSH, num
    EXTERN, ""{ObjArrSet}""
    PUSH, objs
    PUSH, one
    PUSH, str
    EXTERN, ""{ObjArrSet}""
    PUSH, objs
    PUSH, two
    PUSH, flag
    EXTERN, ""{ObjArrSet}""
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string BooleanLogic = $@"
.data_start
    one: %SystemInt32, 1
    two: %SystemInt32, 2
    t: %SystemBoolean, null
    f: %SystemBoolean, null
    a: %SystemBoolean, null
    b: %SystemBoolean, null
    c: %SystemBoolean, null
    m1: %SystemInt32, 0
    m2: %SystemInt32, 0
    m3: %SystemInt32, 0
.data_end
.code_start
    .export _start
    _start:
    PUSH, one
    PUSH, one
    PUSH, t
    EXTERN, ""{IntEq}""
    PUSH, one
    PUSH, two
    PUSH, f
    EXTERN, ""{IntEq}""
    PUSH, t
    PUSH, f
    PUSH, a
    EXTERN, ""{BoolAnd}""
    PUSH, t
    PUSH, f
    PUSH, b
    EXTERN, ""{BoolOr}""
    PUSH, f
    PUSH, c
    EXTERN, ""{BoolNot}""
    PUSH, a
    JUMP_IF_FALSE, skip1
    PUSH, one
    PUSH, m1
    COPY
    skip1:
    PUSH, b
    JUMP_IF_FALSE, skip2
    PUSH, one
    PUSH, m2
    COPY
    skip2:
    PUSH, c
    JUMP_IF_FALSE, skip3
    PUSH, two
    PUSH, m3
    COPY
    skip3:
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string CallReturn = $@"
.data_start
    a: %SystemInt32, 3
    r: %SystemInt32, 0
    ret: %SystemUInt32, null
    ret1: %SystemUInt32, 0x10
    ret2: %SystemUInt32, 0x20
.data_end
.code_start
    .export _start
    _start:
    PUSH, ret1
    JUMP, add
    PUSH, ret2
    JUMP, add
    JUMP, 0xFFFFFFFC
    add:
    PUSH, r
    PUSH, a
    PUSH, r
    EXTERN, ""{IntAdd}""
    PUSH, ret
    COPY
    JUMP_INDIRECT, ret
.code_end
";

        public static readonly string PopUnderflow = @"
.data_start
    x: %SystemInt32, 1
.data_end
.code_start
    .export _start
    _start:
    POP
    PUSH, x
    PUSH, x
    COPY
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string CopyUnderflow = @"
.data_start
    x: %SystemInt32, 1
.data_end
.code_start
    .export _start
    _start:
    PUSH, x
    COPY
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string UnalignedJump = @"
.data_start
    target: %SystemUInt32, 0x6
.data_end
.code_start
    .export _start
    _start:
    JUMP_INDIRECT, target
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string UnsupportedExtern = @"
.data_start
    x: %SystemInt32, 1
.data_end
.code_start
    .export _start
    _start:
    PUSH, x
    EXTERN, ""SystemInt32.__DoesNotExist__SystemInt32""
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string DivideByZero = $@"
.data_start
    x: %SystemInt32, 7
    zero: %SystemInt32, 0
    r: %SystemInt32, 0
.data_end
.code_start
    .export _start
    _start:
    PUSH, x
    PUSH, zero
    PUSH, r
    EXTERN, ""{IntDiv}""
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string ExternOperandNotString = @"
.data_start
    x: %SystemInt32, 1
.data_end
.code_start
    .export _start
    _start:
    EXTERN, x
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string UninitializedCopy = @"
.data_start
    x: %SystemInt32, 1
.data_end
.code_start
    .export _start
    _start:
    PUSH, 500u
    PUSH, x
    COPY
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string TwoEntries = $@"
.data_start
    one: %SystemInt32, 1
    two: %SystemInt32, 2
    a: %SystemInt32, 0
    b: %SystemInt32, 0
.data_end
.code_start
    .export _a
    .export _b
    _a:
    PUSH, one
    PUSH, a
    COPY
    JUMP, 0xFFFFFFFC
    _b:
    PUSH, a
    PUSH, two
    PUSH, b
    EXTERN, ""{IntAdd}""
    NOP
    NOP
.code_end
";

        public static readonly string NopAnnotation = $@"
.data_start
    one: %SystemInt32, 1
    x: %SystemInt32, 0
.data_end
.code_start
    .export _start
    _start:
    NOP
    ANNOTATION, ""note""
    PUSH, x
    ANNOTATION, ""between""
    PUSH, one
    NOP
    PUSH, x
    EXTERN, ""{IntAdd}""
    NOP
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string JumpIntoCompareBranch = $@"
.data_start
    i: %SystemInt32, 0
    one: %SystemInt32, 1
    n: %SystemInt32, 10
    sum: %SystemInt32, 0
    cond: %SystemBoolean, null
.data_end
.code_start
    .export _start
    _start:
    NOP
    PUSH, i
    PUSH, n
    PUSH, cond
    EXTERN, ""{IntLess}""
    JUMP, check
    loop:
    PUSH, sum
    PUSH, one
    PUSH, sum
    EXTERN, ""{IntAdd}""
    PUSH, i
    PUSH, one
    PUSH, i
    EXTERN, ""{IntAdd}""
    top:
    PUSH, i
    PUSH, n
    PUSH, cond
    EXTERN, ""{IntLess}""
    check:
    PUSH, cond
    JUMP_IF_FALSE, done
    JUMP, loop
    done:
    JUMP, 0xFFFFFFFC
.code_end
";

        public static readonly string InfiniteLoop = @"
.data_start
    x: %SystemInt32, 1
.data_end
.code_start
    .export _start
    _start:
    NOP
    loop:
    JUMP, loop
.code_end
";
    }
}
