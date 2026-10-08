# Codex audit · 10 · Review: glass, materials and look

**Re-review of main HEAD on 2026-10-07.** This replaces the 2026-10-04 version of this file. Every 10-04 finding was re-checked; §3 gives each one's status today.

| | |
|---|---|
| Track | glass + materials + look |
| Bases | pre-Codex `7320ed1`; Codex's last 10-03 commit `75cfdff`; reviewed at main HEAD `a5262fb` (2026-10-07 17:25) |
| Later commits in this track | `663e858` + `1114d6b` (kit importer LOD fix), `9eddc35` + `bf2e883` + `70644f0` (G14 prepass in the glass shader), `04c3a6a` + `b5f381f` (CRT ad screens via the Office kit). All by Codex sessions on 2026-10-04, committed as Red. No other commit after `75cfdff` touches a file in this track (§1, row 1) |
| Reference for "verified" | `research/glass/30_final.md` (§4 promotion list, §5 hook, §9 UNVERIFIED) and `research/glass/20_verification.md` (G10 run 3) |
| Red's project | Read only. Unity was never opened on it. Nothing committed. Files written: this report; `images/cx2_*`; rows VL114–VL115 in `Documentation/VERIFICATION_LOG.md` and the codex-audit legend on the log's cover (VL000) |
| Clones | `W/proj_cx_glass` (shader compile, LOD re-import, probes) and `W/proj_cx_glass2` (Play-mode CRT harness). Both = main's files plus clone-only tools in `Assets/Editor/Audit/CxGlass/`. `W` = `/Users/redwang/FrontRoomsVisualWork` |

---

## 0. Short answer

- **The glass track is still exactly what was verified.** All 24 glass files except the shader, and every `.meta`, are byte-identical to the 10-04 review and to `30_final.md` §4. The shader changed only by G14's three commits.
- **G14's shader merge is safe for everything that is not ray traced.** I compiled `ForwardLit` with `_FR_GLASS_RT` off from main's shader and from the verified shader (`3e4bdc16`). **60 of 60 variants are byte-identical**: 6 keyword sets × vertex and fragment × Metal (macOS), GLES3 (WebGL), Vulkan, D3D11 (Windows) and Metal (iOS). So the props, the WebGL tier and RT-off desktop draw exactly what G10 verified. The new `FRGlassRTPrepass` pass compiles on all five targets with 0 messages, and its material constant buffer matches `ForwardLit` (220 B Metal, same declaration).
- **Codex's 10-03 importer bug (old F1) is fixed by `663e858`.** After a forced re-import of all 113 kits in my clone: window frames, door frames and door leaves never cull; blinds cull at 30 m (60 m Ultra). Rendered at 12.3 m and 30 m, the window frame is drawn (VL114).
- **Old F5 (no prepass pass) is fixed by `9eddc35`.** That moves the risk: G14 is ON at High in Red's editor (`Editor.log` L5904), and window glass is now really traced. Its runtime look was never accepted (F5b, handoff).
- **Red's hand-tuned assets are untouched.** `Prop_Glass` and `Prop_BottleBlue` are unchanged since 10-04 (§2). `FrontRoomsLook.cs` keeps the F5 ambient. `FrontRoomsRenderSetup.cs` is still the P0 port + 2 lines, and its glass steps rewrite the 7 glass/lens materials byte-identically (re-run today). No post, URP, renderer, scene or quality asset changed after `75cfdff`.
- **New, from the Office kit change (`04c3a6a`): the CRT ad screens.** Codex's `04c3a6a` puts a runtime ad quad on every `Kit_CRTMonitor` that `FrontRoomsOfficeKit` spawns (map Office zones). Its material comes from `Shader.Find("Universal Render Pipeline/Unlit")`, and that shader is in none of Red's player builds. So in a build every monitor throws `ArgumentNullException` when it spawns, and no ad shows (F6). In the editor the quad works, but it is not in the monitor's LODGroup: past 11.2 m the monitor culls and the quad floats on alone (F7, VL115).
- **Findings:** F6 MAJOR (fix now), F7 MAJOR (fix now, same file), F5b MAJOR (handoff glass-rt-track), F2 MINOR (stale cubes, still open), F3 DECISION (glass look, still open).

---

## 1. What was checked at HEAD, with the result

