# Off-air bars v3.2: the plate and the live clock's key

From 平面视觉 · 广告, 2026-11-03. Generator: `Tools/screens/offair.py` (imports `broadcast.py`). Spec: `Documentation/AD_TYPE_SYSTEM.md` §3.6. To re-render (writes here):

```
python3 offair.py
```

**Why.** Red's live-clock rule, 2026-10-22 in the 游戏视觉 chat: "目前我不确定这个钟表是否是旧的钟表，如果是旧的我希望新的钟表是可以正常走动，并且呼应现实世界的时间的（钟表只是一点，还有游戏里面的电脑等一系列有时间显示相关的设备的）". The off-air date and clock are therefore live, so no picture may carry them. v3.1's bars master (`FrontRooms_Ad_03/states/bars_clock.png`) was a sample with the call, time and date centred in the CG. v3.2 moves to the layout WFLD 32 aired on 17 Dec 1989 (`~/FrontRoomsVisualWork/reference/ad_reference/frames/A1_wfld_bars_0125*.png`):
- the station's logo is keyed in the upper half;
- the date sits over a running clock, bottom right, in a time-and-date inserter's face, not the CG.

## Files

All at 640 × 480 plus `@2x` at 1280 × 960 (the same signal; double every number below for `@2x`).

| File | What | Ships? |
|---|---|---|
| `bars_plate.png` | SMPTE EG 1-1990 bars, byte for byte v3.1's, with the WTCZ-TV 61 logo keyed: the ID slide's ring and numeral, the call letters, and FAIRVIEW in spaced caps (call letters plus city of licence make it a visual legal ID, as WFLD's "WFLD · CHICAGO" did). No date, no time | yes |
| `clock_glyphs.png` | The inserter's 12 glyphs in the order `0123456789-:`, each a 24 × 29 sprite (cell 20 px + a 2 px margin all round), straight-alpha RGBA, pre-keyed (inserter white 219 + a 1-line black border) | yes |
| `offair_key.json` | Positions, formats, tick, window and the compositing rule, as numbers | yes (or copy its numbers) |
| `preview_sample.png`, `preview_sample@2x.png`, `preview_tube.png` | The plate with a **sample** date and time (`12-01-90`, `03:48:27`), and the same with the review tube stand-in | **no**: review only |

**The face is drawn, not a font.** Nothing was downloaded. It is a 3 × 5 block grid slanted 6°, measured from the WFLD frames: '1' is one right-hand stroke, the colon sits low and left in its cell, and every character takes the same cell, so the date's dashes and the time's colons line up. It reads "12-0 1-90 / 03: 48: 27", as WFLD's read "12- 17-89 / 0 1: 25: 36".

## How it should sit in the game (a request; the wiring is 游戏视觉's)

- **When.** Only between 02:00 and 06:00 local time, on sets showing the station (`AD_TYPE_SYSTEM.md` §3.4, `SCREENS_VISUAL_SYSTEM.md` §5). Outside the window nothing changes: the ad plays.
- **One shared picture.** All sets on the station channel are in sync, so one texture per resolution serves them all. Off-air needs no VideoPlayer, so the decoder can stop. The flipbook and WebGL paths can show the same texture.
- **The date:** `MM-dd-90`, the real month and day in 1990. A real Feb 29 shows `02-28-90`, as `FrontRoomsTerminalContent.GameDate` and `FrontRoomsStationPaper` already do.
- **The time:** `HH:mm:ss`, 24-hour with leading zeros, from `FrontRoomsTerminalContent.Now` (it honours `ClockOverride` for captures). In the window 24-hour and 12-hour read the same.
- **The tick.** Read the clock every frame. Redraw a row only when its string changes, which is once a second for the time. Hard cut: no blink, fade or smoothing.
- **Where.** Each row has 8 fixed cells at a 20 px pitch. Cell `i` of a row puts sprite `glyph` at `(x + 20 i − 2, y − 2)`, top-left origin:

  | Row | x | y | Rectangle to restore before redrawing (x, y, w, h) |
  |---|---|---|---|
  | date | 413 | 364 | 411, 362, 164, 29 |
  | time | 413 | 404 | 411, 402, 164, 29 |

  Ink stays inside RP 27.3 title safe: the clock is at x 412–572, y 363–430; the logo at x 223–418, y 65–247.
- **Composite in sRGB-encoded 8-bit values, never in linear light:** `out = plate × (1 − a) + rgb × a`, with `a = alpha / 255`. Blending in linear light thins the black border and shifts the partial pixels; that is the same fault that lightened the logo's S2 in game (VL419). The simple correct path is a CPU compose on `Color32`, then one `Apply` a second (640 × 480 × 4 bytes = 1.2 MB, once a second).
- **Import.** The plate and the glyph atlas: uncompressed RGBA32 (or RGB24 for the plate), sRGB on, Read/Write on, no mip maps. Compression would smear the 1-line border.
- **Tube.** Nothing of the tube is baked in. The station set's tube shader treats this picture as it treats the ad.

A reference sketch of the compose, at 1×. Unity counts texture rows from the bottom, so top-down row `y` is row `H − 1 − y`:

```csharp
const int W = 640, H = 480, Pitch = 20, Margin = 2, SW = 24, SH = 29, N = 12;   // offair_key.json
const string Order = "0123456789-:";
Color32[] plate, glyphs, frame;                         // plate and frame W x H; glyphs (N * SW) x SH
Texture2D texture;                                      // 640 x 480 RGBA32, sRGB: the one picture every off-air set shows
string shownDate, shownTime;

void Tick(DateTime now)                                 // FrontRoomsTerminalContent.Now, inside 02:00-06:00
{
    var game = new DateTime(1990, now.Month, now.Month == 2 && now.Day == 29 ? 28 : now.Day);
    var date = game.ToString("MM-dd-yy", CultureInfo.InvariantCulture);
    var time = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    var dirty = false;
    if (date != shownDate) { DrawRow(413, 364, date); shownDate = date; dirty = true; }
    if (time != shownTime) { DrawRow(413, 404, time); shownTime = time; dirty = true; }
    if (dirty) { texture.SetPixels32(frame); texture.Apply(false); }
}

void DrawRow(int x0, int y0, string s)
{
    for (var y = y0 - Margin; y < y0 - Margin + SH; y++)                       // restore the row from the plate
        for (var x = x0 - Margin; x < x0 - Margin + 7 * Pitch + SW; x++) frame[(H - 1 - y) * W + x] = plate[(H - 1 - y) * W + x];
    for (var c = 0; c < s.Length; c++)                                         // then key its 8 sprites, left to right
    {
        int g = Order.IndexOf(s[c]), sx = x0 + c * Pitch - Margin, sy = y0 - Margin;
        for (var y = 0; y < SH; y++)
            for (var x = 0; x < SW; x++)
            {
                var p = glyphs[(SH - 1 - y) * (N * SW) + g * SW + x];
                if (p.a == 0) continue;
                var i = (H - 1 - (sy + y)) * W + sx + x;
                int a = p.a, ia = 255 - a;
                var b = frame[i];
                frame[i] = new Color32((byte)((b.r * ia + p.r * a + 127) / 255), (byte)((b.g * ia + p.g * a + 127) / 255),
                                       (byte)((b.b * ia + p.b * a + 127) / 255), 255);
            }
    }
}
```

## Open, not this line's call

- **Sound in the window.** The period answer is a steady 1 kHz tone or silence (§3.5). The station set is also each floor's sound beacon (tutorial T6, 声音设计's S1), so silence would take the beacon away from players at night and a tone would change what it sounds like. That is 声音设计's and 系统设计's to decide.
- **The sign-off pages.** `thought_v3`, `signoff_v3` and `signoff_notice_v3` exist as stills. One way to use them: play them once at the start of the window (about 20 + 15 + 25 s), then bars until 06:00, then back to the ad. That is the screen code's choice; the bars alone are enough.
