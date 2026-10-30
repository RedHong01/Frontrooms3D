# Contract map HK1 (+ KP-1): wiring HUD key B into the game (游戏视觉 → 关卡设计)

**STATUS 2026-10-29 14:49 (关卡设计, sha1s checked in main by 游戏视觉): LANDED.** v2.9 (273e57e7) applied clean, plus O-36 (desktop promptText y −45). FrontRooms3DGame.cs 69687b6a → 0bd2190e, FrontRoomsDoorShot.cs b2014396 → ef941ca9, FrontRoomsDoorShotTests.cs abc598da → 8503b314; Roslyn mac + iOS OK. Their tests: DoorShotTests 247/0 (incl. both KP-1 hand-off rows), InspectPlaytest 134/0, play 2554 PASS, -autopilotDoors 33 ok + 1 note. Open: phone EXAMINE with the key HUD, the touch suites after T-1/T-2, O-36's 591 baseline. All three go to 游戏视觉's live check (wf_1a7b25d9-a17, 20_build.md "Live on main").

游戏视觉, package stage, **2026-10-29 14:2x**. Contract **v2.9**. These are 关卡设计's files: 游戏视觉 changed them only in its clone `W/proj_hudkey` to test them. Land it after `W/apply/hud_key_b.sh` (the visual package). Without that package the diff does not compile.

## Bases (main's CURRENT files, re-read 2026-10-29 14:2x)

| File | sha1 | In the diff |
|---|---|---|
| `Assets/Scripts/FrontRooms3DGame.cs` | `69687b6aea16a0861dfbe5fb78c4af8731f1dd60` | HK1 (19 hunks) + KP-1 (4 more) |
| `Assets/Scripts/FrontRoomsShots/FrontRoomsDoorShot.cs` | `b201439610781e58ef8196e05f78b35988cd0027` | KP-1 (5 hunks) |
| `Assets/Editor/FrontRoomsMap/FrontRoomsDoorShotTests.cs` | `abc598daaecee03cdacc6f6d03d8f001708b1414` | KP-1 (2 hunks, the new test M-7) |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | `a8a83b0bb128861e25319d1af19b1fc0d87d5f51` | **not touched** (see "Describe kinds" below) |
| `Assets/Scripts/FrontRooms3DGame.Mobile.cs` | `6869ef97ef2d653e29f1af5c7bd6e305308bf1f8` | not touched (compiled with the diff) |
| `Assets/Scripts/FrontRoomsShots/FrontRoomsCameraRig.cs` | `bfbdb69b38a18272409be6b13c49bf9cbcc65bd5` | not touched (your 10:51 O-25 fix; the E·1 return at 0.150 s depends on it, so commit it with the rest) |

The clone's `FrontRooms3DGame.cs` equals main 69687b6a + this diff, except that it lacks s27's phone-caption lines (main 13:51). The clone's DoorShot and DoorShotTests equal main + this diff byte for byte (checked 14:2x in `W/tools/_test/hud_key_b_contract_1029pkg/`).

Diff files (identical copies in `contract/` next to this file and in `W/hudkey/contract/`):
- `contract/FrontRooms3DGame.contract.diff` (sha1 273e57e7): HK1 + KP-1, the one you land. It is printed in full at the end of this file.
- `contract/FrontRooms3DGame.HK1.diff` (111e1dd5): HK1 alone (Game.cs only; compiles without KP-1).
- `contract/KP1_handoff.diff` (2185f479): KP-1 on top of HK1.
- What each hunk does: `contract/README.md` (M-1 … M-9); why and what is still open: `contract/40_contract.md`.

## The hunks, by name (each carries `// HK M-n` in the code)

