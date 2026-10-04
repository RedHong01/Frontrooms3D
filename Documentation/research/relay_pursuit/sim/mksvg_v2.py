import json, math, random, heapq
from collections import deque
from mapgen import Gen
CS=3.0; D45=math.hypot(3,3)
def f(v): return ('%.1f'%v).rstrip('0').rstrip('.')
seed=1032392419; P=(78,-20); g=Gen(seed)
COST={'Open':0.0,'Arch':0.0,'Door':9.0,'Window':9.0}
def octile(src,cap):
    d={src:0.0}; h=[(0.0,src)]
    while h:
        c,p=heapq.heappop(h)
        if c>d.get(p,1e9): continue
        for s in ((1,0),(-1,0),(0,1),(0,-1)):
            nb=(p[0]+s[0],p[1]+s[1]); e=g.edge(p,nb)
            if e=='Wall': continue
            nc=c+CS+COST[e]
            if nc<=cap and nc<d.get(nb,1e9): d[nb]=nc; heapq.heappush(h,(nc,nb))
        for sx in (1,-1):
            for sy in (1,-1):
                a=(p[0]+sx,p[1]); b=(p[0],p[1]+sy); q=(p[0]+sx,p[1]+sy)
                if g.edge(p,a)=='Open' and g.edge(p,b)=='Open' and g.edge(a,q)=='Open' and g.edge(b,q)=='Open':
                    nc=c+D45
                    if nc<=cap and nc<d.get(q,1e9): d[q]=nc; heapq.heappush(h,(nc,q))
    return d
def edges(x0,y0,nx,ny,px,ox,oy):
    Wl=[];Dr=[];Wi=[]
    for i in range(nx):
        for j in range(ny):
            c=(x0+i,y0+j)
            for s in ((1,0),(0,1)):
                if (s==(1,0) and i==nx-1) or (s==(0,1) and j==ny-1): continue
                nb=(c[0]+s[0],c[1]+s[1]); e=g.edge(c,nb)
                if s==(1,0): X=ox+(i+1)*px; Y1=oy+(ny-j-1)*px; seg=f'M{f(X)} {f(Y1)}V{f(Y1+px)}'
                else: Y=oy+(ny-j-1)*px; X1=ox+i*px; seg=f'M{f(X1)} {f(Y)}H{f(X1+px)}'
                if e=='Wall': Wl.append(seg)
                elif e=='Door': Dr.append(seg)
                elif e=='Window': Wi.append(seg)
    return ''.join(Wl),''.join(Dr),''.join(Wi)
def sq(cells,x0,y0,ny,px,ox,oy,inset=1):
    o=[]
    for c in cells:
        X=ox+(c[0]-x0)*px+inset; Y=oy+(ny-(c[1]-y0)-1)*px+inset; w=px-2*inset
        o.append(f'M{f(X)} {f(Y)}h{f(w)}v{f(w)}h{f(-w)}Z')
    return ''.join(o)
def grid(n,px,ox,oy,ny=None):
    ny=ny or n; s=[]
    for i in range(1,n): s.append(f'<line x1="{f(ox+i*px)}" y1="{f(oy)}" x2="{f(ox+i*px)}" y2="{f(oy+ny*px)}"/>')
    for j in range(1,ny): s.append(f'<line x1="{f(ox)}" y1="{f(oy+j*px)}" x2="{f(ox+n*px)}" y2="{f(oy+j*px)}"/>')
    return '<g stroke="#2c2c2c" stroke-width="1" stroke-dasharray="3 5" fill="none">'+''.join(s)+'</g>'
