from pathlib import Path
import re
p=Path(r'C:\Users\mongo\UnityProjects\147 VR\Assets\Scenes\147VR_MainScene.unity')
text=p.read_text(encoding='utf-8-sig')
parts=re.split(r'(?=^--- !u!\d+ &\S+\s*$)', text, flags=re.M)
roots={'1496539221','1526081159','1599114119','1761855441'}
trans={}; go_of_trans={}; parent={}; docs={}
for d in parts:
 m=re.match(r'^--- !u!(\d+) &(\S+)\s*$', d, re.M)
 if not m: continue
 typ,id=m.groups(); docs[id]=d
 if typ=='4':
  g=re.search(r'm_GameObject: \{fileID: (\d+)\}',d); f=re.search(r'm_Father: \{fileID: (\d+)\}',d)
  if g: go_of_trans[id]=g.group(1)
  if f: parent[id]=f.group(1)
for tid in list(parent):
 if parent[tid] in roots: trans[tid]=1
changed=True
while changed:
 changed=False
 for tid,par in list(parent.items()):
  if par in trans and tid not in trans: trans[tid]=1; changed=True
trans.update({r:1 for r in roots})
gos={go_of_trans[t] for t in trans if t in go_of_trans}
remove=set(trans)|gos
out=[]; removed=0
for d in parts:
 m=re.match(r'^--- !u!\d+ &(\S+)\s*$',d,re.M)
 if m and m.group(1) in remove: removed+=1; continue
 out.append(d)
p.write_text(''.join(out),encoding='utf-8')
print(f'REMOVED_DOCUMENTS={removed} TRANSFORMS={len(trans)} GAMEOBJECTS={len(gos)}')
print('ROOTS='+','.join(sorted(roots)))