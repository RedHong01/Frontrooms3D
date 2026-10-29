# 40 · 给 平面视觉 · 信息设计（HUD 钥匙 B，游戏视觉，2026-10-29 14:2x）

平面视觉 · 信息设计：HUD 钥匙 B（含 E·1 眼睛）安装包已就绪，规格、工程、可读性三项验证都是 READY。游戏视觉 落地、关卡设计 接上 HK1 + KP-1 之后进游戏。以下文件都在 research/ui_key_icon/hud_build/images/。
- 联系表 1：VL297_e1_eye_plan_1080.jpg，真实疏散图上的 E·1：成形、一次眨眼、门控、离开、特写反应、弱化设置、首把钥匙。VL298_locked_deny_stands_1080.jpg 和 VL298b_identity_matrix_1080.jpg：L3·D、D·3、famOut，3 形 × 3 色 × 3 场景，牌号 00 / B3 / B12。
- 联系表 2：VL299_unlock_d_b_1080.jpg（(d) 面板 U·3 + 准星挂锁 (b) / F·1）、VL300_broken_b4_1080.jpg、VL301b_panel_rest_tokens.jpg、VL303b_g6_prompt_keyline.jpg（G6 描边）。请审 VL301c_kp1b_instant_pickup.jpg（KP-1b：钥匙在手持位停 0.5 s，再飞入面板，桌面左下、手机左上）。
- 视频（60 fps，最近邻放大）：VL297_e1_eye_gate_1080.mp4、VL297_e1_closeup_1080.mp4、VL298_lock_deny_lit_1080.mp4、VL299_unlock_panel_u3_1080.mp4、VL299_unlock_crosshair_1080.mp4、VL300_broken_b4_1080.mp4、VL301_pickup_p4_1080.mp4。
- 请决定 O-35(a)：第二行（Plex Mono 13 / 11）的描边保持 1 px（默认），还是跟 G6 一起改成 2 px？
- O-35(b)：手机提示行现在是 1 pt（#23，默认）。改成 2 pt（= 6 px）后，Unity 的四向 Outline 会和 Bayon 17 的笔画分开，看起来像重影（见 VL303b 的 "2 pt (rejected)" 行）。如果仍要 2 pt，只改 FrontRoomsHudStyle 里的一个常量。
- O-35(c)：桌面 2 px 时，Plex "·" 周围的四份描边会挤成一小团深色（VL303b，READ PLAN）。保留，还是让 "·" 单独用 1 px？
- T-3（和 交互设计 一起定）：手机上钥匙 HUD 接管提示行时（LOCKED / UNLOCK / BROKEN / READ / EXAMINE），提示行前的小 USE 图标默认隐藏，因为 USE 键已经显示动词。如果要放在第一行前面，我们补一个 getter（21_contract_touch.md）。
- O-28：USE 动词 READ / EXAMINE 是按 InspectKind 分开（物件特写 = EXAMINE），还是统一用一个？这只影响触屏动词，眼睛不变。
- G2 牌号请在 VL298b 和 VL301b 上核对：00、B1 到 B9 用 Courier Prime Regular，B10 起缩到 0.8×，基线不变。
- 仍是参数、尚未定的项：P4 起始倍数 3× / 1.117×，要等 D1.6 的真钥匙模型做好再测 measure.json；E·3 低垂视线只作备用参数（#30）。P4 的方向（#35）由 总控 去问 Red，不在本表。
- 另：瞄准在门边以 10 Hz 抖动时，圆点每秒开合约 20 次。这是玩家输入引起的，不是 HUD 自己闪。游戏视觉 打算后续加一个最短停留时间；如果你们对这段动效有要求，请写进 handoff。
