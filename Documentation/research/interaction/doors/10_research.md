# Contact doors in real games

Research for FrontRooms 交互设计 (interaction design): double-acting wooden swing doors, door handling without a button, and the Relay, which hunts by sound and sight. Written 2026-10-31 from research_raw.md in this folder. That research used web search and page reads only, with no downloads, no logins and no videos watched.

**How to read this page**

- **[V] verified.** The page was opened and says this. Dates and authors are given where the page showed them.
- **[S] seen in search results only.** The claim appeared in a search-result summary of that URL, but the page itself was blocked (403, 402, age gate or JavaScript) or not opened. Treat it as weaker than [V].
- **[U] unverified.** Prior knowledge or inference that no source confirmed. This page's own readings also carry [U].
- Words inside a tag, such as [S, page 403] or [V that the article says so], come from the notes and narrow the tag.
- "Not in the notes" means the notes say nothing on that point. "Not found" means the notes looked and found nothing. Neither proves that a game lacks the feature.
- Everything is paraphrased except Red's words in section 1 and one short developer quote in The Last of Us block.
- No video was watched, so there are no video timestamps.
- These sites were blocked for the fetcher, so claims from them are [S]: Steam community threads for mature games (age gate, not bypassed), all Fandom wikis, GameSpot, Polygon, The Verge, IGN, X/Twitter, StrategyWiki, the Valve Developer Community, PC Gamer article bodies, Twinfinite and Prima.
- Numbers on "Sources" lines point to the numbered list at the end.

## 1. Why this page

On 2026-10-31, in the 交互设计 chat, Red pasted the V5 door summary: “开关门的三种方式（在开关后面）：慢慢推开：6 米内能听到；奔跑撞开：18 米内能听到；用力甩上：27 米内能听到。奔跑中或刚停下 0.5 秒内开门就算"撞开"，门转动中再按无效。” (three ways to open and close a door, behind a switch: a slow push is heard within 6 m, a running bash within 18 m and a hard slam within 27 m; opening while running or within 0.5 s of stopping counts as a bash, and pressing again while the door is turning does nothing). Red then asked that the interaction itself answer those three ways: “刚刚做的这个逻辑，我希望从交互上，应该能够呼应玩家对门的这几种操作方式，比如说如果玩家要是在奔跑时完全没有停下，并且是奔跑最快的速度，则会直接撞考[撞开]，而甩上门的情况应该是玩家比如说回头，则会种种[重重]甩上门。慢慢推开的情况是玩家一点点的移动相当于蹭门的感觉，把门挤开，而这几种交互手法应该都是不需要玩家按下一个按钮去开门的，都是玩家移动的时候，和门的碰撞体发生碰撞之后的结果。” (square brackets fix two typos). In plain English: a player sprinting at top speed without ever stopping bashes the door open; a slam happens when the player, for example, turns back and slams the door hard; a slow push is the player moving bit by bit, rubbing against the door and squeezing it open; and none of the three needs a button, because each is the result of the player's movement colliding with the door's collider. Red ended with the research ask: “应该有很多3A游戏其实对开门是这样的处理（但是关门和甩门不一定是）因此我希望你能够做一个对应的调研，把开关门这部分优化成我表达的（你也可以根据我的表达去思考，我真正想要的效果是什么）” (many AAA games probably open doors this way, though not necessarily close or slam them; research it, optimise door opening and closing into what Red described, and think about what effect Red really wants). This page is the research part: what real games do, how sure each finding is, and where the evidence stops. It does not set the FrontRooms design.

## 2. Per-game findings

Each block gives open, speed variants, close, slam, noise and AI, feedback and sources, in that order. Fields the notes say nothing about are grouped on one "Not in the notes" line. Short forms: MW2019 is Call of Duty: Modern Warfare (2019), and TLOU2 is The Last of Us Part II.

### AAA action and open-world games

#### Call of Duty: Modern Warfare (2019), Warzone and later games

- **Open.** Infinity Ward's multiplayer design directors Geoff Smith and Joe Cecot list four ways through a door (Activision blog, 2019-08-07): walk up and press Use; sprint through, so the door smashes or clangs open and you are heard; aim down sights and press Use, so the door opens only a crack, silently, which is pitched as the stealth option, after which you can physically push into the door to open it further; or pop it open with explosives or concussions [V]. MP1st restates the same four ways (2019-08-07) [V]. Before this, Call of Duty 4 handled doors by script in its campaign and had no interactive doors online (Gamepressure, 2021-03-26) [V].
- **Speed variants.** A Dutch preview says sprinting at a door kicks it open, while walking up calmly lets you peek through the gap (jfk.nl, 2019-09-16) [V]. The CoD wiki's Tactical Sprint page says a tactical sprint through a door smashes it open loudly and may alert players [S]. Charlie Intel (2019-08-01) says players can open and close many doors and can brute-force charge into doors while sprinting, and a sprint bash can kill an enemy standing right behind the door [V]. Later games made the bash a setting. The Vanguard alpha had a "door bash interaction" toggle that MP1st called familiar to MW2019 players (2021-08-23) [V]. Digital Trends' console-settings list for Modern Warfare II (2022) has "Sprinting door bash", recommended On, under Movement behaviors (2022-11-03) [V], and KontrolFreek and WZStats list the same toggle [S]. Warzone Mobile's official controls blog says its Gameplay tab lets you choose whether sprinting bashes doors open (callofduty.com, 2024-03) [S, fetch timed out].
- **Close.** You can close a door by walking your character into it instead of pressing the button (TheGamer, 2020-09-06) [V]. A Twinfinite Warzone guide says you press the button again to close, and describes the slow open as: stand still, press Use so the door cracks, then walk slowly into it, which is mostly quiet though some metal doors creak [S, page 403]. Closing has an exploit, the "door lock": push the door almost shut and stand beside it, and enemies cannot open it from the other side [V: Charlie Intel 2020-11-02; TheGamer 2020-09-06]. An explosive blows such a door open and shoves the player behind it [V: Charlie Intel], and a bunker-door variant made a player effectively unkillable [V: Charlie Intel]. Cecot says closing a door can pause a firefight, because the attacker must choose to bust through or reroute [V: Activision blog].
- **Slam.** TheGamer says you can also close a door by slamming it in the opposite direction [V]. The notes read this as driving the open door back toward the frame by walking or running into it; the article, as the notes summarise it, does not name the input [U]. Gamepressure says doors in MW2019 online can be opened, slammed or cracked for peeking [V].
- **Noise and AI.** In multiplayer the listeners are other players, and the sprint bash is heard [V]. Critics say doors encouraged camping and claymores behind doors (Dexerto, 2023-08-21 [V]; MP1st [S]) and that door audio cues make flanking hard (upcomer) [S]. Doors on the remastered Modern Warfare III (2023) maps drew fan backlash (Dexerto, 2023-08-21) [V]. Gfinity (2025-08-13) reports a leak that Black Ops 7 has no interactive doors (open, close or bash) on standard 6v6 maps [V that the article says so; the leak itself is unconfirmed]. Smith says usable doors started almost as a joke and then turned out to change matches a lot [V: Activision blog]. The same blog says mounting on a door frame, with a hand on the edge, lets you "pie" a room [V: Activision blog].
- **Feedback.** A loud smash or clang on the bash, and a silent crack on aim-and-Use [V]. Camera shake and rumble are not documented [U].
- **Sources.** 1–12, 14–18, 104.

#### Battlefield 4, 1, V, 2042 and 6

