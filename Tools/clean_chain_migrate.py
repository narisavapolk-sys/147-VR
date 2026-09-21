from pathlib import Path
import re

root = Path(r'C:\Users\mongo\UnityProjects\147 VR')
scene = root / 'Assets/Scenes/147VR_MainScene.unity'
text = scene.read_text(encoding='utf-8')
backup = scene.with_name(scene.name + '.PRE_CLEAN_CHAIN_MANUAL_20260903_2255.bak')
backup.write_text(text, encoding='utf-8')

# Disable only the legacy v004 child prefab instance; keep its parent intact for rollback.
old_start = text.index('--- !u!1001 &159490237')
old_end = text.index('\n--- !u!4 &159490238 stripped', old_start)
old_block = text[old_start:old_end]
if 'propertyPath: m_IsActive' not in old_block:
    marker = '    m_RemovedComponents: []\n'
    insert = ('    - target: {fileID: 5894092392212040973, guid: ccb3f5197b75d7945a9215ef055d9b51, type: 3}\n'
              '      propertyPath: m_IsActive\n'
              '      value: 0\n'
              '      objectReference: {fileID: 0}\n')
    if marker not in old_block:
        raise RuntimeError('Could not locate old prefab modification insertion point')
    old_block = old_block.replace(marker, insert + marker, 1)
    text = text[:old_start] + old_block + text[old_end:]

# Add Golden prefab instance once, as a sibling of the legacy visual under the existing table parent.
new_instance_id = 209730001
new_transform_id = 209730002
new_instance = f'''--- !u!1001 &{new_instance_id}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {{fileID: 1713820525}}\n    m_Modifications:\n    - target: {{fileID: 1701257831388998505, guid: cd255914573c9b04f9b2b77c0fe43f7c, type: 3}}\n      propertyPath: m_Name\n      value: Prefab_WPBSA_12Foot_Snooker\n      objectReference: {{fileID: 0}}\n    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: {{fileID: 100100000, guid: cd255914573c9b04f9b2b77c0fe43f7c, type: 3}}\n--- !u!4 &{new_transform_id} stripped\nTransform:\n  m_CorrespondingSourceObject: {{fileID: 2841700973634272696, guid: cd255914573c9b04f9b2b77c0fe43f7c, type: 3}}\n  m_PrefabInstance: {{fileID: {new_instance_id}}}\n  m_PrefabAsset: {{fileID: 0}}\n'''
if f'--- !u!1001 &{new_instance_id}' not in text:
    insert_at = text.index('\n--- !u!4 &159490238 stripped')
    text = text[:insert_at] + '\n' + new_instance + text[insert_at:]

# Rewire exactly the three requested tableRoot fields.
needle = 'tableRoot: {fileID: 1713820525}'
count = text.count(needle)
if count != 3:
    raise RuntimeError(f'Expected 3 tableRoot references, found {count}')
text = text.replace(needle, f'tableRoot: {{fileID: {new_transform_id}}}', 3)

scene.write_text(text, encoding='utf-8')
print('MIGRATION_OK')
print('backup', backup)
print('golden_instance', new_instance_id, 'transform', new_transform_id)
print('rewired_tableRoots', count)
print('old_v004_guid_remaining', text.count('ccb3f5197b75d7945a9215ef055d9b51'))
print('golden_guid_count', text.count('cd255914573c9b04f9b2b77c0fe43f7c'))
print('physics_runtime_roots_active_blocks', len(re.findall(r'm_Name: Physics Table \(runtime\)[\s\S]{0,250}?m_IsActive: 1', text)))
