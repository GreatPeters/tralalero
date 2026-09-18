"""Record reviewed exports and stage selected movies for Unity scene binding."""
import hashlib
import json
from pathlib import Path
import shutil

root = Path(__file__).resolve().parent.parent
record = root/'map-concepts/chapters-polish-2026-09-12'
outputs = root/'outputs/chapters-polish-2026-09-12'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def copy_verified(source, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists() and digest(source) != digest(destination):
        raise RuntimeError('Preserve existing different asset: '+str(destination))
    if not destination.exists():
        shutil.copy2(source, destination)
    if digest(source) != digest(destination):
        raise RuntimeError('Copy verification failed')

review = {'unityIntegration': 'pending-main-thread-recovery', 'enemies': [], 'props': [], 'movies': []}
for folder in sorted((outputs/'rigged/v2').iterdir()):
    if not folder.is_dir():
        continue
    mesh = json.loads((folder/'fresh-inspection.json').read_text(encoding='utf-8'))
    fbx = json.loads((folder/'fresh-fbx-fullkeys-pose-report.json').read_text(encoding='utf-8'))
    if not mesh['hard_gate_pass'] or fbx['bones'] != 18 or len(fbx['actions']) != 6:
        raise RuntimeError('Rig contract failed: '+folder.name)
    source = folder/(folder.name+'-full-keys.fbx')
    review['enemies'].append({'name': folder.name, 'source': str(source.relative_to(root)), 'sha256': digest(source),
        'triangles': mesh['totals']['triangles'], 'bones': 18, 'actions': 6,
        'fbxGroundMinRange': [min(p['minZ'] for p in fbx['poses']), max(p['minZ'] for p in fbx['poses'])],
        'visualReview': 'front/back and walking/attack evidence reviewed; multiview files retained; live Unity check pending'})
for identity in [49,54,57,64,67,68,69,80]:
    revision = 'v5' if identity in [49,64] else 'v3'
    folder = outputs/('props-'+revision)/str(identity)
    mesh = json.loads((folder/'fresh-inspection.json').read_text(encoding='utf-8'))
    if not mesh['hard_gate_pass']:
        raise RuntimeError('Prop contract failed: '+str(identity))
    source = folder/'Model.fbx'
    review['props'].append({'id': identity, 'source': str(source.relative_to(root)), 'sha256': digest(source),
        'triangles': mesh['totals']['triangles'], 'visualReview': 'removed floor/scraps; retained physical supports; Unity material and placement pending'})

decisions = {
    'highway-A-v3': ('selected', 'Visible moving traffic and frightened shark; two legs and tail shoe retained.', 'Highway_Entry'),
    'highway-B-v3': ('rejected', 'Tail shoe disappears and a free forked tail emerges around2seconds.', None),
    'reststop-A-v3': ('alternate', 'Correct giant scale, fear and tail-shoe anatomy; restrained movement.', None),
    'reststop-B-v3': ('selected', 'Clear giant scale, frightened diners and stronger upper-body reactions; tail shoe retained.', 'RestStop_Entry')
}
for folder in sorted((outputs/'transitions').iterdir()):
    result = folder/'result.json'
    if not result.exists():
        continue
    data = json.loads(result.read_text(encoding='utf-8'))
    status, reason, target = decisions.get(folder.name, ('superseded', 'Predates the user correction to two legs plus a shoe-wearing tail.', None))
    data['visualReview'] = {'status': status, 'reason': reason, 'method': 'sampled frames including start/middle/end and one-second contact sheet'}
    result.write_text(json.dumps(data, indent=2), encoding='utf-8')
    if target:
        source = folder/(folder.name+'-5B.mp4')
        if digest(source) != data['clipSha256']:
            raise RuntimeError('Candidate movie hash changed')
        destination = root/'Assets/JH/UI/ChapterTransitions'/(target+'.mp4')
        copy_verified(source, destination)
        poster = record/'references/transitions'/(folder.name+'.png')
        copy_verified(poster, destination.with_suffix('.png'))
        review['movies'].append({'name': target, 'candidate': folder.name, 'path': str(destination.relative_to(root)),
            'sha256': digest(destination), 'frames': 121, 'fps': 24, 'durationSeconds': 121/24,
            'anatomy': 'two legs plus one shoe-wearing tail', 'sceneBinding': 'pending'})
(record/'review/accepted-exports.json').write_text(json.dumps(review, indent=2), encoding='utf-8')
print(json.dumps({'rigs': len(review['enemies']), 'props': len(review['props']), 'moviesStaged': len(review['movies']),
                  'unityIntegration': review['unityIntegration']}))
