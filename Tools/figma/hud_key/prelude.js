const SEC = await figma.getNodeByIdAsync('2532:4038');
const F = {bayon:{family:'Bayon',style:'Regular'}, mono:{family:'IBM Plex Mono',style:'Regular'}, cour:{family:'Courier Prime',style:'Bold'}, serif:{family:'Source Serif 4',style:'Regular'}};
for (const k in F) await figma.loadFontAsync(F[k]);
const hex = h => ({r:parseInt(h.slice(0,2),16)/255, g:parseInt(h.slice(2,4),16)/255, b:parseInt(h.slice(4,6),16)/255});
const solid = (h, o) => [{type:'SOLID', color:hex(h), opacity:(o===undefined?1:o)}];
const txt = (s, font, size, lh, col, name) => { const t = figma.createText(); t.fontName = font; t.fontSize = size; t.lineHeight = {unit:'PIXELS', value:lh}; t.letterSpacing = {unit:'PERCENT', value:0}; t.characters = s; t.fills = solid(col); t.textAutoResize = 'WIDTH_AND_HEIGHT'; if (name) t.name = name; return t; };
const al = (n, dir, gap) => { n.layoutMode = dir; n.primaryAxisSizingMode = 'AUTO'; n.counterAxisSizingMode = 'AUTO'; n.counterAxisAlignItems = 'MIN'; n.itemSpacing = gap || 0; n.fills = []; n.clipsContent = false; return n; };
const frame = (name, w, h, parent) => { const f = figma.createFrame(); f.name = name; f.resize(w, h); f.fills = []; f.clipsContent = false; if (parent) parent.appendChild(f); return f; };
const byName = (root, nm) => root.findOne(n => n.name === nm);
