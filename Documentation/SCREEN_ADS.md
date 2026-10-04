# Diegetic screen ads

## Current slice

The office kit now attaches `FrontRoomsScreenVideo` to every spawned
`Kit_CRTMonitor`. The component creates a small screen-face quad at the
monitor's authored `screen` anchor and feeds it from a keyed
`VideoPlayer -> RenderTexture -> Unlit material` bank. Screens showing the
same ad share one decoder and one playback clock; a second ad source gets its
own bank instead of silently changing every CRT. The first proof asset is
`Assets/StreamingAssets/FrontRooms_Ad_01.mp4`, a 640x434, 30 fps, 12 second
1990 furniture-sale loop. Its 16:9 creative is letterboxed to the authored
CRT face ratio so the period typography does not stretch.

The screen is interactive without baking controls into the video. A center
camera ray can focus the screen and `E` toggles that screen's power state.
The component exposes `IsFocused`, `IsPoweredOn`, and `InteractionPrompt` so a
future world-space control overlay can use the same hit target and state.

## Why this structure

AAA implementations separate the media layer from the interaction layer:

1. A decoder outputs a texture or render target.
2. A world surface applies the texture; CRT glass/scanline treatment stays as
   a separate material pass so it can be tuned without re-encoding the ad.
3. A separate world-space UI or UV hit-region handles hover, focus, and press.
4. Visibility, distance, and screen-area gates decide when the decoder should
   prepare or pause.

This matches Unity's `VideoPlayer` targets (Render Texture or Material
Override), while retaining a single shared Render Texture for this project's
repeated CRTs. It also follows Epic's Media Framework TV example and Widget
Interaction model: the display is a world object, and interaction is a raycast
and state transition rather than a button permanently painted into the movie.

## WebGL constraint

This project ships a WebGL profile. Web builds cannot rely on a `VideoClip`
asset; they need a URL source. The bank therefore resolves the file through
`Application.streamingAssetsPath`, while desktop/editor builds can use a
`VideoClip` or a `Resources/Videos` clip when one is supplied. The first
browser validation still needs a real Chrome WebGL build, because codec support,
HTTP range requests, and hosting headers are runtime facts. A failed prepare
now clears its retry lock and retries after a short delay, but this slice does
not yet include a poster fallback.

## Next implementation layer

For an interactive advertisement, keep the movie as the broadcast base and add
a separate world-space overlay with states such as `idle`, `hover`, `pressed`,
`disabled`, and `reward`. Use a dedicated screen-only collider/layer before
mapping `RaycastHit.textureCoord` to overlay-local coordinates; the current
prototype uses the monitor's shared collision path only for the E power toggle.
For distant CRTs, replace video decoding with a poster or authored flipbook;
reserve live video for the one or two screens that are close enough to read.

Research references:

- Unity VideoPlayer: <https://docs.unity.cn/Manual/class-VideoPlayer.html>
- Unity VideoPlayer target texture: <https://docs.unity.cn/2020.3/Documentation/ScriptReference/Video.VideoPlayer-targetTexture.html>
- Unity WebGL video: <https://docs.unity.cn/Manual/webgl-graphics.html>
- Epic Media Framework: <https://dev.epicgames.com/documentation/unreal-engine/media-framework-in-unreal-engine>
- Epic Media Framework quick start TV: <https://dev.epicgames.com/documentation/en-us/unreal-engine/media-framework-quick-start?application_version=4.27>
- Epic world widgets: <https://dev.epicgames.com/documentation/en-us/unreal-engine/widget-components-in-unreal-engine>
- Epic widget interaction: <https://dev.epicgames.com/documentation/en-us/unreal-engine/umg-widget-interaction-components-in-unreal-engine>
- GDC, *Cutting Apart the Diegetic Interface of Hardspace: Shipbreaker*: <https://gdcvault.com/play/1027158/Cutting-Apart-the-Diegetic-Interface>