| # | Item | Evidence | Result |
|---|---|---|---|
| 1 | Track files changed after `75cfdff` | `git log 75cfdff..HEAD` per path: `FrontRoomsGlass.shader` 3 commits, `FrontRoomsKitImporter.cs` 1, `FrontRoomsOfficeKit.cs` 1 (`04c3a6a`), `Scripts/Media/FrontRoomsScreenVideo.cs` new in `04c3a6a` and changed in `b5f381f` (E key, U flip, video audio off); 0 for `FrontRoomsReflectionBlend.shader`, `Reflections/*`, the 7 glass/lens `.mat`, grime textures, `FrontRoomsLook.cs`, `FrontRoomsRenderSetup.cs`, `FrontRoomsRoomStream.cs`, `Assets/Settings`, `QualitySettings`, `GraphicsSettings`. No uncommitted change and no iCloud copy (`* 2.*`) under these paths | PASS |
| 2 | Hashes today | md5 (first 8): `FrontRoomsGlass.shader` `d3a53c99` (was `3e4bdc16`; only G14 hunks, row 4); `ReflectionBlend` `81dc1692`; cubes `d6acc839 / cc315e5a / c0f1b6e7 / 44d83e56`; manifest `f34a1523`; `ZoneReflection` `42d371db`; Driver `0b3329bb`; `GlassPane` `b2792eef`; `GlassSetup` `c43bf659`; `ReflectionCapture` `ceec26df`; `GlassVerification` `2aa3e10b`; `CompileCheck` `0d1144b1`; `RTStripper` `2e22d16d`; `Glass_Window` `f6285cd6`; `Glass_Edge` `0ffba08c`; `Glass_Shard` `a8ff20b2`; `Glass_ShardClear` `92a4c8ef`; `GlassGrime_M` `5ce543f2`; `GlassSmear_N` `8de9065b`; `Prop_Glass` `0583e4e9`; `Prop_BottleBlue` `85bf5dbb`; `Troffer_Lens_Cool` `1eecb065`; `FrontRoomsPost_Office` `2d46ab21`; `FrontRoomsPost` `c88c9e52`; `FrontRooms_URP` `406dc1aa` | PASS: all equal the 10-04 values |
| 3 | `FrontRoomsLook.cs` | L17–19: Sky (.20, .19, .15), Equator (.26, .24, .17), Ground (.40, .36, .24); Red's comment L12–16 | PASS |
| 4 | Glass shader, keyword off | `CxGlassShaderCheck` (clone): `ForwardLit` of main vs `Hidden/CxAudit/GlassVerified` (= `3e4bdc16`, renamed only), offline `CompileVariant`, SHA1 of the bytes. Sets: none; grime; grime + main/additional shadows + soft + cluster + cookies + fog; grime + additional + soft medium + cluster + fog + instancing; grime + probe blending + box projection + cluster + fog; cascade + soft high + layers + probe atlas + fog. **60 / 60 identical** on Metal macOS, GLES3x WebGL, Vulkan, D3D11 and Metal iOS. Output: `W/proj_cx_glass/Verification/cx_glass/shader_check.txt` | PASS: G10's verified look holds wherever RT is off |
| 5 | Glass shader, RT on and prepass | Same run: `ForwardLit` + `_FR_GLASS_RT` 60 / 60 OK, 0 messages (texture `_FR_GlassRTReflection` bound). `FRGlassRTPrepass` × {none, grime, instancing, both} × 5 targets: 40 / 40 OK, 0 messages. `UnityPerMaterial` 220 B in both passes (Metal, vertex and fragment); the two `CBUFFER` blocks are textually identical, so the SRP Batcher layout rule holds. Import messages: 0 | PASS |
| 6 | WebGL gating | `FrontRoomsGlassRTWebGLStripper.WouldStrip`: prepass pass and hidden prepass shader stripped for WebGL only (`True`), not for StandaloneOSX, Win64 or iOS. `FrontRoomsGlassRTStripper` (keyword) unchanged since 10-04 (`2e22d16d`, returns unless the active target is WebGL) | PASS. Note: iOS ships both RT variant sets unused (no plugin there); a size cost only, not a WebGL-rule breach |
| 7 | RenderSetup glass steps | Clone: `EnsureGlassMaterials` (old URP Lit pass) → `FrontRoomsGlassSetup.EnsureAll` → `SaveAssets`. md5 of `Glass_Window/Edge/Shard/ShardClear`, `Prop_Glass`, `Prop_BottleBlue`, `Troffer_Lens`, `Troffer_Lens_Cool` and the two grime `.meta`: identical before and after (`W/cx_glass/md5_before.txt` = `md5_after.txt`) | PASS |
| 8 | Kit importer LOD fix (`663e858`) | Forced re-import of 113 `Kit_*.fbx` at High (lodBias 1), `GetVersion()` = 2. 14 frames/doors: LOD0→1 4.0 m, cull never. 2 blinds: 3.0 m / 30.0 m (60 m Ultra). 46 kits without sidecar distances keep the 10 % / 2–3 % default (e.g. `Kit_Hutch` 12.2 / 60.8 m). Render of `Kit_WindowFrame_Wood` at FOV 76: drawn at 6.0, 11.7, 12.3 and 30.0 m (centre crop 1,729 and 296 px below luma 40 at 12.3 and 30 m; VL081 had 0 at 30 m). The loop is guarded by `lodDistances.Length >= lods.Length` (`FrontRoomsKitImporter.cs:80`), so a short sidecar cannot index out of range | PASS (VL114) |
| 9 | Red's editor since 10-04 | `~/Library/Logs/Unity/Editor.log`, session 2026-10-05 13:43, 11,333 lines: two Play sessions, both title only (`READY` L5699, L10973). L5904 `[FrontRoomsGlassRT] plugin loaded · caps 0x31ff · on: Apple9, High, kernel 9 ms (async), layout OK`. 0 shader errors, 0 glass/zone/RT exceptions, 0 `Collection was modified`, 0 ZBinningJob. 1,188 `AudioClip.SetData` lines are the sound track's | PASS (no map entered, so map windows and Office CRTs were not exercised) |
| 10 | Load-time side effects | Unchanged since 10-04: `[InitializeOnLoad]` only in `FrontRoomsGlassVerification.cs:31` and `FrontRoomsGlassRTVerify.cs:31`, both gated by their own batch `SessionState` flag | PASS |
| 11 | Material pass queries in batch | Note for future tools: in a batch editor session `Shader.passCount` and `Material.FindPass("FRGlassRTPrepass")` report the FallBack subshader (1 pass, −1) for `FrontRooms/Glass` and also for `URP/Lit`. `ShaderUtil.GetShaderData` sees the real subshader 0 with both passes. This is an editor artefact, not a shader fault | INFO |
| 12 | Red's player builds vs the new CRT ads (`04c3a6a`) | Shader name table in `globalgamemanagers` of the Mac (10-04 17:25) and iOS TestFlight (10-04 20:28) builds; `Assembly-CSharp.dll` / `global-metadata.dat` | FAIL: `Universal Render Pipeline/Unlit` absent from both builds, `Unlit/Texture` maps to a null PPtr (F6) |
| 13 | CRT ad quad vs the monitor's LOD (clone harness `CxCrtMini`) | 6 views × screen on/off; LODGroup readout | FAIL: quad in no LOD, floats past the 11.18 m cull (F7, VL115) |

