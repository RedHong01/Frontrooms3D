# G14: the RT plugin is in no player build (2026-10-22)

Reported by App Store · 构建 on 2026-10-22. Snapshot 20261021-2221 (unsigned) and the signed Mac TestFlight archive 21 both log "[FrontRoomsGlassRT] off: plugin not loaded: FrontRoomsMetalGlassRT", and `find <app> -iname '*GlassRT*'` finds nothing in either .app. So ray-traced glass has been off in every shipped Mac build, whatever the CPU.

**Cause (游戏视觉, read from main):** `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` is arm64 only (`file`: Mach-O 64-bit arm64), and its importer enables `OSXUniversal` with `CPU: ARM64`. The Mac players are built as universal apps (x86_64 + arm64), so Unity leaves an ARM64-only plugin out of the bundle. Only the Editor (`CPU: ARM64`, OSX) loads it.

**Fix:** RD2 (Red 2026-10-21, "全部按推荐"): build the dylib universal (arm64 + x86_64) and set the importer to `CPU: AnyCPU` for `OSXUniversal`, GUID kept. This is part of workflow `glass-rt-g14-universal` (its Universal stage builds a universal player in a clone and requires the plugin to load in both slices). After it lands, App Store · 构建 checks that the dylib appears in `Contents/PlugIns` of the snapshot and of the Xcode-exported store build, signed by Xcode under the Hardened Runtime + sandbox. The plugin compiles its MSL at runtime through the OS Metal framework (no JIT entitlement needed for that).