META={}
# ---------- RP02 proposed: sprint step 27 m of path (octile, shut doors +9) ----------
W,H=864,560; n=25; px=21; size=n*px; ox=W-size-18; oy=(H-size)//2; x0,y0=P[0]-12,P[1]-12
F=octile(P,27.0)
cells=[c for c in F if abs(c[0]-P[0])<=12 and abs(c[1]-P[1])<=12]
Wl,Dr,Wi=edges(x0,y0,n,n,px,ox,oy)
o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}"><rect width="{W}" height="{H}" fill="#141414"/>',grid(n,px,ox,oy)]
o.append(f'<path d="{sq(cells,x0,y0,n,px,ox,oy)}" fill="#f4df3b" fill-opacity="0.55"/>')
o.append(f'<path d="{Wl}" stroke="#ffffff" stroke-width="3" stroke-linecap="square" fill="none"/>')
o.append(f'<path d="{Dr}" stroke="#ffffff" stroke-width="4" stroke-dasharray="5 4" fill="none"/>')
o.append(f'<path d="{Wi}" stroke="#8a8a8a" stroke-width="4" stroke-dasharray="3 3" fill="none"/>')
cx=ox+(P[0]-x0+.5)*px; cy=oy+(n-(P[1]-y0)-.5)*px
o.append(f'<circle cx="{f(cx)}" cy="{f(cy)}" r="9" fill="#f4df3b" stroke="#141414" stroke-width="3"/></svg>')
open('svg/v2_rp02_new.svg','w').write(''.join(o)); META['rp02_cells']=len(F)
# ---------- RP05 zones: W = min(F, 3 x straight) ----------
W,H=864,560; px=26; nx,ny=31,20; x0,y0=P[0]-15,P[1]-10; ox=(W-nx*px)/2; oy=(H-ny*px)/2
F=octile(P,45.0)
def Wd(c):
    st=math.hypot((c[0]-P[0])*CS,(c[1]-P[1])*CS)
    return min(F.get(c,1e9),3*st)
allc=[(x0+i,y0+j) for i in range(nx) for j in range(ny)]
st1=[c for c in allc if Wd(c)<=30]; st2=[c for c in allc if Wd(c)<=18]
# room set: BFS open edges depth<=2 <=9
room=[P]; q=deque([(P,0)]); seen={P}
while q and len(room)<9:
    p,dd=q.popleft()
    if dd>=2: continue
    for s in ((1,0),(-1,0),(0,1),(0,-1)):
        nb=(p[0]+s[0],p[1]+s[1])
        if nb in seen or g.edge(p,nb)!='Open': continue
        seen.add(nb); room.append(nb); q.append((nb,dd+1))
        if len(room)>=9: break
# relay path: start at a cell with W>=42 inside window, walk the walking path toward P, stop when W<=12
cand=[c for c in allc if 42<=Wd(c)<=48 and 1<=c[0]-x0<nx-1 and 1<=c[1]-y0<ny-1 and c in F]
cand.sort(key=lambda c:(c[0],c[1]))
start=cand[0] if cand else (P[0]-12,P[1])
par={start:None}; qq=deque([start])
while qq:
    p=qq.popleft()
    if p==P: break
    for s in ((1,0),(-1,0),(0,1),(0,-1)):
        nb=(p[0]+s[0],p[1]+s[1])
        if nb in par or g.edge(p,nb) in ('Wall','Window'): continue
        par[nb]=p; qq.append(nb)
path=[]; c=P
while c is not None and c in par: path.append(c); c=par[c]
path=path[::-1]; path=[c for c in path if Wd(c)>=12]
relay=path[-1]
roomlamps=[c for c in room if max(abs(c[0]-relay[0]),abs(c[1]-relay[1]))>2]
Wl,Dr,Wi=edges(x0,y0,nx,ny,px,ox,oy)
def cxy(c): return (ox+(c[0]-x0+.5)*px, oy+(ny-(c[1]-y0)-.5)*px)
o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}"><rect width="{W}" height="{H}" fill="#141414"/>']
o.append(f'<path d="{sq(st1,x0,y0,ny,px,ox,oy)}" fill="#ffffff" fill-opacity="0.10"/>')
o.append(f'<path d="{sq(st2,x0,y0,ny,px,ox,oy)}" fill="#f4df3b" fill-opacity="0.30"/>')
lw=0.6/CS*px; lh=1.2/CS*px; dim=[]; lit=[]
for c in allc:
    X=ox+(c[0]-x0)*px+1.2/CS*px; Y=oy+(ny-(c[1]-y0)-1)*px+(CS-2.4)/CS*px
    r=f'M{f(X)} {f(Y)}h{f(lw)}v{f(lh)}h{f(-lw)}Z'
    (lit if c in roomlamps else dim).append(r)
