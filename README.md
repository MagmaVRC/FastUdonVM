# Fast Udon VM

An editor-only, drop-in replacement for VRChat's Udon virtual machine. It runs the same bytecode against the same heap and the same extern wrapper as the stock VM, produces the same results, and spends a fraction of the time doing it. Nothing in this package ships in a world; the client always runs the stock VM. It exists to measure what a faster VM would buy, to test worlds faster in the editor, and as a concrete, compatible proposal.

The numbers below were captured with the companion Udon Profiler (`Window > Udon Profiler`), which shows per-script, per-event and per-extern cost.

## Quick start

1. Drop the package into `Packages/` (Unity 2022.3, `com.vrchat.worlds` ≥ 3.7).
2. `VRChat SDK > Udon VM > Use Fast VM`. Applies on the next play-mode entry.
3. Optional: `Fused Ops` (on by default), `Benchmark`, `Log Stats`.

Run your world in play mode as usual. If anything behaves differently from the stock VM, that is a bug here - see *Compatibility*.

## How Udon works

Everything below was established by reading the SDK's shipped assemblies (`VRC.Udon.VM`, `VRC.Udon.Common`, `VRC.Udon.Wrapper`, `VRC.Udon.ClientBindings`, `VRC.Udon.UAssembly`, `VRC.Udon.Security`) and the open `UdonBehaviour` / `UdonManager` source. This is the short version.

### A program

An Udon program is four things: a byte array of **bytecode**, a **heap** (an array of slots, one value each), a **symbol table** (names → slot addresses) and an **entry-point table** (event names → bytecode addresses). UdonSharp and the graph compiler both produce this; the SDK serializes it into a `SerializedUdonProgramAsset`, and at scene load each `UdonBehaviour` gets its own copy of the heap.

### The instruction set

There are nine opcodes, all 4-byte aligned:

| Opcode | Operand | Effect |
|---|---|---|
| `NOP` | - | nothing |
| `PUSH` | heap address | push an address on the stack |
| `POP` | - | drop one |
| `COPY` | - | pop dest, pop src, copy the slot value |
| `JUMP` | address | absolute jump |
| `JUMP_IF_FALSE` | address | pop a slot address, jump if the bool there is false |
| `JUMP_INDIRECT` | heap address | jump to the address stored in a slot (how calls return) |
| `EXTERN` | heap address | pop N addresses, call the named function with them |
| `ANNOTATION` | heap address | ignored (debug info) |

That is the whole machine. There is no `ADD`, no `LOAD`, no locals, no registers. Every arithmetic operation, comparison, array index, field read, string operation and Unity call is an `EXTERN`:

```
PUSH a            ; int
PUSH b            ; int
PUSH result       ; int
EXTERN "SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32"
```

### The interpreter

`UdonVM.Interpret()` is a `while` loop with a `switch`. It pre-decodes the byte array into a `uint[]` once, then executes until the program counter passes the end or hits the return sentinel (`0xFFFFFFFF` / `0xFFFFFFFC`). Each branch and each extern also checks a 10-second watchdog.

`EXTERN` is where the time goes. The stock VM:

1. reads the slot named by the operand and type-checks it - the first time it is a `string` (the signature), afterwards a cached delegate object the VM wrote back into the heap;
2. pops `N` addresses off its stack into a `Span<uint>`;
3. invokes the wrapper delegate.

### The heap

Each slot is an `IStrongBox` - a heap-allocated `StrongBox<T>` holding one value. Reading a slot as `T` means a bounds check, a null check and a type check (`is StrongBox<T>`), with a slower "is the boxed value a T" fallback. Writing replaces the box when the type differs. Value types are boxed into `StrongBox<int>` etc.; there is no unboxed storage.

### The wrapper

The "wrapper" is ~640 generated modules, one per exposed .NET/Unity type, each a dictionary from signature string to a method like:

```csharp
void __op_Addition__SystemInt32_SystemInt32__SystemInt32(IUdonHeap heap, Span<uint> p)
{
    int a = heap.GetHeapVariable<int>(p[0]);
    int b = heap.GetHeapVariable<int>(p[1]);
    heap.SetHeapVariable(p[2], a + b);
}
```

Anything that touches a `UnityEngine.Object` also runs the **security filter** - a per-type "safe?" cache plus a blacklist of objects the world may not touch - which is how worlds are sandboxed.

### Why it is slow

Put the pieces together. `a + b` costs:

- three `PUSH`es (stack writes),
- one `EXTERN` dispatch: heap read + type check for the cached delegate, pop slice, delegate invoke,
- inside the wrapper: two typed heap reads (bounds + cast each), one typed write (cast, possible allocation),
- a watchdog read.