- **Open.** Documentation is very thin. In Battlefield 4, small player-triggered Levolution changes include opening and closing selected doors and shutters (a guide summary) [S]. Operation Locker has gates and doors that can be locked to keep enemies out of an area or pathway (HNGN, 2013-11-11) [V], and bollards are raised and lowered to block vehicles (PCGamesN) [V]. On destruction, a search summary attributes to a DICE developer that some Battlefield 1 melee weapons might break down doors, likely from GameSpot's melee article [S], while EA's own melee article mentions only wooden barricades, not doors [V]. Battlefield V's wooden doors are destructible cover (EA bullet-penetration article) [S]. Battlefield 6 coverage only mentions destroying doors with gunfire as part of destruction (setup.gg) [S]. Nothing was found on openable doors in 2042 or 6. The notes recall that Frostbite-era interior doors open on contact and are destructible, but could not source it [U].
- **Speed variants.** Neither walk-into pushing nor sprint bashing could be confirmed in any Battlefield.
- **Not in the notes.** Slam, noise and AI, feedback. Closing appears only in the Battlefield 4 guide summary above.
- **Sources.** 20–26.

#### GTA V

- **Open.** Doors are physics objects: FiveM's asset docs say a swinging door's archetype needs the Dynamic and Enable Door Physics flags, with the model origin on the hinge (docs.fivem.net) [V]. A 2017 Unreal forum post asking how to copy GTA V's doors says players walk into them and they open as if on a pivot [V that the poster says this; it is an observation, not a Rockstar source].
- **Close.** How doors swing back, auto-close or latch is UNVERIFIED [U]. The notes recall a door-tuning data file with auto-close and latch fields but found no source [U].
- **Not in the notes.** Speed variants, slam, noise and AI, feedback. Saloon doors were not found.
- **Sources.** 27, 28.

#### Red Dead Redemption 2

- **Open.** When you walk up to a door you can kick it with the melee button (Circle or B) or open it normally (GameRevolution, 2019-11-04) [V]. A Steam tip says the prompt appears if you do not run at the door and keep your hands empty [S, forum]. Kotaku (2018-11-23) covers bolted doors that look openable but are not [V].
- **Speed variants.** PC Gamer's "19 times I burst through doors to startle people in RDR2" says running into doors bursts them open, and that most doors do not open at all, so you keep bumping your shoulder into locked ones [S, body not loaded].
- **Noise and AI.** Bursting through a door startles NPCs (same PC Gamer piece) [S].
- **Feedback.** Arthur's hand-push animation and the saloon-door physics are UNVERIFIED [U]. The heavy, committed animation is a known responsiveness complaint (a New Frame Plus essay title, and a community input-lag estimate of about 330 ms) [S].
- **Not in the notes.** Close, slam.
- **Sources.** 29–34.

#### The Last of Us Part II and Part I

The best-documented case. The primary source is a 13-tweet thread by co-game director Kurt Margenau on 2021-03-09, read via threadreaderapp [V]. PlayStation LifeStyle (2021-03-10) [V], Kotaku (2023-02-28) [V] and ppe.pl (2021-03-11) [V] report the same content. Combat doors exist because, in stealth, doors block line of sight, slow enemies, and give more chances to escape and re-establish stealth [V].

- **Open.** The character must reach for the knob and push; a door may not simply swing open by itself [V]. Opening slows the player only a little and never takes control away [V]. That implies opening while moving, but the notes could not confirm that every door opens with no button press [U]. The animation is layered: how far the hand reaches depends on the approach, so it can be interrupted (Gigazine, on the Vox interview) [V]. All doors always open away from the player, because that is faster and never impedes, and the door art uses double hinges so it looks right either way (tweet 11/13) [V]. Gamepressure (2021-03-26) describes the same choice, double-hinged doors that keep the player always on the push side [V]; it adds that manual closing did not feel right, and that the doors are about twice character height to help the camera [V].
- **Speed variants.** Lead animator Maksym Zhuravlov's 2023 clip shows that jogging, sprinting or pushing through give different reactions: a staggered opening, a sharp burst open, and a heavy close (Kotaku, 2023-02-28) [V]. The tweet is linked from 80.lv (2023-03-01) [V link seen, tweet not opened].
- **Close.** The team tried letting the player close doors, with held buttons and various other odd schemes, and none worked [V]. Margenau's solution: "in combat tension the doors will slowly automatically close." [V] It was chosen because it favours the player most and because closed doors block the AI better [V]. Outside combat, doors stay open so you can see where you have been [V].
- **Slam.** Because the doors are physics-based, they slam shut behind you when you open them violently, that is, when you sprint through them (tweet 6/13) [V]. Naughty Dog built a new engine physics object that the player can push and that can push the player (tweet 7/13) [V].
- **Noise and AI.** Nearly every department was affected, including AI, melee and buddies, and a named colleague made the AI work with doors (tweet 10/13) [V]. NPCs also open doors, so designers must handle whether an NPC knows a door is locked and whether it can open it (Gigazine) [V]. Wikipedia notes navmesh changes and movement-state behaviour [V]. Sound was its own challenge (tweet 12/13) [V]; audio director Neil Uchitel tweeted about door audio, including dynamic portalling, as cited by Wikipedia's "Door problem" page [V citation; tweet not opened].
- **Feedback.** The door's own motion (a burst versus a staggered opening), the knob reach and the slam sound [V]. Rumble is not documented [U].
- **Part I (2022 remake).** No door-specific source was found. A review mentions dishwasher doors closing as you press against them in cover (GameSpew) [S]. Contact doors in Part I are UNVERIFIED [U].
- **Sources.** 35–40, 42–44, 103, 104.

### Tactical shooters

#### Ready or Not (VOID)

