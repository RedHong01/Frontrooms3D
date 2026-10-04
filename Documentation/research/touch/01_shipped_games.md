# 01 — How shipped iOS/Android games map first-person controls to touch (touch T1)

Status: RESEARCH, 2026-10-03. Nothing in the game or the project was changed (this file is the only new file). **No media was downloaded**: every image/video below is a URL only; image sizes come from HTTP `HEAD` requests or the wiki/API file listing; video lengths come from the YouTube watch-page text. I did **not** look at any screenshot or video frame, so "what it shows" for media is taken from the page/file name/caption text and is marked where unconfirmed.

Task (relayed brief): research how shipped mobile games map first-person move, look, sprint, interact / hold-interact, pause and menus to touch, so FrontRooms' desktop verbs can be mapped:

| FrontRooms desktop verb (today) | Notes from the brief |
|---|---|
| WASD move, mouse look | first person |
| Shift sprint | 5 s stamina, **loud** — the Relay hears it |
| E tap | open/shut the door under the centre dot |
| E hold (or tap-mode) | break a glass pane, hold bar fills |
| walk into a broken frame | climb through (no button) |
| S | cancels a short scripted camera "shot" |
| Esc / O / R / Space | pause / settings / restart / start from title |
| HUD | zone name top-left, centre dot, prompt line + hold bar + 5 stamina segments under the dot, key glyph bottom-left, hint card bottom-centre, captions |

**Tags** (same house style as `../door_break/01_aaa_doors.md`)

| Tag | Meaning |
|---|---|
| **[DOC]** | Developer/publisher documentation, FAQ, patch notes, store description, official blog. |
| **[CODE]** | Shipped client code (Roblox's own PlayerModule scripts, read from a public mirror of the shipped client). |
| **[STORE]** | Apple App Store / Google Play listing data (read through the public iTunes lookup API or the store page). |
| **[USER]** | App Store / Google Play user reviews (paraphrased; star rating and app version given). |
| **[PRESS]** | A review or article I opened and read. |
| **[WIKI]** | Fan wiki (Fandom / minecraft.wiki). Good for "what", weak for "why". |
| **[SEARCH]** | From a search-engine summary; the page itself was not opened or was blocked. |
| **UNVERIFIED** | Not confirmed from a source I read. |
| **PROPOSAL** | My suggestion for FrontRooms. Not a decision. |

Quotes: reviews and docs are **paraphrased** (copyright); option/setting names are given verbatim as labels. One short direct quote is used (Pocket Gamer, §3).

---

## 0. Headline findings (read this first)

1. **Dark Deception (the primary reference) never shipped a 3D mobile version.** A first-person Dark Deception mobile port was shown on 6 Sep 2019 ([tweet, verified via oEmbed][dd-tweet]) and then shelved when the porting company folded during COVID ([WIKI][dd-wiki]). What did ship on iOS/Android is **Super Dark Deception**, a 2D top-down retro reimagining ([STORE][sdd-as]). So there is no Dark Deception first-person touch scheme to copy; §1.
2. **The closest first-person horror precedent with a praised touch scheme is Alien: Isolation (Feral, iOS + Android, 16 Dec 2021).** Its own FAQ documents: trackpad-style look (drag anywhere on the right half) or virtual right stick; a full layout editor (5 custom slots, drag, pinch-resize, opacity, add/delete, left- or right-handed reset); **"Rapid Tap Assist"** (turns mashing into tap-and-hold); separate gameplay and touch-control vibration toggles ([DOC][ai-faq]). Interaction = look at the object and tap the screen ([PRESS][ai-ap]).
3. **Alien's one big touch failure is exactly FrontRooms' risk:** speed = stick distance and sprint triggers automatically near the stick edge, so a stealthing player who pushes a little too far drops out of crouch into a loud sprint, with no option to turn it off ([PRESS][ai-pg]). FrontRooms' sprint is loud and the Relay hears it → sprint must be a deliberate gesture, not "stick pushed a bit far".
4. **Hold-to-do on touch is shown as a radial fill on/around the touched thing.** Roblox lets phones tap a ProximityPrompt itself, and the docs' sample that recreates the default prompt style draws a circular progress bar over `HoldDuration` and scales the prompt up while held on touch ([DOC][rbx-pp], [DOC code sample][rbx-ppapi]); Minecraft Bedrock breaks a block by holding a finger on it, with a radial timer icon ([WIKI][mc-ctrl]); Granny's gasoline filler appears only when aimed at the tank and is held until full ([WIKI][granny-ctrl]).
5. **Contextual buttons that appear only when relevant are the norm for "use":** DOORS' single interact button changes icon per verb (hand / eye / hide / loot / climb …) ([WIKI][doors-ctrl]); CoD Mobile pops a Weapon-Swap button over a ground weapon and a knife button near an enemy ([DOC][codm-blog]); Minecraft's interact button carries a text label (Feed, Ride, Milk …) ([WIKI][mc-ctrl]).
6. **Doors are often auto-opened on touch** (CoD Mobile BR option, Fortnite "Auto Open Doors", PUBG Mobile auto-door option) ([DOC][codm-blog], [PRESS][fn-ss], [SEARCH][pubg-door]); Super Dark Deception even removed the need to interact with one exit door ([STORE notes][sdd-as]). For FrontRooms, shutting a door on the Relay is a tactical verb, so auto-open can only be an optional assist (PROPOSAL, §10).
7. **"Controller drawn on glass" ports are panned; bespoke touch schemes are praised.** Capcom's RE Village/RE4 and Death Stranding put the whole pad on screen and reviewers say play with a controller ([PRESS][rev-ta], [PRESS][ds-ta]); Assassin's Creed Mirage and Alien get praised for touch built for touch ([PRESS][acm-ta], [PRESS][ai-ta]).
8. **Floating vs fixed stick: ship both.** Roblox's default touch mode is the floating Dynamic Thumbstick ([CODE][rbx-cm]); CoD Mobile floats by default and offers "Fixed Joystick" ([DOC][codm-blog]); The Exit 8's mobile players complain about having to follow a moving stick and ask for it to be anchored ([USER][e8-rss]).

---

## Summary table

Each cell is sourced in its game section; the link in the cell is the main source for that cell.

| Game (platforms, mobile date) | Move | Look | Sprint | Interact / hold | Pause / menu | Layout customise | Gyro | Haptics |
|---|---|---|---|---|---|---|---|---|
| **Dark Deception 3D** (PC/console only) | — mobile port shown 2019, shelved ([WIKI][dd-wiki]) | — | — | — | — | — | — | — |
| **Super Dark Deception** (2D top-down; iOS demo 2022-11-20, full 2024-09-16; Android) | thumbstick bottom-left + strafe button beside it ([USER][sdd-rss]) | n/a (top-down) | n/a ("speed boost" power) | exit door made walk-through in a patch ([STORE][sdd-as]) | pause menu exists ([STORE notes][sdd-as]) | UNVERIFIED | UNVERIFIED | UNVERIFIED |
| **The Exit 8** (iOS 2025-03-27/28, Android Mar 2025) | stick, apparently floating — players ask to anchor it ([USER][e8-rss]) | drag; sensitivity + acceleration settings ([WIKI-guide][e8-game8cam]) | n/a (walk) | n/a (walk / turn back) | UNVERIFIED | UNVERIFIED | UNVERIFIED | UNVERIFIED |
| **Alien: Isolation** (iOS + Android 2021-12-16) | left stick; speed = distance ([PRESS][ai-pg]) | Virtual Trackpad (right half) or Virtual Joystick ([DOC][ai-faq]) | **auto at stick edge**, cancels crouch, can't disable ([PRESS][ai-pg]) | look + tap screen ([PRESS][ai-ap]); Rapid Tap Assist → hold ([DOC][ai-faq]) | pause menu → Options → Controls ([DOC][ai-faq-layout]); Radial Menu Pause option | **full editor**: 5 slots, drag, pinch, opacity, add/delete, L/R reset ([DOC][ai-faq-layout]) | none (reviewer wanted it) ([PRESS][ai-ta]) | 2 toggles: gameplay / touch-input (sprint) ([DOC][ai-faq]) |
| **RE Village / RE4 / RE7** (Capcom; Apple only: 2023-10-30 / 2023-12-20 / 2024-07-02) | on-screen stick (full pad on glass) ([PRESS][rev-imore]) | touch-drag or virtual stick ([USER][rev-rss]) | pad-style button | pad buttons; RE7 adds Auto Fire ([PRESS][re7-ta]) | on-screen | position, opacity, auto-hide time, presets ([PRESS][rev-gsm]) | UNVERIFIED | none on touch found; DualSense haptics wished for ([PRESS][rev-ta]) |
| **Roblox default** (engine) | **Dynamic Thumbstick** (floating) in left zone ([CODE][rbx-cm], [CODE][rbx-dyn]) | drag elsewhere ([WIKI][doors-ctrl]) | per game | ProximityPrompt: tap prompt; hold = circular fill ([CODE/DOC][rbx-ppapi]) | Roblox menu (top bar) UNVERIFIED detail | per game | per game | per game |
| **DOORS** (Roblox; 2022) | Roblox stick ([WIKI][doors-ctrl]) | swipe ([WIKI][doors-ctrl]) | none (crouch button) | **one morphing interact button** (hand/eye/hide/loot/climb…) ([WIKI][doors-ctrl]) | gear top-left ([WIKI][doors-ctrl]) | UNVERIFIED | — | — |
| **Apeirophobia** (Roblox Backrooms; 2022) | Roblox stick | drag | Run button, drains stamina [SEARCH][apeiro-mej] | UNVERIFIED | UNVERIFIED | UNVERIFIED | — | — |
| **Backrooms Descent / Noclip / POOLS** (iOS 2022 / 2022 / 2025) | stick | drag | Noclip: sprint button breaks when camera finger moves ([USER][noclip-rss]) | Noclip: hide under tables ([STORE][noclip-as]) | — | — | — | — |
| **Granny** (iOS 2017-12-12) | joystick ([WIKI][granny-ctrl]) | drag | **none** | Hand button (doors, items); filler appears when aimed, **hold** until full ([WIKI][granny-ctrl]) | Settings button pauses + sensitivity ([WIKI][granny-ctrl]) | none found | — | — |
| **Poppy Playtime Ch.1** (iOS 2022-03-08) | stick | drag | **no sprint button** (players ask) ([USER][poppy-rss]) | two hand buttons; no interact button (players ask) ([USER][poppy-rss]) | UNVERIFIED | none (players ask) | — | — |
| **CoD Mobile** (iOS + Android 2019-10-01) | floating stick; "Fixed Joystick" option ([DOC][codm-blog]) | drag right side ([DOC][codm-blog]) | "Sprint Forward" (hold stick forward) + **Auto-Run** button ([DOC][codm-blog]) | pop-up contextual buttons; BR auto-open doors option ([DOC][codm-blog]) | settings icon ([DOC][codm-blog]) | drag/size/opacity of everything ([DOC][codm-blog]) | yes ([DOC][codm-blog]) | UNVERIFIED |
| **PUBG Mobile** (iOS + Android 2018-03-19) | stick | drag + free-look eye button ([PRESS][pubg-pg]) | **sprint-lock icon**: slide onto it and release ([PRESS][pubg-pg]) | auto loot; auto-door option ([SEARCH][pubg-door]) | gear | drag/size/transparency ([SEARCH][pubg-ac]) | yes ([PRESS][pubg-pg]) | UNVERIFIED |
| **Fortnite** (iOS 2018-04, Android 2018-08; US App Store again 2025-05) | stick | drag | UNVERIFIED | **Auto Open Doors**; Auto Open Containers ON by default on mobile ([PRESS][fn-ss], [DOC][fn-auto]) | — | **HUD Layout Tool** (combat/build layouts) ([PRESS][fn-hud]) | Gyro in "Touch and Motion" ([DOC][fn-gyro]) | UNVERIFIED |
| **Genshin Impact** (3rd person; App Store 2020-09-26) | touchpad; walk/run by finger distance or toggle button ([WIKI][gi-ctrl]) | drag; sensitivity 1–5 ([WIKI][gi-ctrl]) | sprint button: **tap = dash, hold = sprint**, stamina ([WIKI][gi-sprint]) | tap | — | — | aimed shot only ([WIKI][gi-ctrl]) | — |
| **Minecraft Bedrock** (iOS 2011-11-17; Android) | joystick or D-pad modes ([WIKI][mc-ctrl]) | drag anywhere ([WIKI][mc-ctrl]) | stick-top (toggle) or sprint button; D-pad double-tap ([WIKI][mc-ctrl]) | tap / **hold on block with radial timer**; labelled interact button ([WIKI][mc-ctrl]) | menu buttons always top ([WIKI][mc-ctrl]) | location/size/opacity ([WIKI][mc-ctrl]) | — | — |
| **AC Mirage** (Apple only, 2024-06-06) | UNVERIFIED | UNVERIFIED | UNVERIFIED | UNVERIFIED | — | per-context layouts + **red overlap zone** ([PRESS][acm-ta]) | UNVERIFIED | UNVERIFIED |
| **Death Stranding DC** (Apple only, 2024-01-30) | pad on glass ([PRESS][ds-ta]) | drag/stick | pad | pad (trigger handling tweaked) | photo-mode button | customise + presets ([PRESS][ds-ta]) | **yes, with touch** ([PRESS][ds-ta]) | — |

---

## 1. Dark Deception (PRIMARY reference) — and Super Dark Deception

### 1.1 What exists on mobile

- **Dark Deception (3D, first person)** lists Steam, Epic, PlayStation, Xbox and Switch on Glowstick's own site; Super Dark Deception is the one with Google Play and App Store links ([DOC][glow-site]). Glowstick's Google Play developer page lists only Super Dark Deception ([STORE][glow-gp]).
- **The 3D mobile port was announced and abandoned.** Glowstick's account posted a first look of Dark Deception mobile running on a Samsung S8 on 6 Sep 2019 (tweet text verified through Twitter's oEmbed endpoint) ([DOC][dd-tweet]). The wiki history: planned for Halloween 2019 with Chapter 1, then Chapters 2–3 added, an ad-supported model with Yodo1 discussed, then the porting company went bankrupt during COVID and the port was shelved; Glowstick says mobile DD will come only after the PC version is complete ([WIKI][dd-wiki]). A fan-made "Dark Deception Mobile Port" exists on itch.io but is unofficial and currently not downloadable ([itch.io][dd-fanport]).
- **Super Dark Deception (2D top-down)**: iOS full game first released 2024-09-16, now v2.2.5 (2026-09-29), $5.99, 4.45★ from 293 ratings ([STORE][sdd-as]); free "Lite"/demo on iOS since 2022-11-20 ([STORE][sdd-lite]); wiki gives demo dates iOS 2022-11-20 / Android 2022-11-23, full game 2024-09-17 on both, Chapter 2 on mobile 2026-06-11 ([WIKI][sdd-wiki]). Google Play listing updated 2026-09-29 ([STORE][sdd-gp]).