---

## 2. `Prop_Glass` and `Prop_BottleBlue`

Unchanged since the 10-04 review: no commit after `75cfdff` touches them, md5 `0583e4e9` and `85bf5dbb`, and the glass steps of RenderSetup rewrite them byte-identically (§1 #7). The 10-04 property diff stands:
- **Red's tuned values were not lost.** The old colours and smoothness were RenderSetup's own `GlassDefs` (`FrontRoomsRenderSetup.cs:392-393`). The only hand change in their history is `_SrcBlend` (1 → 5 → 1 by URP validation). The on-screen blend is the same: it was One / OneMinusSrcAlpha, and `FrontRooms/Glass` hard-codes `Blend One OneMinusSrcAlpha` (`FrontRoomsGlass.shader:91`).
- **The new values are the glass track's G4 result:** `FrontRoomsGlassSetup.EnsureAll` (`Prop_Glass` L114-123, `Prop_BottleBlue` L137-142), measured in G10 run 3.
- **G14 does not reach them:** `_RTReceive` 0, so the roll gate and the RT block are skipped (`FrontRoomsGlass.shader:348`, `:486`), and §1 #4 shows the RT-off code is bit-identical.
- **Revert path (if Red picks the old prop look, F3):** restore both files from `7320ed1` and remove their two `Ensure(...)` blocks in `FrontRoomsGlassSetup.cs`.

---

## 3. Findings

### F6 · MAJOR · CONFIRMED · The CRT ad screens cannot create their material in a player build

- **What the code does.** `FrontRoomsOfficeKit.BuildStation` calls `FrontRoomsScreenVideo.Attach(monitorObject)` for every monitor (`FrontRoomsOfficeKit.cs:674-675`; 88 % of stations get one, L662). Map Office zones reach `BuildStation` through `FrontRoomsMapWorld.cs:1824` (reflection call into `FrontRoomsOfficeKit`). `Awake` → `CreateSurface` → `MakeMaterial` (`FrontRoomsScreenVideo.cs:56-62, 163-182, 208-220`). L210: `Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture")`; L211: `new Material(shader)`.
- **The shader is not in Red's builds.** A player's `Shader.Find` only finds shaders that are in the build. No asset in `Assets/`, `Packages/` or `ProjectSettings/` references URP Unlit (guid `650dd9526735d5b46b79224bc6e94025`: 0 files). `GraphicsSettings.asset` Always Included lists only the 7 Unity defaults (L29-36). I parsed the shader name table (name → PPtr) in each build's `globalgamemanagers`:

| Build | Built | `Universal Render Pipeline/Unlit` | `Unlit/Texture` | `Universal Render Pipeline/Lit` (control) |
|---|---|---|---|---|
| `Builds/Mac/FrontRoomsss.app` (Mono) | 2026-10-04 17:25 | absent | name only, PPtr (0, 0) = null | (5, 2) = `sharedassets0.assets` |
| `Builds/Mobile/iOS/FRONTROOMSSS` (TestFlight, IL2CPP) | 2026-10-04 20:28 | absent | (0, 0) | (5, 2) |
| `Builds/Mobile/iOSSimulator/FrontRooms3D`, `iOS/FrontRooms3D` | 10-04 17:03, 17:13 | absent | (0, 0) | (5, 2) |

  `FrontRoomsScreenVideo` is compiled into both newest builds (3 hits in the Mac `Assembly-CSharp.dll`, 3 in the iOS `global-metadata.dat`), and both ship `StreamingAssets/FrontRooms_Ad_01.mp4`. Both were built after `04c3a6a` (13:34) and `b5f381f` (14:04). Nothing added since then references URP Unlit, so the next build is the same.
- **What happens then** (clone probe `CxNullShaderProbe`, `W/proj_cx_glass/Verification/cx_glass/null_shader_probe.txt`): `new Material(null)` throws `System.ArgumentNullException: Value cannot be null. Parameter name: shader`. Unity logs an exception thrown in `Awake` during `AddComponent`, and the caller carries on, so `Dress` still finishes. Each monitor is left with a quad `GameObject` whose fresh `MeshRenderer` has 1 empty material slot (probe: `sharedMaterials.Length = 1`, null, enabled). It gets no ad and no collider, so there is no E prompt. I did not test how a player draws that empty slot (nothing, or the error shader).
- **Effect in Red's game now:** one exception with a stack trace per monitor, each time an Office zone streams in, in every Mac and iOS build (TestFlight builds 2 and 3 included). No ads in any build. The editor is not affected, because there `Shader.Find` finds both shaders (probe: True, True), so Red never sees the bug in Play Mode.
- **Owner:** visual (`Scripts/Media/` belongs to the Office kit dress; Codex-authored). Not a map file.
- **Fix (this audit's Fix phase; all platforms, because it is a bug, not a WebGL optimisation):**
  1. New asset `Assets/Resources/Media/CRT_AdSurface.mat` (+ `.meta`, + `Media.meta`): shader `Universal Render Pipeline/Unlit`, Opaque, `_BaseColor` white, no texture. Make it with Unity in a clone and copy the files over. Being under `Resources/`, it pulls URP Unlit's opaque variants into every build. Do not use Always Included Shaders: that compiles every Unlit variant on every platform.
  2. `MakeMaterial` (L208-220): load the template first, and never construct a material from null:
```csharp
static Material MakeMaterial(Texture texture)
{
    // A player only finds shaders that are in the build; this Resources material keeps URP/Unlit in every build.
    var template = Resources.Load<Material>("Media/CRT_AdSurface");
    var shader = template == null ? Shader.Find("Universal Render Pipeline/Unlit") : null;
    if (template == null && shader == null) return null;
    var material = template != null ? new Material(template) : new Material(shader);
    material.name = "FrontRooms / CRT ad surface";
    // ... texture and colour lines unchanged (L212-219)
}
```
  3. `CreateSurface` (L163): make the material before the quad. If it is null, log one warning per session and return, without creating the quad or the collider: `surfaceMaterial = MakeMaterial(bank != null ? bank.TargetTexture : null); if (surfaceMaterial == null) { /* warn once */ return; }`, then the existing body. `Update` already returns while `surfaceRenderer` is null (L101).
- **Verify the fix:** (a) a clone compile and `CxCrtMini` frames identical to today's at 0.85 / 2.8 / 9.3 m (same shader, so the editor look is unchanged); (b) a Mac development player of a one-monitor scene in a clone: 0 exceptions in `Player.log`, the material reports `Universal Render Pipeline/Unlit`, and the build's `globalgamemanagers` table maps that name to a non-null PPtr; (c) URP Unlit compiles for GLES3, since it is a stock URP shader.

### F7 · MAJOR · CONFIRMED · The CRT ad quad floats on after its monitor culls (VL115)

- **Cause.** `CreateSurface` parents the quad under the monitor (`FrontRoomsScreenVideo.cs:166-176`) but never adds it to the monitor's `LODGroup`, which the kit importer puts on the FBX root. The renderer stays enabled at any distance; `visibleDistance` 18 m (L18, L104) only pauses the video. Past the cull distance the monitor disappears and the quad stays.
- **Measured** (clone `W/proj_cx_glass2`, clone-only harness `CxCrtMini`: Play Mode, Metal, quality High, lodBias 1; one `Kit_CRTMonitor` spawned through `FrontRoomsKitLibrary.Spawn` + `Attach`; F5 ambient; `FrontRoomsPost_Office`; four spots in the V5 Office colour; log `W/proj_cx_glass2/Verification/cx_crt_mini/log.txt`):
  - LODGroup size 0.524 m. LOD0 → LOD1 at 3.35 m; **culled at 11.18 m** at FOV 76. "ad quad inside a LOD: False".
  - Each view was rendered with the screen on, then off (`TogglePower`). Pixels where any channel is more than 8/255 apart: 0.85 m **0**, 2.8 m **0**, 9.3 m **0** (monitor drawn; the empty quad matches the dark glass), **14.3 m 138** and **17.3 m 94**. Every changed pixel at 14 m and 17 m sits in one 14 × 10 px box where the culled monitor stood (x 953–966, y 535–544). The quad draws at RGB ≈ (34, 41, 35) against the wall at (46, 50, 40).
  - The video did not prepare in batch mode (no error logged), so these frames show the quad with an empty render texture. In Red's editor it carries the ad: a lit 640 × 434 picture floating in mid-air over every desk past 11.2 m (Very High's lodBias 1.5 moves the cull to about 16.8 m, Ultra's 2 to about 22.4 m). Past 18 m the paused video leaves its last frame on the quad.
- **Fix (this audit's Fix phase, same file as F6):** at the end of `CreateSurface`, add the quad to every LOD of the monitor's group, so it switches and culls with the monitor:
```csharp
var group = GetComponent<LODGroup>();
if (group != null)
{
    var lods = group.GetLODs();
    for (var i = 0; i < lods.Length; i++)
    {
        var list = new List<Renderer>(lods[i].renderers) { surfaceRenderer };
        lods[i].renderers = list.ToArray();
    }
    group.SetLODs(lods);
}
```
  Verify: re-run `CxCrtMini`. 14.3 m and 17.3 m must change 0 px (on vs off), and the 0.85, 2.8 and 9.3 m frames must match today's.
- **Also seen; not findings; for whoever owns the CRT screen next:**
  - At 0.9 m and 40°, the flat quad sits in front of the bezel's rounded inner corner: 329 px over 8/255 in a 7 px strip (x 1110–1117, y 390–668). A screen drawn through the model's own `Prop_ScreenCRT` slot would keep the curved glass and the recess.
  - The render texture uses point filtering and no mips (L281-289). Small ad type will likely shimmer beyond about 2 m. Not measured, because the video does not play in batch mode.
  - Out of this track: `b5f381f` reads `Keyboard.current.eKey` directly (L141) instead of `FrontRoomsInput.UseDown`, so touch players cannot toggle a screen. This belongs to the input/touch owner.

### F5b · MAJOR · PLAUSIBLE · Handoff glass-rt-track · Traced window reflections are live on desktop Mac without acceptance

- **Old F5 is fixed.** `9eddc35` added the `FRGlassRTPrepass` pass (`FrontRoomsGlass.shader:526-766`), the G-2 fade range (L507-509) and the G-3 roll scale. `bf2e883` only adds `[G14-HOOK]` markers. `70644f0` gates the roll attenuation by the traced coverage (`:342-362`), so pixels without RT data keep today's roll. Shader-level checks pass (§1 #4–6). Main's renderer feature already sets `_FR_GlassRTFade`, `_FR_GlassRTRollScale` and `_FRGlassRTSceneDepth` (`FrontRoomsMetalGlassRTRendererFeature.cs:63-67`).
- **What changed for Red:** before `9eddc35`, G14 traced nothing (VL086: RT High = RT Off within dither). Now every `Glass_Window` slab in the map (`_RTReceive` 1) takes the traced reflection whenever the plugin is ready. Red's editor runs it: `Editor.log` L5904 "on: Apple9, High".
- **Why it is not accepted:**
  - The only run on the merged shader is Codex's own G10 proof at `70644f0` (Codex session `rollout-2026-10-04T09-26-25`, 19:59 UTC). After it patched the harness for Unity 6 keywords it reported 162 / 175. 12 failures were props-isolation checks it called a fixture problem. 1 was `38_deadlamp_window_1.5m` (P5 depth tag), left open. Its outputs were in `/tmp` and are lost.
  - The last RT window frames (pre-merge `proj_rt` shader) failed the design's bars: traced-vs-prepass IoU .734 / .750 / .946 / .951 (bar ≥ .995); 65,034 traced px on an unregistered occluder (bar 0); 463,290 traced px on a broken pane; lamp correlation −.811 (bar ≥ .99). Codex's own review (same session, 19:50 UTC) concluded the window look was not verified.
  - VL087 measured the main-thread cost on a streaming walk: p99 5.66 ms (budget 0.3 ms).
  - `VISUAL_CHAT_TASKS.md` G14 row still says "Metal/runtime acceptance pending" and does not mention `9eddc35`/`70644f0`.
- **Not fixed here.** Turning G14 off by default would change the desktop look, which is Red's call; reverting `9eddc35` would hide the defect, not fix it. This audit's Fix phase makes no change.
- **Owner:** visual. **Handoff:** glass-rt-track: re-run the 175-check proof (fix the props fixture, investigate the P5 dead-lamp case) and the G10 window set (v1–v5, occluder, Relay, broken pane, dead lamp) with RT Off / High / Ultra on today's main; then either accept or default G14 to Off on desktop with Red's sign-off. Update the G14 row.

### F2 · MINOR · CONFIRMED · The four zone cubes are still stale

- Unchanged since 10-04. `capture_manifest.txt` (captured 2026-10-03 11:57) records `FrontRoomsSurface.shader = 079f93ca0983`; today it is `8e4f3ac0cde2` (4 × `_FR_PRINT`). It predates the Level 0 lens ×1.5 (`FrontRoomsMapWorld.cs:3155`, `const float Level0LensEmissionScale = 1.5f`) and N1 V5's cool Office lamps (`FrontRoomsMapWorld.cs:3200-3202`), which Red keeps on (`RED_DECISIONS.md`).
- `WarnIfStale()` still compares only ambient, fog, the Surface shader, the level profile and the print (`FrontRoomsReflectionCapture.cs:189-196, 257-262`), not the lens scale or lamp colours.
- **Fix (visual, glass owner; not this audit's Fix phase):** recapture the 4 cubes in a clone of today's main, run G10's subset check, promote the 4 `.exr` + manifest (keep main's metas), and add the lens scale and the Office lamp colour to `CurrentInputs()`.

### F3 · DECISION · The glass look is live without Red's sign-off

- Unchanged since 10-04 and still open: `30_final.md` says "ready to promote, not signed off". Live now: the glass props and bottle, every map window slab, zone reflections at 0.5 linear, and the Level 0 cube in all five title rooms.
- Measured on 10-04 (VL080): Lobby, Shift, Office and Exit +1.1 to +2.1 mean luma; **Run +2.7 to +5.8**, with its red floor and bench steel picking up Level 0's yellow.
- Red decides per part: keep; drop the title hook (`FrontRoomsRoomStream.cs:351-352`) or skip it for Run; revert the props (§2); windows go with window-landing.

### Resolved since 10-04

| 10-04 finding | Fixed by | Verified here |
|---|---|---|
| F1 · importer culled window frames and doors at 12 m | `663e858` (last imported LOD takes the sidecar's last entry; `GetVersion() => 2`) | §1 #8, VL114 PASS. The forced re-import in Red's editor happens when it first imports with version 2; nothing else is needed |
| F5 · glass shader had no `FRGlassRTPrepass` pass | `9eddc35` (+ `bf2e883`, `70644f0`) | §1 #4–6. The runtime look is F5b |

---

## 4. Handoffs per workflow

| Workflow | What to take from this review |
|---|---|
| this audit's Fix phase (visual) | F6 + F7: both in `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` (visual-owned), plus one new material asset `Resources/Media/CRT_AdSurface.mat`. Exact patches and checks are in §3. Neither changes the editor look at monitor distances under 11.2 m |
| glass-rt-track | F5b: accept or reject the traced window look on today's main; the shader side (prepass, fade, roll gate) is clean |
| visual (glass owner) | F2: recapture the cubes; extend the manifest inputs |
| interactables-kit, window-landing | Nothing new: the importer fix is in main and verified (VL114). Kit FBX merges no longer risk the 12 m cull |
| Red | F3 (glass look per part); nothing for F6/F7: they are bugs in Codex's CRT ads, not look choices |

## 5. Verification images (Figma "FRONTROOMS · VISUAL VERIFICATION LOG", section `2595:6093`)

| VL | Slide | Check | Verdict | Images |
|---|---|---|---|---|
| VL114 | `2771:6093` | Kit frames drawn past 12 m | PASS | `images/cx2_kit_lod_windowframe_drawn.jpg` · `images/cx2_kit_lod_readout.png` |
| VL115 | `2791:7171` | CRT ad quad outlives monitor | FAIL | `images/cx2_crt_14m_on.jpg` · `images/cx2_crt_14m_off.jpg` · `images/cx2_crt_09m_on.jpg` |

Earlier slides from this review: VL080 (`2624:6093`, title rooms reflect Level 0, WAIT-RED) and VL081 (`2624:6105`, kit frames cull at 12 m, FAIL; superseded by VL114).

## 6. Files

- Images: `research/codex_audit/images/cx2_*` (this pass); `cx_glass_title_*`, `cx_kit_lod_*` (10-04).
- Clone-only tools (never merge): `W/proj_cx_glass/Assets/Editor/Audit/CxGlass/` — `CxGlassShaderCheck.cs`, `CxGlassVerified.shader` (verified shader renamed), `CxNullShaderProbe.cs`, `CxKitLodReadout.cs` (recovered from the 10-04 transcript), `CxGlassAfter.cs`; `W/proj_cx_glass2/Assets/Editor/Audit/CxGlass/CxCrtMini.cs` (CRT Play-mode harness; `CxNullShaderProbe` was extended on 10-07 18:5x to read a fresh `MeshRenderer`'s material slots).
- Outputs: `W/proj_cx_glass/Verification/cx_glass/{shader_check.txt, null_shader_probe.txt, lod/}`; `W/proj_cx_glass2/Verification/cx_crt_mini/` (12 frames + `log.txt`); logs in `W/cx_glass/logs/` (`null_probe2.log`, `crt_mini.log`). The earlier full-game CRT run (`W/proj_cx_glass2/Verification/cx_crt/`) timed out at the start room before reaching an Office zone; the small harness replaced it.
- VL115 images (crops ×6 nearest from the 1920 × 1080 frames `08_far_14m_on/off`, `06_far_09m_on`): `research/codex_audit/images/cx2_crt_14m_on.jpg`, `cx2_crt_14m_off.jpg`, `cx2_crt_09m_on.jpg`. Supporting crops for the F7 bezel note, not on a slide because the 7 px overhang barely shows on an empty screen: `cx2_crt_side40_on.jpg`, `cx2_crt_side40_off.jpg` (×6 Lanczos from `02/03_side_0p7m_40deg`).
