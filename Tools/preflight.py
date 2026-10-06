"""Validate every sheet cell/reference, then generate one C# config struct per row."""
import json,math,re,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sheets={p.stem:json.loads(p.read_text()) for p in sorted((root/'Sheets').glob('*.json'))}
errors=[];checks=[];parts=['// GENERATED FROM JSON SHEETS. Edit Sheets/*.json first.','namespace UltraHaul.Design {']
def ident(x): return ''.join(w[:1].upper()+w[1:] for w in re.split(r'[^A-Za-z0-9]+',x))
for name,s in sheets.items():
    if not s.get('evidence'): errors.append(f'{name}: missing verification evidence')
    ids=[row.get('id') for row in s['rows']]
    if len(ids)!=len(set(ids)): errors.append(f'{name}: duplicate row ids')
    for row in s['rows']:
        extra=set(row)-set(s['columns'])
        if extra: errors.append(f'{name}/{row.get("id")}: extra columns {extra}')
        fields=[]
        for col,t in s['columns'].items():
            v=row.get(col)
            key=f'{name}/{row.get("id")}/{col}'
            good=v is not None and (not isinstance(v,str) or bool(v.strip()))
            if t=='float': good=good and isinstance(v,(float,int)) and not isinstance(v,bool) and math.isfinite(v)
            elif t=='int': good=good and isinstance(v,int) and not isinstance(v,bool)
            elif t=='bool': good=good and isinstance(v,bool)
            elif t.startswith('ref:'): good=good and v in [r['id'] for r in sheets.get(t[4:],{'rows':[]})['rows']]
            else: good=good and isinstance(v,str)
            if not good: errors.append(key+': unfilled, invalid or unresolved')
            checks.append({'cell':key,'definitionVerified':good,'evidence':s['evidence']})
            cs='float' if t=='float' else 'int' if t=='int' else 'bool' if t=='bool' else 'string'
            literal=(str(float(v))+'f') if cs=='float' and good else str(v) if cs=='int' and good else ('true' if v else 'false') if cs=='bool' else json.dumps(v,ensure_ascii=True)
            fields.append(f'public const {cs} {ident(col)} = {literal};')
        if name=='hooks':
            p=root.parent/'reference'/row['source']
            if not p.exists() or row['member']+'(' not in p.read_text(): errors.append(f'{name}/{row["id"]}: source hook does not resolve')
            if row['system'] not in sheets: errors.append(f'{name}/{row["id"]}: missing system sheet')
        parts.append('public readonly struct '+ident(name)+'_'+ident(row['id'])+' { '+' '.join(fields)+' }')
parts.append('}')
report={'definitionChecks':len(checks),'errors':errors,'cells':checks,'runtimeVerification':'separate; not claimed by preflight'}
(root/'preflight-report.json').write_text(json.dumps(report,indent=2)+'\n')
for e in errors:print(e)
if errors:sys.exit(1)
(root/'Source').mkdir(exist_ok=True)
(root/'Source'/'SheetRows.g.cs').write_text('\n'.join(parts)+'\n')
print(f'Preflight clean: {len(checks)} cells, {sum(len(s["rows"]) for s in sheets.values())} row structs; all references resolve. Runtime testing still required.')
