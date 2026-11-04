"""Terminal faces per language (W18): C20 with the CP850 Latin and CP866 Cyrillic letters (平面视觉, 2026-11-03).

Red, 11-03 16:43 (叙事设计 chat): "...也就是所有的diegetic narratives 需要adapt 语言的变化。" A 1990 PC kept its 8 x 16
face and added the code page's letters (research/localization/WORLD_TYPE.md 3b): CP850 for de, fr, es and pt-BR, CP866
for ru. This file draws them in C20's hand (face_c20.py: caps rows 2-12, x-height 5-12, stems 2 px, horizontals 1 px,
text in columns 0-6) and writes one face per language in the layout FrontRoomsTerminalFace.cs reads (its summary):

  <code>.png   128 px wide, white ink on black: the language's 8 x 16 slots first (slot k at x = (k % 16) * 8,
               y = (k // 16) * 16), then the big-type bands
  <code>.json  {"half": "<the slots' characters in order>", "wide": "", "big": [{"role", "mode", "chars", "w", "h",
               "top", "perRow"}]}

Accents follow the VGA's CP850 glyphs (the 1990 PC's own font): over a lowercase letter in rows 2-3 (the i dot's rows,
row 4 clear); over a capital in rows 0-1, the capital one row shorter (rows 3-12, row 2 clear); cedillas and the
Cyrillic tails in the descender rows. The Latin languages share one CP850 set; ru has CP866's full alphabet.

Big type (MAIN's headline and second line, 41 section 3.3): C20's block letters stand two cells a pixel, so a line holds
6 headline letters or 8 second-line characters. A language whose words run longer sets that line condensed, one
column a pixel, as a "halfblock" band (the same block shapes at half-row steps; an accented capital is shortened at
half-row steps with its accent inside the line's rows, as the VGA did). ru draws its own block alphabets: the headline
at two columns a pixel and one column apart, the second line 5 x 5 (Cyrillic needs five columns for Ж, Ш, Щ, Ю).

Run: /usr/bin/python3 face_lang.py [out_dir]   (default ~/FrontRoomsVisualWork/terminal_faces/out, outside the project)
"""
import json
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import face_c20 as C20  # noqa: E402  C20's glyphs (G, BIG, SMALL, glyph_cell)

BLANK = '........'


def c20(ch):
    """C20's 16-row cell for a printable ASCII character."""
    return list(C20.glyph_cell(ord(ch)))


def art(top, drawing):
    """A 16-row cell from a drawing whose first line is row `top` (lines split on white space; '#' ink, '.' clear)."""
    rows = [BLANK] * 16
    for i, line in enumerate(drawing.split()):
        rows[top + i] = (line + BLANK)[:8]
    return rows


def ink(*cells):
    """The cells laid over each other."""
    return [''.join('#' if any(c[y][x] == '#' for c in cells) else '.' for x in range(8)) for y in range(16)]


def clear(cell, *ys):
    return [BLANK if y in ys else r for y, r in enumerate(cell)]


def shorten(cell, row):
    """A capital one row shorter (rows 3-12), as the VGA's CP850 capitals are: `row` goes, the rows above move down."""
    rows = list(cell)
    del rows[row]
    return [BLANK] + rows


def mirror(cell):
    """Left for right inside columns 0-6."""
    return [''.join(r[6 - x] for x in range(7)) + r[7] for r in cell]


def turn(cell):
    """Half a turn inside rows 2-12 and columns 0-6 (the Spanish ¿ ¡ from ? !)."""
    m = mirror(cell)
    return [BLANK, BLANK] + [m[14 - y] for y in range(2, 13)] + [BLANK] * 3


# ------------------------------------------------------------------ accents (two rows each)
MARK = {
    'acute': '...##... ..##....',
    'grave': '..##.... ...##...',
    'circ': '..###... .##.##..',
    'diaer': '.##.##.. .##.##..',
    'diaer_i': '##..##.. ##..##..',   # around the narrow i and I (stem in columns 2-3)
    'tilde': '.###.##. ##.###..',
    'breve': '.##.##.. ..###...',
}
CEDILLA = '...##... ....##.. .####...'   # rows 13-15, from the foot of c and C