### 1.2 Super Dark Deception touch scheme (what can be confirmed)

- **Move:** on-screen thumbstick bottom-left. Two separate 4★/5★ App Store reviewers (v1.2.4) say the **strafe button sits in the bottom-left corner where the thumbstick is**, which makes toggling strafe awkward mid-chase ([USER][sdd-rss]).
- **Doors:** patch notes for the current version say the Stage 7 exit door collision was fixed to make it easier to pass and that the player **no longer has to interact with that door to go through it** (paraphrase) ([STORE release notes][sdd-as]).
- **Controller:** players use a Backbone controller ([USER][sdd-rss]); release notes fix a bug where touching the screen did not re-engage "Screen Controls" — i.e. on-screen controls hide when a controller is used and come back on touch (inference from the note) ([STORE release notes][sdd-as]).
- **Pause:** a pause menu exists (release note mentions a Load button wrongly appearing in it) ([STORE release notes][sdd-as]).
- Look / sprint / gyro / haptics / layout editor: **UNVERIFIED** (top-down game; no first-person look).
- Praise: one reviewer calls the controls smooth ([USER][sdd-rss]).

### 1.3 Media candidates

| URL | What it shows | Time / size |
|---|---|---|
| https://pbs.twimg.com/media/EJPzdd9U4AIRxL6.jpg | Original tweet image of the **unreleased 3D DD mobile build** (wiki captions it "Monkey Business"); whether touch controls are visible is UNVERIFIED | 79,667 B (HEAD) |
| https://static.wikia.nocookie.net/dark-deception-game/images/4/4c/DD_Mobile_on_Galaxy_S8.jpg | Wiki copy: "On Samsung Galaxy S8" — 3D DD mobile on a phone (2020 upload) | 70,706 B (HEAD); 1200×675 |
| https://static.wikia.nocookie.net/dark-deception-game/images/9/9d/DD_Mobile_on_iPad.jpg | Wiki copy: "On iPad" | 92,566 B; 1080×1080 |
| https://static.wikia.nocookie.net/dark-deception-game/images/d/d0/DD_Mobile_%28Elementary_Evil_1%29.jpg | Wiki copy: 3D DD mobile, Elementary Evil level (2019 upload, 2048×996 = phone aspect, so likely a device capture) | 83,172 B |
| https://twitter.com/DarkDeceptionDD/status/1169904702936367105 | 2019-09-06 announcement: first look at DD mobile on a Samsung S8 (paraphrase) | video (not fetched) |
| https://www.youtube.com/watch?v=zw7YX9CKzEc | TapGameplay, "Super Dark Deception Mobile – Walkthrough Part 1 (iOS, Android)", 2026-06-15 — on-device capture, thumbstick/strafe HUD expected | 17:44; HUD timestamps UNVERIFIED (not viewed) |
| https://www.youtube.com/watch?v=rVsKuzyqtP8 | Marconaeus, SDD demo walkthrough [Android/iOS], 2023-07-30 | 13:12; UNVERIFIED |
| https://www.youtube.com/watch?v=fLgACETtn-8 | Glowstick official "SDD – Chapter 2 Official Trailer 1" (2025-01-18); description lists App Store + Google Play | 2:06; likely PC footage, touch HUD UNVERIFIED |
| https://www.youtube.com/watch?v=o_OLjrfkj5c | SmackNPie, "Dark Deception Mobile Release Date Delay EXPLAINED" (2020-05-11) — fan news on the shelved 3D port | 7:28 |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource221/v4/03/b5/f1/03b5f1a3-763e-b9ab-1c44-adaa5781f012/51c88739-a32c-4b33-a783-142bebdc20ea_SDD_AppStore_Screenshot_Iphone_01.png/406x228bb.png | SDD App Store iPhone screenshot 01 (contents not viewed) | 91,517 B at 406×228 thumb |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource211/v4/84/36/f9/8436f9d0-dbf8-153c-5d44-69d5d7f8b30f/9fb8ab86-4f84-4771-b105-2a82ff854c43_SDD_AppStore_Screenshot_Ipad_01.png/552x414bb.png | SDD App Store iPad screenshot 01 | 163,470 B |

Note on all `mzstatic.com/image/thumb/…/NNNxNNNbb.png` URLs: the last path segment is the thumbnail size Apple's image service renders; it can normally be raised (e.g. `2688x1242bb.png`) for a larger copy — not tested here.

---

## 2. The Exit 8 (mobile)