o.append(f'<path d="{"".join(dim)}" fill="#ffffff" fill-opacity="0.22"/>')
o.append(f'<path d="{sq(room,x0,y0,ny,px,ox,oy,0)}" fill="none" stroke="#f4df3b" stroke-width="2"/>')
o.append(f'<path d="{"".join(lit)}" fill="#f4df3b"/>')
o.append(f'<path d="{Wl}" stroke="#ffffff" stroke-width="3" stroke-linecap="square" fill="none"/>')
o.append(f'<path d="{Dr}" stroke="#ffffff" stroke-width="4" stroke-dasharray="5 4" fill="none"/>')
o.append(f'<path d="{Wi}" stroke="#8a8a8a" stroke-width="4" stroke-dasharray="3 3" fill="none"/>')
pts=' '.join(('M' if k==0 else 'L')+'%s %s'%tuple(map(f,cxy(c))) for k,c in enumerate(path))
o.append(f'<path d="{pts}" stroke="#ffffff" stroke-width="3" stroke-dasharray="8 6" fill="none"/>')
ex,ey=cxy(path[0]); rx,ry=cxy(relay); pxx,pyy=cxy(P)
o.append(f'<circle cx="{f(rx)}" cy="{f(ry)}" r="{f(2.5*px)}" fill="none" stroke="#8a8a8a" stroke-width="1.5" stroke-dasharray="3 4"/>')
o.append(f'<circle cx="{f(ex)}" cy="{f(ey)}" r="11" fill="none" stroke="#ffffff" stroke-width="2" stroke-dasharray="4 4"/>')
o.append(f'<circle cx="{f(rx)}" cy="{f(ry)}" r="11" fill="#ffffff"/>')
o.append(f'<circle cx="{f(pxx)}" cy="{f(pyy)}" r="10" fill="#f4df3b" stroke="#141414" stroke-width="3"/></svg>')
open('svg/v2_rp05_zones.svg','w').write(''.join(o))
META['rp05']={'entry':[ex,ey],'relay':[rx,ry],'player':[pxx,pyy],'room':len(room),'roomlamps':len(roomlamps),'st1':len(st1),'st2':len(st2),'pathlen':len(path),'W_relay':Wd(relay),'W_entry':Wd(path[0])}
# ---------- RP05 approach timeline v2 ----------
TW,TH=888,500; T=17.0
def X(t): return t/T*TW
lanes={'dist':(0,170),'lamps':(214,300),'steps':(344,408),'cue':(452,484)}
t1=(45-30)/2.6; t2=(45-18)/2.6; tsee=(45-10)/2.6; tlock=tsee+0.30+0.04*10; tcue=tlock+0.6; tch=tlock+0.7
o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{TW}" height="{TH}" viewBox="0 0 {TW} {TH}">']
o.append(f'<rect x="{f(X(t1))}" y="0" width="{f(X(t2)-X(t1))}" height="{TH}" fill="#f4df3b" fill-opacity="0.10"/>')
o.append(f'<rect x="{f(X(t2))}" y="0" width="{f(X(tlock)-X(t2))}" height="{TH}" fill="#f4df3b" fill-opacity="0.24"/>')
o.append(f'<rect x="{f(X(tlock))}" y="0" width="{f(X(tch)-X(tlock))}" height="{TH}" fill="#f4df3b" fill-opacity="0.65"/>')
o.append(f'<rect x="{f(X(tch))}" y="0" width="{f(TW-X(tch))}" height="{TH}" fill="#0a0a0a" fill-opacity="0.06"/>')
y0d,y1d=lanes['dist']
def Yd(d): return y1d-(d/50)*(y1d-y0d)
dp=[]
for k in range(341):
    t=T*k/340
    if t<tch: d=max(45-2.6*t,10)
    else:
        u=t-tch; v=min(4.2,2.6+ (4.2-2.6)*min(1,u/0.6)); d=max(10-(u*3.4),5)
    dp.append(('M' if k==0 else 'L')+f'{f(X(t))} {f(Yd(d))}')
