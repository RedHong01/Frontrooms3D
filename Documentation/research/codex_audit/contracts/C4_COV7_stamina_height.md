# Contract C4 · COV-7 · Desktop stamina bar height (Red decides first)

| | |
|---|---|
| For | **Red first**, then 关卡设计 (map chat; `FrontRooms3DGame.cs` is the map chat's file). The touch session confirms the phone layout. |
| Finding | COV-7 (decision), `../20_findings.md` §2.8 |
| File | `Assets/Scripts/FrontRooms3DGame.cs`, line 2006 (stamina segments) |
| Base | main `0a9ee7f` + working tree, 2026-10-14. File sha1 `70bedd38112e23ceb93373bed44500b702edeb01`, unchanged by the working tree. |
| Patch | `C4_COV7_stamina_height.diff`. `git apply --check` passes on today's file. |
| Depends on | nothing |

## What changed, and who

- Before Codex (`7320ed1`, line 1798), each of the 5 stamina segments was **24 × 6 px** on every target.
- Codex's iPhone HUD commit `c1f2d31` (2026-10-04 19:50) made them **24 × 5 px** for every target. On the same line it gated the position on `mobileHud` (−23.5 on phones, −92 on desktop), but not the size.
- `22bb75f` later fixed the other ungated change in that commit (the desktop key panel, V1). This one is still there.
- Nobody asked for a thinner desktop bar. It may be a deliberate phone tweak that leaked to desktop.

## Red's choice

- **(a) Recommended default:** restore 6 px on desktop, keep 5 px on phones (this patch).
- (b) Keep 5 px everywhere (no change; close the finding).

## The change (option a)

```diff
@@ -2003,7 +2003,7 @@ public sealed partial class FrontRooms3DGame : MonoBehaviour
         holdBar.SetActive(false);
         for (var i = 0; i < staminaSegments.Length; i++)
         {
-            staminaSegments[i] = Panel(crosshairObject.transform, "Stamina " + (i + 1), new Vector2(.5f, .5f), new Vector2(-60f + i * 30f, mobileHud ? -23.5f : -92f), new Vector2(24f, 5f), accent).GetComponent<Image>();
+            staminaSegments[i] = Panel(crosshairObject.transform, "Stamina " + (i + 1), new Vector2(.5f, .5f), new Vector2(-60f + i * 30f, mobileHud ? -23.5f : -92f), new Vector2(24f, mobileHud ? 5f : 6f), accent).GetComponent<Image>();
             staminaSegments[i].enabled = false;
         }
```

Apply from the project root, only after Red picks (a): `git apply Documentation/research/codex_audit/contracts/C4_COV7_stamina_height.diff`.

## Verified

- Roslyn compile with macOS and iOS defines, together with C1–C3: 0 errors, 0 warnings.
- Edit-mode suites and a Play run in clone `W/proj_cxreport` with the patch: see `README.md` §2.
- No frame was made: the change is 1 px on a bar that shows only while tired.

## How to test

1. Desktop Play: sprint until the bar shows. Each segment is 24 × 6 px, 92 px below the crosshair.
2. iPhone (or the touch layout in the editor): segments stay 24 × 5 px at −23.5. The autopilot line "AUTOPILOT HUD all panels on screen" (added in `22bb75f`) must still print in every life.
