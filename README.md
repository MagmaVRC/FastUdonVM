# Fast Udon VM

Editor-only drop-in replacement for VRChat's Udon virtual machine. It runs the same bytecode against the same heap and extern wrapper as the stock VM, with lower dispatch cost. Nothing in this package ships in a world; the client always runs the stock VM.

## Install

Add the package folder to `Packages/` (or reference it from your VPM repository). Requires `com.vrchat.worlds` 3.7 or newer and Unity 2022.3.

## Use

- `VRChat SDK > Udon VM > Use Fast VM` - toggles the replacement. Takes effect on the next play-mode entry.
- `VRChat SDK > Udon VM > Fused Ops` - enables native fast paths for pure primitive externs and the superinstruction rewrite. Disable to measure the dispatch-only gain.
- `VRChat SDK > Udon VM > Benchmark` - runs fixed workloads on both VMs and logs the ratio.
- `VRChat SDK > Udon VM > Log Stats` / `Reset Stats` - instruction, extern and fused-op counters for the current play session (counting is off unless `FastUdonVMStats.Enabled` is set).

Pair it with the Udon Profiler (`Window > Udon Profiler`) to compare per-script timings between the two VMs.

## Compatibility

Observable behaviour matches the stock VM: heap state after every instruction, stack-underflow halts, `0xFFFFFFFC` return, unaligned-jump and watchdog exceptions, `DebugLogging` dumps, and extern error wrapping. Fused ops only run when the heap slot types match exactly; otherwise the real wrapper delegate runs. The test suite under `Tests/Editor` verifies this against the stock VM for hand-written programs, for every fused signature on random inputs, and for every serialized program in the project whose entry points are pure.

Known deviation: the heap slot holding an extern signature keeps its `string` instead of the stock VM's cached delegate object. Bytecode cannot read that slot.

## Internals

See `docs/udon-runtime.md` in the project for the runtime reference and `docs/superpowers/plans/2026-08-23-fast-udon-vm.md` for the design, including the superinstruction pass and the experimental JIT mode.
