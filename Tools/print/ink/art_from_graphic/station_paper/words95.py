"""The station paper's words in nine languages, read from research/narrative/95_world_text_localization.md §5.3 (叙事设计's
signed AI drafts, Red 11-03 "全部按推荐"), plus the rows the game's tables already hold (95 §5: tables win). words(lang) -> {english: word}."""
import os, re
ROOT = os.path.expanduser("~/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/")
DOC = ROOT + "Documentation/research/narrative/95_world_text_localization.md"
LANGS = ["zh-Hans", "zh-Hant", "ja", "ko", "de", "fr", "es", "pt-BR", "ru"]


def _section(text, head):
    i = text.index(head); j = text.find("\n### ", i + 1); k = text.find("\n## ", i + 1)
    ends = [e for e in (j, k) if e > 0]
    return text[i:min(ends) if ends else len(text)]


def words(lang):
    text = open(DOC, encoding="utf-8").read()
    out = {}
    for head in ("### 5.3", "## 5. Glossary"):
        sec = _section(text, head)
        for line in sec.splitlines():
            if not line.startswith("| ") or line.startswith("| English") or line.startswith("|---"): continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) != 10: continue
            m = re.findall(r"`([^`]+)`", cells[0])
            if not m: continue
            en = m[0]; tx = cells[1 + LANGS.index(lang)]
            ep = [e.strip() for e in en.split(" · ")]; tp = [t.strip() for t in tx.split(" · ")]
            if len(ep) > 1 and len(ep) == len(tp):
                for a, b in zip(ep, tp): out.setdefault(a, b)
            else:
                out.setdefault(en, tx)
    # the game's tables win over the doc for a word they share (95 §5)
    tab = {}
    for line in open(ROOT + f"Assets/Resources/Language/{lang}.txt", encoding="utf-8"):
        if line.startswith("#"): continue
        f = line.rstrip("\n").split("\t")
        if len(f) >= 2 and f[1]: tab[f[0]] = f[1]
    for k in list(out):
        if k in tab: out[k] = tab[k]
    for k in ("STAIR A", "SECURITY DESK", "PLEASE SIGN IN"):
        if k in tab: out[k] = tab[k]
    return out


if __name__ == "__main__":
    import sys
    w = words(sys.argv[1] if len(sys.argv) > 1 else "de")
    for k in sorted(w): print(f"{k!r:45} -> {w[k]}")
