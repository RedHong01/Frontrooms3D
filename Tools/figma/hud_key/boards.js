// ---- injected: BD = {twins:{name:componentId}, boards:[{key,name,page,total,title,lede,statement,at:{x,y},cells,heads}]}
const ST = {title:'S:4cc59baf3230acd570aeedd2f3163c738d822402,', body:'S:60138e985390dcae06a002de377d05c5808fc81c,', statement:'S:8ea64620976dfe88c39216b12a4fa8d0d71a53be,', label:'S:44581afa083a5f9b8da214be893def6dc2449af1,', meta:'S:e15151982bb3e6d485ad51045c6714f087f0c2eb,'};
const INK = {r:0.0392, g:0.0392, b:0.0392};
async function styled(parent, s, style, x, y, name, w) {
  const t = figma.createText(); parent.appendChild(t); t.characters = s;
  await t.setTextStyleIdAsync(ST[style]); t.fills = [{type:'SOLID', color:INK}];
  if (w) { t.textAutoResize = 'HEIGHT'; t.resize(w, t.height); t.textAutoResize = 'HEIGHT'; } else t.textAutoResize = 'WIDTH_AND_HEIGHT';
  t.x = x; t.y = y; t.name = name; return t;
}
for (const st of Object.values(ST)) { const s = await figma.getStyleByIdAsync(st); await figma.loadFontAsync(s.fontName); }
const out = [];
for (const b of BD.boards) {
  const f = figma.createFrame(); f.name = b.name; f.resize(1920, 1080); f.fills = [{type:'SOLID', color:{r:1,g:1,b:1}}]; f.clipsContent = true;
  SEC.appendChild(f); f.x = b.at.x; f.y = b.at.y;
  const hdr = ['Individual Game Project', 'FrontRooms · HUD key', 'Week 3 · Oct 3, 2026', 'Red Wang', b.page + ' / ' + b.total];
  for (let i = 0; i < 5; i++) await styled(f, hdr[i], 'meta', [72, 522, 972, 1422, 1722][i], 28, i === 4 ? 'header page' : 'header');
  await styled(f, b.title, 'title', 72, 139, 'title');
  if (b.lede) await styled(f, b.lede, 'body', 1272, 173, 'lede', 576);
  if (b.statement) await styled(f, b.statement, 'statement', 72, 969, 'statement', 1776);
  for (const h of b.heads) await styled(f, h.text, 'label', h.x, h.y, 'head · ' + h.text);
  for (const c of b.cells) {
    const v = figma.createFrame(); v.name = 'view · ' + c.twin + (c.s === 2 ? ' · 2x' : ''); v.resize(c.w, c.h); v.clipsContent = true; v.fills = [];
    f.appendChild(v); v.x = c.x; v.y = c.y;
    const comp = await figma.getNodeByIdAsync(BD.twins[c.twin]);
    const inst = comp.createInstance(); v.appendChild(inst);
    if (c.s === 2) inst.rescale(2);
    inst.x = -c.dx; inst.y = -c.dy;
  }
  out.push([b.key, f.id]);
}
return out;