- **Release:** iOS App Store `releaseDate` 2025-03-27 (US) ([STORE][e8-as]); PLAYISM announced iOS + Google Play availability on 28 Mar 2025 at $3.99 ([DOC][e8-playism]); Game8 lists iOS 2025-03-28 and Android 2025-03-31, both ¥400 ([WIKI-guide][e8-game8]); Inside Games said Android would follow in sequence ([PRESS][e8-ig]). **Android date conflict: Mar 28 vs Mar 31 — UNVERIFIED which is right.** Current iOS rating 3.79★ from 62 ratings ([STORE][e8-as]). Google Play 10K+ downloads ([STORE][e8-gp]).
- **Movement:** an on-screen stick that appears to be **floating**: a 3★ reviewer asks the developer to anchor the controls to a static point so players do not have to chase the control around the screen; a Google Play reviewer says the mobile controls feel like Roblox's ([USER][e8-rss], [USER][e8-gp]). Exact behaviour UNVERIFIED (no developer doc found).
- **Look:** camera settings the Game8 motion-sickness guide lists for the game — camera shake, camera sensitivity, **camera acceleration**, motion blur ([WIKI-guide][e8-game8cam]); that guide is platform-generic, so presence on mobile is likely but UNVERIFIED.
- **Complaints:** several reviewers call the iOS controls hard to use, too sensitive or glitchy, one even after adjusting settings; one recommends a PS5/Xbox controller ([USER][e8-rss]).
- **Controller:** works (players report PS5/Xbox pads) ([USER][e8-rss]); official statement UNVERIFIED.
- Sprint / interact / pause / layout editor / gyro / haptics: **UNVERIFIED** (no developer documentation of the mobile scheme found in English or Japanese sources: [e8-game8], [e8-ig], [e8-famitsu]).

**Media candidates**

| URL | What it shows | Time / size |
|---|---|---|
| https://www.youtube.com/watch?v=BEfRtp5mcSo | TapGameplay "The Exit 8 Mobile – Walkthrough Part 1 – Tutorial & Ending (iOS, Android)", 2025-09-23 — on-device capture | 13:59; HUD timestamps UNVERIFIED |
| https://www.youtube.com/watch?v=6bWhEQVIAZw | GeekyGameplay "Speedrunning The Exit 8 on Mobile", 2025-09-25 | 3:24; UNVERIFIED |
| https://www.youtube.com/shorts/q8uJY9uJINg | PLAYISM official "now available for iOS and Android – Release Trailer #shorts", 2025-03-27 | 0:13; likely no HUD (UNVERIFIED) |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource221/v4/7b/c8/39/7bc839ea-78ea-4f75-bf44-853491637646/2796x1290_ios_Exit8_02.png/320x480bb.jpg | App Store iPhone screenshot 02 (source file is 2796×1290) | 20,108 B thumb |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource211/v4/83/c1/d6/83c1d645-e4f2-e15b-03a4-ec58628bdb1a/2732x2048_ios_Exit8_02.png/360x480bb.jpg | App Store iPad screenshot 02 | 35,844 B thumb |
| https://play-lh.googleusercontent.com/C26dR8IMbFR9JNwgpT-Q0pdrYy-KFd8hxE_Ou_WDdEBCrJNNThfd4eAzMTmV6-GFsh6EDLRWWK-I4XMJjQHb | Google Play screenshot 1 (contents not viewed) | size not checked |

---

## 3. Alien: Isolation (Feral Interactive, iOS + Android, 16 Dec 2021)

**Release / platforms.** Feral announced iOS and Android for 16 Dec 2021, with a fully customisable touch interface, controller support and all seven DLC ([DOC][ai-news]). App Store `releaseDate` 2021-12-16; now free to try (two missions) with one IAP to unlock the rest; 4.48★ from 1,297 ratings ([STORE][ai-as]). Feral's "Out now for iOS & Android" video (2021-12-16) links both stores ([video][yt-ai-out]). Google Play 500K+ downloads ([STORE][ai-gp]). The App Store description advertises full touchscreen control with resizable/repositionable buttons and joysticks, gamepad and iPadOS mouse & keyboard ([STORE][ai-as]).

**Move.** Virtual stick on the left; **speed is set by how far the stick is pushed** ([PRESS][ai-pg]). The default layout is mostly clutter-free: stick left, crouch button right ([PRESS][ai-ap]).

**Look.** Mobile-only option **Aim Input**: *Virtual Trackpad* (drag anywhere on the right-hand side, like a mouse) or *Virtual Joystick* (on-screen right stick) ([DOC][ai-faq]). Feral says each camera method has its own settings such as sensitivity and dead zone ([PRESS interview][ai-gf]). Reviews: Pocket Tactics found turning the camera by dragging the thumb awkward at first because there is no right stick by default ([PRESS][ai-pt]); Pocket Gamer found the default look far too fast and imprecise even at minimum trackpad sensitivity ([PRESS][ai-pg]); one App Store user finds the maximum look speed too slow to spot the alien ([USER][ai-rss]). → Wide sensitivity range needed, both ends.

**Sprint.** Automatic once the stick passes a threshold; Pocket Gamer: "the threshold between walking and sprinting is fairly small" — pushing slightly too far while crouched cancels the crouch into a full sprint the enemies notice, and neither auto-sprint nor the crouch cancel could be switched off ([PRESS][ai-pg]). An App Store 5★ reviewer independently calls the stick-sprint very sensitive ([USER][ai-rss]). A haptic pulse marks the walk→sprint switch on supported phones ([PRESS][ai-pg]); the FAQ lists this as **Virtual Controller Vibration** (certain touch inputs, e.g. sprinting, vibrate) ([DOC][ai-faq]).

**Interact.** Look at an object and tap the screen ([PRESS][ai-ap]). TouchArcade notes some interactions were changed for touch versus controller ([PRESS][ai-ta]); an App Store user praises that the in-game computer terminals become direct touchscreens ([USER][ai-rss]). **Rapid Tap Assist**: when on, minigames that need rapid tapping become tap-and-hold ([DOC][ai-faq]). Aim Assist has three levels (Auto-Aim / On / Off) ([DOC][ai-faq]).

**Pause / menus.** Options are reachable from Main Menu or Pause Menu; **Radial Menu Pause** option pauses the game while the weapon/equipment radial is open ([DOC][ai-faq], [DOC][ai-faq-layout]).

**Layout editor** (Feral FAQ, paraphrased) ([DOC][ai-faq-layout]): Options → Controls → Control Scheme → step to one of **five Custom slots** → Configure. Tap an input to select (green highlight) → drag to move, **pinch to resize**; a down-arrow menu gives fine position/size and **opacity**; trash icon deletes, plus icon adds; save over a preset; **reset to left- or right-hand default**. An App Store user reports a bug where deleted buttons stayed and questions why pause/map can be deleted at all ([USER][ai-rss]).

**Gyro.** None; TouchArcade lists gyro aiming as a wished-for addition ([PRESS][ai-ta]).

**Haptics.** Two toggles: **Gameplay Vibration** (loud noises, damage) and **Virtual Controller Vibration** (touch inputs such as sprint) ([DOC][ai-faq]); Feral mentions haptics on buttons ([PRESS interview][ai-gf]). One 3★ user reports random haptic thumps on iPhone 15 Pro Max even with vibration off ([USER][ai-rss]).

**Controllers / KB&M.** Auto-switches when a pad connects; official: DUALSHOCK 4, DualSense, Xbox One, Xbox Series, Razer Kishi ([DOC][ai-faq-pad]); any iPadOS mouse & keyboard, F1 as pause if Esc is missing ([DOC][ai-faq-kbm]).

**Reception.** TouchArcade 5★ (2021-12-16): touch interface excellent even for players who rarely use touch; ranks it among the best iOS conversions (paraphrase) ([PRESS][ai-ta]). Pocket Tactics 8/10 (2021-12-14) ([PRESS][ai-pt]). Pocket Gamer 3.5/5 (2022-01-24): functional but stealth suffers from sensitivity and auto-sprint ([PRESS][ai-pg]). Android Police (2021-12-16): controls intuitive and stay out of the way ([PRESS][ai-ap]). Feral built a couple of default schemes after studying its own and other popular mobile games and playtesting, because grip differs between people ([PRESS interview][ai-gf]).

**Media candidates**

| URL | What it shows | Time / size |
|---|---|---|
| https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_custom_layout | Feral FAQ: layout editor steps (text; the source of the editor description) | text |
| https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_new_options | Feral FAQ: mobile-only options list | text |
| https://www.youtube.com/watch?v=35Zdm2SgTDM | Zade, "Alien: Isolation for iOS Review", 2021-12-15 — review with iOS footage | 6:22; touch HUD timestamps UNVERIFIED |
| https://www.youtube.com/watch?v=GHTMmJZcOeo | Homo Ludens, "Alien: Isolation mobile – The Torrens" (Android), 2026-01-10 | 22:06; UNVERIFIED whether touch or pad |
| https://www.youtube.com/watch?v=HRh6uBLvfNk | Feral "Out now for iOS & Android", 2021-12-16 | 0:58; likely cinematic (UNVERIFIED) |
| https://www.youtube.com/watch?v=DboBLkA5WRw | Feral "Coming to iOS and Android on December 16", 2021-11-17 (embedded in the TouchArcade review) | 0:58; UNVERIFIED |
| https://www.youtube.com/watch?v=WH7oKSPVzFI | IGN, "Official iOS and Android Release Date Trailer", 2021-11-17 | 0:58 |
| https://cdn.toucharcade.com/wp-content/uploads/2021/12/alien-isolation-mobile-review-iphone-2.png | TouchArcade review image (iPhone) | CDN refused HEAD (403, hotlink-protected); open via the review page |
| https://cdn.toucharcade.com/wp-content/uploads/2021/12/alien-isolation-mobile-review-ipad-2.png | TouchArcade review image (iPad) | 403 on HEAD |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource116/v4/03/34/20/033420a8-2e52-cafc-9fa1-a89ee160480c/856e5aec-89d6-4f4d-9f60-a45cf1a1ef78_rk_U002falienisolation_U002fGameData_U002fAlienIsolation_U002fProduction_U002fArtwork_U002f_U002f_U002fScreenshots_U002fScreenshot_iOS-5.5-in_1_EN.png/406x228bb.png | App Store iPhone screenshot 1 | 169,943 B thumb |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource116/v4/fe/12/19/fe1219cb-1ed9-b4ba-ef6a-28b4b98450ec/b333d87a-d0d7-4b35-b97a-5509be924fad__U002falienisolation_U002fGameData_U002fAlienIsolation_U002fProduction_U002fArtwork_U002f_U002f_U002fScreenshots_U002fScreenshot_iOS-iPad-Pro_1_EN.png/552x414bb.png | App Store iPad Pro screenshot 1 | 368,185 B thumb |

