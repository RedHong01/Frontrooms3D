# Localization terminology audit (平面视觉, 2026-10-31)

**Red, 10-30 23:55 (平面视觉 chat, account B):** check the terminology in every language; the translations must not be literal. His example was the zh-Hant settings tabs 畫面 舒適 操作 輔助.

The tables belong to 设置菜单与画质预设, which lands these rows. The English source stays 叙事's.

## Scope of this pass

- **Covered:** BRIEF priorities 1–3 (57 rows) in all nine translated languages: settings tabs and rows, menu and game verbs, and the game's nouns. A scripted check also confirmed that the core nouns are translated the same way across all 337 rows.
- **Not covered:** priority 4, the line-by-line review of every other row.
- **Evidence:**
  - Platform terms (Apple, Microsoft, PlayStation, Adobe) are given as their shipped UIs name them. They were not re-checked online in this pass.
  - Claims about how games word things are marked UNVERIFIED. Account B's six research agents stopped at its weekly limit before writing any output.
  - **A native player of each language should confirm before release.**
- **Files:**
  - `terminology_changes.tsv`: 99 rows, LF. Columns: lang, english, current, proposed, severity, reason, evidence.
  - Per-language tables: `~/FrontRoomsVisualWork/terminology/out/<code>.tsv`.

| Language | Rows to change | wrong / literal / inconsistent / minor |
|---|---|---|
| 简体中文 | 13 | 1 / 9 / 2 / 1 |
| 繁體中文 | 13 | 1 / 9 / 2 / 1 |
| 日本語 | 9 | 2 / 4 / 0 / 3 |
| 한국어 | 6 | 1 / 4 / 0 / 1 |
| Русский | 9 | 3 / 4 / 1 / 1 |
| Español (neutral) | 13 | 1 / 9 / 1 / 2 |
| Português (Brasil) | 12 | 1 / 10 / 1 / 0 |
| Deutsch | 15 | 0 / 12 / 1 / 2 |
| Français | 9 | 0 / 6 / 1 / 2 |

## What matters most

1. **ASSIST in Chinese is 辅助 / 輔助.** To many players that word means cheat software (开辅助). Change it to 辅助功能 / 輔助功能; the platforms name Accessibility 辅助功能 (Apple and Steam, Simplified). For zh-Hant, the Microsoft and PlayStation word 協助工具 is the alternative.
2. **Red's tab example (zh-Hant, and the same in zh-Hans):** 畫面 stays (games say 畫面), 舒適 → 舒適度, 操作 stays, 輔助 → 輔助功能.
3. **LOOK SPEED** is translated as "speed" everywhere, but games call it sensitivity: 灵敏度 / 靈敏度, 感度, 감도, ЧУВСТВИТЕЛЬНОСТЬ, SENSIBILIDAD, SENSIBILIDADE, EMPFINDLICHKEIT, SENSIBILITÉ.
4. **CAMERA MOTION** is translated as "camera movement", but the row turns off sway and shake. Proposed: 镜头晃动, カメラの揺れ, 카메라 흔들림, ТРЯСКА КАМЕРЫ, BALANCEO, BALANÇO, KAMERAWACKELN, TREMBLEMENT.
5. **Wrong meanings:**
   - pt-BR BLOOM = BRILHO, which is Brightness (Apple pt-BR).
   - ru UPSCALING = РАСТЯЖЕНИЕ ("stretching").
   - ru QUIT TO TITLE = ЗАСТАВКА ("splash or screensaver").
   - es LOCKED = CON LLAVE ("with a key").
   - ja CAUGHT = 捕獲 (reads as capturing a creature).
   - FRAME RATE is cut to "frames" in ja, ko, ru, es and pt-BR.
6. **One word, two actions on the USE disc:** es and fr use the same word for UNLOCK and OPEN (ABRIR / OUVRIR). Proposed: DESBLOQUEAR / DÉVERROUILLER.
7. **Two English terms, one translation:** zh-Hant PRESET and DEFAULT are both 預設; proposed PRESET → 預設集 (Adobe's word). pt-BR's desktop key line says O  AJUSTES, while every other SETTINGS is CONFIGURAÇÕES.

## For the settings line: width

On the USE disc (guide about 10 Latin letters or 4 CJK characters), four proposals run long:

| Language | Proposed | Letters | Keep if it doesn't fit |
|---|---|---|---|
| es | DESBLOQUEAR | 11 | ABRIR |
| de | UNTERSUCHEN | 11 | PRÜFEN |
| de | EINSCHLAGEN | 11 | BRECHEN |
| fr | DÉVERROUILLER | 13 | OUVRIR |

Three segment cells have short forms ready:

| Language | Proposed | Short form |
|---|---|---|
| ru | ПОЛЬЗОВАТЕЛЬСКИЙ | СВОЙ |
| de | BENUTZERDEFINIERT | EIGENE |
| es | TASA DE FOTOGRAMAS | FPS |

## Not settled (options)

- **zh-Hant YOU ARE HERE:** 您在此處 or 現在位置 (both appear on Taiwanese maps).
- **es "stick":** the table says palanca; PlayStation's Spanish says joystick.
- **ko COMFORT:** 편의 (convenience) may stay, or become 편의성.
- **de UNLOCK:** AUFSPERREN is southern / Austrian; AUFSCHLIESSEN is standard but 13 letters.
