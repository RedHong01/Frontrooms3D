// ---- injected: DATA = {lib:{name:componentId}, bgh:{bg:hash}, xh:crosshairComponentId, crops:[{file,bg,s,box,ops,at:{parent,x,y}}]}
const comp = {};
for (const k in DATA.lib) comp[k] = await figma.getNodeByIdAsync(DATA.lib[k]);
const XH = await figma.getNodeByIdAsync(DATA.xh);
const FN = {BAYON: F.bayon, MONO: F.mono, COURIER_B: F.cour, SERIF: F.serif};
const LHF = (font, size) => font === 'BAYON' ? 24 : font === 'MONO' ? (size < 16 ? 16 : 20) : font === 'COURIER_B' ? 26 : 20;
const boCache = {};
const tmp = frame('tmp-probe', 10, 10, SEC);
async function bo(font, size, lh) {               // baseline offset from text-box top, measured on an 'H'
  const k = font + size + '/' + lh; if (boCache[k] !== undefined) return boCache[k];
  const t = txt('H', FN[font], size, lh, 'FFFFFF'); tmp.appendChild(t); t.x = 0; t.y = 0;
  const v = figma.flatten([t], tmp); const b = v.y + v.height; v.remove(); boCache[k] = b; return b;
}
async function serifOutline(f, o, x0, y0) {       // Unity draws Source Serif 4 at opsz 20: outline 20 px text, scale up
  const k = o.size / 20;
  const b20 = await bo('SERIF', 20, 20);
  const t = txt(o.text, F.serif, 20, 20, o.colour); tmp.appendChild(t); t.x = 0; t.y = 0;
  const v = figma.flatten([t], tmp); const vx = v.x, vy = v.y;
  v.rescale(k); f.appendChild(v);
  v.x = o.x - x0 + k * vx; v.y = o.baseline - y0 - k * (b20 - vy);
  v.name = o.text + ' · Source Serif 4 ' + o.size + ' px at opsz 20 (outlined)';
  return v;
}
const made = [];
for (const c of DATA.crops) {
  const [x0, y0, x1, y1] = c.box, w = x1 - x0, h = y1 - y0;
  const f = figma.createFrame(); f.name = c.file.replace(/\.(png|jpg)$/, ''); f.resize(w, h); f.clipsContent = true;
  f.fills = [{type: 'IMAGE', imageHash: DATA.bgh[c.bg], scaleMode: 'CROP', imageTransform: [[w / 1920, 0, x0 / 1920], [0, h / 1080, y0 / 1080]]}];
  for (const o of c.ops) {
    if (o.op === 'sprite') {
      const inst = comp[o.name].createInstance(); f.appendChild(inst); inst.x = o.x - x0; inst.y = o.y - y0;
      if (o.fig_opacity !== undefined) inst.opacity = o.fig_opacity;
      if (o.fig_inner) for (const k in o.fig_inner) inst.findAll(n => n.name === k).forEach(n => n.opacity = o.fig_inner[k]);
    } else if (o.op === 'text') {
      if (o.font === 'SERIF' && o.size > 24) { await serifOutline(f, o, x0, y0); continue; }
      const lh = LHF(o.font, o.size);
      const t = txt(o.text, FN[o.font], o.size, lh, o.colour); f.appendChild(t);
      t.x = o.x - x0; t.y = o.baseline - (await bo(o.font, o.size, lh)) - y0;
      if (o.fig_opacity !== undefined) t.opacity = o.fig_opacity; else if (o.opacity < 0.999) t.opacity = o.opacity;
    } else if (o.op === 'rect') {
      const r = figma.createRectangle(); r.name = 'rule'; r.resize(o.w, o.h); r.fills = solid(o.colour, 1); r.opacity = o.opacity; f.appendChild(r); r.x = o.x - x0; r.y = o.y - y0;
    } else if (o.op === 'crosshair') {
      const xi = XH.createInstance(); f.appendChild(xi); xi.x = o.x - x0; xi.y = o.y - y0;
    }
  }
  const par = await figma.getNodeByIdAsync(c.at.parent); par.appendChild(f); f.x = c.at.x; f.y = c.at.y;
  if (c.s === 2) f.rescale(2);
  let node = f;
  if (c.comp) { node = figma.createComponentFromNode(f); node.description = 'Native twin of ui_key_icon/design/hud/' + c.file + ' (游戏视觉 composite, 2026-10-03). Opacities fitted per background so Figma\'s sRGB blend matches Unity\'s linear blend.'; }
  made.push(node.id);
}
tmp.remove();
return made.join(',');