def low(base, mark):
    """A lowercase letter with its accent in rows 2-3."""
    return ink(base, art(2, MARK[mark]))


def cap(base, drop, mark):
    """A capital one row shorter with its accent in rows 0-1."""
    return ink(shorten(base, drop), art(0, MARK[mark]))


DOTLESS_I = clear(c20('i'), 2, 3)
DROP = {'A': 4, 'E': 3, 'I': 3, 'O': 3, 'U': 2, 'N': 7}   # the row each capital gives up under an accent


# ------------------------------------------------------------------ CP850: the letters of de, fr, es and pt-BR
LATIN = {}
for ch, (base, mark) in {
    'À': ('A', 'grave'), 'Á': ('A', 'acute'), 'Â': ('A', 'circ'), 'Ã': ('A', 'tilde'), 'Ä': ('A', 'diaer'),
    'È': ('E', 'grave'), 'É': ('E', 'acute'), 'Ê': ('E', 'circ'), 'Ë': ('E', 'diaer'),
    'Í': ('I', 'acute'), 'Î': ('I', 'circ'), 'Ï': ('I', 'diaer_i'), 'Ñ': ('N', 'tilde'),
    'Ó': ('O', 'acute'), 'Ô': ('O', 'circ'), 'Õ': ('O', 'tilde'), 'Ö': ('O', 'diaer'),
    'Ù': ('U', 'grave'), 'Ú': ('U', 'acute'), 'Û': ('U', 'circ'), 'Ü': ('U', 'diaer'),
}.items():
    LATIN[ch] = cap(c20(base), DROP[base], mark)
LATIN['Ç'] = ink(c20('C'), art(13, CEDILLA))
for ch, (base, mark) in {
    'à': ('a', 'grave'), 'á': ('a', 'acute'), 'â': ('a', 'circ'), 'ã': ('a', 'tilde'), 'ä': ('a', 'diaer'),
    'è': ('e', 'grave'), 'é': ('e', 'acute'), 'ê': ('e', 'circ'), 'ë': ('e', 'diaer'),
    'ñ': ('n', 'tilde'), 'ó': ('o', 'acute'), 'ô': ('o', 'circ'), 'õ': ('o', 'tilde'), 'ö': ('o', 'diaer'),
    'ù': ('u', 'grave'), 'ú': ('u', 'acute'), 'û': ('u', 'circ'), 'ü': ('u', 'diaer'), 'ÿ': ('y', 'diaer'),
}.items():
    LATIN[ch] = low(c20(base), mark)
for ch, mark in (('í', 'acute'), ('î', 'circ'), ('ï', 'diaer_i')):
    LATIN[ch] = ink(DOTLESS_I, art(2, MARK[mark]))
LATIN['ç'] = ink(c20('c'), art(13, CEDILLA))
LATIN['ß'] = art(2, """
.####...
##..##..
##..##..
##..##..
##.##...
##..##..
##...##.
##...##.
##...##.
##..##..
##.##...
""")
LATIN['°'] = art(2, '..###... .##.##.. .##.##.. ..###...')
LATIN['º'] = art(2, '..###... .##.##.. .##.##.. ..###... ........ .#####..')
LATIN['ª'] = art(2, '.####... ....##.. .#####.. ##..##.. .#####.. ........ ######..')
LATIN['«'] = art(6, '..##.##. .##.##.. ##.##... .##.##.. ..##.##.')
LATIN['»'] = art(6, '##.##... .##.##.. ..##.##. .##.##.. ##.##...')
LATIN['¿'] = turn(c20('?'))
LATIN['¡'] = turn(c20('!'))
LATIN['·'] = art(8, '..##.... ..##....')
LATIN['§'] = art(2, """
.#####..
##...##.
##......
.####...
##..##..
##...##.
.##..##.
..####..
.....##.
##...##.
.#####..
""")