- **Open.** Door verbs share one key, and the verb depends on where you aim: aim at the knob for "Peek Door", a slight open [S, guide], or aim lower-middle to kick [S, Steam]. Other verbs are a mirror or Optiwand under the door, which works on wooden doors only and through ajar doors [S, afkgaming], a wedge that stops a door being opened or kicked [S, Epic guide], and a lockpick (no source given in the notes) [U]. A community procedure (GamePretty, 2021-12-28, from tRose888's Steam guide) uses "peek" for opening a closed door partly and "swing" for opening it fully [V].
- **Noise and AI.** The same procedure says that if a door will not swing, someone is behind it, so you kick and flash [V]. Rules for how noise or AI react to doors were not found [U].
- **Feedback.** One key with aim-position context makes players shuffle in front of the door to line up the right option, and players asked for separate binds for open, kick and peek (devtrackers, 2021) [V].
- **Not in the notes.** Speed variants, close, slam.
- **Sources.** 45–49.

#### SWAT 4

All points come from search summaries only [S: GiantBomb, SWAT 4 wiki].

- **Open.** Breach charges open locked doors, and C2 briefly stuns suspects behind the door [S].
- **Speed variants.** Kicking was not found; the notes believe SWAT 4 has no kick [U].
- **Noise and AI.** Officers stacking on a door automatically check the lock and report it, and the wedge blocks suspects' path [S].
- **Feedback.** A snake camera looks under doors [S].
- **Not in the notes.** Close, slam.
- **Sources.** 50, 51.

#### Rainbow Six Siege

- **Open.** A Ubisoft developer article (2014-11-18) says doorways get wooden barricades meant to block sight, not bullets; attackers shoot holes or breach-charge through them, and setup or teardown takes about 2.5 s [V]. Melee breaks a standard barricade in about 3 hits (fan wiki) [S]. Openable swing doors: none found [U].
- **Not in the notes.** Speed variants, close, slam, noise and AI, feedback.
- **Sources.** 52, 53.

#### Escape from Tarkov

- **Open.** Approaching a door brings up a small menu: open, breach (kick) or unlock (xboxplay.games, 2020-01-08) [V]. Breaching works only on doors that open away from you [V]. Opening animations were slower at launch [V].
- **Close.** A streamer died waiting on a door; closing only nudged it a little, and reopening was delayed (Dexerto, 2020-02-11) [V].
- **Noise and AI.** A door left open tells others that someone has been there (guides) [S].
- **Feedback.** Doors felt sluggish (same Dexerto piece) [V].
- **Not in the notes.** Speed variants, slam.
- **Sources.** 54–56.

#### Insurgency: Sandstorm

- **Open.** You can carefully open doors to stay quiet, or bash them down with a shoulder charge (Worth Playing, 2018-12-07) [V]. PS France trailer text says to slowly open a door to slip a grenade through, or kick it down for a breach [S]. Kick is the middle mouse button, per a fan cheat site [S].
- **Speed variants.** Whether sprinting into a door bashes it is not confirmed [U].
- **Noise and AI.** Careful opening keeps you quiet [V].
- **Not in the notes.** Close, slam, feedback.
- **Sources.** 57–59.

#### Hunt: Showdown

- **Open.** A Crytek dev diary (DualShockers, 2017-09-08) says some doors are barricaded and open only from inside; to get in, you shoot the lock through a gap, which is very loud, or use a sledgehammer or axe [V]. Community tips (Reddit, 2018) say shooting a door can swing it open, and doors with a plank nearby can be barricaded [S].
- **Speed variants.** Doors open more quietly when crouched, and sprinting with a charged melee opens a door much faster (same tips) [S].
- **Not in the notes.** Close, slam, feedback.
- **Sources.** 60, 61.

#### Apex Legends

An extra AAA reference, all from wiki.gg [V].

- **Open.** Doors use the interact button only, and players, death boxes and ability objects obstruct them [V]. Melee or a shot on the handle-side stile can knock a door open, and 2 melee hits or 1 explosive destroy it [V]. Catalyst reinforces a door, and Wattson electrifies doors and stuns enemies who open or kick them [V].
- **Not in the notes.** Speed variants, close, slam, noise and AI, feedback.
- **Sources.** 62.

### Horror and chase games

#### Outlast (2013)

- **Open.** Press the button to smash the door open, which makes noise, or hold it to creak the door open slowly and peek in (GeekCulture review, 2014-02-15) [V].
- **Speed variants.** The notes describe no movement-based variant; the loud and the quiet open are a tap and a hold of the button [V].
- **Close.** Steam threads from 2013 and 2015 (age-gated, not opened) say that holding the button and walking the door shut closes it quietly, but only while you are still in the slow-open grab, so a fully open door can only be slammed [S, forum].
- **Slam.** The same threads say a quick press slams a door shut, and that players read the slam as the tool for blocking pursuers [S, forum].
- **Noise and AI.** Smashing a door open makes noise [V]. Closing doors impedes enemies, and squeezing through small spaces can lose them (Outlast fan wiki) [S].
- **Not in the notes.** Feedback beyond the noise.
- **Sources.** 63, 65–67.

#### Outlast 2

- **Open.** Press to open, or hold Square to open slowly; some doors have deadbolts you can lock (ludo.guide walkthrough) [V].
- **Not in the notes.** Speed variants, close, slam, noise and AI, feedback.
- **Sources.** 64.

#### The Outlast Trials

- **Speed variants.** Steam posts say there is a sprint door bash that slightly stuns enemies behind the door; it costs stamina and has a short recovery, so it can be slower than walking the door open and running on [S, forum].
- **Noise and AI.** Enemies still break barricaded or locked doors; it just takes longer [S, forum].
- **Not in the notes.** Open, close, slam, feedback.
- **Sources.** 68.

#### Amnesia: The Dark Descent

- **Open.** You grab the door and swing it open or shut with the mouse (Shacknews review, 2010-09-22) [V]. Under pursuit you realise a door opens only one way, not both [V].
- **Close.** Players fully control doors, including closing them behind them to slow monsters (Gamepressure) [V].
- **Slam.** A right-click "throw" while holding a door slams it: UNVERIFIED, from a vague search summary only [U]. One reviewer said the controls got in the way while frantically slamming doors behind them when fleeing [S, outlet not identified].
- **Noise and AI.** You can stack boxes in front of a closed door to ward off an enemy (fan analysis) [S].
- **Not in the notes.** Speed variants, feedback.
- **Sources.** 69, 104. Source 70 is listed with the Amnesia sources and may be the fan analysis, but the notes do not tie it to a claim.

#### Amnesia: The Bunker

- **Noise and AI.** Breaking doors is one of the noises that draw the Beast (CommonSenseGamer) [V]. Players report that the Beast smashes wooden doors when it cannot find a path (Steam) [S].
- **Not in the notes.** Open, speed variants, close, slam, feedback.
- **Sources.** 71, 72.

#### SOMA

- **Open and feedback.** You grab and drag with the mouse; the grabbed object lags behind the cursor, which gives it weight, and when you let go a door drifts on under its own momentum (PC Gamer, "The weighty doors and switches of Soma") [S, body not loaded].
- **Not in the notes.** Speed variants, close, slam, noise and AI.
- **Sources.** 73.

#### Phasmophobia

- **Open.** Hold the mouse button on a door and drag in its swing direction (guides) [S]; if it will not move, drag the other way (guides) [S]. Grab-and-drag doors do not map to a controller, because a held button is not a held mouse button (Steam mapping guides) [S]; an older guide says to hold RT and move the camera [S].
- **Close.** Holding a door shut stops ghosts opening it, though ghosts still walk through room doors (Game Informer, 2021-03-08) [V]. A delay was added before closing the truck door, to stop trolling [V].
- **Noise and AI.** Since the March 2021 update, ghosts open doors, closets and lockers during hunts [V]. When a hunt starts, the exit doors close and lock (guide) [S].
- **Not in the notes.** Speed variants, slam, feedback.
- **Sources.** 74–76.

#### Visage

- **Open and close.** Opening and closing doors feels oddly awkward, and the reviewer ended up walking into doors to close them, unlike in Amnesia (WayTooManyGames review, 2020-12-04) [V].
- **Slam.** The house opens and closes doors at random to scare you [V], and Thumb Culture (2020-11-25) also describes doors slamming shut as scares [V].
- **Not in the notes.** Speed variants, noise and AI, feedback.
- **Sources.** 77, 78.

#### Alien: Isolation

- **Open.** Doors are mostly powered sliding doors that open on approach; a preview mentions passing through sliding doors [S]. Some need codes or tools, and the maintenance jack forces them (Prima) [S].
- **Noise and AI.** Whether door noise attracts the alien is disputed on Steam [S]; the notes give no URL for that thread. A developer, InvertMouse (maker of Clea), observed that Mr. X and the Alien seem not to react to doors opening or closing [V, devlog].
- **Not in the notes.** Speed variants, close, slam, feedback.
- **Sources.** 79, 80, 82.

#### Resident Evil 2 (2019 remake)

- **Speed variants, noise and AI.** Slamming doors open, running and gunshots are noises that draw Mr. X, and quieter play slows him (GameRevolution, 2019-02-28) [V, guide]. Steam players say walking, and opening doors while walking, do not alert him [S]. InvertMouse thinks Mr. X ignores doors [V]. These sources disagree, so Mr. X's reaction to doors is marked UNCERTAIN.
- **Not in the notes.** Close, slam shut, feedback.
- **Sources.** 81–83.

#### Resident Evil 7

- **Open.** A review in the search results (outlet not identified) says you press up against a door and it creaks open slowly [S].
- **Close.** The Baker doors are weighted to close on their own, though closing them yourself is still advised (cheatcc guide) [S]. A review describes a door you forgot to close shutting itself with an audible click (Twinfinite) [S].
- **Not in the notes.** Speed variants, slam, noise and AI. Feedback appears only as the creak and the click above.
- **Sources.** 84, 85. Source 86 (a GameWatcher review) is listed in the notes, but no claim cites it.

#### Resident Evil Village

- Nothing found [U].

#### Classic Resident Evil

- **Open and feedback.** Door transitions serve as loading screens and as suspense: the knob turns, a click, a slow swing, and no view of what is ahead (Dread Central, 2021-03-06) [V].
- **Not in the notes.** Speed variants, close, slam, noise and AI.
- **Sources.** 87.

#### Dark Deception (primary reference)

All points come from fan-wiki search summaries [S], not official sources. No developer (Glowstick) source was found.

- **Open.** In Torment Therapy (the hospital), double doors open just by walking near them, with no interaction [S]. In Elementary Evil, school doors need an interaction [S]. In Stranger Sewers, heavy bulkhead-like doors open slowly, and that slowness is the hazard, because Doom Ducky catches up [S]. In Chapter 2, manor doors are progression gates that open after soul shards [S].
- **Close.** In Elementary Evil, opened doors stay open even after a lost life, except in the Doom Ducky chase, where they shut [S].
- **Noise and AI.** Opening Torment Therapy's doors by walking near them alerts the Reaper Nurses, and the nurses open them too, so doors show you where nurses are [S]. In Mascot Mayhem, Joy Joy Gang members open doors while active, and Hangry breaks doors in his path when he spots Doug; broken doors respawn only after Doug dies [S].
- **Not in the notes.** Speed variants, slam, feedback.
- **Sources.** 88–91.

#### Granny

- Only low-quality fan pages: opening doors makes noise, you should close them slowly, and a slam attracts her [S, unreliable]. UNVERIFIED. The notes give no URL.

#### Left 4 Dead

- **Close, noise and AI.** Closed doors force the infected to break them, and special infected have to bash through (Prima, wiki) [S].
- **Sources.** 92, 93.

#### Friday the 13th: The Game

- **Close, noise and AI.** Counselors lock and barricade doors; Jason breaks plain locks fast and breaks barricades with repeated swings (wiki, guides) [S].
- **Sources.** 94.

#### Dying Light 2

- **Speed variants.** Sprinting into a locked door with the Bash skill breaks it open (PC Gamer) [S].
- **Sources.** 95.

### Stealth games

#### Metal Gear Solid 3

The notes call this the closest precedent for Red's spec. All points come from a StrategyWiki "Advanced Moves" search summary; the page returned 403 [S].

- **Open.** You normally open doors by walking straight into them [S].
- **Speed variants.** Running into a door at top speed bashes it open, and approaching carefully opens it more slowly [S].
- **Noise and AI.** The bash oddly does not alert guards [S].
- **Not in the notes.** Close, slam, feedback.
- **Sources.** 96.

#### Metal Gear Solid V

- Door behaviour not found [U].

#### Splinter Cell: Chaos Theory

All points come from GameSpot's walkthrough and hands-on, both of which returned 403 [S].

- **Open.** The interact menu at a door offers Open Door, Open Door Stealthily, Bash Door and Optic Cable [S].
- **Speed variants.** The stealthy open turns the knob slowly, and the analog stick sets how far the door swings [S].
- **Noise and AI.** Bash Door with a guard right behind the door knocks the guard out instantly, suspicious guards may bash a door open themselves, and thermal vision through thin doors helps you time the bash [S].
- **Not in the notes.** Close, slam, feedback.
- **Sources.** 97, 98.

#### Thief: The Dark Project

- **Noise and AI.** Guards react to visual changes such as opened doors, blood and bodies (Wikipedia, citing the 1998 manual and Tom Leonard 2003) [V]. Sound propagates through a per-mission "room database", around corners and through rooms [V].
- **Not in the notes.** Open, speed variants, close, slam, feedback.
- **Sources.** 99.

#### Deus Ex: Invisible War

- **Noise and AI.** The game uses a Thief-like sound propagation system (GameBanshee dev diary, 2004-02-13, sound programmer Brian Sharp) [V]. Closing a solid door makes sound on the far side much softer, frail or battered doors block less, and solid metal doors may block all sound [V].
- **Not in the notes.** Open, speed variants, close, slam, feedback.
- **Sources.** 100.

#### Hitman (2016 to World of Assassination)

- **Open.** The character's motion and the door opening do not mesh well (Gigazine, on the Vox video) [V]. Doors can be shot open, and tougher doors need explosives (Steam) [S]. Whether doors open by walking into them is UNVERIFIED [U].
- **Not in the notes.** Speed variants, close, slam, noise and AI, feedback.
- **Sources.** 39, 101.

### Covered elsewhere

Games that appear only as production or pitfall examples are in sections 3 and 5: Gone Home, The Witcher 3, Terminator: Resistance, Control, South Park: The Fractured But Whole, Assassin's Creed, Arizona Sunshine, Uncharted 4, PUBG Mobile, Fortnite, Arma 3, Valve's Source doors, Clea and Death Trash.

## 3. Patterns across games

Each bold line below is this page's reading [U]. The evidence after it keeps its own tags, and the game blocks in section 2 give the sources.

Reading [U]: set against Red's three movements, the running bash has the strongest support: MW2019 [V]; TLOU2, where sprinting through opens a door violently and it slams shut behind you [V], though whether a button is involved is open [U]; and Metal Gear Solid 3, Red Dead Redemption 2, Dying Light 2 and The Outlast Trials [S]. For the slow push by contact, the verified examples are MW2019's push into a cracked door [V] and TLOU2's "pushing through" reaction [V], again with TLOU2's button question open [U]. Plain walk-into opening otherwise rests on a GTA V forum observation [V that the poster says so] and on [S] sources (Metal Gear Solid 3, Resident Evil 7, Dark Deception). The turn-back slam has no direct precedent; see section 4.

**1. Speed decides how the door opens [U].** In MW2019, sprinting through smashes the door open and you are heard, while aiming down sights and pressing Use cracks it silently, and you can then push it further [V]; a Dutch preview describes the same split, a sprint kicking the door open and a calm walk letting you peek [V]. In TLOU2, jogging, sprinting and pushing through give different reactions: a staggered opening, a sharp burst open, a heavy close [V]. In Metal Gear Solid 3, walking into a door opens it, a careful approach opens it more slowly, and a top-speed run bashes it [S]. In Red Dead Redemption 2, running into doors bursts them open [S]. In Dying Light 2, sprinting into a locked door with the Bash skill breaks it [S]. In Hunt: Showdown, crouching opens doors more quietly, and a sprint with a charged melee opens them much faster [S]. Insurgency: Sandstorm splits careful, quiet opening from a shoulder charge [V], and Outlast puts the same split on a tap or a hold of the button [V]. Splinter Cell: Chaos Theory lets the analog stick set how far a stealthy open swings [S].

**2. Opening by contact alone appears in several AAA games, but most of the evidence is weak [U].** In MW2019 the sprint bash is listed as its own way through, apart from walking up and pressing Use [V], and Digital Trends' Modern Warfare II settings list puts its toggle under Movement behaviors [V]; this page reads that as a bash with no button press [U]. MW2019 also lets you push into a cracked door to open it further [V]. Contact opening otherwise appears in a forum observation about GTA V [V that the poster says so], in Metal Gear Solid 3 [S], in Resident Evil 7, where pressing against a door creaks it open [S], in Dark Deception's hospital level, where double doors open as you walk near them [S], and in Red Dead Redemption 2, where running into doors bursts them open [S]. TLOU2 opens doors without taking control and slows the player only a little [V], but the notes could not confirm that no button is involved [U]. Battlefield contact doors are only a recollection [U]. Many games in the notes keep a button, a menu or a mouse drag: Apex Legends (interact button only) [V], Escape from Tarkov (a small menu) [V], Ready or Not (one key, verb chosen by aim) [V], Outlast and Outlast 2 (a button) [V], Amnesia (the mouse) [V], SOMA and Phasmophobia (a mouse drag) [S], Splinter Cell: Chaos Theory (a menu) [S], Dark Deception's school level (an interaction) [S], and Red Dead Redemption 2's normal opening (a prompt) [S]. PUBG Mobile offers auto-open as a setting [V], and Fortnite does too according to third-party guides [S].

**3. Quiet opening is a separate mode, slow or partial [U].** MW2019 cracks the door silently with aim and Use [V], and a Twinfinite Warzone guide describes the slow open as standing still, pressing Use, then walking slowly into the door, mostly quietly [S]. Insurgency: Sandstorm lets you open carefully to stay quiet [V]. Outlast creaks a door open on a held button [V], and Outlast 2 opens slowly on a held Square [V]. Splinter Cell: Chaos Theory's stealthy open turns the knob slowly [S]. Hunt: Showdown doors open more quietly when crouched [S]. Ready or Not's "Peek Door" opens a door slightly [S]. Metal Gear Solid 3's careful approach opens a door more slowly [S]. Gamepressure names Ready or Not and Arizona Sunshine for door peeking [V].

**4. The bash is also a weapon, and some games let players turn it off [U].** A Call of Duty sprint bash can kill an enemy standing right behind the door [V]. Splinter Cell: Chaos Theory's Bash Door knocks out a guard right behind it [S]. The Outlast Trials' sprint bash slightly stuns enemies behind the door, but costs stamina and has a recovery [S]. In Apex Legends, melee or a shot on the handle-side stile can knock a door open [V]. Call of Duty later made the sprint bash a setting in the Vanguard alpha [V], Modern Warfare II [V] and Warzone Mobile [S].

**5. Closing is the hard part, and the best-documented answer takes it off the buttons [U].** TLOU2 tried letting the player close doors with held buttons and other schemes, and none worked [V]. Its doors instead close slowly by themselves in combat tension [V], stay open outside combat [V], and slam shut behind a violent pass [V]. In Warzone you can close a door by walking into it [V]. Visage's reviewer ended up walking into doors to close them [V]. Resident Evil 7's weighted doors close on their own [S]. Where closing stays on the hands, the notes record trouble: Outlast can close a door quietly only while still in the slow-open grab, so a fully open door can only be slammed [S]; an Amnesia reviewer found the controls got in the way when slamming doors while fleeing [S]; Tarkov's closing only nudged a door a little [V]; and PUBG Mobile's auto-open still needs a tap to close [V].

**6. Doors swing away from whoever pushes them [U].** TLOU2's doors always open away from the player, using double hinges [V], and Gamepressure describes the same choice [V]. Valve's doors swing away from whatever opened them unless blocked on that side [S], and an Arma 3 feedback request asks for doors that open both ways or always away from the player [S]. Elsewhere a one-way swing stays: Amnesia's one-way doors add tension under pursuit [V], Tarkov only lets you breach doors that open away from you [V], and Phasmophobia guides say to drag the other way if a door will not move [S].

**7. A closed door must cost the hunter something [U].** Line of sight: TLOU2 uses doors to block it [V]. Sound: Thief propagates sound through a room database [V], and in Deus Ex: Invisible War a closed solid door makes sound on the far side much softer, while frail doors block less and metal doors may block all of it [V]. Time: Left 4 Dead's infected must break closed doors [S]; Jason in Friday the 13th breaks locks fast and barricades with repeated swings [S]; closing doors impedes Outlast's enemies [S]; Amnesia players close doors behind them to slow monsters [V]; The Outlast Trials' enemies still break barricaded or locked doors, only more slowly [S]; and holding a door shut in Phasmophobia stops ghosts opening it [V]. Choice: in MW2019 a closed door can pause a firefight, because the attacker must bust through or reroute [V].

**8. Door noise must be selective [U].** In Resident Evil 2 (2019), a guide says slamming doors open draws Mr. X [V, guide], Steam players say opening doors while walking does not [S], and a developer thinks he ignores doors [V], so the notes mark his reaction UNCERTAIN. The Clea developer tried enemies that react to door creaks and scrapped it for causing too many interruptions [V]. Breaking doors draws the Beast in Amnesia: The Bunker [V]. In Dark Deception, opening the hospital's doors by walking near them alerts the Reaper Nurses [S]. Metal Gear Solid 3's bash oddly does not alert guards [S]. Whether door noise draws the alien in Alien: Isolation is disputed [S]. In Call of Duty the sprint bash is heard [V], and door audio cues make flanking hard [S].

**9. A door's state is information [U].** In Ready or Not, a door that will not swing means someone is behind it [V]. In Tarkov, a door left open tells others that someone has been there [S]. Thief's guards notice opened doors [V]. Dark Deception's nurses open doors, so doors show you where they are [S]. TLOU2 leaves doors open outside combat so you can see where you have been [V]. Visage opens, closes and slams doors as scares [V]. Classic Resident Evil uses the door transition itself for suspense [V].

## 4. Slamming without hands

Red's slam is the player turning back and slamming the door hard, with no button, only the player's movement against the door. This section lists what the notes found for that, and what they did not.

### What exists

1. **The door slams itself after a violent pass.** In TLOU2, the physics-based doors slam shut behind you when you sprint through them (tweet 6/13) [V]. Naughty Dog built a new engine physics object that the player can push and that can push the player (tweet 7/13) [V]. Zhuravlov's animation clip includes a heavy close among the door reactions [V].
2. **The player drives an open door shut by moving into it.** In Warzone you can close a door by walking into it instead of pressing the button, or by slamming it in the opposite direction (TheGamer) [V]. The notes read the slam as driving the open door back toward the frame by walking or running into it; the article, as the notes summarise it, does not name the input [U]. Visage's reviewer walked into doors to close them [V], which is a player workaround rather than a designed slam.
3. **Doors close themselves.** TLOU2's doors close slowly by themselves in combat tension [V]. Resident Evil 7's doors are weighted to close on their own [S], and a forgotten door shuts itself with an audible click [S].
4. **The game slams doors as a scare.** Visage's house opens and closes doors at random, and doors slam shut as scares [V]. Dark Deception's school doors shut during the Doom Ducky chase [S]. Phasmophobia's exit doors close and lock when a hunt starts [S].
5. **For contrast, slams done by hand.** In Outlast a quick press slams a door shut, and players use the slam to block pursuers [S]. In Amnesia you swing a grabbed door shut with the mouse [V], and a reviewer said the controls got in the way when frantically slamming doors while fleeing [S]; a right-click throw slam is UNVERIFIED [U]. Granny's fan pages say a slam attracts her [S, unreliable].

### What the notes did not find

- No game that slams a door when the player turns the camera or turns back.
- No description of how TLOU2's slam-back is tuned. The notes say only that the doors are physics-based and slam shut after a violent pass [V].
- No saloon doors: GTA V's were not found, and Red Dead Redemption 2's saloon-door physics are UNVERIFIED [U].

### The notes' reading

The notes' synthesis, which is a reading and not a source [U]:

- Two movement-based precedents fit "turn back and slam": the door's own swing-back after a violent pass (TLOU2), and driving an open door back toward the frame by walking or running into it (Warzone) [U].
- For double-acting doors on spring hinges, like real office doors, the first comes for free: a sprint pass swings the door wide, and the closer snaps it back [U].
- The second needs a damped "clap" as the door crosses the closed position, or the door swings straight through the frame [U].

## 5. Pitfalls and the fixes games used

The design writing in the notes treats doors as expensive. Liz England's "The Door Problem" (2014-04-21) lists the questions every role asks about a single door: can it be opened, how does the player know, is it locked and what unlocks it; can enemies or allies use it, does it lock behind you, and in co-op does it wait for both players; how big must it be; how does its sound depend on position and facing; and what about markers, lighting and network sync [V] (source 102). In a 2021 developer thread [V: Gigazine; PS LifeStyle; ppe.pl; Wikipedia "Door problem"] (sources 36, 39, 40, 103), Death Trash developer Stephan Hövelbrinks (@talecrafter, 2021-03-09) described doors as a dynamic funnel and block in pathfinding, possibly locked or destructible [S], and Kurt Margenau replied with the TLOU2 thread. Polygon (2021-03-09), IGN (2021-03-09), PC Gamer (2021-03-10) and The Verge (2021-03-12) covered it; all were blocked for the fetcher and are cited by Wikipedia [V citation] (sources 103, 107–110). The Verge quotes Owlchemy's Pete Galbraith saying door-opening animation must be very accurate for players to believe the physics [V via Wikipedia]. Gamepressure (2021-03-26) adds that Control's doors took more man-months than most abilities, that doors were the most complex object in South Park: The Fractured But Whole, and that Assassin's Creed mostly had no doors until Valhalla [V] (source 104). Vox's "Why video game doors are so hard to get right" (Edward Vega, 2021-09-07) features Bryan (or Brian) Singh (Uncharted 4, TLOU2 doors) [V title only]; Gigazine's summary of it gives a 0–3 difficulty scale, says real-size doors make third-person cameras clip so doors get oversized, and covers NPC door logic [V Gigazine] (sources 105, 106, 39). For MW2019 the only Infinity Ward source is the Activision blog [V], and no GDC talk or postmortem was found. For Naughty Dog there are only the tweets and press coverage; no GDC talk on doors was found [U], and a GDC Vault search for "doors" found no door talk.

1. **Body blocking: a player holds a door shut by standing beside it.** Warzone players push a door nearly shut and stand beside it so enemies cannot open it, and one variant made a player unkillable [V: Charlie Intel]. Fixes: in Call of Duty, explosives and concussions force the door open and shove the player [V]. Apex Legends lists what obstructs a door and lets melee destroy it [V]. Valve's door entities (the prop_door_rotating and func_door_rotating wiki pages) have an OnBlockedOpening output, an optional damage-to-blocker value, and a kill-blocker option noted for blast doors [S] (sources 120–122). Ready or Not turns the blocked swing into information: someone is behind the door [V].
2. **Doors shove, launch or trap the player.** In Gone Home (Unity; Fullbright programmer Johnnemann, 2012-07-02), physics doors plus an unstoppable player capsule meant that low doors and bottom drawers slid under the player and launched them, and players tore doors off their hinges and jammed them into walls [V]. Its fixes were to raise the step height so low doors are not treated as stairs, and to stop an opening door when it hits the player and hold it in place until the player clears [V] (source 112). TLOU2 built an engine object that can push the player and be pushed [V]. In Unity, a CharacterController is not moved by forces, and hinge doors jitter when wedged between the player capsule and a wall if the joint limit exceeds the wall stop [S: Unity forum]. The forum's fixes are to match the hinge limits to the physical stops, use IgnoreCollision between the door leaf and the player, balance the masses, or drive the door kinematically [S] (sources 114–116). For Unreal, an itch.io devlog (2023-05-06) [V] and Unreal Answers threads [S] give these fixes, without the notes saying which comes from which: pause or reverse the door on player contact; use a hinge collider that nudges the player each frame (stuck about 2 in 1000 tries); disable door collision while the door moves; or push the player out at the end of the swing. Doors too narrow for the capsule trap players [S] (sources 113, 117, 118).
3. **Wrong-way swings and doors into your face.** TLOU2 always opens doors away from the player [V]. Valve's doors swing away from whatever opened them unless blocked on that side [S] (sources 120–122). Bohemia feedback request T60981 for Arma 3 asks that doors open both ways or always away from the player, which is better for close quarters [S] (source 123). Elsewhere a one-way swing stays: Amnesia's one-way doors add tension under pursuit [V], Tarkov only lets you kick doors that open away from you [V], and Phasmophobia guides say to drag the other way if a door will not move [S].
4. **Accidental opening (auto-open, strafing past).** PUBG Mobile's "Auto open doors" (Settings > Basic) gets you in fast, but closing still needs a tap (Beebom, updated 2024-01-06) [V], and it can open a door at the wrong moment (Smartprix, 2020) [S] (sources 124, 125). Call of Duty made the sprint bash a toggle in the Vanguard alpha [V], Modern Warfare II [V] and Warzone Mobile [S]. Fortnite has an "Auto Open Doors" option according to third-party guides [S], but the only Epic help page found covers "Auto Open Containers" on mobile [S] (sources 126, 127). No source was found on false triggers from strafing past a door [U].
5. **Slow or sluggish doors under pressure.** A Tarkov streamer died waiting on a door [V]. In Outlast a fully open door cannot be closed gently [S]. Amnesia's controls got in the way while fleeing [S]. Visage's drag doors are awkward [V]. Red Dead Redemption 2's animation is heavy and committed [S]. The Outlast Trials' sprint bash has a recovery that can make it slower than walking the door open [S]. The notes record no fix for this. For comparison, TLOU2's opening slows the player only a little and never takes control away [V].
6. **Controllers and touch.** Phasmophobia's grab-and-drag doors do not map to a controller, because a held button is not a held mouse button (Steam mapping guides) [S]; an older guide says to hold RT and move the camera [S]. Splinter Cell: Chaos Theory lets the analog stick set how far the door swings [S]. Ready or Not's one key plus aim position means lining up on the knob or the kick zone [V]. On touch, PUBG Mobile has auto-open [V], and Warzone Mobile has the sprint-bash toggle [S].
7. **AI pathing and door state.** Hövelbrinks describes doors as dynamic funnels and blocks [S]. In Unreal, doors are set up as navmesh obstacles plus nav-link proxies whose "reached" event opens the door; the AI often reaches the link too late for a stop-and-turn animation, so the advice is to scan the path ahead [S], and a "disabled" link may only be given a high cost and still be used if it is the only route [S] (sources 117, 118). TLOU2 needed dedicated AI work [V]. The Witcher 3 had door-state bugs: a siege command closed every door in the game, and doors locked for boss fights failed to unlock in 12 cases; the fix was to stop locking them [V]. In Terminator: Resistance the Terminator breaches doors, so they never need reopening or closing; interrupting a breach would leave a half-broken door state [V] (source 104).
8. **Oscillation and jank.** Hinge joints jitter from mismatches between joint limits and colliders, and from mass ratios (Unity forum) [S] (sources 114, 116). No other source was found [U].
9. **Pace and camping.** Doors slowed Call of Duty and encouraged camping behind them [V]. Black Ops 7 reportedly drops them from 6v6 maps [V that the article says so; the leak itself is unconfirmed].
10. **Over-alerting.** The Clea developer prototyped enemies that react to door creaks; it caused too many interruptions and was scrapped [V] (source 82).

## 6. Gaps

- **No numbers to compare against.** The notes contain no hearing distances or speed thresholds from any game, so V5's 6 m, 18 m and 27 m and its 0.5 s window have no outside benchmark here.
- **No turn-back slam.** No game was found that slams a door when the player turns back (section 4). The input behind Warzone's "slam it in the opposite direction" is not stated in the notes, and how TLOU2's slam-back is tuned is not described.
- **The closest precedent is the weakest source.** Metal Gear Solid 3 (walk-into opening, slower careful opening, top-speed bash) rests on one StrategyWiki search summary, and the page returned 403 [S].
- **TLOU2 details.** Whether doors open with no button press is not confirmed [U]. Rumble is not documented [U]. Contact doors in Part I are UNVERIFIED [U]. No GDC talk on doors was found [U].
- **Call of Duty details.** Camera shake and rumble are not documented [U]. The Warzone Mobile blog fetch timed out [S]. The Black Ops 7 leak is unconfirmed. No GDC talk or postmortem was found.
- **Battlefield.** Neither walk-into pushing nor sprint bashing could be confirmed; Frostbite contact doors are only a recollection [U].
- **Rockstar.** GTA V's swing-back, auto-close and latch are UNVERIFIED [U], and its saloon doors were not found. Red Dead Redemption 2's hand-push animation and saloon-door physics are UNVERIFIED [U].
- **Horror AI and noise.** Mr. X's reaction to doors in Resident Evil 2 (2019) is UNCERTAIN because the sources disagree. Alien: Isolation's door noise is disputed [S], with no URL in the notes. Resident Evil Village: nothing found [U]. Granny is UNVERIFIED, with no URL. Ready or Not's noise and AI rules were not found [U].
- **Dark Deception, the primary reference,** has only fan-wiki search summaries [S] and no developer (Glowstick) source.
- **Other open points.** Sprint bashing in Insurgency: Sandstorm is not confirmed [U]. SWAT 4 is believed to have no kick [U]. No openable swing doors were found in Rainbow Six Siege [U]. Metal Gear Solid V's doors were not found [U]. Walk-into opening in Hitman is UNVERIFIED [U]. Amnesia's right-click throw slam is UNVERIFIED [U].
- **Pitfall coverage.** No source was found on false triggers from strafing past a door [U]. Oscillation and jank come only from the Unity forum [S].
- **Touch and controller.** The notes have only PUBG Mobile's auto-open [V], Warzone Mobile's bash toggle [S], Phasmophobia's controller mapping [S] and Splinter Cell's analog swing [S]. Nothing covers contact doors driven by a touch stick.
- **No video.** The Vox video, Zhuravlov's animation clip and all gameplay footage were not watched, so nothing here was checked against footage and there are no timestamps.
- **Loose ends in the notes.** Sources 13, 19, 41, 86, 111 and 119 are listed, but no claim cites them. Source 70 may be the Amnesia fan analysis, but the notes do not tie it to that claim. The Battlefield 4 guide claim is tagged [S], although the one Battlefield 4 guide URL (22) is listed as opened. The Call of Duty camping criticism cites "MP1st [S]" without saying which MP1st page. Three [S] claims have no URL: the Hövelbrinks tweet, the Alien: Isolation Steam dispute and the Granny fan pages. The Ready or Not "Peek Door" guide and lockpick have no named source.

## Sources

Every URL in research_raw.md, numbered in the notes' order and grouped as the notes group them. [V] means the page was opened; [S] means it was seen in search results only. Notes in parentheses come from the raw notes; notes after a dash are this page's.

### Call of Duty

1. [V] https://blog.activision.com/call-of-duty/2019-08/Modern-Warfare-Multiplayer-First-Look-New-Gameplay-Mechanics
2. [V] https://blog.activision.com/call-of-duty/2019-08/Modern-Warfare-Multiplayer-First-Look-Creating-Multiplayer-Maps
3. [V] https://mp1st.com/news/call-of-duty-modern-warfare-executions-realism-option-door-breaching-more-detailed
4. [V] https://jfk.nl/news/modern-warfare-call-of-duty-wordt-strategischer-dan-ooit/
5. [V] https://www.charlieintel.com/modern-warfare/call-of-duty-modern-warfare-introduces-new-gameplay-mechanics-55580/
6. [V] https://www.charlieintel.com/call-of-duty-warzone/new-door-lock-bug-in-warzone-makes-player-un-killable-63789/
7. [V] https://www.thegamer.com/how-to-lock-doors-in-warzone/
8. [V] https://www.dexerto.com/call-of-duty/modern-warfare-3-players-dread-return-of-doors-after-leak-2258681/
9. [V] https://mp1st.com/news/cod-vanguard-sees-return-of-doors-tactical-sprint-weapon-mounting-alpha-intro-surfaces
10. [V] https://digitaltrends.com/?p=3215906 ("The best Call of Duty: Modern Warfare II console settings")
11. [V] https://www.gfinityesports.com/article/black-ops-7-will-reportedly-not-feature-tactical-sprint-or-maps-with-interactive-doors
12. [S] https://twinfinite.net/guides/cod-warzone-open-doors-slowly/
13. [S] https://primagames.com/tips/how-lock-doors-modern-warfare-and-warzone - not cited by any claim in the notes
14. [S] https://callofduty.fandom.com/wiki/Tactical_Sprint
15. [S] https://www.kontrolfreek.com/blogs/kfb/call-of-duty-modern-warfare-ii-beta-controller-settings-best-weapons-and-new-features
16. [S] https://wzstats.gg/warzone/settings
17. [S] https://www.callofduty.com/blog/2024/03/call-of-duty-warzone-mobile-complete-control-plus-customization-controller-options
18. [S] https://upcomer.com/are-there-doors-on-the-remastered-modern-warfare-3-maps
19. [S] https://www.ggrecon.com/articles/doors-are-back-in-call-of-duty-vanguard-and-players-are-furious/ - not cited by any claim in the notes

### Battlefield

20. [V] https://www.hngn.com/articles/17108/20131111/battlefield-4-levolution-map-changes-learn-how-to-dynamically-effect-each-multiplayer-map-vol-2.htm
21. [V] https://pcgamesn.com/battlefield/battlefield-4-s-levolution-lets-you-flood-cities-block-alleys-shoot-out-power
22. [V] https://www.ludo.guide/guide/battlefield-4/levolution
23. [V] https://www.ea.com/games/battlefield/news/melee-combat-of-battlefield-1
24. [S] https://www.gamespot.com/articles/battlefield-1-heres-how-melee-weapons-work/1100-6443793/
25. [S] https://www.ea.com/games/battlefield/news/bullet-penetration-in-battlefield-5
26. [S] https://www.setup.gg/game/battlefield-6/battlefield-6-brings-back-full-destruction-and-fans-are-loving-it/

### GTA V and Red Dead Redemption 2

27. [V] https://docs.fivem.net/docs/assets-manual/beginner-series/part-8/
28. [V] https://forums.unrealengine.com/t/gta-5-door/88353
29. [V] https://www.gamerevolution.com/guides/450597-red-dead-redemption-2-kick-down-doors
30. [V] https://kotaku.com/the-fun-and-frustration-of-that-doctor-robbery-early-in-1830619045
31. [S] https://www.pcgamer.com/au/19-times-i-burst-through-doors-to-startle-people-in-red-dead-redemption-2
32. [S] https://steamcommunity.com/app/1174180/discussions/0/4555968543727036919
33. [S] https://amara.org/videos/GUFGvT5sAbYn/info/is-red-dead-redemption-2s-animation-too-slow
34. [S] https://mein-mmo.de/en/red-dead-redemption-2-experte-steuerung-traege,288771

### The Last of Us

35. [V] https://threadreaderapp.com/thread/1369342602177220609.html (Kurt Margenau thread, 2021-03-09)
36. [V] https://www.playstationlifestyle.net/2021/03/10/last-of-us-2-dev-on-in-game-doors/amp/
37. [V] https://kotaku.com/tlou-hbo-video-game-developer-doors-twitter-1850167827
38. [V] https://80.lv/articles/a-look-at-the-last-of-us-part-ii-s-door-animations
39. [V] https://gigazine.net/gsc_news/en/20210906-game-door
40. [V] https://www.ppe.pl/news/155302/The-Last-of-Us-2-i-problem-drzwi-Kurt-Margenau-wspomina-o-elemencie-ktory-zajal-deweloperom-najwiecej-czasu.html
41. [V] https://www.gameinformer.com/index.php/preview/2020/06/01/15-things-i-learned-from-two-hours-playing-the-last-of-us-part-ii - not cited by any claim in the notes
42. [S] https://twitter.com/kurtmargenau/status/1369342602177220609 (linked, not opened)
43. [S] https://twitter.com/MaksZhuravlov/status/1629668635839963136 (linked, not opened)
44. [S] https://www.gamespew.com/2022/08/the-last-of-us-part-i-review/

### Tactical shooters

45. [V] https://gamepretty.com/ready-or-not-how-to-clear-rooms-standard-operating-procedures/
46. [V] https://devtrackers.gg/ready-or-not/p/24e4746e-please-add-an-option-to-have-separate-key-bindings-for-opening-kicking-peeking-doors
47. [S] https://afkgaming.com/gaming/general/how-to-effectively-use-the-optiwand-in-ready-or-not
48. [S] https://store.epicgames.com/news/ready-or-not-guide-tactical-tips-squad-control-game-changing-mods
49. [S] https://steamcommunity.com/app/1144200/discussions/0/4036979414415343764
50. [S] https://giantbomb.com/wiki/Games/SWAT_4
51. [S] https://swat-4.fandom.com/wiki/Door_Wedge
52. [V] https://www.ubisoft.com/en-ca/game/rainbow-six/siege/news-updates/2RyPUypY9DdzDH2433hqTs/behind-the-wall-series-tools-of-defense
53. [S] https://rainbowsix.fandom.com/wiki/Destruction
54. [V] https://xboxplay.games/escape-from-tarkov/-how-to-breach-doors-easily-tips-and-tricks-5551
55. [V] https://www.dexerto.com/escape-from-tarkov/summit1g-calls-on-tarkov-devs-to-fix-annoying-issue-after-bizarre-death-1326782/
56. [S] https://progameguides.com/escape-from-tarkov/how-to-breach-doors/
57. [V] https://www.worthplaying.com/article/2018/12/7/news/112099-insurgency-sandstorm-all-open-beta-weekend-launched/
58. [S] https://www.weareplaystation.fr/communautes/playstation-4/actualites-communaute/insurgency-sandstorm-console-gameplay-overview-trailer-ps4
59. [S] https://www.cyberly.org/game-cheats/pc/insurgency-sandstorm.html
60. [V] https://www.dualshockers.com/hunt-showdown-level-design-dev-diary/
61. [S] https://reddit.birdcat.cafe/r/HuntShowdown/comments/8167b3/tips_and_tricks
62. [V] https://apexlegends.wiki.gg/wiki/Door

### Horror

63. [V] https://geekculture.co/geek-review-outlast-playstation-4
64. [V] https://www.ludo.guide/guide/outlast-2/outlast-2-part-5
65. [S] https://outlast.fandom.com/wiki/Outlast
66. [S] https://steamcommunity.com/app/238320/discussions/0/846963710940918803
67. [S] https://steamcommunity.com/app/238320/discussions/0/616198623980233624
68. [S] https://steamcommunity.com/app/1304930/discussions/0/4514379558395344424
69. [V] https://shacknews.com/article/65701/amnesia-the-dark-descent-review
70. [S] https://toiletovhell.com/video-game-career-analysis-frictional-games/ - not tied to a claim in the notes; may be the Amnesia fan analysis
71. [V] https://commonsensegamer.com/can-you-kill-the-monster-in-amnesia-the-bunker-and-how-to-avoid/
72. [S] https://steamcommunity.com/app/1944430/discussions/0/6163789601642586028
73. [S] https://www.pcgamer.com/uk/the-weighty-doors-and-switches-of-soma
74. [V] https://gameinformer.com/2021/03/08/ghosts-can-now-open-doors-in-new-phasmophobia-update
75. [S] https://attackofthefanboy.com/guides/phasmophobia-how-to-unlock-and-open-doors/
76. [S] https://gamepretty.com/phasmophobia-ps4-xb-controller-setup/
77. [V] https://waytoomany.games/2020/12/04/review-visage/
78. [V] https://www.thumbculture.co.uk/visage-time-to-face-some-ghosts
79. [S] https://gamesbeat.com/alien-isolation-preview/
80. [S] https://primagames.com/tips/alien-isolation-how-survive-alien
81. [V] https://www.gamerevolution.com/?p=486811 (RE2: Hide from Mr. X)
82. [V] https://invertmouse.itch.io/clea/devlog/66966/mr-xs-lessons
83. [S] https://steamcommunity.com/app/883710/discussions/0/2155476542891775832
84. [S] https://www.cheatcc.com/?p=114635 (RE7 achievements guide)
85. [S] https://twinfinite.net/ps4/resident-evil-7-biohazard-review/
86. [V] https://www.gamewatcher.com/resident-evil-7/review - not cited by any claim in the notes
87. [V] https://www.dreadcentral.com/editorials/481530/pathways-to-ruin-the-masterful-design-of-resident-evils-doors/
88. [S] https://dark-deception-game.fandom.com/wiki/Environmental_Hazards
89. [S] https://dark-deception-game.fandom.com/wiki/Stranger_Sewers
90. [S] https://dark-deception-game.fandom.com/wiki/Chapter_2:_Into_Madness
91. [S] https://gamepretty.com/?p=15201 (Dark Deception: Elementary Evil guide)
92. [S] https://primagames.com/eguides/left-4-dead-eguide/walkthrough/no-mercy/the-sewers
93. [S] https://left4dead.fandom.com/wiki/Bugs
94. [S] https://fridaythe13th.gamepedia.com/How_To_Play_Guide
95. [S] https://pcgamer.com/dying-light-2-lockpick-bash

### Stealth

96. [S] https://strategywiki.org/wiki/Metal_Gear_Solid_3:_Snake_Eater/Advanced_Moves
97. [S] https://gamespot.com/articles/tom-clancys-splinter-cell-chaos-theory-walkthrough/1100-6121234/
98. [S] https://www.gamespot.com/articles/tom-clancys-splinter-cell-chaos-theory-hands-on/1100-6106280/
99. [V] https://en.wikipedia.org/wiki/Thief:_The_Dark_Project
100. [V] http://www.gamebanshee.com/ka3h4 (Deus Ex: Invisible War dev diary #2)
101. [S] https://steamcommunity.com/app/1659040/discussions/0/4360125073497808725 (Hitman doors shot open)

### Design writing

102. [V] https://lizengland.com/blog/2014/04/the-door-problem/
103. [V] https://en.wikipedia.org/wiki/Door_problem
104. [V] https://www.gamepressure.com/editorials/why-are-game-developers-afraid-of-doors/z93ea
105. [V] https://www.youtube.com/watch?v=AYEWsLdLmcc (Vox; title only)
106. [S] https://www.vox.com/videos/22661048/video-game-development-door-difficulty-design
107. [S] https://www.polygon.com/2021/3/9/22322089/the-last-of-us-part-2-co-game-director-doors-design
108. [S] https://www.theverge.com/22328169/game-development-doors-design-difficult
109. [S] https://www.ign.com/articles/putting-doors-in-video-games-is-a-nightmare-say-developers
110. [S] https://www.pcgamer.com/why-game-developers-hate-doors/
111. [S] https://pokde.net/gaming/game-industry/game-developers-hate-making-doors - not cited by any claim in the notes

### Pitfalls and tech

112. [V] https://fullbright.company/2012/07/02/code-judo
113. [V] https://itch.io/blog/526846/animated-meshes-and-collision-overlap
114. [S] https://forum.unity.com/threads/how-to-stop-a-door-from-jerking-when-its-stuck-between-a-collider-and-my-character.903344/
115. [S] https://forum.unity.com/threads/push-to-open-door-making.287920/
116. [S] https://forum.unity.com/threads/i-figured-out-why-joints-are-jittering.527802/
117. [S] https://answers.unrealengine.com/questions/165188/view.html
118. [S] https://answers.unrealengine.com/questions/573448/view.html
119. [S] https://www.leadwerks.com/community/topic/12825-physics-based-doors/ - not cited by any claim in the notes
120. [S] https://developer.valvesoftware.com/wiki/Prop_door_rotating
121. [S] https://developer.valvesoftware.com/wiki/WiseOneWayPropDoor
122. [S] https://docs.cssharp.dev/api/CounterStrikeSharp.API.Core.PropDoorRotatingOpenDirection_e.html
123. [S] https://feedback.bistudio.com/T60981
124. [V] https://beebom.com/pubg-mobile-tips-tricks/amp/
125. [S] https://www.smartprix.com/bytes/best-pubg-mobile-tips-tricks
126. [S] https://gamersdecide.com/articles/fortnite-best-settings-1
127. [S] https://www.epicgames.com/help/fortnite-battle-royale-c-202300000001636/gameplay-c-202300000001721/how-do-i-turn-on-off-auto-open-containers-in-fortnite-mobile-a202300000086491