---

## 4. Resident Evil Village / RE4 / RE7 on iPhone & iPad (Capcom)

**Release / platforms.** App Store `releaseDate`: Village 2023-10-30, RE4 2023-12-20, RE7 2024-07-02 (also RE2 2024-12-10, RE3 2025-03-18 in search results) ([STORE][rev-as], [STORE][re4-as], [STORE][re7-as]). All three descriptions say: controller recommended, **keyboard and mouse not supported**, part of the game free with a full-game IAP ([STORE][rev-as]). Apple platforms only; no Android release found (Google Play not exhaustively checked → UNVERIFIED).

**Scheme.** Essentially the console pad drawn on the screen: iMore says every Xbox-controller button is on screen, movable, and gets in the way while fleeing ([PRESS][rev-imore]). GSMArena (2023-11-11): key positions adjustable; **opacity** and **how long the controls take to auto-hide when unused** are adjustable; changes save to **multiple presets** switched from a top-right button; the screen still gets cluttered ([PRESS][rev-gsm]). TouchArcade (2023-10-26): good customisation, but playing on touch is still a poor experience; only Xbox prompts; recommends a Backbone ([PRESS][rev-ta]). App Store users: one finished the game on touch after moving buttons, adjusting opacity/size and switching aim from hold to **toggle**; others say the layout covers the whole screen; one notes the virtual look stick feels laggy but plain touch-drag aiming feels fine ([USER][rev-rss]).

**RE7.** TouchArcade (2024-07-08, 4.5★): an **Auto Fire** option (fires after aiming at an enemy for a set time), less reliance on on-screen buttons for navigation, FOV 70–90; not as good as AC Mirage on touch; Capcom confirmed no KB&M ([PRESS][re7-ta]). GSMArena: fine while walking, hard to aim/fire/move at once; the game recommends a controller at every launch ([PRESS][re7-gsm]).

**RE4.** Touch details only from a search summary (virtual gamepad like Village, clunky with two sticks) → [SEARCH][re4-taptap], UNVERIFIED.

Gyro: UNVERIFIED. Haptics on touch: none found; TouchArcade wants DualSense haptics ([PRESS][rev-ta]).

**Media candidates**

| URL | What it shows | Time / size |
|---|---|---|
| https://cdn.toucharcade.com/wp-content/uploads/2023/10/re-village-touch-control-customization.jpg | **RE Village touch-control customisation screen** (the review text introduces it as the screenshot of the customisation options) | 403 on HEAD (hotlink-protected); view via [review][rev-ta] |
| https://cdn.toucharcade.com/wp-content/uploads/2024/07/re7-iphone-review-controls.jpg | RE7 iPhone review image named "controls" (contents not viewed) | 403 on HEAD |
| https://www.youtube.com/watch?v=Fzyar232PZ8 | Datura Plays, "I Played Resident Evil: Village on iPhone 15 Pro Max", 2023-10-26 | 48:44; touch HUD UNVERIFIED |
| https://www.youtube.com/watch?v=lseDQie5oRU | Digital Foundry, RE Village iPhone 15 Pro vs iPad Pro vs Steam Deck, 2023-11-08 | 15:43; UNVERIFIED |
| https://www.youtube.com/watch?v=UJ5JS3xJHuQ | Capcom USA, "RE Village for iPhone / iPad – Launch Trailer", 2023-10-30 | 1:05; likely cinematic |
| https://www.youtube.com/watch?v=E7UesnsWxss | "Resident Evil 7 – iPhone 15 Pro Performance" (embedded in TouchArcade RE7 review), 2024-07-01 | 45:01; UNVERIFIED |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource126/v4/43/f8/97/43f89730-a1cc-ae23-475e-8815092a1bf1/f70c16dc-b7ff-4d02-93e3-28e1dec19a85_ss001.png/406x228bb.png | RE Village App Store iPhone screenshot 1 | 164,395 B thumb |

---

## 5. Roblox horror on mobile: the Roblox default, DOORS, Apeirophobia

### 5.1 Roblox default touch layout (engine level — applies to every Roblox game unless overridden)

