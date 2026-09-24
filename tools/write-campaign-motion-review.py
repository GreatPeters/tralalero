"""Retain the evidence-backed motion refinement decision."""
import json
from pathlib import Path

root=Path('tmp/image-previews/campaign-balance-2026-09-23')
roles=['ParkingMarshal','CoffeeVendor','SnackChef']
checks=[]
for role in roles:
    folder=root/'reststop-motion-v2'/role
    for kind in ['blend','fbx','glb']:
        path=folder/f'{kind}-metrics.json';data=json.loads(path.read_text(encoding='utf-8'))
        assert data['hard_gate_pass'] and not data['issues'],path
        checks.append({'role':role,'format':kind,'metrics':path.as_posix(),'triangles':data['totals']['triangles'],'passed':True})
review={
 'contract':'map-concepts/campaign-balance-2026-09-23/reststop-motion-contract.md',
 'decision':'retain_repair',
 'selectedDefect':'Attack moves behind the face direction; tools and release do not match the final hand pose.',
 'sourceCause':'Earlier generic action curves used the wrong forward sign, with independent throw timing and root attachments.',
 'repair':'Preserve bodies/textures and 18-bone hierarchy; author seven actions, fit native tools, normalize native height, retime strike to workbook delay and release after LateUpdate.',
 'candidateEvidence':{r:(root/'reststop-motion-v2'/r/'before').as_posix() for r in roles},
 'requirementLedger':[
  {'requirement':'Forward attack with anticipation and recovery','before':'fail','after':'pass','evidence':'reststop-motion-v2/*/after/animation_contact_sheet.png; native-motion.mp4'},
  {'requirement':'Visible hand/tool connection and readable cup launch','before':'fail','after':'pass','evidence':'showcase-native-v2/CoffeeVendor; projectile-native-v2/CoffeeVendor; projectile-retimed-055/TireBruiser; projectile-retimed-080/TireBruiser'},
  {'requirement':'Comparable native height and clear hit/death response','before':'fail','after':'pass','evidence':'showcase-native-v1/ParkingMarshal; showcase-native-v2/CoffeeVendor; showcase-native-v2/SnackChef'},
  {'requirement':'Source and fresh-import technical gates','before':'pass with one Coffee GLB tiny-degenerate warning','after':'pass','evidence':checks},
  {'requirement':'Body identity, textures and hierarchy retained','before':'pass','after':'pass','evidence':'before/after fixed-view sheets and motion-report.json for all three roles'}
 ],
 'invariantResults':{'bodyGeometry':'preserved','textures':'preserved','boneNamesAndHierarchy':'preserved','authoredScale':'preserved; native height intentionally normalized to about3.05m','capsuleRadius':'preserved','tools':'native project assets; legacy Equipment hidden in FBX for stable node IDs','glbExport':'animated bodies only; native tools separately verified'},
 'finalEvidence':{r:{'freshImport':(root/'reststop-motion-v2'/r/'fresh-glb-clean').as_posix(),'native':(root/('showcase-native-v1' if r=='ParkingMarshal' else 'showcase-native-v2')/r).as_posix()} for r in roles},
 'renderCorrection':'Exclude glTF_not_exported importer gizmos from bounds; use evaluated geometry. Earlier fresh-glb screenshots are retained diagnostics and do not establish foot contact.'
}
path=Path('map-concepts/campaign-balance-2026-09-23/iteration_review.json')
path.write_text(json.dumps(review,ensure_ascii=False,indent=2),encoding='utf-8');print(path)