for dd in (30,18): o.append(f'<path d="M0 {f(Yd(dd))}H{TW}" stroke="#0a0a0a" stroke-width="1" stroke-dasharray="4 4"/>')
o.append(f'<path d="{" ".join(dp)}" stroke="#0a0a0a" stroke-width="3" fill="none"/>')
o.append(f'<path d="M0 {y1d}H{TW}" stroke="#0a0a0a" stroke-width="1"/>')
y0l,y1l=lanes['lamps']
def Yl(v): return y1l-v*(y1l-y0l)
dips=[(t1+0.6+i*0.55,m) for i,m in enumerate((0.62,0.58,0.66))]+[(t2+i*0.55,m) for i,m in enumerate((0.6,0.64))]
def lamp(t):
    if t>=tlock+0.6 and t<tlock+0.6: return 1.0
    v=1.0
    for td,m in dips:
        u=t-td; a,h,r=0.10,0.10,0.22
        e= u/a if 0<=u<a else 1 if a<=u<a+h else 1-(u-a-h)/r if a+h<=u<a+h+r else 0
        v=min(v,1-(1-m)*e)
    return v
lp=[('M' if k==0 else 'L')+f'{f(X(T*k/1700))} {f(Yl(lamp(T*k/1700)))}' for k in range(1701)]
o.append(f'<path d="M0 {y1l}H{TW}" stroke="#0a0a0a" stroke-width="1"/>')
o.append(f'<path d="{" ".join(lp)}" stroke="#0a0a0a" stroke-width="2.5" fill="none"/>')
y0s,y1s=lanes['steps']
o.append(f'<path d="M0 {y1s}H{TW}" stroke="#0a0a0a" stroke-width="1"/>')
o.append(f'<rect x="{f(X(t2))}" y="{f(y1s-8)}" width="{f(X(T)-X(t2))}" height="8" fill="#8a8a8a" fill-opacity="0.6"/>')
t=t2; tl=t2+1.8
while t<tch:
    hgt=(y1s-y0s)*(0.45 if t<tl else 0.9); col='#8a8a8a' if t<tl else '#0a0a0a'
    o.append(f'<rect x="{f(X(t)-2)}" y="{f(y1s-hgt)}" width="4" height="{f(hgt)}" fill="{col}"/>'); t+=0.44
while t<T:
    o.append(f'<rect x="{f(X(t)-2)}" y="{f(y0s)}" width="4" height="{f(y1s-y0s)}" fill="#0a0a0a"/>'); t+=0.29
y0c,y1c=lanes['cue']
o.append(f'<rect x="{f(X(tlock))}" y="{y0c}" width="{f(X(tcue)-X(tlock))}" height="{y1c-y0c}" fill="#0a0a0a"/>')
o.append(f'<rect x="{f(X(tcue))}" y="{y0c}" width="{f(X(tch)-X(tcue))}" height="{y1c-y0c}" fill="#8a8a8a"/>')
o.append(f'<path d="M{f(X(tch))} {f((y0c+y1c)/2)}H{TW}" stroke="#0a0a0a" stroke-width="3"/></svg>')
open('svg/v2_rp05_timeline.svg','w').write(''.join(o))
META['rp05t']={'X':{'t1':X(t1),'t2':X(t2),'tlock':X(tlock),'tch':X(tch)},'Yd30':Yd(30),'Yd18':Yd(18),'tsee':tsee,'tlock':tlock,'tch':tch}
# ---------- RP04 attention v2 ----------
AW,AH,AT=1776,300,210.0
def XA(t): return t/AT*AW
def YA(a): return AH-a/100*AH
doors=[(8,1),(9.5,1),(36,1),(37,1),(53,12)]
sprints=[(20,25),(50,53),(55,59)]
steps=set()
for a,b in sprints:
    k=1
    while a+k*0.3<=b: steps.add(round(a+k*0.3,2)); k+=1
