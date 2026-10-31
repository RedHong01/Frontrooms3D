# Terminal face (C20): FR816 C20

The office terminal's 8 x 16 bitmap face, drawn by 平面视觉 (Red 2026-10-30, C20 = 1). Spec: `Documentation/research/computer_ui/45_c20_face.md` (envelope: `41_c19_period_interaction.md` §6).

- `face_c20.py`: every glyph as text art, the two block alphabets, the atlas writer and the metric / coverage check.
- Rebuild: `/usr/bin/python3 Tools/terminal_face/face_c20.py <out_dir>`, then copy `<out_dir>/TerminalFace.png` over `Assets/Resources/Terminal/TerminalFace.png` (keep its `.meta`: readable, point, uncompressed, no mips). Write `<out_dir>` outside the project.
- Previews (tube simulation, specimen, the comparison sheet) live outside the repo in `~/FrontRoomsVisualWork/c20_face/` (`preview.py`, `compare.py`).
