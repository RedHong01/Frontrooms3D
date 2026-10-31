# Per-chase table for the V4b round. Usage: python3 -I report.py <folder of reports> [--csv out.csv]
import glob, json, os, sys, csv, statistics as st
RENDER = ('RenderTexture.Create failed', 'Unable to find surface for attachment', 'Not inside a Renderpass')  # -nographics only: no device to render to
folders = [a for a in sys.argv[1:] if os.path.isdir(a)]
rows, runs = [], []
for path in sorted(p for f in folders for p in glob.glob(os.path.join(f, '*.json'))):
    d = json.load(open(path))
    other = [e for e in d.get('errorLog', []) if not any(r in e for r in RENDER)]
    verdict = 'PASS' if (d['verdict'].startswith('PASS') or (not other and 'checks failed' not in d['verdict'])) else 'FAIL'
    lo = d.get('lockOnLog', [])
    dr = d.get('director', {}) or {}
    lure = '-lure-' in os.path.basename(path)
    runs.append(dict(lure=lure, name=os.path.basename(path)[:-5], seed=d['seed'], mode=d['mode'], tier=d['tier'], verdict=verdict, endedBy=d['endedBy'],
                     sec=d.get('secondsAfterRelease', 0), lockOns=len(lo), long=sum(1 for l in lo if l['longHold']), chases=len(d.get('chaseLog', [])),
                     caught=d.get('caught'), calls=dr.get('calls', 0), gate=dr.get('gateHolds', 0), errors=len(other)))
    for i, c in enumerate(d.get('chaseLog', [])):
        l = lo[c['lockOn']] if 0 <= c.get('lockOn', -1) < len(lo) else {}
        rows.append(dict(lure=lure, seed=d['seed'], mode=d['mode'], tier=d['tier'], i=i, cause=c['cause'], evaded=c['evaded'],
                         lockM=c.get('lockMetres', -1), hold=c.get('lockHold', -1), long=c.get('lockLong', False), reacq=c.get('lockReacquired', False), touch=c.get('lockTouch', False),
                         startM=c['straightAtStart'], secs=c['seconds'], end=c['end'], reaction=l.get('reaction', -1), s1=l.get('stage1Seconds', -1), s2=l.get('stage2Seconds', -1), startAt=c['startAt']))
def esc(rs):
    n = len(rs); k = sum(1 for r in rs if r['end'] not in ('caught', 'open'))
    return f"{k}/{n}" + (f" ({100*k/n:.0f} %)" if n else "")
print(f"{len(runs)} runs, {sum(r['verdict']=='PASS' for r in runs)} PASS, errors {sum(r['errors'] for r in runs)}; seconds on budget {sum(r['sec'] for r in runs)/60:.0f} min")
lo_total = sum(r['lockOns'] for r in runs); print(f"lock-ons {lo_total}, long holds {sum(r['long'] for r in runs)}, chases {len(rows)}, runs with a chase {sum(1 for r in runs if r['chases'])}, caught runs {sum(1 for r in runs if r['caught'])}")
for mode in sorted({r['mode'] for r in rows}):
    for tier in sorted({r['tier'] for r in rows}):
        rs = [r for r in rows if r['mode'] == mode and r['tier'] == tier]
        if rs: print(f"{mode} T{tier}: {len(rs)} chases, escaped {esc(rs)}; evaded {esc([r for r in rs if r['evaded']])}")
print("\nby lock-on, evaded chases (not open):")
ev = [r for r in rows if r['evaded'] and r['end'] != 'open']
for label, f in [("long 1.7 s (<= 6 m)", lambda r: r['long']), ("first, 0.7 s (> 6 m)", lambda r: not r['long'] and not r['reacq']), ("re-acquired 0.35 s", lambda r: r['reacq'])]:
    rs = [r for r in ev if f(r)]
    if rs: print(f"  {label}: escaped {esc(rs)}; lock median {st.median(r['lockM'] for r in rs):.1f} m, chase start median {st.median(r['startM'] for r in rs):.1f} m, chase median {st.median(r['secs'] for r in rs):.1f} s")
print("\nseed mode T i cause evaded lockM hold kind startM secs end react s1 s2")
for r in rows:
    kind = 'long' if r['long'] else 'reacq' if r['reacq'] else 'first'
    print(f"{r['seed']} {r['mode']} T{r['tier']} {r['i']} {r['cause']} {'E' if r['evaded'] else '-'} {r['lockM']:.1f} {r['hold']:.2f} {kind}{'/touch' if r['touch'] else ''} {r['startM']:.1f} {r['secs']:.1f} {r['end']} {r['reaction']:.2f} {r['s1']:.1f} {r['s2']:.1f}")
if '--csv' in sys.argv:
    with open(sys.argv[sys.argv.index('--csv') + 1], 'w', newline='') as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()), lineterminator='\n'); w.writeheader(); w.writerows(rows)
