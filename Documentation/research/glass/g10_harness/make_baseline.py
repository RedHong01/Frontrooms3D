"""Hidden/FrontRooms/GlassBaseline = FrontRooms/Glass with every [G14-HOOK-BEGIN]..[G14-HOOK-END] block removed."""
import sys, re
src, dst = sys.argv[1], sys.argv[2]
out, skip, blocks = [], False, 0
for line in open(src, encoding="utf-8"):
    if "[G14-HOOK-BEGIN]" in line:
        assert not skip; skip = True; blocks += 1; continue
    if "[G14-HOOK-END]" in line:
        assert skip; skip = False; continue
    if not skip: out.append(line)
assert not skip
text = "".join(out)
assert text.count('Shader "FrontRooms/Glass"') == 1
text = text.replace('Shader "FrontRooms/Glass"', 'Shader "Hidden/FrontRooms/GlassBaseline"')
assert "_FR_GlassRT" not in text and "_RTReceive" not in text and "_FR_GLASS_RT" not in text
open(dst, "w", encoding="utf-8").write(text)
print("blocks removed", blocks, "lines", len(out))