Roughly 100-300 ns for something native code does in under a nanosecond - two to three orders of magnitude. The cost is per *operation*, not per *amount of work*: a `string.Split` extern that does real work is cheap relative to the fifty scalar externs around it. Scalar-heavy code (parsing, hashing, loops over arrays, vector math by component) is where Udon falls off a cliff, and every value-type result is a fresh box for the garbage collector.

It is also strictly single-threaded on the main thread, and on Quest the interpreter runs under IL2CPP on a CPU several times slower than a desktop.

## What this VM does differently

Same bytecode, same heap object, same wrapper, same results - only `Interpret()` changes.

**Resolve externs once.** At `LoadProgram` every `EXTERN` site gets its delegate and parameter count resolved and stored in a side array indexed by instruction. No per-call heap lookup, no type check.

**Touch the slot array directly.** The heap's private `IStrongBox[]` is read through a cached field accessor. `COPY`, `JUMP_IF_FALSE` and `JUMP_INDIRECT` read and write boxes directly for the common types and fall back to the heap's own methods otherwise.

**Fused ops.** ~840 externs whose wrapper body is a pure expression over its parameters (integer/float/bool/char operators, `Convert`, `Math`/`Mathf`, `string` basics, typed array get/set/length, `Vector2/3/4`, `Quaternion`, `Color`, frame-stable `Time` getters) are implemented natively, generated from the wrapper's own source so the expression is identical. A fused op runs only when every slot is exactly the expected `StrongBox<T>`; anything else returns `false` and the real wrapper delegate runs, so odd cases keep stock semantics bit for bit.

**Superinstructions.** At load, `PUSH...PUSH EXTERN` runs that end in a fused op are collapsed into one operation with constant addresses - no stack traffic at all. `PUSH cond; JUMP_IF_FALSE` becomes a direct bool test and branch. The original words stay in place, so a jump landing mid-sequence executes the plain path.

**Inline caches for cross-behaviour calls.** `GetProgramVariable`, `SetProgramVariable` and `SendCustomEvent` normally go string → dictionary → slot (or → entry point → another dictionary). Each call site remembers its last target, program and resolved address and reuses them while nothing changed.

**Filtered object ops.** `UnityEngine.Object ==`, `!=`, truthiness and `Utilities.IsValid` run the same security-filter instance the wrapper uses, directly on the slot value.

No new opcodes, no heap layout change. A program that works on the stock VM works here, and a world cannot tell the difference from inside its own bytecode. The one observable deviation is that the slot holding an extern's signature string keeps its string instead of the stock VM's cached delegate object; bytecode cannot read that slot.

## Measured

All numbers from the Udon Profiler in editor play mode on the same machine, same session, stock VM first.

**A real world, 300-frame window of per-frame loop events:**

| | stock | fast | |
|---|---|---|---|
| Udon ms / frame | 2.20 | 1.18 | 1.86× |
| worst frame | 33.9 ms | 9.2 ms | 3.7× |

**A join-time hitch.** One script validated its permission data by re-hashing the full dataset on every query, and on data load a dozen systems queried it at once: 3.4 million externs in a single frame.

| | stock | fast |
|---|---|---|
| that frame | **773 ms** | **145 ms** (5.3×) |

The same frame on Quest would be several times longer again; the fast VM turns a freeze into a stutter, and the profiler shows exactly which twenty lines to cache to remove it entirely.

**Synthetic (best of 20, GC isolated):** arithmetic loop 2.45×, array fill+sum 2.47×, string concatenation 1.11× (allocation-bound), dispatch improvements alone 1.33×.

What does not get faster: externs that do real Unity work (`SetText`, `GetComponent`, physics, networking). Scripts dominated by those see 1.3-1.6×; scripts that parse, hash, compare and loop see 5×.

## Compatibility and verification

The package is only as useful as it is trustworthy, so equivalence is tested, not assumed:

- **Differential tests**: hand-written UAssembly programs (arithmetic, strings, arrays, calls and returns, recursion, stack underflow, unaligned jumps, unsupported externs, externs that throw, uninitialised slots, re-entrancy, the watchdog, debug dumps) run on both VMs; final heaps, return codes and exception types must match.
- **Per-signature equivalence**: every fused op runs against its wrapper delegate on 50 random heaps per signature, including wrong-typed, aliased, uninitialised and out-of-range slots, NaN/±∞/-0 floats and destroyed Unity objects, compared bitwise.
- **Corpus**: every serialized program in the project is loaded twice; each exported entry point whose reachable externs are pure runs on both VMs and the heaps are compared slot by slot.
- **Object ops and inline caches** have their own tests with live, destroyed and null Unity objects and with program swaps between calls.

Run `MagmaVRC.UdonVM.Tests` in the Test Runner (EditMode). A full failure list is written to `Library/udonvm-testresults.txt`.

License: MIT.