# ------------------------------------------------------------------ CP866: the Russian alphabet
CYR = {}
for ch, latin in zip('АВЕКМНОРСТХ', 'ABEKMHOPCTX'):   # the capitals Cyrillic shares with Latin
    CYR[ch] = c20(latin)
for ch, latin in zip('аеорсухк', 'aeopcyx'):            # and the lowercase
    CYR[ch] = c20(latin)
CYR['к'] = clear(c20('k'), 2, 3, 4)                     # k without its ascender
CYR['З'] = c20('3')
CYR['Я'] = mirror(c20('R'))
CYR['Ё'] = cap(c20('E'), DROP['E'], 'diaer')
CYR['ё'] = low(c20('e'), 'diaer')
CAPS = {   # rows 2-12 (Д Ц Щ to 14)
    'Б': '####### ##...... ##...... ##...... ##...... ######.. ##...##. ##...##. ##...##. ##...##. ######..',
    'Г': '#######. ' + '##...... ' * 10,
    'Д': '..#####. ..##.##. ..##.##. ..##.##. ..##.##. ..##.##. .##..##. .##..##. ##...##. ##...##. #######. '
         '##...##. ##...##.',
    'Ж': '##.#.##. ##.#.##. .#.#.#.. .#.#.#.. ..###... ..###... ..###... .#.#.#.. .#.#.#.. ##.#.##. ##.#.##.',
    'И': '##...##. ##...##. ##..###. ##.####. ####.##. ###..##. ##...##. ##...##. ##...##. ##...##. ##...##.',
    'Л': '..#####. ' + '..##.##. ' * 7 + '.##..##. .##..##. ##...##.',
    'П': '#######. ' + '##...##. ' * 10,
    'У': '##...##. ' * 5 + '.######. .....##. .....##. .....##. ##...##. .#####..',
    'Ф': '...#.... .#####.. ' + '##.#.##. ' * 5 + '.#####.. ...#.... ...#.... ...#....',
    'Ц': '##..##.. ' * 10 + '#######. .....##. .....##.',
    'Ч': '##...##. ' * 5 + '.######. ' + '.....##. ' * 5,
    'Ш': '##.#.##. ' * 10 + '#######.',
    'Щ': '##.#.##. ' * 10 + '#######. .....##. .....##.',
    'Ъ': '###..... ' + '.##..... ' * 4 + '.#####.. ' + '.##..##. ' * 4 + '.#####..',
    'Ы': '##...##. ' * 5 + '####.##. ' + '##.#.##. ' * 4 + '####.##.',
    'Ь': '##...... ' * 5 + '######.. ' + '##...##. ' * 4 + '######..',
    'Э': '.#####.. ##...##. .....##. .....##. .....##. ..#####. .....##. .....##. .....##. ##...##. .#####..',
    'Ю': '##..##.. ' + '##.#..#. ' * 4 + '####..#. ' + '##.#..#. ' * 4 + '##..##..',
}
for ch, drawing in CAPS.items():
    CYR[ch] = art(2, drawing)
