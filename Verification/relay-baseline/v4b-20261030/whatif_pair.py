# Paired what-if comparison: evaderv2 lure runs, base vs hold07 vs radius9, by (seed, tier).
import glob, json, os, sys
# Usage: python3 -I whatif_pair.py (run from anywhere; reads this folder and its whatif-* subfolders).
R = os.path.dirname(os.path.abspath(__file__))
arms = {'base': R, 'hold07': R + '/whatif-hold07', 'radius9': R + '/whatif-radius9'}
data = {}
for arm, d in arms.items():
    for p in glob.glob(d + '/*-evaderv2-lure-T*.json'):
        j = json.load(open(p)); lo = j.get('lockOnLog', [])
        ch = j.get('chaseLog', [])
        rows = []
        for c in ch:
            l = c.get('lockOn', -1)
            rows.append(dict(kind=('long' if c.get('lockLong') else 'reacq' if c.get('lockReacquired') else 'first'), lockM=c.get('lockMetres', -1), hold=c.get('lockHold', -1), end=c['end'], evaded=c['evaded']))
        data[(arm, j['seed'], j['tier'])] = dict(caught=j['caught'], chases=rows, lockOns=len(lo), sec=j.get('secondsAfterRelease', 0))
def esc(rs): n=len(rs); k=sum(1 for r in rs if r['end'] not in ('caught','open')); return f"{k}/{n} ({100*k/n:.0f} %)" if n else "0/0"
for arm in arms:
    keys = [k for k in data if k[0] == arm]
    rs = [r for k in keys for r in data[k]['chases'] if r['evaded']]
    first = [data[k]['chases'][0] for k in keys if data[k]['chases'] and data[k]['chases'][0]['evaded']]
    print(f"{arm:8s} runs {len(keys):2d} caught runs {sum(data[k]['caught'] for k in keys):2d} lock-ons {sum(data[k]['lockOns'] for k in keys):3d} | evaded chases escaped {esc(rs)} | first chase escaped {esc(first)}",
          "| long", esc([r for r in rs if r['kind']=='long']), "first", esc([r for r in rs if r['kind']=='first']), "reacq", esc([r for r in rs if r['kind']=='reacq']))
    for t in (1, 5):
        rt = [r for k in keys if k[2]==t for r in data[k]['chases'] if r['evaded']]
        print(f"   T{t}: escaped {esc(rt)}; caught runs {sum(data[k]['caught'] for k in keys if k[2]==t)}/{len([k for k in keys if k[2]==t])}")
# Paired first chase: same encounter (seed, tier); does the outcome flip?
for arm in ('hold07', 'radius9'):
    same = flip_worse = flip_better = n = 0
    for (a, s, t), v in data.items():
        if a != 'base' or (arm, s, t) not in data: continue
        b, w = v['chases'], data[(arm, s, t)]['chases']
        if not b or not w: continue
        n += 1
        eb, ew = b[0]['end'] == 'caught', w[0]['end'] == 'caught'
        if eb == ew: same += 1
        elif ew: flip_worse += 1
        else: flip_better += 1
    print(f"paired first chase base vs {arm}: {n} pairs, same {same}, caught only in {arm} {flip_worse}, escaped only in {arm} {flip_better}")
