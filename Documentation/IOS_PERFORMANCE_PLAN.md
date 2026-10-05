# iPhone Performance Profile

## Scope

The `iphone-build` branch/worktree is the isolated release line for the iPhone player. Android and desktop rendering remain outside this profile. The source of truth stays in the Unity project; Xcode output is generated from this worktree.

## Baseline findings

The previous iPhone player inherited the desktop URP asset: HDR, 4x MSAA, render scale 1.0, SSAO with depth/normal work, additional-light shadows, a 40 m shadow distance and a 4096 shadow atlas. The runtime map builds a 5x5 chunk window (about 1,600 cells and fixtures) and the non-WebGL fixture path evaluated every lamp every frame. The project has no baked occlusion data for the generated map, so the high-resolution render target and shadow passes are the likely frame-rate limiters.

The existing desktop/WebGL measurement report is used only to order the work: it recorded roughly 2,500–2,900 batches, about 1,000 SetPass calls, and frame-time spikes around 52–60 ms at 1920x1080 with the desktop render path. Xcode `Game Performance`/`Metal System Trace` could not attach during this pass because the connected iPhone was reported offline by `xctrace`, so numerical iPhone GPU counters are still pending.

## iPhone profile in this branch

`FrontRoomsMobilePerformance.Apply()` runs only when `Application.platform == RuntimePlatform.IPhonePlayer` and applies: 

- URP render scale `0.82` with 2x MSAA; HDR and the authored post grade remain enabled.
- SSAO remains enabled at the user's request so wall corners and contact shading stay visible.
- Main shadow distance capped at 24 m and shadow-map resolutions capped at 1024.
- Fixture/ambient light shadows disabled on iPhone; distant fixture updates use the near-only path.
- Map light radius capped at 12 m and shadow radius at 5 m.
- Stream volumetric beams disabled on iPhone.

The profile is runtime-only and leaves the desktop asset values intact. Because SSAO remains enabled, the final iPhone frame time must be checked on the device after the other savings are applied.

## Acceptance sequence

1. Build and install the `iphone-build` Xcode export on the connected iPhone.
2. Record the title screen and a fixed-seed 60-second map route.
3. Capture Xcode GPU Frame Capture or `Game Performance` metrics when the device is visible to `xctrace`: CPU/GPU frame time, draw count, shadow passes, thermal state and memory.
4. Compare the title, Office and Run screens against the previous build. If the authored grade changes beyond the acceptable visual budget, move post-process reductions to a separate A/B commit rather than changing the desktop profile.