CYR['Й'] = cap(CYR['И'], 10, 'breve')
SMALLS = {   # rows 5-12 (д ц щ to 14), б from row 2, ф rows 2-15
    'б': '..#####. .##..... ##...... ######.. ' + '##...##. ' * 6 + '.#####..',
    'в': '######.. ##...##. ##...##. ######.. ##...##. ##...##. ##...##. ######..',
    'г': '######.. ' + '##...... ' * 7,
    'д': '..#####. ..##.##. ..##.##. .##..##. .##..##. ##...##. ##...##. #######. ##...##. ##...##.',
    'ж': '##.#.##. .#.#.#.. .#.#.#.. ..###... ..###... .#.#.#.. .#.#.#.. ##.#.##.',
    'з': '.#####.. ##...##. .....##. ..####.. .....##. .....##. ##...##. .#####..',
    'и': '##...##. ##...##. ##..###. ##.####. ####.##. ###..##. ##...##. ##...##.',
    'л': '..#####. ' + '..##.##. ' * 5 + '.##..##. ##...##.',
    'м': '##...##. ###.###. #######. ##.#.##. ##...##. ##...##. ##...##. ##...##.',
    'н': '##...##. ' * 3 + '#######. ' + '##...##. ' * 4,
    'п': '#######. ' + '##...##. ' * 7,
    'т': '######.. ' + '..##.... ' * 7,
    'ф': '...#.... ' * 3 + '.#####.. ' + '##.#.##. ' * 6 + '.#####.. ' + '...#.... ' * 3,
    'ц': '##..##.. ' * 7 + '#######. .....##. .....##.',
    'ч': '##...##. ' * 4 + '.######. ' + '.....##. ' * 3,
    'ш': '##.#.##. ' * 7 + '#######.',
    'щ': '##.#.##. ' * 7 + '#######. .....##. .....##.',
    'ъ': '###..... .##..... .##..... .#####.. .##..##. .##..##. .##..##. .#####..',
    'ы': '##...##. ' * 3 + '####.##. ' + '##.#.##. ' * 3 + '####.##.',
    'ь': '##...... ' * 3 + '######.. ' + '##...##. ' * 3 + '######..',
    'э': '.#####.. ##...##. .....##. ..#####. .....##. .....##. ##...##. .#####..',
    'ю': '##..##.. ##.#..#. ##.#..#. ####..#. ##.#..#. ##.#..#. ##.#..#. ##..##..',
    'я': '.######. ##...##. ##...##. .######. ..##.##. .##..##. ##...##. ##...##.',
}
for ch, drawing in SMALLS.items():
    CYR[ch] = art(2 if ch in 'бф' else 5, drawing)
CYR['й'] = ink(CYR['и'], art(2, MARK['breve']))
CYR['№'] = art(2, '#..#.### ##.#.#.# #.##.### #..#.... #..#.### ' + '#..#.... ' * 6)   # N + raised o and bar (8 columns)
CYR['°'] = LATIN['°']
CYR['·'] = LATIN['·']

CP850_ORDER = 'ÀÁÂÃÄÇÈÉÊËÍÎÏÑÓÔÕÖÙÚÛÜàáâãäçèéêëíîïñóôõöùúûüÿß°ºª«»¿¡·§\u00a0'   # NBSP last (0xFF in CP850 and CP866): a blank slot, so a word can be held to the next
CP866_ORDER = ('АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ'
               'абвгдеёжзийклмнопрстуфхцчшщъыьэюя№°·§\u00a0')   # § is CP866's 0x15 too; the memos use it


# ------------------------------------------------------------------ big type (41 section 3.3, W18)
BIG_RU = {   # 5 x 7, the Russian headline's capitals (the shared ones are C20's)
    'Б': "#####/#..../#..../####./#...#/#...#/####.", 'Г': "#####/#..../#..../#..../#..../#..../#....",
    'Д': ".###./.#.#./.#.#./.#.#./.#.#./#####/#...#", 'Ж': "#.#.#/#.#.#/.###./..#../.###./#.#.#/#.#.#",
    'З': "####./....#/....#/.###./....#/....#/####.", 'И': "#...#/#...#/#..##/#.#.#/##..#/#...#/#...#",
    'Л': "..###/.#..#/.#..#/.#..#/.#..#/.#..#/#...#", 'П': "#####/#...#/#...#/#...#/#...#/#...#/#...#",
    'У': "#...#/#...#/#...#/.####/....#/#...#/.###.", 'Ф': "..#../.###./#.#.#/#.#.#/#.#.#/.###./..#..",
    'Ц': "#..#./#..#./#..#./#..#./#..#./#####/....#", 'Ч': "#...#/#...#/#...#/.####/....#/....#/....#",
    'Ш': "#.#.#/#.#.#/#.#.#/#.#.#/#.#.#/#.#.#/#####", 'Щ': "#.#.#/#.#.#/#.#.#/#.#.#/#.#.#/#####/....#",
    'Ъ': "##.../.#.../.#.../.###./.#..#/.#..#/.###.", 'Ы': "#...#/#...#/#...#/##..#/#.#.#/#.#.#/##..#",
    'Ь': "#..../#..../#..../####./#...#/#...#/####.", 'Э': ".###./#...#/....#/..###/....#/#...#/.###.",
    'Ю': "#..#./#.#.#/#.#.#/###.#/#.#.#/#.#.#/#..#.", 'Я': ".####/#...#/#...#/.####/..#.#/.#..#/#...#",
}
for ch, latin in zip('АВЕКМНОРСТХ', 'ABEKMHOPCTX'):
    BIG_RU[ch] = C20.BIG[latin]