- **Default = floating stick.** Roblox's shipped `ControlModule` maps `TouchMovementMode.Default` to `DynamicThumbstick` (and the legacy DPad/Thumbpad modes also map to it) ([CODE][rbx-cm]). Developer setting `DevTouchMovementMode.UserChoice` is the default and lets players pick ([DOC][rbx-dtm]). Docs: the Dynamic Thumbstick appears where the player touches the lower part of the screen; jump is a separate button ([DOC][rbx-dtm]).
- **Stick numbers from the shipped code** ([CODE][rbx-dyn]): activation frame in landscape = **left 40 % of width × bottom two-thirds of height**; in portrait = full width × bottom 40 %. Dead-zone radius **2 px**, full speed at **20 px** throw (both ×2 when the screen's short side > 500 px); outer ring 74 px (148 px on large screens); the stick graphic fades in/out (0.15 s tween) and its background fades to max alpha 0.35. The classic fixed `Thumbstick` uses a **5 % scaled radial dead zone** and sits bottom-left ([CODE][rbx-thumb]).
- **Jump button** bottom-right: 72 px on small screens (short side ≤ 500 px) or 120 px, inset 64/100 px from the right and 64/112 px from the bottom in the newer layout ([CODE][rbx-jump]).
- **Look:** drag/swipe on the rest of the screen (the DOORS wiki says rotate by swiping) ([WIKI][doors-ctrl]).
- **Layout guidance** (Roblox docs, paraphrased): default controls occupy the bottom-left and bottom-right corners, so keep important info and custom buttons out of those corners; most players use one thumb on the stick and one on jump; reachable zones differ phone vs tablet, so place custom buttons relative to the jump button; during gameplay show only vital info and use proximity prompts to surface buttons when relevant ([DOC][rbx-xplat]).
- **ProximityPrompt = Roblox's hold-to-interact.** On phone/tablet the player can always tap the prompt itself ([DOC][rbx-pp]). The API page's code sample, which the docs describe as close to the default prompt style, shows a tap icon for touch input, scales the prompt to **1.6×** while held on touch (1.33× otherwise), and when `HoldDuration > 0` draws a **circular progress bar** that fills linearly over the hold ([DOC code sample][rbx-ppapi]). Whether the built-in default UI uses exactly these values is UNVERIFIED.

### 5.2 DOORS (LSPLASH)

- **Platforms:** PC, mobile (iOS/Android), Xbox and PlayStation; the Controls mechanic page is dated 10 Aug 2022 ([WIKI][doors-ctrl]). 7.8 billion visits; game page recommends headphones and max graphics ([DOC][doors-api]).
- **Mobile mapping** (wiki): same actions as PC on the Roblox app; move with the Roblox joystick; rotate by swiping; settings via a **gear icon top-left** ([WIKI][doors-ctrl]).
- **One contextual interact button that changes icon by verb** (bottom-right, explained in the wiki's `MobileButtonExplanation.png`): Hand = pick up, unlock a door, open/close containers, levers, puzzles; Eye = inspect; Hide icon = hiding spots; Loot; Coins; Heal; Climb (ladders); Fire; Enter; Revive; etc. ([WIKI][doors-ctrl]).
- **Hold verbs:** PC "Unlock Door" = hold E; on mobile, inventory slot **tap = equip/inspect, hold = drop** ([WIKI][doors-ctrl]). One door type opens by **holding Interact** (the wiki reads it as pushing) ([WIKI][doors-doors]).
- **Doors:** several door types open by the player pushing into them; one opens when the player passes through and closes when they leave ([WIKI][doors-doors]). Whether the standard numbered door needs a button on mobile is not stated in the wiki text I read → UNVERIFIED.
- **Hiding:** the hiding icon appears near a wardrobe/bed; tapping the mobile hiding button plays an enter animation; exit by walking forward or an exit button ([WIKI][doors-hide]).
- **Known mobile bug:** jump had a delay on mobile that caused unfair deaths, fixed 2025-04-10 ([WIKI][doors-ctrl]).

### 5.3 Apeirophobia (Polaroid Studios, Backrooms)

- Roblox page states PC, mobile and Xbox support; inspired by Kane Pixels and the Backrooms wiki; created 2022-07-19; 437 M visits ([DOC][apeiro-api]).
- PC binds per a third-party guide: L-Shift sprint (drains stamina), L-Ctrl crouch, F flashlight, Z camera ([PRESS][apeiro-mej]). That guide's mobile section is empty; a search summary says mobile has Run and Crouch buttons ([SEARCH][apeiro-mej]). **Mobile layout UNVERIFIED.**

**Media candidates**

| URL | What it shows | Time / size |
|---|---|---|
| https://static.wikia.nocookie.net/doors-game/images/b/bd/MobileButtonExplanation.png/revision/latest?cb=20250927053814 | **DOORS wiki diagram explaining the mobile buttons at the bottom right** (caption text) | 2150×1434, 635,929 B per wiki API (CDN served 244,768 B) |
| https://prod.docsiteassets.roblox.com/assets/ui/misc/Touch-Reserved-Zones.png | **Roblox official diagram of touch-reserved zones** (default stick + jump corners) | 954,286 B (HEAD) |
| https://www.youtube.com/watch?v=XCycyp8--30 | Ops Gamer, "DOORS (mobile gameplay) #1", 2022-08-22 | 12:56; HUD timestamps UNVERIFIED |
| https://www.youtube.com/watch?v=2dLWDP73xxQ | TapGameplay, "Roblox Mobile – Doors Retro Mode (iOS, Android)", 2024-04-06 | 15:30; UNVERIFIED |
| https://www.youtube.com/watch?v=s6XeDJr6pqM | "DOORS Floor 2 Pro Mobile Gameplay", 2024-08-31 | 12:38; UNVERIFIED |
| https://www.youtube.com/watch?v=-nSo3AxA094 | TapGameplay, "Roblox Mobile – Apeirophobia (iOS, Android)", 2022-08-17 | 20:46; UNVERIFIED |
| https://www.youtube.com/watch?v=lfgTjmoQSeY | RSTURBOGAMING, "Apeirophobia Levels 0–5 (Android, iOS, PC)", 2022-07-10 | 25:38; platform of capture UNVERIFIED |

---

## 6. Backrooms games on mobile — what exists

- **Escape the Backrooms** (Fancy Games): Windows, PS5, Xbox Series, Switch 2 — **no mobile release** listed ([Wikipedia][etb-wiki]). Sites offering "Escape the Backrooms Android" in search results are SEO spam, not official.
- **The Backrooms 1998**, **Inside the Backrooms**: no official iOS listing found in an App Store search on 2026-10-03 ([STORE search][as-search-br]). An "Inside The Backrooms Together" (AYBERK KORCAN, 2026-07-07, 1 rating) exists but its link to the Steam game is UNVERIFIED (likely unrelated).
- **Backrooms Descent: Horror Game** (Pedro Crispim / Sushi Studios): the most-rated Backrooms title on the US App Store (6,300 ratings, 4.5★), iOS since 2022-05-21; latest notes promise smoother movement and better controls ([STORE][brd-as]). Users: look control hard to control, messed-up default sensitivity, heavy ads ([USER][brd-rss]). Android package `com.SushiStudios.BackroomsDescent` (from a video description) ([video][yt-brd]).
- **Noclip: Backrooms Multiplayer** (iOS 2022-11-06; hide under tables, proximity voice) ([STORE][noclip-as]). A 4★ user: **the sprint button stops whenever they move the camera**, so they die while running — a multi-touch failure; many users ask for a jump button ([USER][noclip-rss]).
- **Backrooms 97 – Retro Descent** (iOS 2023-08-08, 3,382 ratings) ([STORE][b97-as]); a user calls the controls a little weird ([USER][b97-rss]).
- **POOLS** (Tensori; iOS/iPadOS/macOS/visionOS 2025-11-06; also Windows, Linux, PS5, Switch) ([DOC][pools-gd], [Wikipedia][pools-wiki]). Liminal, no monsters ([STORE][pools-as]). Users split: one praises controls that stay out of the way; one finds swipe-only movement nauseating and wants arrow buttons; controller support praised; one wants pinch-zoom on touch ([USER][pools-rss]). Exact scheme UNVERIFIED.

**Media candidates**

| URL | What it shows | Time / size |
|---|---|---|
| https://www.youtube.com/watch?v=WyD1UQ2ZD_E | App Unwrapper, "POOLS: iOS/Android Walkthrough Part 1", 2025-11-10 | 29:13; touch HUD UNVERIFIED |
| https://www.youtube.com/watch?v=8N_S9zEqe4Y | Comblang Gaming, "Backrooms Descent – Gameplay (Android, iOS)", 2024-08-25 | 20:41; UNVERIFIED |
| https://www.youtube.com/watch?v=vE6CWrK8aS4 | "Backrooms Descent – Full Game All Levels (iOS, Android)", 2025-02-19 | 52:18; UNVERIFIED |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource221/v4/d4/c4/20/d4c420ae-5848-a57e-1a5c-4de389434803/16798ffa-0169-4a1f-b429-c084ab458763_BackroomsDescent__0004_BackroomsDescent_Horror_Level_0_Scary.png/406x228bb.png | Backrooms Descent App Store screenshot, Level 0 | 137,489 B thumb |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource211/v4/5d/04/65/5d04655b-41d3-d985-f1e4-2422d3aa1ef2/fcea8477-6733-4dd9-bd9e-2d4d155b4829_ipad13size1.png/552x414bb.png | POOLS App Store iPad screenshot 1 | 372,388 B thumb |

---

## 7. Granny, Poppy Playtime, Hello Neighbor, FNAF

### 7.1 Granny (DVloper, iOS 2017-12-12; 101K ratings) ([STORE][granny-as])
- Mobile buttons (wiki) ([WIKI][granny-ctrl]): **Joystick** walk; **Player icon** crouch/stand; **Hand** = pick up/use item, **open/close a door**, go through the backyard window; **Shoot** appears only when a loaded weapon is held; **Drop** (dropped items make noise that draws Granny); **Settings** pauses until Continue, lets you **change sensitivity at any time**, exit to menu; **Gasoline filler appears only when you aim at the car's tank holding the can — hold until full**, with a fill gauge; hide/unhide arrows and per-spot icons (chest, bed, car); well winch = hold.
- **No sprint** — users suggest adding one ([USER][granny-rss]). Options menu: difficulty, "Darker", extra locks, music, quality, limping toggle ([WIKI][granny-opt]).
- Release notes / Android date: Android release date UNVERIFIED.

### 7.2 Poppy Playtime Chapter 1 (Mob Entertainment; iOS 2022-03-08; Android package `com.MOBGames.PoppyMobileChap1`) ([STORE][poppy-as], [video][yt-poppy])
- PC binds for reference: Shift sprint, Ctrl crouch, E interact, hold click to pull with a hand ([WIKI][poppy-ctrl]).
- Mobile complaints ([USER][poppy-rss]): jump button too far toward the middle to press while moving; **no crouch or sprint button** (crouch happens automatically); **no interact button**; requests to move the left/right hand buttons to matching sides; hard to handle both hand buttons at once.

### 7.3 Hello Neighbor (tinyBuild; iOS 2018-08-05) ([STORE][hn-as])
- Users: the joystick sometimes does not respond; an update lowered the camera so pick-ups needed aiming below the item ([USER][hn-rss]). Scheme details UNVERIFIED.

### 7.4 FNAF
- FNAF 1–4 / Sister Location on iOS are the stationary security-office games (no free roam) ([STORE][fnaf-as]); no free-roam FNAF (e.g. Security Breach) found on the App Store in this search → not a model for first-person movement.

**Media candidates**

| URL | What it shows | Time / size |
|---|---|---|
| https://www.youtube.com/watch?v=dV6CvSUUR7k | Marconaeus, "GRANNY v1.8 Full Game [Android/iOS] – No Commentary", 2022-12-13 | 14:21; HUD timestamps UNVERIFIED |
| https://www.youtube.com/watch?v=Dlproy81azo | "Granny – iOS/iPadOS/Android – Mobile Game Review", 2025-03-31 | 18:52; UNVERIFIED |
| https://www.youtube.com/watch?v=RbahSFwRWQQ | TapGameplay, "Poppy Playtime Mobile – Chapter 1 (iOS, Android)", 2022-03-12 | 32:57; UNVERIFIED |
| https://www.youtube.com/watch?v=_Tb5Gkj5Qow | Mob Entertainment official "Chapter 1 – Mobile Release Trailer", 2022-03-11 | 0:37; UNVERIFIED |
| https://is1-ssl.mzstatic.com/image/thumb/Purple123/v4/5c/32/5d/5c325df3-8bbc-49a4-b12b-5e12d0c7ada7/pr_source.jpg/406x228bb.jpg | Granny App Store screenshot 1 | 28,236 B thumb |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource116/v4/c6/dd/99/c6dd993a-d031-498a-8175-ad2d02412476/4aaabcd3-cea4-43d2-839e-f289179ca5d9_preview1.png/406x228bb.png | Poppy Ch.1 App Store screenshot 1 | 139,389 B thumb |

---

## 8. Mainstream reference layouts

### 8.1 Call of Duty: Mobile (Activision/TiMi; iOS + Android 2019-10-01) ([SEARCH][codm-date], [STORE][codm-as])
Activision's own controls guide (2019-10-09) ([DOC][codm-blog]):
- **Move:** virtual stick; forward/back-pedal/strafe. **"Fixed Joystick"** option locks it in place instead of it moving to where you press on the left side (so the default floats).
- **Look:** drag a finger around the right side of the screen.
- **Sprint:** **"Sprint Forward"** option = sprint while the stick is held forward; **Auto-Run** button = tap and the soldier sprints forward automatically.
- **Contextual buttons:** Weapon Swap pops up when passing over a ground weapon; a big knife button appears when an enemy is close (or Throw Back on a grenade); Grenade Cancel appears after pulling a grenade; Jump becomes **Mantle** when an object is in front.
- **Crouch:** tap = crouch, hold = prone, tap while sprinting = slide.
- **Doors / loot (BR):** option to **open doors automatically when close**; option to auto-loot.
- **Menus:** Settings icon (settings / loadout / exit / resume).
- **Customise:** drag, drop, resize and change **opacity** of everything; sensitivity sliders; **gyroscope** option; 3D Touch aim (press-strength fire); Simple (auto-fire) vs Advanced fire.
- Haptics: UNVERIFIED.

### 8.2 PUBG Mobile (Tencent/Krafton; iOS + Android 2018-03-19) ([Wikipedia][pubg-wiki], [STORE][pubg-as])
- **Sprint lock:** when running at full speed an auto-sprint icon appears; slide the thumb onto it and let go to keep running hands-free ([PRESS][pubg-pg]).
- **Free look:** a small eye button under the map; drag it to look around without changing run direction ([PRESS][pubg-pg]).
- Auto loot; gyroscope aiming; sensitivity and icon repositioning ([PRESS][pubg-pg]); drag/size/transparency editor ([SEARCH][pubg-ac]).
- **Auto-open doors** setting added in update 0.4.0, a door-opening bug fixed in 0.12.0 ([SEARCH][pubg-door]; the GameSpot patch notes page refused the fetch).

### 8.3 Fortnite (Epic)
- iOS April 2018, Android August 2018 ([SEARCH][fn-dates]); back on the US App Store in May 2025 after the Apple dispute ([SEARCH][fn-return]); current US listing date 2025-05-20 ([STORE][fn-as]).
- **HUD Layout Tool** (2018): place the movement control, jump, crouch, aim and inventory where you want; separate combat and build layouts; restore default ([PRESS][fn-hud]); expanded 2020 ("multiple HUDs") ([video][yt-fn2]).
- **Touch and Motion** settings tab: Automatic Actions include **Auto Open Containers, ON by default for mobile players** ([DOC][fn-auto]); **Gyro aiming** toggle lives here too ([DOC][fn-gyro]). A SteelSeries guide recommends **Auto Open Doors** (doors open as you approach, no tap needed) ([PRESS][fn-ss]).
- UEFN (creators) can reposition the default touch controls (Jump, Shoot, Crouch, Reload, Sprint …) per island — experimental ([DOC][fn-dev]).

### 8.4 Genshin Impact (HoYoverse; third person; App Store 2020-09-26) ([STORE][gi-as])
- Mobile: all actions by tapping; **walk vs run decided by finger distance from the touchpad centre**, or an optional small walk/run toggle button; camera sensitivity 1–5; **gyro only for Aimed Shot** ([WIKI][gi-ctrl]).
- **Sprint button: tap = dash (18 stamina), hold = continuous sprint (18 stamina/s)** ([WIKI][gi-sprint]).

### 8.5 Minecraft Bedrock (Mojang; iOS since 2011-11-17) ([STORE][mc-as])
Wiki Touch section ([WIKI][mc-ctrl]):
- Modes: **Joystick & tap to interact**; **Joystick & aim crosshair**; **D-pad & tap to interact** (+ "Split controls" = crosshair aiming). Changelogs 1.21.60 / 1.21.130 refine modes and add a new joystick ([SEARCH][mc-cl]).
- All buttons and the joystick: **location, size, opacity** customisable. Menu buttons always at the **top** of the screen.
- **Sprint:** stick knob pushed to the top = sprint **if "Sprint using the joystick" is ON**, otherwise a sprint button; D-pad: double-tap forward.
- Sneak: **double-tap to toggle**.
- **Break block: hold a finger on the block; break time shown by a 24-segment radial icon**; with crosshair mode, aim and hold anywhere off the HUD.
- **Interact button with a context label** above the hotbar (Feed, Ride, Milk, Shear, Sit, Talk …).
- Long-press a hotbar slot to drop the stack.

### 8.6 Assassin's Creed Mirage (Ubisoft; Apple only, 2024-06-06) ([STORE][acm-as])
- TouchArcade (2024-06-19, 3.5★; headline praises the touch controls): Ubisoft added a **bespoke touch option** instead of a virtual button for every pad input; layouts per **control set** (base, swimming …); button size; a **red zone flags overlapping buttons** while editing; some menu touch targets too small; Backbone prompts supported ([PRESS][acm-ta]).
- Ubisoft Sofia producer: uncountable iterations, internal and external playtests, chose fully customisable controls to cover iPhone and iPad ([PRESS][acm-int]).
- Specific move/look/parkour gestures: UNVERIFIED.

### 8.7 Death Stranding Director's Cut (505 Games / Kojima Productions; Apple only, 2024-01-30) ([STORE][ds-as])
- TouchArcade (2024-01-30): touch customisable with presets but not enjoyable; trigger interaction slightly changed for touch; **gyro works with touch controls**; photo mode has a dedicated touch button; multiple button-prompt sets; partial adaptive-trigger support; recommends a controller; asks for Feral-style touch ([PRESS][ds-ta]).

**Media candidates (8.x)**

| URL | What it shows | Time / size |
|---|---|---|
| https://blog.activision.com/content/dam/atvi/activision/atvi-touchui/blog/callofduty/body/Mobile_Controls_Multiplayer.jpg | **CoD Mobile official numbered HUD diagram (Multiplayer)** — the blog's 23-item breakdown refers to it | 261,080 B (HEAD) |
| https://blog.activision.com/content/dam/atvi/activision/atvi-touchui/blog/callofduty/body/Mobile_Controls_BR.jpg | CoD Mobile HUD diagram (Battle Royale) | 548,362 B |
| https://blog.activision.com/content/dam/atvi/activision/atvi-touchui/blog/callofduty/body/Mobile_Controls_Custom.jpg | CoD Mobile custom-layout editor | 110,393 B |
| https://www.cultofmac.com/wp-content/uploads/2018/05/75E56762-959E-4F12-A37A-317C622DC90D.jpeg | Fortnite HUD Layout Tool (2018 article image) | 33,204 B |
| https://www.cultofmac.com/wp-content/uploads/2018/05/8DE788D9-BCC8-47EA-B429-08D9C87B4970.jpeg | Fortnite HUD Layout Tool (2nd image) | 49,351 B |
| https://www.youtube.com/watch?v=jpCuKzEjv5g | Surfnboy, "NEW HUD Layout Tool in Fortnite Mobile (Multiple HUDs)", 2020-07-21 | 10:17; UNVERIFIED |
| https://minecraft.wiki/images/Customize_Touch_Controls_%28Bedrock%29.png?5800d | **Minecraft touch-control customisation UI** (wiki caption) | 2400×1080, 91,881 B (wiki API) |
| https://minecraft.wiki/images/Ore_UI_-_Settings_Screen_Menu_%22Touch%22_Tab_%28Bedrock%29.png?4ad19 | Minecraft Touch settings tab | 3840×2160, 245,097 B |
| https://minecraft.wiki/images/Touch_Controls_Feed_BE2.png?19028 | Minecraft contextual "Feed" interact button | 764×307, 3,791 B |
| https://www.youtube.com/watch?v=F1RHPgFm_CE | ItzAaronHI, compares Minecraft mobile schemes (non-split, split, crosshair), 2025-12-21 | 8:33; UNVERIFIED |
| https://cdn.toucharcade.com/wp-content/uploads/2024/06/assassins-creed-mirage-iphone-15-pro-review-control-customization-touch.jpg | **AC Mirage touch control customisation** (file name) | 403 on HEAD |
| https://www.youtube.com/watch?v=VjtodH83_I0 | Ubisoft, "AC Mirage – Launch Trailer for iOS", 2024-06-10 | 1:03; UNVERIFIED |
| https://cdn.toucharcade.com/wp-content/uploads/2024/01/death-stranding-directors-cut-iphone-15-pro-review-triggers.jpg | Death Stranding iPhone review image named "triggers" (touch trigger handling) | 403 on HEAD |
| https://www.youtube.com/watch?v=wCCYXENXn7g | Kojima Productions, "DS DC on iPhone, iPad, Mac Introduction Trailer", 2024-01-30 | 1:30; UNVERIFIED |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource221/v4/73/22/c5/7322c584-de7b-a2bb-f332-2f7ab4f88d73/Roach_2688x1242_Final.jpg/320x480bb.jpg | CoD Mobile App Store screenshot 1 | 20,025 B thumb |
| https://is1-ssl.mzstatic.com/image/thumb/PurpleSource221/v4/12/44/11/124411bd-7435-f006-e5b7-6203ada37693/0.jpg/406x228bb.jpg | PUBG Mobile App Store screenshot 1 | 45,526 B thumb |

---

## 9. Patterns that recur

1. **Two-thumb split, look = drag.** Left thumb moves, right thumb drags to look (trackpad style) in CoD Mobile, Alien, Minecraft, Roblox/DOORS, PUBG ([DOC][codm-blog], [DOC][ai-faq], [WIKI][mc-ctrl], [WIKI][doors-ctrl], [PRESS][pubg-pg]). A virtual right stick is an option, not the default (Alien) ([DOC][ai-faq]).
2. **Floating stick is the default; fixed is the option.** Roblox (code), CoD (Fixed Joystick option) ([CODE][rbx-cm], [DOC][codm-blog]); a vocal minority wants it anchored (Exit 8) ([USER][e8-rss]).
3. **Speed from stick throw; sprint is where schemes differ.** (a) edge-of-stick auto-sprint (Alien, CoD "Sprint Forward", Minecraft optional) — bad for stealth ([PRESS][ai-pg]); (b) **sprint lock/latch** (CoD Auto-Run, PUBG lock icon) ([DOC][codm-blog], [PRESS][pubg-pg]); (c) **hold button with stamina** (Genshin: tap dash / hold sprint) ([WIKI][gi-sprint]). Multi-touch fragility is a real bug class (Noclip) ([USER][noclip-rss]).
4. **Context buttons appear only when needed**, often with a verb icon or label (DOORS, Granny, CoD, Minecraft, Roblox prompts) ([WIKI][doors-ctrl], [WIKI][granny-ctrl], [DOC][codm-blog], [WIKI][mc-ctrl], [DOC][rbx-xplat]). Alien goes further: no button — look and tap ([PRESS][ai-ap]).
5. **Hold actions show a radial fill at the touch point** (Roblox ProximityPrompt circle; Minecraft radial break timer; Granny fill gauge) ([CODE/DOC][rbx-ppapi], [WIKI][mc-ctrl], [WIKI][granny-ctrl]). Accessibility converts mashing to holding (Alien Rapid Tap Assist) ([DOC][ai-faq]).
6. **Doors and pickups are automated on touch** in shooters (CoD, Fortnite, PUBG) ([DOC][codm-blog], [PRESS][fn-ss], [SEARCH][pubg-door]); horror games keep doors manual (Granny Hand button, DOORS hand icon) ([WIKI][granny-ctrl], [WIKI][doors-ctrl]).
7. **Layout editors are standard:** drag, pinch/resize, opacity, presets, reset, left/right-hand defaults, overlap warning (Alien, CoD, Fortnite, Minecraft, RE, AC Mirage) ([DOC][ai-faq-layout], [DOC][codm-blog], [PRESS][fn-hud], [WIKI][mc-ctrl], [PRESS][rev-gsm], [PRESS][acm-ta]). Missing editor draws complaints (Poppy) ([USER][poppy-rss]).
8. **"Controller on glass" is panned** (RE, Death Stranding) — **bespoke touch is praised** (Alien, AC Mirage) ([PRESS][rev-ta], [PRESS][ds-ta], [PRESS][ai-ta], [PRESS][acm-ta]).
9. **Controls fade or hide when unused / when a pad is connected** (RE Village auto-hide timing; Roblox stick fade; SDD hides screen controls with a controller) ([PRESS][rev-gsm], [CODE][rbx-dyn], [STORE][sdd-as]).
10. **Pause/menu lives on the top edge** (DOORS gear top-left; Minecraft menu buttons top; CoD settings icon) and **sensitivity is reachable from pause** (Granny, Alien) ([WIKI][doors-ctrl], [WIKI][mc-ctrl], [WIKI][granny-ctrl], [DOC][ai-faq-layout]).
11. **Haptics mark state changes and are toggleable** (Alien: sprint engage; separate gameplay vs input toggles); unwanted haptics are reviewed harshly ([DOC][ai-faq], [USER][ai-rss]).
12. **Gyro is optional and usually aim-only** (Genshin, Fortnite, CoD, PUBG; Death Stranding allows it with touch) ([WIKI][gi-ctrl], [DOC][fn-gyro], [DOC][codm-blog], [PRESS][ds-ta]); Alien shipped without it and reviewers asked ([PRESS][ai-ta]).
13. **Long-press on a HUD element = secondary verb** (DOORS / Minecraft hold slot to drop) ([WIKI][doors-ctrl], [WIKI][mc-ctrl]).

---

## 10. What applies to FrontRooms (PROPOSAL — verb by verb)

Scope rule kept from the project: this is a **mobile-build-only input layer**; desktop/editor behaviour stays the reference and is not changed (same principle as the WebGL track).

| Desktop verb | Touch mapping (PROPOSAL) | Why (precedent) |
|---|---|---|
| **WASD move** | **Floating stick** that spawns where the left thumb lands inside a left zone (Roblox uses left 40 % × lower ⅔ in landscape); stick graphic fades in on touch and out on release; **Fixed** as a setting. Small dead zone (Roblox: 2 px, full speed at 20 px, doubled on big screens); speed = throw. | Roblox default + numbers ([CODE][rbx-dyn]); CoD Fixed option ([DOC][codm-blog]); Exit 8 anchoring complaint ([USER][e8-rss]). Unity Input System supports both: `OnScreenStick.Behaviour.ExactPositionWithDynamicOrigin` (floating) vs `…StaticOrigin` (fixed) ([DOC][unity-stick]). |
| **Mouse look** | **Drag anywhere on the right half** (trackpad), separate X/Y sensitivity with a wide range, acceleration **off** by default; optional virtual right stick; **gyro look off by default** (assist toggle). | Alien Virtual Trackpad / Joystick ([DOC][ai-faq]); Alien sensitivity complaints both ways ([PRESS][ai-pg], [USER][ai-rss]); Exit 8 exposes acceleration ([WIKI-guide][e8-game8cam]); gyro optional elsewhere ([DOC][fn-gyro]). |
| **Shift sprint** (5 s stamina, loud, Relay hears it) | **Never auto-sprint from a slightly long stick push.** Default = **sprint latch**: push the stick past its ring into a small notch above it (or flick up) to start; sprint runs until the stamina segments empty, the stick is released, or the stick is pulled back. Haptic tick on engage and on the last segment. Setting: "Sprint by stick edge" (Minecraft-style), **OFF** by default; alt. a hold-to-sprint button that is **independent of the look finger**. Keep the 5 segments under the centre dot (world-first). | Alien's stealth failure ([PRESS][ai-pg]); CoD Auto-Run / PUBG lock ([DOC][codm-blog], [PRESS][pubg-pg]); Minecraft's opt-in toggle ([WIKI][mc-ctrl]); Noclip's sprint dropping when the camera finger moves ([USER][noclip-rss]); Alien's sprint haptic ([DOC][ai-faq]). |
| **E tap: open/shut door** | A **contextual USE button** appears in the right-thumb zone **only while the centre dot is on a door** (door glyph = the existing prompt line, mirrored on the button). Also accept a **short tap on the door itself** (tap < ~150 ms and < ~10 px travel = tap, longer = look drag — thresholds to tune). **No auto-open by default**: shutting a door on the Relay is tactical; an "Auto-open doors" assist may open but must **never** auto-shut. | DOORS / Granny / CoD contextual buttons ([WIKI][doors-ctrl], [WIKI][granny-ctrl], [DOC][codm-blog]); Alien look+tap ([PRESS][ai-ap]); auto-door options in shooters ([DOC][codm-blog], [PRESS][fn-ss]). |
| **E hold: break glass** (hold bar; tap-mode) | Same USE button, glyph switches to the pane/break icon when the dot is on glass; **press and hold** fills a **radial ring around the button** that mirrors the hold bar under the dot; release = cancel; dot leaving the pane = cancel; haptic pulse per crack stage, stronger on shatter. Keep **tap-mode** as the accessibility path (tap = start, tap = stop), as Alien does the reverse with Rapid Tap Assist. | Roblox ProximityPrompt circle + 1.6× touch scale ([CODE/DOC][rbx-ppapi]); Minecraft hold-to-break radial ([WIKI][mc-ctrl]); Granny hold-to-fill gauge ([WIKI][granny-ctrl]); Alien Rapid Tap Assist ([DOC][ai-faq]). |
| **Walk into broken frame: climb** | Keep automatic (no button). | CoD turns Jump into Mantle only when needed ([DOC][codm-blog]); fewer buttons is the praised direction ([PRESS][acm-ta]). |
| **S: cancel a camera shot** | **Pull the stick back** (keeps "S = back" meaning) **or** tap a small "skip" chip that appears only during a shot. | Context-only buttons ([DOC][rbx-xplat]). |
| **Esc pause** | Small pause icon on the **top edge** (top-right, clear of the zone name); auto-pause when the app goes to background (UNVERIFIED as a cited precedent — standard platform behaviour, to confirm). | DOORS gear top-left, Minecraft menu top ([WIKI][doors-ctrl], [WIKI][mc-ctrl]). |
| **O settings / layout** | Inside pause. v1 editor: control size, opacity, **left/right-handed swap**, fixed/floating stick, look sensitivity. v2: per-control drag/pinch + overlap warning. | Alien editor & L/R reset ([DOC][ai-faq-layout]); AC Mirage red overlap zone ([PRESS][acm-ta]). |
| **R restart** | Pause-menu item with confirm (no on-screen button). | Avoid accidental destructive taps; Alien users question deletable pause buttons ([USER][ai-rss]). |
| **Space: start from title** | Tap anywhere. | — |

**HUD placement (PROPOSAL).** Roblox's guidance — default controls own the bottom-left and bottom-right corners, keep info out of them ([DOC][rbx-xplat]) — collides with FrontRooms' **key glyph bottom-left** and **hint card bottom-centre**. On touch builds: move the key glyph up under the zone name (top-left), lift the hint card and captions above the thumb zones. Persistent touch elements stay at **three at most** (stick when touched, sprint notch, pause); the USE button exists only while aimed at a door/pane. This keeps the "world-first" HUD and avoids the RE/Death Stranding "pad on glass" look ([PRESS][rev-ta], [PRESS][ds-ta]).

**Controller.** Detect MFi/Backbone/Kishi and hide touch UI; bring it back on first touch (SDD had to fix exactly this) ([STORE][sdd-as]); players of SDD, Exit 8 and Alien all report playing with pads ([USER][sdd-rss], [USER][e8-rss], [DOC][ai-faq-pad]).

**Haptics.** Two toggles like Alien — "Gameplay" (Relay footsteps close, door slam, glass shatter) and "Controls" (sprint engage, last stamina segment, hold-ring complete) — and both must truly silence output ([DOC][ai-faq], [USER][ai-rss]).

**Dark Deception takeaway.** Our primary reference never shipped a first-person touch scheme ([WIKI][dd-wiki]); its 2D mobile spin-off shows two small lessons: don't park a secondary button right where the stick lives ([USER][sdd-rss]), and remove interaction friction where it adds nothing (exit door made walk-through) ([STORE][sdd-as]) — which supports keeping FrontRooms' climb-through automatic, but not auto-opening doors.

---

## 11. Not verified / open

- Exit 8 mobile: stick type, look/run controls, settings on mobile, Android date (Mar 28 vs Mar 31).
- Apeirophobia mobile layout (Run/Crouch buttons only from a search summary).
- DOORS: whether numbered doors need the interact button on mobile; hold time for unlocking on mobile.
- AC Mirage concrete gestures; RE4 touch specifics; CoD/PUBG/Fortnite haptics; Fortnite auto-sprint on mobile.
- Granny Android release date; Hello Neighbor scheme.
- **All video timestamps**: none viewed (no-download rule). On-device walkthroughs (TapGameplay, Marconaeus, Ops Gamer) are the best bets for visible touch HUD; trailers are likely cinematic. Someone needs to scrub them in a browser and add timestamps.
- TouchArcade images return HTTP 403 to direct requests (hotlink protection); view them inside their review pages.

---

## 12. Sources (reference-style link targets used above)

[dd-tweet]: https://twitter.com/DarkDeceptionDD/status/1169904702936367105
[dd-wiki]: https://dark-deception-game.fandom.com/wiki/Dark_Deception
[dd-fanport]: https://sahsa84yt.itch.io/dark-deception-mobile-port
[glow-site]: https://www.glowstickentertainment.com/
[glow-gp]: https://play.google.com/store/apps/dev?id=6738715645713292301
[sdd-as]: https://apps.apple.com/us/app/super-dark-deception/id1659946589
[sdd-lite]: https://apps.apple.com/us/app/super-dark-deception-lite/id1494973595
[sdd-gp]: https://play.google.com/store/apps/details?id=com.GlowstickEntertainment.SuperDD
[sdd-rss]: https://itunes.apple.com/us/rss/customerreviews/id=1659946589/sortby=mosthelpful/json
[sdd-wiki]: https://dark-deception-game.fandom.com/wiki/Super_Dark_Deception
[e8-as]: https://apps.apple.com/us/app/the-exit-8/id6670469064
[e8-gp]: https://play.google.com/store/apps/details?id=com.PLAYISM.TheExit8
[e8-rss]: https://itunes.apple.com/us/rss/customerreviews/id=6670469064/sortby=mosthelpful/json
[e8-playism]: https://playism.com/en/news/2025/0327/1629/
[e8-game8]: https://game8.jp/exit8/699192
[e8-game8cam]: https://game8.jp/exit8/699195
[e8-ig]: https://www.inside-games.jp/article/2025/03/28/165814.html
[e8-famitsu]: https://www.famitsu.com/article/202503/38058
[ai-as]: https://apps.apple.com/us/app/alien-isolation/id1573029040
[ai-gp]: https://play.google.com/store/apps/details?id=com.feralinteractive.alienisolation_android
[ai-news]: https://www.feralinteractive.com/en/news/alien-isolation-stalks-onto-ios-android-december-16th
[ai-faq]: https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_new_options
[ai-faq-layout]: https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_custom_layout
[ai-faq-pad]: https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_gamepad
[ai-faq-kbm]: https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_kb_m
[ai-ta]: https://toucharcade.com/2021/12/16/alien-isolation-mobile-review-frame-rate-resolution-performance-battery-saver-ipad-pro-iphone-11-settings/
[ai-pg]: https://www.pocketgamer.com/alien-isolation/review
[ai-pt]: https://www.pockettactics.com/alien-isolation/mobile-review
[ai-ap]: https://www.androidpolice.com/alien-isolation-is-a-fantastic-example-of-how-to-properly-port-a-console-game-to-mobile/
[ai-gf]: https://www.gfinityesports.com/alien-isolation-mobile/interview/
[ai-rss]: https://itunes.apple.com/us/rss/customerreviews/id=1573029040/sortby=mosthelpful/json
[yt-ai-out]: https://www.youtube.com/watch?v=HRh6uBLvfNk
[rev-as]: https://apps.apple.com/us/app/resident-evil-village/id6450980545
[re4-as]: https://apps.apple.com/us/app/resident-evil-4/id6462360082
[re7-as]: https://apps.apple.com/us/app/resident-evil-7-biohazard/id1640629241
[rev-ta]: https://toucharcade.com/2023/10/26/resident-evil-village-iphone-15-pro-review-frame-rate-resolution-max-capcom/
[rev-gsm]: https://m.gsmarena.com/resident_evil_village_for_iphone_review-news-60519.php
[rev-imore]: https://www.imore.com/gaming/resident-evil-village-for-iphone-15-pro-hands-on-terrifyingly-good-and-just-plain-scary-without-a-controller
[rev-rss]: https://itunes.apple.com/us/rss/customerreviews/id=6450980545/sortby=mosthelpful/json
[re7-ta]: https://toucharcade.com/2024/07/08/resident-evil-7-iphone-review-15-pro-gameplay-settings/
[re7-gsm]: https://www.gsmarena.com/resident_evil_7_biohazard_for_iphone_review-news-63606.php
[re4-taptap]: https://www.taptap.io/post/6683237
[rbx-dtm]: https://create.roblox.com/docs/reference/engine/enums/DevTouchMovementMode
[rbx-xplat]: https://create.roblox.com/docs/ui/cross-platform-design
[rbx-pp]: https://create.roblox.com/docs/ui/proximity-prompts
[rbx-ppapi]: https://create.roblox.com/docs/reference/engine/classes/ProximityPrompt
[rbx-cm]: https://github.com/MaximumADHD/Roblox-Client-Tracker/blob/roblox/scripts/PlayerScripts/StarterPlayerScripts/PlayerModule.module/ControlModule.lua
[rbx-dyn]: https://github.com/MaximumADHD/Roblox-Client-Tracker/blob/roblox/scripts/PlayerScripts/StarterPlayerScripts/PlayerModule.module/ControlModule/DynamicThumbstick.lua
[rbx-thumb]: https://github.com/MaximumADHD/Roblox-Client-Tracker/blob/roblox/scripts/PlayerScripts/StarterPlayerScripts/PlayerModule.module/ControlModule/TouchThumbstick.lua
[rbx-jump]: https://github.com/MaximumADHD/Roblox-Client-Tracker/blob/roblox/scripts/PlayerScripts/StarterPlayerScripts/PlayerModule.module/ControlModule/TouchJump.lua
[doors-ctrl]: https://doors-game.fandom.com/wiki/Controls
[doors-hide]: https://doors-game.fandom.com/wiki/Hiding
[doors-doors]: https://doors-game.fandom.com/wiki/Doors_(feature)
[doors-api]: https://games.roblox.com/v1/games?universeIds=2440500124
[apeiro-api]: https://games.roblox.com/v1/games?universeIds=3761186887
[apeiro-mej]: http://www.mejoress.com/roblox-apeirophobia-controls/
[etb-wiki]: https://en.wikipedia.org/wiki/Escape_the_Backrooms
[as-search-br]: https://itunes.apple.com/search?term=backrooms&country=us&entity=software&limit=25
[brd-as]: https://apps.apple.com/us/app/backrooms-descent-horror-game/id1622822489
[brd-rss]: https://itunes.apple.com/us/rss/customerreviews/id=1622822489/sortby=mosthelpful/json
[yt-brd]: https://www.youtube.com/watch?v=8N_S9zEqe4Y
[noclip-as]: https://apps.apple.com/us/app/noclip-backrooms-multiplayer/id6444107785
[noclip-rss]: https://itunes.apple.com/us/rss/customerreviews/id=6444107785/sortby=mosthelpful/json
[b97-as]: https://apps.apple.com/us/app/backrooms-97-retro-descent/id6452627183
[b97-rss]: https://itunes.apple.com/us/rss/customerreviews/id=6452627183/sortby=mosthelpful/json
[pools-as]: https://apps.apple.com/us/app/pools/id6670792878
[pools-rss]: https://itunes.apple.com/us/rss/customerreviews/id=6670792878/sortby=mosthelpful/json
[pools-gd]: https://www.gamedeveloper.com/press-release/pools-arriving-on-the-app-store
[pools-wiki]: https://en.wikipedia.org/wiki/Pools_(video_game)
[granny-as]: https://apps.apple.com/us/app/granny/id1323957120
[granny-rss]: https://itunes.apple.com/us/rss/customerreviews/id=1323957120/sortby=mosthelpful/json
[granny-ctrl]: https://granny.fandom.com/wiki/Controls_%26_Symbols
[granny-opt]: https://granny.fandom.com/wiki/Options_Menu
[poppy-as]: https://apps.apple.com/us/app/poppy-playtime-chapter-1/id1610947489
[poppy-rss]: https://itunes.apple.com/us/rss/customerreviews/id=1610947489/sortby=mosthelpful/json
[poppy-ctrl]: https://poppyplaytime.fandom.com/wiki/Controls
[yt-poppy]: https://www.youtube.com/watch?v=_Tb5Gkj5Qow
[hn-as]: https://apps.apple.com/us/app/hello-neighbor/id1386358600
[hn-rss]: https://itunes.apple.com/us/rss/customerreviews/id=1386358600/sortby=mosthelpful/json
[fnaf-as]: https://apps.apple.com/us/app/id912536422
[codm-blog]: https://blog.activision.com/call-of-duty/2019-10/Getting-a-Grip-on-the-Call-of-Duty-Mobile-Controls
[codm-as]: https://apps.apple.com/us/app/id1287282214
[codm-date]: https://investor.activision.com/node/32841
[pubg-pg]: https://www.pocketgamer.com/articles/077003/r/
[pubg-wiki]: https://en.wikipedia.org/wiki/PUBG_Mobile
[pubg-as]: https://apps.apple.com/us/app/id1330123889
[pubg-door]: https://nerdschalk.com/pubg-mobile-0-12-0-update-darkest-night-spectate-friends-fixed-auto-door-opening-and-more/
[pubg-ac]: https://www.androidcentral.com/best-advanced-touchscreen-controls-pubg-mobile
[fn-as]: https://apps.apple.com/us/app/id6483539426
[fn-dates]: https://vgtimes.com/games/fortnite-mobile/release-dates/
[fn-return]: https://www.tomsguide.com/phones/iphones/fortnite-is-back-on-iphones-in-the-us-heres-what-we-know
[fn-hud]: https://www.cultofmac.com/?p=548403
[fn-auto]: https://www.epicgames.com/help/fortnite-battle-royale-c-202300000001636/gameplay-c-202300000001721/how-do-i-turn-on-off-auto-open-containers-in-fortnite-mobile-a202300000086491
[fn-gyro]: https://www.epicgames.com/help/en-US/c-Category_Fortnite/c-Fortnite_Gameplay/a000093609?lang=en-US
[fn-ss]: https://steelseries.com/blog/how-win-fortnite-mobile-81
[fn-dev]: https://dev.epicgames.com/documentation/en-us/fortnite/developer-customizable-touchscreen-controls-in-fortnite
[yt-fn2]: https://www.youtube.com/watch?v=jpCuKzEjv5g
[gi-as]: https://apps.apple.com/us/app/id1517783697
[gi-ctrl]: https://genshin-impact.fandom.com/wiki/Controls
[gi-sprint]: https://genshin-impact.fandom.com/wiki/Sprinting
[mc-as]: https://apps.apple.com/us/app/id479516143
[mc-ctrl]: https://minecraft.wiki/w/Controls
[mc-cl]: https://www.minecraft.net/en-us/article/minecraft-1-21-130-bedrock-changelog
[acm-as]: https://apps.apple.com/us/app/id6472704261
[acm-ta]: https://toucharcade.com/2024/06/19/assassins-creed-mirage-iphone-15-pro-review-backbone-controller/
[acm-int]: https://toucharcade.com/2024/06/24/assassins-creed-mirage-iphone-interview-ubisoft-sofia-producer-controller-support-touch-controls-uncapped-frame-rate-ipad-m1-metalfx/
[ds-as]: https://apps.apple.com/us/app/id6449748961
[ds-ta]: https://toucharcade.com/2024/01/30/death-stranding-directors-cut-iphone-15-pro-review-graphics-performance-settings-controller/
[unity-stick]: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/api/UnityEngine.InputSystem.OnScreen.OnScreenStick.Behaviour.html

How the data was gathered (for re-checking): App Store metadata and screenshots via `https://itunes.apple.com/lookup?id=<id>&country=us`; App Store reviews via the public RSS JSON feeds listed above (pages 1–10, "mosthelpful" and "mostrecent"); Fandom wiki text via each wiki's `api.php?action=parse&prop=wikitext` (the HTML pages are bot-blocked); Feral FAQ answers via the site's `/helpers/faq.php` endpoint behind the FAQ page; YouTube titles/channels via `youtube.com/oembed`, lengths and dates from watch-page text; image sizes via `HEAD`. Nothing was downloaded beyond page text.