| Name | Part | Where | What |
|---|---|---|---|
| M-1 | HK1 | BuildHud | `keyHud = FrontRoomsKeyHud.Attach(hudCanvas, crosshair, keyPanel, mobileHud)`. The key HUD then draws the dot, the crosshair states and the key panel. |
| M-2 | HK1 | UpdateAim / UpdateHud / SetPhase | The per-frame seam: `aimKind` from `map.Kind` (doors, glass, keys) or from the EXAMINE target (Read / Examine); `SetAim(aimKind, HudIdentity(zone), haveKey, words)`; the map's dot and prompt step aside only while `OwnsDot` / `OwnsPrompt`; `SetHeldKey`; the map's own key panel stays off. |
| M-3 | HK1 | ApplyGameplayHudAlpha | `keyHud.SetHudAlpha(gameplayHudAlpha, rig.HudFade)` and the one (d) line below. |
| M-4 | HK1 | UseDoor | Rattled → `NoKey()`, KeyTurn → `Unlocking()`, Broken → `BrokenTry()`, for the door under the crosshair only. |
| M-5 | KP-1 | DoorShot + Game | The P4 hand-off: `KeyHandoff` at `KeyPickup.HoldUntil`, raised only when someone listens; the 3D key hidden (not destroyed) there; `HandedOff(zone)`; `OnKeyHandoff` → `PickupFlight`. |
| M-6 | HK1 | the autopilot's door scenario | Its key-panel checks read the panel the player sees (`keyHud.PanelShown` / `PanelArrival`). |
| M-7 | KP-1 | DoorShotTests | New test `21 hand-off (KP-1)`. |
| M-8 | HK1 | OnKeyTaken | **KP-1b** (DECISIONS #37, Red 10-29): `keyHud.PickupInstant(HudIdentity(zone), BaseFov)` on the instant pickup: 0.5 s held at the hand position (0.65, 0.30), then the flight into the panel slot (desktop bottom-left (90, 914), phone top-left). |
| M-9 | HK1 | BuildHud | `AddKeyline(promptText, 2f)` → `AddKeyline(promptText, FrontRoomsHudStyle.PromptKeyline(mobileHud))` (G6, DECISIONS #38: 2 px desktop, 1 pt phone). |

## The one ApplyGameplayHudAlpha line for (d) (DECISIONS #13 = (d), #14)

```diff
-        if (keyHudGroup != null) keyHudGroup.alpha = hud * keyPanelFade;
+        if (keyHudGroup != null) keyHudGroup.alpha = (keyHud != null && keyHud.PanelShotExempt ? gameplayHudAlpha : hud) * keyPanelFade;
+        // HK M-3: the key HUD applies the takeover fade itself (DECISIONS #13 (b)/(c), #14 (d)).
+        if (keyHud != null) keyHud.SetHudAlpha(gameplayHudAlpha, rig != null ? rig.HudFade : 0f);
```

`PanelShotExempt` is true from the unlock shot's start to its end. Under it the key panel stays at full alpha and plays U·3 (push, fold, click) on `FrontRoomsDoorShot.Beat` (KeyInsert 0.55 s, KeyTurn 0.68 s, BoltRetract 0.86 s), while the crosshair padlock hides with the shot and returns open at 1.45 s, then drops (F·1 at return + 0.10 s, #25). While the key HUD is attached the map's own panel is off, so the first line matters only without it; the key HUD's own panel takes the exemption through `SetHudAlpha`.

## The P4 hand-off (KP-1, `lock_motion/contract_map_KP1.md`) and the first-key rule (#18)

- **Later keys, a presented key (D1.6's real key; in the editor, the placeholder):** M-5. DoorShot raises `KeyHandoff(zone, viewport, depth, clock)` at `HoldUntil` 0.75 s; the game calls `keyHud.PickupFlight(HudIdentity(zone), viewport, depth, BaseFov, clock)`. The 3D key is hidden at that frame. The pocket beat (its sound) stays at 0.85 s; `Presenting`, `Release` and the destroy stay at 0.95 s. With no listener nothing changes, so your tests at DoorShotTests :1226 / :1307 keep their clock.
- **Shipped play today (instant pickup, no presented key):** M-8 (KP-1b) plays the full P4 from the glyph alone.
- **The first key of a run (#18): not in main yet (O-27).** Main has no EXAMINE K hold on the first key. The key HUD's side is built: `PickupFlight(id, viewport, depth, fov, shotClock, takeOffClock)`. When you land the K hold:
  1. during the hold, set `aimKind = FrontRoomsMapWorld.AimKind.Examine` and pass the words `E · EXAMINE KEY` to `SetAim` (the eye shows; no padlock, no TAKE KEY);
  2. when the hold ends (move, look away or 2.0 s) or on HeldReturned plus the pocket beat, call `keyHud.PickupFlight(HudIdentity(zone), viewport, depth, BaseFov, clock, takeOffClock)` with `takeOffClock` = the presentation clock the take-off was due at; P4 then starts on that frame;
  3. do not raise `KeyHandoff` at 0.75 s for that key (its hand-off is the hold end).
  Captured with the aim override in the clone (VL297, `eye_firstkey`).

## Describe kinds (grammar H1)

**No MapWorld hunk is needed.** Main's MapWorld a8a83b0b already has H1: `AimKind { None, Open, Shut, Locked, LockedHaveKey, Broken, Glass, GlassTap, TakeKey, Read, Examine }` and `Kind(collider, from, out keyZone, out haveKey)` (DECISIONS #20/#21). Read / Examine come from EXAMINE's aim registry in Game.cs (M-2): `inspectTarget.Kind == InspectKind.Object ? AimKind.Examine : AimKind.Read`. The key HUD maps the kinds itself (`FrontRoomsKeyHud.AimOf`): Locked / LockedHaveKey = the padlock, Broken = B·4, Read / Examine = the E·1 eye, Open / Shut / TakeKey = the dot with your prompt, Glass / GlassTap = the dot with your hold ring.

## Checks you can repeat (nothing is written in main)

```sh
# fresh copies of the three bases, then:
patch -p1 --dry-run < contract/FrontRooms3DGame.contract.diff      # combined
patch -p1 --dry-run < contract/FrontRooms3DGame.HK1.diff           # HK1 alone
# or all of it in one go, from 游戏视觉's tools (re-makes the diffs from main's files first):
W/hudkey/contract/regen_contract.sh <tag> v2.9
```

Results (2026-10-29): `patch --dry-run` clean for HK1 alone, combined, and KP-1 on top of HK1 (no fuzz, no offset, 0 rejects; re-checked 14:2x on main's current files); Roslyn `--all` (Unity 6000.6.3f1's compiler, `--ref W/proj_ref66`) COMPILE OK on macOS and iOS for main + payload, + HK1, + HK1 + KP-1 (`W/tools/_rebase/hud_key_b_fix1029_r2/`); in the clone with HK1 + KP-1: DoorShotTests 247/0, InspectPlaytest 134/0, the autopilot's door scenario 33 ok + 1 note (0 fail).

If Game.cs moves before you land it: put its new sha1 in `W/hudkey/contract/apply_contract.py`'s BASES and run `regen_contract.sh` (游戏视觉 runs it; it refuses if an anchor no longer applies).

## The diff (FrontRooms3DGame.contract.diff, v2.9, HK1 + KP-1)

```diff
--- a/Assets/Scripts/FrontRooms3DGame.cs (main 69687b6a)
+++ b/Assets/Scripts/FrontRooms3DGame.cs (HK1 + KP-1 v2.9)
@@ -123,6 +123,11 @@
     GameObject holdBar;
     readonly Image[] staminaSegments = new Image[5];
     GameObject keyPanel;
+    // HK: the visual chat's key HUD (FrontRoomsKeyHud) and what the crosshair is on for it (the map's Kind, grammar H1).
+    FrontRoomsKeyHud keyHud;
+    FrontRoomsMapWorld.AimKind aimKind;
+    GridCoord aimKeyZone;
+    bool aimKeyHeld;
     Image logoImage;
     Image logoLeftImage, logoSlideImage;
     Transform logoMotionRoot;
@@ -902,6 +907,9 @@
         map.KeyTurnOwner = doorShot;
         // It also presents picked-up keys, only when there is a model to show (none in shipped play: pickups stay instant).
         map.KeyPresenter = doorShot.BeginKeyPickup;
+        // HK M-5 (KP-1): with the key HUD attached the pickup hands over to it at HoldUntil (its P4 flight).
+        FrontRoomsDoorShot.KeyHandoff -= OnKeyHandoff;
+        if (keyHud != null) FrontRoomsDoorShot.KeyHandoff += OnKeyHandoff;
 #if UNITY_EDITOR
         // FrontRooms/Map/Play with keys on: this run's map needs keys (the profile asset is untouched). Never for the autopilot.
         if (!autopilot && UnityEditor.SessionState.GetBool(KeysOnSessionKey, false))
@@ -1554,6 +1562,11 @@
     void OnKeyTaken(GridCoord zone)
     {
         keysTaken++;
+        // HK M-8 (KP-1b, DECISIONS #37, Red 2026-10-29): on the INSTANT pickup (nothing presents the key: shipped play
+        // until D1.6's real key) the key HUD's B glyph plays the full lab P4 by itself: 0.5 s held at the hand position
+        // (0.65, 0.30), then into the key panel's slot. A presented key hands over at HoldUntil instead (KP-1, M-5).
+        if (keyHud != null && (doorShot == null || !doorShot.Presenting(zone)))
+            keyHud.PickupInstant(HudIdentity(zone), rig != null ? rig.BaseFov : cam.fieldOfView);
         // The key panel fading in is the pickup cue until keys open doors (audit F1, 1.8).
         Event("key", zone.ToString());
 #if UNITY_EDITOR
@@ -2007,6 +2020,13 @@
         if (map == null) return FrontRoomsMapWorld.UseResult.None;
         var result = map.Use(c);
         if (result == FrontRoomsMapWorld.UseResult.KeyTurn) BeginUnlock(map.DoorOf(c));
+        // HK M-4: only for the door under the crosshair (a touch look-tap can use another door).
+        if (keyHud != null && c == aimed)
+        {
+            if (result == FrontRoomsMapWorld.UseResult.Rattled) keyHud.NoKey();
+            else if (result == FrontRoomsMapWorld.UseResult.KeyTurn) keyHud.Unlocking();
+            else if (result == FrontRoomsMapWorld.UseResult.Broken) keyHud.BrokenTry();
+        }
         return result;
     }
 
@@ -2089,6 +2109,7 @@
         aimed = null;
         prompt = null;
         aimedHold = false;
+        aimKind = FrontRoomsMapWorld.AimKind.None;
         // Gameplay aims from the base eye: shots, shakes and leans are picture only.
         var eye = rig != null ? rig.BaseEye : new Pose(cam.transform.position, cam.transform.rotation);
         var hitPoint = Vector3.zero;
@@ -2102,6 +2123,8 @@
         {
             inspectAimed = inspectTarget;
             prompt = inspectTarget.Prompt;
+            // HK (grammar H1): the aim registry supplies Read / Examine (an object close-up = Examine, a face = Read).
+            aimKind = inspectTarget.Kind == InspectKind.Object ? FrontRoomsMapWorld.AimKind.Examine : FrontRoomsMapWorld.AimKind.Read;
         }
         else if (hitAny)
         {
@@ -2115,6 +2138,11 @@
             if (prompt == null && !FrontRoomsHandheld.Active && map.Kind(hit.collider, playerRoot.position, out _, out _) == FrontRoomsMapWorld.AimKind.Broken)
                 prompt = Time.time < brokenTriedUntil && map.DoorOf(hit.collider) == brokenTried ? WontShutPrompt : BrokenPrompt;
             if (prompt != null) aimed = hit.collider;
+            // HK (grammar H1): the crosshair's kind, consistent with the prompt (a pane beyond 1.2 m has none); a broken
+            // door is aimed so E reaches Use (Broken), with no map prompt.
+            aimKind = map.Kind(hit.collider, playerRoot.position, out aimKeyZone, out aimKeyHeld);
+            if (prompt == null && aimKind != FrontRoomsMapWorld.AimKind.Broken) aimKind = FrontRoomsMapWorld.AimKind.None;
+            if (aimKind == FrontRoomsMapWorld.AimKind.Broken && aimed == null) aimed = hit.collider;
             hitPoint = hit.point;
         }
         // Look+E keys (map.KeyPickupNeedsE, off by default): a key's trigger on the same ray, nearer than anything
@@ -2126,6 +2154,7 @@
             {
                 prompt = map.Describe(key, out aimedHold);
                 aimed = key;
+                aimKind = FrontRoomsMapWorld.AimKind.TakeKey;
             }
         }
         if (previous != null && previous != aimed)
@@ -3122,7 +3151,8 @@
         promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
         // No dark card under the prompt (hud_build DECISIONS #4): paper with the keyline, phones too (#23, O-5), at 2 px
         // (G6, 平面视觉 2026-10-29). The Outline follows the text's alpha, so the close-up hint (HintAlpha) keeps it in step.
-        AddKeyline(promptText, 2f);
+        // HK M-9: the key HUD's line 1 reads the same value (G6: 2 px desktop; phone the 1 pt keyline of #23).
+        AddKeyline(promptText, FrontRoomsHudStyle.PromptKeyline(mobileHud));
         holdBar = Panel(crosshairObject.transform, "Hold bar", new Vector2(.5f, .5f), new Vector2(0f, mobileHud ? -48.5f : -68f), mobileHud ? new Vector2(100f, 3f) : new Vector2(120f, 4f), new Color(1f, 1f, 1f, .25f));
         holdBarFill = Panel(holdBar.transform, "Hold bar fill", new Vector2(0f, .5f), Vector2.zero, new Vector2(0f, mobileHud ? 3f : 4f), accent).GetComponent<Image>();
         holdBar.SetActive(false);
@@ -3143,6 +3173,8 @@
         keyText.text = KeyPanelLabel;
         keyText.horizontalOverflow = HorizontalWrapMode.Overflow;
         keyText.verticalOverflow = VerticalWrapMode.Overflow;
+        // HK M-1: the key HUD draws the dot, the crosshair states and the key panel.
+        keyHud = FrontRoomsKeyHud.Attach(hudCanvas, crosshairObject.GetComponent<RectTransform>(), keyPanel.GetComponent<RectTransform>(), mobileHud);
         gameplayHudAlpha = 0f;
         ApplyGameplayHudAlpha();
 
@@ -3452,7 +3484,7 @@
         // is leaving. This is the only frame in which the title layer and the
         // playing HUD overlap; all title children are non-raycast targets.
         var keepMobileTitleFade = FrontRoomsHandheld.Active && playing && logoFading;
-        overlay.SetActive(!hudPhase || keepMobileTitleFade); if (crosshairImage != null) crosshairImage.enabled = playing;
+        overlay.SetActive(!hudPhase || keepMobileTitleFade); if (crosshairImage != null) crosshairImage.enabled = playing && (keyHud == null || !keyHud.OwnsDot);
         if (overlayImage != null) overlayImage.color = p == Phase.Title || FrontRoomsHandheld.Active
             ? new Color(0f, 0f, 0f, 0f)
             : new Color(.93f, .92f, .88f, .98f);
@@ -3470,7 +3502,7 @@
         }
         if (roomPanel != null) roomPanel.SetActive(hudPhase);
         if (threatPanel != null) threatPanel.SetActive(hudPhase);
-        if (keyPanel != null) keyPanel.SetActive(hudPhase);
+        if (keyPanel != null) keyPanel.SetActive(hudPhase && keyHud == null);
         if (contextPanel != null) contextPanel.SetActive(false);
         ApplyGameplayHudAlpha();
         Cursor.lockState = hudPhase ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !hudPhase;
@@ -3620,6 +3652,11 @@
         else
             gameplayHudAlpha = 0f;
         ApplyGameplayHudAlpha();
+        if (keyHud != null && (!play || !mapPlay || map == null || playerRoot == null))
+        {
+            keyHud.SetAim(FrontRoomsKeyHud.Aim.None, default, false);
+            keyHud.SetHeldKey(false, default);
+        }
         if (!mapPlay || map == null || playerRoot == null) return;
         // In the stream rooms the run starts in: Level 0's lobby, before zone 01.
         var zone = inStartRooms ? new ZoneInfo { height = ZoneHeight.Standard, theme = ZoneTheme.Level0 } : map.ZoneOf(map.CellOf(playerRoot.position));
@@ -3633,15 +3670,22 @@
         var showThreat = hudOn && released && FrontRoomsSettings.RelayReadout;
         threatStateText.text = showThreat ? threat : "";
         distanceText.text = showThreat ? "IT  " + Mathf.RoundToInt(RelayDistance()) + " M" : "";
-        if (crosshairImage != null) crosshairImage.enabled = play;
+        if (crosshairImage != null) crosshairImage.enabled = play && (keyHud == null || !keyHud.OwnsDot);
         // The hint line shows flashes only. M1 (Red's T5, 2026-10-22): the first-run CalmHint card is gone; the
         // controls are on the title and the pause card (the T5 legend; on phones 交互设计's X2 hints).
         var hint = flashTime > 0f && !string.IsNullOrEmpty(flash) ? flash : "";
         contextText.text = play ? hint : "";
+        // HK M-2: what the crosshair is on, its zone identity, whether that key is held, and for READ / EXAMINE the
+        // words as shown (the eye's line; one predicate with the prompt, grammar sec. 1). No string parsing. In a
+        // close-up (Phase.Inspect) the aim is None: the eye is latched in its react / close-up state until
+        // FrontRoomsInspect.Closed (the key HUD hears it itself), and the crosshair is hidden by the shot's fade.
+        // Before the prompt line: OwnsPrompt reads this frame's aim, so the map never shows a state's words itself.
+        if (keyHud != null && play)
+            keyHud.SetAim(aimKind, HudIdentity(aimKeyZone), aimKeyHeld, FrontRoomsKeyHud.WantsWords(aimKind) && prompt != null ? DisplayPrompt(prompt) : null);
         if (promptText != null)
         {
             if (inspecting) promptText.text = DisplayPrompt(FrontRoomsInspect.HintText);
-            else promptText.text = play && prompt != null ? DisplayPrompt(prompt) : "";
+            else promptText.text = play && prompt != null && (keyHud == null || !keyHud.OwnsPrompt) ? DisplayPrompt(prompt) : "";
             var c = promptText.color;
             c.a = inspecting ? FrontRoomsInspect.HintAlpha : 1f;   // the hint's own alpha, not the HUD fade
             promptText.color = c;
@@ -3678,7 +3722,15 @@
             keyLabelSet = true;
             keyText.text = KeyLabel(zone);
         }
-        if (keyPanel != null) keyPanel.SetActive(showKey);
+        // HK M-2: the key HUD owns the panel (and the P4 flight); the map's own panel stays off.
+        if (keyHud != null)
+        {
+            if (keyPanel != null && keyPanel.activeSelf) keyPanel.SetActive(false);
+            // KP-1: from the hand-off the key HUD shows the key (its P4 flight), not only after PocketEnd
+            var handedOff = presentingKey && hudOn && !inStartRooms && map.HasKeyFor(zone.id) && doorShot.HandedOff(zone.id);
+            keyHud.SetHeldKey(showKey || handedOff, HudIdentity(zone.id));
+        }
+        else if (keyPanel != null) keyPanel.SetActive(showKey);
         if (contextPanel != null) contextPanel.SetActive(play && hint.Length > 0);
         UpdateCaptions(play);
     }
@@ -3728,6 +3780,18 @@
     // The key panel's fade-in after a presented pickup (0..1; 1 whenever no key is being presented).
     float keyPanelFade = 1f;
 
+    // HK1: a zone's key identity for the key HUD. DECISIONS #20: FrontRoomsKeyHud.StubIdentity (shape and colour from a
+    // hash) until D1.6 brings the map's rule; then this line calls it instead. The map is not touched for it. The tag's
+    // token is the run's floor (DECISIONS #28/#32/#33: C22's floor 1 = LEVEL 0 = "00", floor 2 = "B1", ...).
+    FrontRoomsKeyHud.Identity HudIdentity(GridCoord zone) => FrontRoomsKeyHud.StubIdentity(zone, floor - 1);
+
+    // KP-1 (M-5): the presented key hands over to the HUD key (DECISIONS #18: the first key's hand-off comes at its
+    // EXAMINE hold end; call PickupFlight(..., clock, takeOffClock) then).
+    void OnKeyHandoff(GridCoord zone, Vector2 viewport, float depth, float clock)
+    {
+        if (keyHud != null) keyHud.PickupFlight(HudIdentity(zone), viewport, depth, rig != null ? rig.BaseFov : cam.fieldOfView, clock);
+    }
+
     /// <summary>
     /// What the key panel calls the key it shows (main's A panel, until KeyHud B): the stair key, on every floor
     /// and for every zone (C22 D13; 叙事设计's string, approved by Red 2026-10-22: "LEVEL 0 KEY" read as "LEVEL O
@@ -3747,7 +3811,11 @@
         if (captionHudGroup != null) captionHudGroup.alpha = gameplayHudAlpha;
         if (crosshairHudGroup != null) crosshairHudGroup.alpha = phase == Phase.Inspect ? gameplayHudAlpha : hud;   // the hint under it keeps its own alpha (C11)
         // The key panel also fades in after a presented pickup (keyPanelFade stays 1 on the instant path).
-        if (keyHudGroup != null) keyHudGroup.alpha = hud * keyPanelFade;
+        // HK1 (DECISIONS #14): in mode (d) the key panel is exempt from the unlock shot's fade. (Under this seam the
+        // map's own panel is off while the key HUD is attached, so this line matters only without it.)
+        if (keyHudGroup != null) keyHudGroup.alpha = (keyHud != null && keyHud.PanelShotExempt ? gameplayHudAlpha : hud) * keyPanelFade;
+        // HK M-3: the key HUD applies the takeover fade itself (DECISIONS #13 (b)/(c), #14 (d)).
+        if (keyHud != null) keyHud.SetHudAlpha(gameplayHudAlpha, rig != null ? rig.HudFade : 0f);
     }
     void Flash(string message, float duration = 3f) { flash = message; flashTime = duration; }
 
@@ -3831,6 +3899,7 @@
         if (phase == Phase.Paused) Paused?.Invoke(false);
         if (map != null && map.KeyTurnOwner == doorShot) map.KeyTurnOwner = null;
         if (map != null && map.KeyPresenter != null && map.KeyPresenter.Target == doorShot) map.KeyPresenter = null;
+        FrontRoomsDoorShot.KeyHandoff -= OnKeyHandoff;
         if (map != null) MapRunEnded?.Invoke();
         OnDestroyMobile();
     }
@@ -4591,6 +4660,10 @@
     int autoKeyPickupsBefore, autoKeyTakenBefore, autoKeyBreaksBefore;
     float autoKeyStartedAt, autoKeyPocketAt, autoKeyGoneAt, autoKeyPanelFirstAlpha, autoKeyMaxHudFade;
     bool autoKeyPanelEarly, autoKeyParentedToMap, autoKeyPanelSeen;
+    // HK M-6: the key panel the player sees: the key HUD's when it is attached (the map's own panel is off then), else
+    // the map's; and how far it has come in, 0..1 (the key HUD: its P4 flight, below 1 until it lands; the map: its fade).
+    bool AutoKeyPanelUp => keyHud != null ? keyHud.PanelShown : keyPanel != null && keyPanel.activeSelf;
+    float AutoKeyPanelIn => keyHud != null ? keyHud.PanelArrival : keyPanelFade;
     static readonly float[] AutoKeyCaptureAt = { .10f, .25f, .50f, .85f };
     void AutoKeyBeat(ShotKind kind, DoorBeat beat, Vector3 p)
     {
@@ -5030,21 +5103,24 @@
                 if (!presentedCase)
                 {
                     // The frame after contact: the cube gone, the panel up at full alpha (as before step 5).
-                    AutoDoorCheck(autoKey == null && (!inZone || (keyPanel != null && keyPanel.activeSelf && keyPanelFade >= 1f)),
-                        "doors: key (instant): the cube is gone at once and the key panel is up at full alpha" + (inZone ? "" : " (the player stands in another zone: panel not checked)"));
+                    // HK M-6 / M-8: the key HUD's panel is up on the frame after contact too, but it starts its P4 there (KP-1b)
+                    AutoDoorCheck(autoKey == null && (!inZone || (AutoKeyPanelUp && (keyHud != null || AutoKeyPanelIn >= 1f))),
+                        "doors: key (instant): the cube is gone at once and the key panel is up " + (keyHud != null ? "(the key HUD: its P4 from the hand position, KP-1b, " + AutoKeyPanelIn.ToString("0.00") + ")" : "at full alpha") + (inZone ? "" : " (the player stands in another zone: panel not checked)"));
                     AutoDoorNext(5);
                     return;
                 }
                 var presenting = doorShot.Presenting(autoKeyZone);
-                if (presenting && keyPanel != null && keyPanel.activeSelf) autoKeyPanelEarly = true;
+                // HK M-6: with the key HUD the 3D key hides at the hand-off (KP-1), so its panel waits for that, not for PocketEnd
+                var keyInView = presenting && autoKey != null && autoKey.gameObject.activeSelf;
+                if (keyInView && AutoKeyPanelUp) autoKeyPanelEarly = true;
                 if (rig != null) autoKeyMaxHudFade = Mathf.Max(autoKeyMaxHudFade, rig.HudFade + (rig.InShot ? 1f : 0f));
                 while (autoDoorCapture < AutoKeyCaptureAt.Length && (doorShot.KeyClock >= AutoKeyCaptureAt[autoDoorCapture] || !presenting))
                     AutopilotCapture((autoShots < 10 ? "0" : "") + autoShots + "_key_pickup_t" + Mathf.RoundToInt(AutoKeyCaptureAt[autoDoorCapture++] * 100f).ToString("000"));
                 if (!presenting && autoKeyGoneAt < 0f) autoKeyGoneAt = since;
-                if (!presenting && !autoKeyPanelSeen && keyPanel != null && keyPanel.activeSelf)
+                if (!keyInView && !autoKeyPanelSeen && AutoKeyPanelUp)
                 {
                     autoKeyPanelSeen = true;
-                    autoKeyPanelFirstAlpha = keyPanelFade;
+                    autoKeyPanelFirstAlpha = AutoKeyPanelIn;
                 }
                 if (since < 1.6f) return;
                 var keySlack = Mathf.Max(.03f, autoDoorMaxDt);
@@ -5055,8 +5131,8 @@
                     "doors: key (presented): the pocket beat at " + autoKeyPocketAt.ToString("0.000") + " s (" + FrontRoomsShotTimings.KeyPickup.PocketSound.ToString("0.00") + ")");
                 AutoDoorCheck(autoKeyGoneAt >= 0f && Mathf.Abs(autoKeyGoneAt - FrontRoomsShotTimings.KeyPickup.PocketEnd) <= keySlack + autoDoorMaxDt && autoKey == null,
                     "doors: key (presented): gone at " + autoKeyGoneAt.ToString("0.000") + " s (" + FrontRoomsShotTimings.KeyPickup.PocketEnd.ToString("0.00") + "), the object destroyed");
-                AutoDoorCheck(!autoKeyPanelEarly && (!inZone || (autoKeyPanelSeen && autoKeyPanelFirstAlpha < 1f - 1e-3f && keyPanelFade >= 1f)),
-                    "doors: key (presented): the key panel waited for the pocket and faded in (first frame × " + autoKeyPanelFirstAlpha.ToString("0.00") + ", not at once; " + keyPanelFade.ToString("0.00") + " at the end)"
+                AutoDoorCheck(!autoKeyPanelEarly && (!inZone || (autoKeyPanelSeen && autoKeyPanelFirstAlpha < 1f - 1e-3f && AutoKeyPanelIn >= 1f)),
+                    "doors: key (presented): the key panel waited for the pocket (the key HUD: for the hand-off) and came in (faded; the key HUD: its P4 flight) (first frame × " + autoKeyPanelFirstAlpha.ToString("0.00") + ", not at once; " + AutoKeyPanelIn.ToString("0.00") + " at the end)"
                     + (inZone ? "" : " (the player stands in another zone: fade not checked)"));
                 AutoDoorCheck(autoKeyMaxHudFade <= 0f, "doors: key (presented): no rig shot and no HUD fade (" + autoKeyMaxHudFade.ToString("0.00") + ")");
                 AutoDoorEnd();
--- a/Assets/Scripts/FrontRoomsShots/FrontRoomsDoorShot.cs (main b2014396)
+++ b/Assets/Scripts/FrontRoomsShots/FrontRoomsDoorShot.cs (KP-1 v2.9)
@@ -48,6 +48,13 @@
     /// <summary>A beat of a door shot landed this frame: the shot, the beat, and where (the keyhole; the key for KeyPickup's Pocket).</summary>
     public static event Action<ShotKind, DoorBeat, Vector3> Beat;
 
+    /// <summary>HK M-5 (KP-1): at KeyPickup.HoldUntil the presented key hands over to the HUD key: the zone, the held
+    /// spot (viewport), its real depth (after ClampToWorld) and the presentation clock. Only while someone listens (the
+    /// game, with the key HUD attached): the 3D key is hidden that frame and the HUD key flies (P4); the pocket beat and
+    /// the end keep their clock (PocketSound, PocketEnd). With no listener the key drops into the pocket as before.
+    /// DECISIONS #18 (the map's, with EXAMINE): the FIRST key of a run hands over when its EXAMINE K hold ends, not here.</summary>
+    public static event Action<GridCoord, Vector2, float, float> KeyHandoff;
+
     /// <summary>
     /// How much of the unlock shot is on screen this frame, 0..1: its blend weight × the Camera motion
     /// setting × what is left before its return (1.15–1.55 s); 0 when none runs. For the visual chat's
@@ -86,7 +93,7 @@
     Transform key;
     IFrontRoomsKeyRig keyRig;
     GridCoord keyZone;
-    bool presenting, keyArrived, keyPocketed;
+    bool presenting, keyArrived, keyPocketed, keyHandedOff;
     float keyT;
     Vector3 keyFrom, keyFromScale;
     Quaternion keyFromRotation;
@@ -443,6 +450,10 @@
 
     /// <summary>A key of this zone is being presented (from contact to KeyPickup.PocketEnd): the game's key panel waits for it.</summary>
     public bool Presenting(GridCoord zone) => presenting && keyZone == zone;
+
+    /// <summary>HK M-5 (KP-1): this zone's presented key has handed over to the HUD key (HoldUntil to PocketEnd): it is
+    /// hidden and the HUD key flies, so the key panel need not wait for PocketEnd.</summary>
+    public bool HandedOff(GridCoord zone) => presenting && keyHandedOff && keyZone == zone;
 
     /// <summary>The presented key's clock (s from contact), or −1 when none is presented.</summary>
     public float KeyClock => presenting ? keyT : -1f;
@@ -467,7 +478,7 @@
         keyRig = dressed;
         keyZone = zone;
         presenting = true;
-        keyArrived = keyPocketed = false;
+        keyArrived = keyPocketed = keyHandedOff = false;
         keyT = 0f;
         keyFrom = key.position;
         keyFromRotation = key.rotation;
@@ -551,6 +562,17 @@
         }
         keyT += dt;
         if (!keyArrived && keyT + 1e-5f >= KP.Arrive) ArriveKey();
+        // HK M-5 (KP-1): with the key HUD listening, its P4 flight replaces the drop into the pocket: the 3D key hides
+        // at HoldUntil; the pocket beat and the end below keep their clock.
+        if (!keyHandedOff && KeyHandoff != null && keyT + 1e-5f >= KP.HoldUntil)
+        {
+            keyHandedOff = true;
+            var lens = camera.transform;
+            var held = rig.ClampToWorld(lens.position, camera.ViewportToWorldPoint(new Vector3(KP.HeldViewportX, KP.HeldViewportY, KP.HeldDistance)));
+            var depth = Vector3.Dot(held - lens.position, lens.forward);
+            key.gameObject.SetActive(false);
+            KeyHandoff.Invoke(keyZone, new Vector2(KP.HeldViewportX, KP.HeldViewportY), depth, keyT);
+        }
         if (!keyPocketed && keyT + 1e-5f >= KP.PocketSound)
         {
             keyPocketed = true;
--- a/Assets/Editor/FrontRoomsMap/FrontRoomsDoorShotTests.cs (main abc598da)
+++ b/Assets/Editor/FrontRoomsMap/FrontRoomsDoorShotTests.cs (KP-1 v2.9 (M-7))
@@ -1093,6 +1093,7 @@
             KeyPlaceholder(h);
             KeyWall(h, roots);
             KeyDressed(h, roots);
+            KeyHandoffKP1(h, roots);
             KeyAimAssist(h, roots);
             KeyLookE(h);
         }
@@ -1306,6 +1307,73 @@
               && Mathf.Approximately(dressed.swayHz, FrontRoomsShotTimings.KeyPickup.TagSwingHz) && Mathf.Approximately(dressed.swayDecay, FrontRoomsShotTimings.KeyPickup.TagSwingDecay)
               && !releasedEarly && dressed.releases == 1 && Near(goneAt, FrontRoomsShotTimings.KeyPickup.PocketEnd, Dt * 1.01f),
             "21 dressed key: the tag's sway once at " + dressed.swayAt.ToString("0.000") + " s (25 deg, 2.2 Hz, 2.5); Release once at the end, the root destroyed at " + goneAt.ToString("0.000") + " s");
+    }
+
+    // 21. HK M-5 (KP-1, HUD key B): with a KeyHandoff listener the presented key hands over at HoldUntil: one event with
+    // the zone, the held viewport point (.65, .30), its depth and the clock; the 3D key hidden from that frame; the pocket
+    // beat still at 0.85; Presenting and HandedOff until 0.95, then the object destroyed. With no listener nothing changes
+    // (21 placeholder / dressed key above).
+    static void KeyHandoffKP1(H h, List<GameObject> roots)
+    {
+        FrontRoomsMapWorld.ShowPlaceholderKey = true;
+        h.shot.Abort();
+        h.rig.ResetLayers();
+        h.rig.Tick(Dt);
+        h.Clear();
+        var lens = h.camera.transform;
+        var fake = GameObject.CreatePrimitive(PrimitiveType.Cube);
+        UnityEngine.Object.DestroyImmediate(fake.GetComponent<Collider>());
+        fake.hideFlags = HideFlags.DontSave;
+        roots.Add(fake);
+        fake.transform.position = lens.position + lens.forward * .5f - Vector3.up * .5f;
+        fake.transform.localScale = new Vector3(.32f, .12f, .12f);
+        Physics.SyncTransforms();
+        var key = fake.transform;
+        var calls = 0;
+        var at = -1f;
+        var clockAt = -1f;
+        var depthAt = -1f;
+        var viewport = Vector2.zero;
+        var zoneAt = default(GridCoord);
+        Action<GridCoord, Vector2, float, float> listener = (z, v, d, c) => { calls++; at = h.clock; clockAt = c; depthAt = d; viewport = v; zoneAt = z; };
+        var shownBefore = true;
+        var hiddenAfter = true;
+        var handedEarly = false;
+        var handedLast = -1f;
+        var goneAt = -1f;
+        var began = false;
+        FrontRoomsDoorShot.KeyHandoff += listener;
+        try
+        {
+            began = h.shot.BeginKeyPickup(FakeZone, key);
+            RunTo(h, 1.05f, () =>
+            {
+                if (key != null)
+                {
+                    if (calls == 0) { shownBefore &= key.gameObject.activeSelf; handedEarly |= h.shot.HandedOff(FakeZone); }
+                    else hiddenAfter &= !key.gameObject.activeSelf;
+                }
+                if (h.shot.HandedOff(FakeZone)) handedLast = h.clock;
+                if (goneAt < 0f && key == null && !h.shot.Presenting(FakeZone)) goneAt = h.clock;
+            });
+        }
+        finally
+        {
+            FrontRoomsDoorShot.KeyHandoff -= listener;
+            h.shot.Abort();
+        }
+        var hold = FrontRoomsShotTimings.KeyPickup.HoldUntil;
+        Check(began && calls == 1 && Near(at, hold, Dt * 1.01f) && Near(clockAt, hold, Dt * 1.01f) && zoneAt == FakeZone
+              && viewport == new Vector2(FrontRoomsShotTimings.KeyPickup.HeldViewportX, FrontRoomsShotTimings.KeyPickup.HeldViewportY)
+              && depthAt > .05f && depthAt <= FrontRoomsShotTimings.KeyPickup.HeldDistance + 1e-3f,
+            "21 hand-off (KP-1, a KeyHandoff listener): one KeyHandoff at " + at.ToString("0.000") + " s (0.75), clock " + clockAt.ToString("0.000")
+            + ", viewport (" + viewport.x.ToString("0.00") + ", " + viewport.y.ToString("0.00") + "), depth " + depthAt.ToString("0.000") + " m");
+        Check(shownBefore && hiddenAfter && !handedEarly && Near(handedLast, FrontRoomsShotTimings.KeyPickup.PocketEnd - Dt, Dt * 1.51f)
+              && h.pocketAt.Count == 1 && Near(h.pocketAt[0], FrontRoomsShotTimings.KeyPickup.PocketSound, Dt * 1.01f)
+              && Near(goneAt, FrontRoomsShotTimings.KeyPickup.PocketEnd, Dt * 1.01f) && !h.shot.Presenting(FakeZone),
+            "21 hand-off: the 3D key shown until the hand-off and hidden after it; HandedOff until " + handedLast.ToString("0.000")
+            + " s; the pocket beat still at " + (h.pocketAt.Count > 0 ? h.pocketAt[0].ToString("0.000") : "never") + " s (0.85); gone and Presenting false at "
+            + goneAt.ToString("0.000") + " s (0.95)");
     }
 
     // 22. The aim assist: a key 20 deg below the view with the mouse idle draws the pitch 6 deg (x Camera motion) toward it over
```