SECOND_RU = {   # 5 x 5, the Russian second line (four columns cannot hold Ж Ш Щ Ю)
    'А': ".###./#...#/#####/#...#/#...#", 'Б': "#####/#..../####./#...#/####.", 'В': "####./#...#/####./#...#/####.",
    'Г': "#####/#..../#..../#..../#....", 'Д': ".###./.#.#./.#.#./#####/#...#", 'Е': "#####/#..../####./#..../#####",
    'Ж': "#.#.#/.###./..#../.###./#.#.#", 'З': "####./....#/.###./....#/####.", 'И': "#...#/#..##/#.#.#/##..#/#...#",
    'К': "#...#/#..#./###../#..#./#...#", 'Л': "..###/.#..#/.#..#/.#..#/#...#", 'М': "#...#/##.##/#.#.#/#...#/#...#",
    'Н': "#...#/#...#/#####/#...#/#...#", 'О': ".###./#...#/#...#/#...#/.###.", 'П': "#####/#...#/#...#/#...#/#...#",
    'Р': "####./#...#/####./#..../#....", 'С': ".###./#...#/#..../#...#/.###.", 'Т': "#####/..#../..#../..#../..#..",
    'У': "#...#/#...#/.####/....#/####.", 'Ф': ".###./#.#.#/#.#.#/.###./..#..", 'Х': "#...#/.#.#./..#../.#.#./#...#",
    'Ц': "#..#./#..#./#..#./#####/....#", 'Ч': "#...#/#...#/.####/....#/....#", 'Ш': "#.#.#/#.#.#/#.#.#/#.#.#/#####",
    'Щ': "#.#.#/#.#.#/#.#.#/#####/....#", 'Ъ': "##.../.#.../.###./.#..#/.###.", 'Ы': "#...#/#...#/##..#/#.#.#/##..#",
    'Ь': "#..../#..../####./#...#/####.", 'Э': ".###./#...#/..###/#...#/.###.", 'Ю': "#..#./#.#.#/###.#/#.#.#/#..#.",
    'Я': ".####/#...#/.####/.#..#/#...#",
    '0': ".###./#..##/#.#.#/##..#/.###.", '1': "..#../.##../..#../..#../.###.", '2': "####./....#/.###./#..../#####",
    '3': ".###./....#/..##./....#/.###.", '4': "#..#./#..#./#####/...#./...#.", '5': "#####/#..../####./....#/####.",
    '6': ".###./#..../####./#...#/.###.", '7': "#####/....#/...#./..#../..#..", '8': ".###./#...#/.###./#...#/.###.",
    '9': ".###./#...#/.####/....#/.###.",
    '.': "...../...../...../...../..#..", ',': "...../...../...../..#../.#...", ':': "...../.#.../...../.#.../.....",
    '-': "...../...../.###./...../.....", '/': "....#/...#./..#../.#.../#....", "'": "..#../..#../...../...../.....",
    '!': "..#../..#../..#../...../..#..", '?': ".###./#...#/..##./...../..#..",
}
HALF_MARK = {   # an accent at half-row steps over a block capital: (rows of the accent, by alphabet width)
    'acute': {5: ['...#.', '..#..'], 4: ['..#.', '.#..']},
    'grave': {5: ['.#...', '..#..'], 4: ['.#..', '..#.']},
    'circ': {5: ['..#..', '.#.#.'], 4: ['.##.', '#..#']},
    'tilde': {5: ['.##.#', '#.##.'], 4: ['.#.#', '#.#.']},
    'breve': {5: ['#...#', '.###.'], 4: ['#..#', '.##.']},
    'diaer': {5: ['.#.#.'], 4: ['#..#']},
}
ACCENTS = {   # accented capital -> (base, accent)
    'À': ('A', 'grave'), 'Á': ('A', 'acute'), 'Â': ('A', 'circ'), 'Ã': ('A', 'tilde'), 'Ä': ('A', 'diaer'),
    'È': ('E', 'grave'), 'É': ('E', 'acute'), 'Ê': ('E', 'circ'), 'Ë': ('E', 'diaer'),
    'Í': ('I', 'acute'), 'Î': ('I', 'circ'), 'Ï': ('I', 'diaer'), 'Ñ': ('N', 'tilde'),
    'Ó': ('O', 'acute'), 'Ô': ('O', 'circ'), 'Õ': ('O', 'tilde'), 'Ö': ('O', 'diaer'),
    'Ù': ('U', 'grave'), 'Ú': ('U', 'acute'), 'Û': ('U', 'circ'), 'Ü': ('U', 'diaer'),
    'Ё': ('Е', 'diaer'), 'Й': ('И', 'breve'),
}


