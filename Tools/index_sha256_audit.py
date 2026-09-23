#!/usr/bin/env python3
import hashlib, re, subprocess
INDEX='Docs/AI_TEAM/COACH_ARTIFACT_INDEX_20260923.md'
def gb(path):
    return subprocess.run(['git','show','HEAD:'+path],check=True,stdout=subprocess.PIPE).stdout
lines=gb(INDEX).decode('utf-8').splitlines()
rows=[]
for line in lines:
    parts=line.split('|')
    if len(parts)==4 and parts[1].strip().startswith('`') and parts[2].strip().startswith('`'):
        path=parts[1].strip().strip('`')
        if '/' not in path: path='Docs/AI_TEAM/'+path
        expected=parts[2].strip().strip('`')
        if re.fullmatch(r'[0-9a-f]{64}',expected): rows.append((path,expected))
seen=set(); match=mismatch=missing=0; out=[]
head=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()
out += ['A1.3 INDEX SHA256 AUDIT — canonical git bytes',f'HEAD={head}',f'rows={len(rows)}']
for path,expected in rows:
    if path in seen: raise SystemExit('DUPLICATE '+path)
    seen.add(path)
    try: actual=hashlib.sha256(gb(path)).hexdigest()
    except subprocess.CalledProcessError: missing+=1; out.append('MISSING '+path); continue
    if actual==expected: match+=1; out.append('MATCH '+path+' '+actual)
    else: mismatch+=1; out.append('MISMATCH '+path+' index='+expected+' blob='+actual)
out.append(f'SUMMARY MATCH={match} MISMATCH={mismatch} MISSING={missing} DUPLICATES=0')
print('\n'.join(out))
raise SystemExit(2 if mismatch or missing else 0)