dt=0.05; t=0; A=0; last=-99; pts=[]; call=None
while t<=AT:
    gsum=sum(v for td,v in doors if abs(t-td)<dt/2)+(3 if round(t,2) in steps else 0)
    if gsum: A+=gsum; last=t
    elif t-last>3: A=max(0,A-3*dt)
    pts.append((t,A))
    if A>=60: call=t; break
    t=round(t+dt,2)
onmap=(call,call+3+52); relax=(onmap[1],onmap[1]+100)
dpath=' '.join(('M' if i==0 else 'L')+f'{f(XA(tt))} {f(YA(a))}' for i,(tt,a) in enumerate(pts))
post=f'M{f(XA(relax[0]))} {f(YA(20))}H{f(XA(min(AT,relax[1])))}'
o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{AW}" height="{AH}" viewBox="0 0 {AW} {AH}">']
for a,b in sprints: o.append(f'<rect x="{f(XA(a))}" y="0" width="{f(XA(b)-XA(a))}" height="{AH}" fill="#f4df3b" fill-opacity="0.35"/>')
o.append(f'<rect x="{f(XA(onmap[0]))}" y="0" width="{f(XA(onmap[1])-XA(onmap[0]))}" height="{AH}" fill="#0a0a0a"/>')
o.append(f'<rect x="{f(XA(relax[0]))}" y="0" width="{f(XA(min(AT,relax[1]))-XA(relax[0]))}" height="{AH}" fill="#0a0a0a" fill-opacity="0.07"/>')
o.append(f'<path d="M0 {f(YA(60))}H{f(XA(onmap[0]))}" stroke="#0a0a0a" stroke-width="2" stroke-dasharray="8 6"/>')
o.append(f'<path d="M0 {AH}H{AW}" stroke="#0a0a0a" stroke-width="1.5"/>')
for td,v in doors: o.append(f'<path d="M{f(XA(td))} {AH}V{AH-(22 if v>5 else 12)}" stroke="#0a0a0a" stroke-width="3"/>')
o.append(f'<path d="{dpath}" stroke="#0a0a0a" stroke-width="3.5" fill="none"/>')
o.append(f'<path d="{post}" stroke="#0a0a0a" stroke-width="3.5" fill="none"/>')
o.append(f'<circle cx="{f(XA(call))}" cy="{f(YA(60))}" r="10" fill="#f4df3b" stroke="#0a0a0a" stroke-width="3"/></svg>')
open('svg/v2_rp04_attention.svg','w').write(''.join(o))
META['rp04']={'call':call,'X':{'call':XA(call),'onmapEnd':XA(onmap[1]),'s':[XA(a) for a,b in sprints],'doors':[XA(td) for td,v in doors],'ticks':{str(tt):XA(tt) for tt in (0,30,60,90,120,150,180,210)}},'Y60':YA(60),'Y20':YA(20),'peak1':max(a for tt,a in pts if tt<30)}
# ---------- RP06 cone v2 (±60°) ----------
CW,CH=888,486; s=26
o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{CW}" height="{CH}" viewBox="0 0 {CW} {CH}"><rect width="{CW}" height="{CH}" fill="#141414"/>']
for i in range(1,int(CW/(3*s))+1): o.append(f'<path d="M{f(i*3*s)} 0V{CH}" stroke="#2c2c2c" stroke-dasharray="3 5"/>')
for j in range(1,int(CH/(3*s))+1): o.append(f'<path d="M0 {f(j*3*s)}H{CW}" stroke="#2c2c2c" stroke-dasharray="3 5"/>')
cx,cy=170,CH/2; R=12*s; a1,a2=math.radians(-60),math.radians(60)
o.append(f'<path d="M{cx} {cy}L{f(cx+R*math.cos(a1))} {f(cy+R*math.sin(a1))}A{R} {R} 0 0 1 {f(cx+R*math.cos(a2))} {f(cy+R*math.sin(a2))}Z" fill="#ffffff" fill-opacity="0.12"/>')
o.append(f'<circle cx="{cx}" cy="{cy}" r="{f(1.5*s)}" fill="none" stroke="#ffffff" stroke-width="2" stroke-dasharray="4 4"/>')
o.append(f'<path d="M{cx+6*s} {f(cy-190)}V{f(cy-70)}" stroke="#ffffff" stroke-width="5"/>')
o.append(f'<circle cx="{cx}" cy="{cy}" r="12" fill="#ffffff"/><path d="M{cx+12} {cy}L{cx+40} {cy}" stroke="#ffffff" stroke-width="3"/>')
p1=(cx+9*s*math.cos(math.radians(15)),cy+9*s*math.sin(math.radians(15)))
p2=(cx+9*s*math.cos(math.radians(-45)),cy+9*s*math.sin(math.radians(-45)))
p3=(cx+5*s*math.cos(math.radians(-110)),cy+5*s*math.sin(math.radians(-110)))
o.append(f'<circle cx="{f(p1[0])}" cy="{f(p1[1])}" r="11" fill="#f4df3b"/>')
for p in (p2,p3): o.append(f'<circle cx="{f(p[0])}" cy="{f(p[1])}" r="11" fill="none" stroke="#f4df3b" stroke-width="2.5"/>')
o.append(f'<path d="M{cx} {cy}L{f(p1[0])} {f(p1[1])}" stroke="#f4df3b" stroke-width="2" stroke-dasharray="6 5"/></svg>')
open('svg/v2_rp06_cone.svg','w').write(''.join(o))
# wall check for p2 blocked
wx=cx+6*s; tt=(wx-cx)/(p2[0]-cx) if p2[0]!=cx else 2; yb=cy+tt*(p2[1]-cy)
META['rp06']={'relay':[cx,cy],'p1':p1,'p2':p2,'p3':p3,'p2_blocked': (0<tt<1 and cy-190<=yb<=cy-70)}
# ---------- RP09 pacing v2 ----------
PW,PH=1776,250; Tm=300.0
def XP(t): return t/Tm*PW
o=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{PW}" height="{PH}" viewBox="0 0 {PW} {PH}">']
o.append(f'<rect x="0" y="0" width="{f(XP(10))}" height="70" fill="#d9d9d9"/><rect x="{f(XP(10))}" y="0" width="{f(PW-XP(10))}" height="70" fill="#0a0a0a"/>')
cyc=[('relax',0,60),('build',60,95),('arrive',95,98),('on',98,128),('chase',128,140),('on',140,152),('withdraw',152,162),('relax',162,262),('build',262,295),('arrive',295,300)]
col={'relax':'#ececec','build':'#d9d9d9','arrive':'#6b6b6b','on':'#0a0a0a','chase':'#f4df3b','withdraw':'#6b6b6b'}
for k,a,b in cyc: o.append(f'<rect x="{f(XP(a))}" y="150" width="{f(XP(b)-XP(a))}" height="70" fill="{col[k]}"/>')
for a,b in ((101,128),(140,158)): o.append(f'<rect x="{f(XP(a))}" y="228" width="{f(XP(b)-XP(a))}" height="8" fill="#f4df3b"/>')
o.append('</svg>')
open('svg/v2_rp09_pacing.svg','w').write(''.join(o))
onmap=sum(b-a for k,a,b in cyc if k in('on','chase','withdraw'))
META['rp09']={'onmap_share':onmap/300,'X':{str(t):XP(t) for t in (0,60,95,98,128,140,152,162,262,295)}}
json.dump(META,open('svgmeta_v2.json','w'),default=float)
print(json.dumps(META,default=lambda x: round(float(x),1)))