def halfrows(spec):
    """A block glyph ('/' between rows) at half-row steps: each row twice."""
    return [r for r in spec.split('/') for _ in (0, 1)]


def squeeze(rows, k):
    """Take k half-rows out of a glyph, VGA-style: from its longest run of equal rows, then the lightest, then the top."""
    rows = list(rows)
    for _ in range(k):
        runs, i = [], 0
        while i < len(rows):
            j = i
            while j < len(rows) and rows[j] == rows[i]:
                j += 1
            if j - i >= 2:
                runs.append((-(j - i), rows[i].count('#'), i))
            i = j
        del rows[sorted(runs)[0][2]]
    return rows


def accented(abc, ch):
    """An accented capital at half-row steps inside the line's rows: accent, a clear half-row, the shortened letter."""
    base, mark = ACCENTS[ch]
    rows = halfrows(abc[base])
    w = len(rows[0])
    top = HALF_MARK[mark][w]
    body = squeeze(rows, len(top) + 1)
    return top + ['.' * w] + body


def cedilla(abc, base='C'):
    """Ç in block type: the C shortened by a row, its cedilla in the last row (block type has no descender)."""
    rows = halfrows(abc[base])
    w = len(rows[0])
    tail = ['..#..', '.#...'] if w == 5 else ['..#.', '.#..']
    return squeeze(rows, 2) + tail


def band(abc, chars, scale=1, gap_col=False):
    """One halfblock band: {char: rows at half-row steps, each pixel `scale` columns (+ a blank column)}."""
    out = {}
    for ch in chars:
        if ch == ' ':
            rows = ['.' * len(next(iter(abc.values())).split('/')[0])] * (2 * len(next(iter(abc.values())).split('/')))
        elif ch in ACCENTS:
            rows = accented(abc, ch)
        elif ch == 'Ç':
            rows = cedilla(abc)
        else:
            rows = halfrows(abc[ch])
        rows = [''.join(p * scale for p in r) + ('.' if gap_col else '') for r in rows]
        out[ch] = rows
    return out


LATIN_BIG = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,:-/\'!? '
CYR_BIG = 'АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ0123456789.,:-/\'!? '
LANG_CAPS = {'de': 'ÄÖÜ', 'fr': 'ÀÂÇÉÈÊËÎÏÔÙÛÜ', 'es': 'ÁÉÍÑÓÚÜ', 'pt-BR': 'ÁÂÃÀÇÉÊÍÓÔÕÚ'}


