import os, re, sys, json, glob
import numpy as np
from PIL import Image
PROJ = sys.argv[1] if __name__ == '__main__' else None
SURF = os.path.join(PROJ, 'Assets/Resources/Surfaces') if PROJ else None
guid2path = {}
def _index():
    if guid2path: return
    for meta in glob.glob(os.path.join(PROJ, 'Assets/Resources/**/*.meta'), recursive=True):
        if not re.search(r'\.(png|jpg|tga|psd|exr|tif)\.meta$', meta, re.I): continue
        try:
            with open(meta) as f:
                for line in f:
                    if line.startswith('guid:'):
                        guid2path[line.split()[1]] = meta[:-5]; break
        except Exception: pass
def lin(v): v=np.asarray(v,float); return np.where(v<=0.04045, v/12.92, ((v+0.055)/1.055)**2.4)
def srgb(v): v=np.clip(np.asarray(v,float),0,1); return np.where(v<=0.0031308, v*12.92, 1.055*v**(1/2.4)-0.055)
def hexc(c): return '#'+''.join('%02x'%int(round(float(x)*255)) for x in c)
def parse(mat):
    t=open(mat).read()
    d={'name':os.path.basename(mat)[:-4]}
    m=re.search(r'- _BaseMap:\s*\n\s*m_Texture: \{fileID: (\d+)(?:, guid: (\w+))?', t)
    d['baseGuid']= m.group(2) if m and m.group(1)!='0' else None
    m=re.search(r'- _BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)', t)
    d['tint']=[float(m.group(i)) for i in (1,2,3)] if m else [1,1,1]
    for k in ('_Metallic','_Smoothness','_UseEmission'):
        m=re.search(r'- %s: ([\d.e-]+)'%k, t); d[k]=float(m.group(1)) if m else None
    m=re.search(r'- _EmissionColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)', t)
    d['emission']=[float(m.group(i)) for i in (1,2,3)] if m else None
    m=re.search(r'm_Shader: \{fileID: (\d+), guid: (\w+)', t); d['shader']=m.group(2) if m else None
    return d
def swatch(name):
    _index()
    mat=os.path.join(SURF, name+'.mat')
    if not os.path.exists(mat): return None
    d=parse(mat)
    tex=guid2path.get(d['baseGuid']) if d['baseGuid'] else None
    d['baseMap']=os.path.relpath(tex, PROJ) if tex else None
    if tex:
        im=np.asarray(Image.open(tex).convert('RGB'),float)/255.
        a_srgb=im.reshape(-1,3).mean(0)              # plain mean of sRGB values (texmean.py)
        a_lin=lin(im).reshape(-1,3).mean(0)          # mean in linear
    else:
        a_srgb=np.ones(3); a_lin=np.ones(3)
    t=np.array(d['tint'])
    d['A_srgbMean_x_tintLinear']=hexc(srgb(lin(a_srgb)*lin(t)))
    d['B_linMean_x_tintLinear']=hexc(srgb(a_lin*lin(t)))
    d['C_srgbMean_x_tintRaw']=hexc(a_srgb*t)
    d['texMeanSrgb']=hexc(a_srgb)
    return d
if __name__=='__main__':
    for n in sys.argv[2:]:
        print(json.dumps(swatch(n)))