def big_bands(code):
    """The language's big-type bands: (role, chars, glyph rows) per band."""
    if code == 'ru':
        head = band(BIG_RU | {c: C20.BIG[c] for c in LATIN_BIG if c in C20.BIG}, CYR_BIG, scale=2)
        second = band(SECOND_RU, CYR_BIG)
        return [('headline', head), ('second', second)]
    chars = LATIN_BIG + LANG_CAPS[code]
    bands = []
    if code != 'fr':   # NOTES fits at two cells a pixel: the French headline keeps C20's block letters
        bands.append(('headline', band(C20.BIG, chars)))
    bands.append(('second', band(C20.SMALL, chars)))
    return bands


# ------------------------------------------------------------------ the faces
FACES = {'de': CP850_ORDER, 'fr': CP850_ORDER, 'es': CP850_ORDER, 'pt-BR': CP850_ORDER, 'ru': CP866_ORDER}


def glyph(ch):
    if ch == '\u00a0':
        return [BLANK] * 16
    return LATIN.get(ch) or CYR[ch]


def build(code):
    half = FACES[code]
    slot_rows = (len(half) + 15) // 16
    bands = big_bands(code)
    layout, y = [], slot_rows * 16
    for role, glyphs in bands:
        w, h = len(next(iter(glyphs.values()))[0]), len(next(iter(glyphs.values())))
        per = 128 // w
        n = len(glyphs)
        layout.append((role, glyphs, w, h, y, per))
        y += ((n + per - 1) // per) * h
    im = Image.new('RGB', (128, y), (0, 0, 0))
    px = im.load()
    for k, ch in enumerate(half):
        cx, cy = (k % 16) * 8, (k // 16) * 16
        for yy, row in enumerate(glyph(ch)):
            for xx, v in enumerate(row):
                if v == '#':
                    px[cx + xx, cy + yy] = (255, 255, 255)
    big = []
    for role, glyphs, w, h, top, per in layout:
        chars = ''.join(glyphs)
        for i, ch in enumerate(chars):
            gx, gy = (i % per) * w, top + (i // per) * h
            for yy, row in enumerate(glyphs[ch]):
                for xx, v in enumerate(row):
                    if v == '#':
                        px[gx + xx, gy + yy] = (255, 255, 255)
        big.append({'role': role, 'mode': 'halfblock', 'chars': chars, 'w': w, 'h': h, 'top': top, 'perRow': per})
    return im, {'half': half, 'wide': '', 'big': big}


def check():
    problems = []
    for ch in CP850_ORDER + CP866_ORDER:
        rows = glyph(ch)
        if len(rows) != 16 or any(len(r) != 8 for r in rows):
            problems.append(f'{ch}: not 16 x 8')
            continue
        if ch != '№' and any(r[7] == '#' for r in rows):
            problems.append(f'{ch}: ink in column 7')
        if ch != '\u00a0' and not any('#' in r for r in rows):
            problems.append(f'{ch}: empty')
    for code in FACES:
        for role, glyphs in big_bands(code):
            sizes = {(len(g[0]), len(g)) for g in glyphs.values()}
            if len(sizes) != 1:
                problems.append(f'{code} {role}: glyph sizes {sizes}')
    return problems


if __name__ == '__main__':
    out_dir = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser('~/FrontRoomsVisualWork/terminal_faces/out')
    os.makedirs(out_dir, exist_ok=True)
    for code in FACES:
        im, meta = build(code)
        im.save(os.path.join(out_dir, code + '.png'))
        with open(os.path.join(out_dir, code + '.json'), 'w', encoding='utf-8', newline='\n') as f:
            json.dump(meta, f, ensure_ascii=False, indent=1)
            f.write('\n')
        print(code, im.size, len(meta['half']), 'slots,', ', '.join(f"{b['role']} {b['w']}x{b['h']} x{len(b['chars'])}" for b in meta['big']))
    problems = check()
    print('problems:', len(problems))
    for p in problems:
        print('  ', p)
