import hashlib
import json
import pathlib
import re
import subprocess
import sys
import zipfile

project = pathlib.Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
label = sys.argv[1]
if not re.fullmatch(r'[A-Za-z0-9-]+', label):
    raise ValueError('Invalid build label')
folder = project / ('outputs/chapters45-2026-10-02/build-' + label)
apk = project / ('Builds/Android/TralaleroShooter-20261002-Chapters45-' + label + '.apk')
android = pathlib.Path(r'C:\Program Files\Unity\Hub\Editor\6000.2.6f1\Editor\Data\PlaybackEngines\AndroidPlayer')
jar = sorted((android / 'SDK/build-tools').glob('*/lib/apksigner.jar'))[-1]
signed = subprocess.run([str(android / 'OpenJDK/bin/java.exe'), '-jar', str(jar), 'verify', '--verbose', str(apk)], capture_output=True, text=True, encoding='utf-8', timeout=55)
(folder / 'signature-verification.txt').write_text(signed.stdout + signed.stderr, encoding='utf-8')
if signed.returncode != 0 or 'Verified using v2 scheme (APK Signature Scheme v2): true' not in signed.stdout:
    raise RuntimeError('APK signature verification did not pass: ' + signed.stdout + signed.stderr)
with zipfile.ZipFile(apk) as archive:
    names = archive.namelist()
    bad = archive.testzip()
with apk.open('rb') as stream:
    apk_hash = hashlib.file_digest(stream, 'sha256').hexdigest()
before = (folder / 'ProjectSettings.before.asset').read_bytes()
after = (project / 'ProjectSettings/ProjectSettings.asset').read_bytes()
record = {
    'path': str(apk), 'bytes': apk.stat().st_size, 'sha256': apk_hash,
    'zipCrcError': bad, 'manifestPresent': 'AndroidManifest.xml' in names,
    'nativeArchitectures': sorted(set(n.split('/')[1] for n in names if n.startswith('lib/'))),
    'serializedSceneIndices': sorted(set(int(m.group(1)) for n in names if (m := re.match(r'assets/bin/Data/level(\d+)(?:\.split\d+)?$', n)))),
    'entryCount': len(names), 'signatureVerificationExitCode': signed.returncode,
    'signatureV2Verified': True, 'development': False,
    'signing': 'local Android debug key for review; not store signing',
    'projectSettingsRestoredByteForByte': before == after,
    'projectSettingsBeforeSha256': hashlib.sha256(before).hexdigest(),
    'projectSettingsAfterSha256': hashlib.sha256(after).hexdigest(),
}
assert bad is None and record['manifestPresent'] and record['nativeArchitectures'] == ['arm64-v8a'] and record['serializedSceneIndices'] == [0, 1, 2, 3, 4] and before == after
(folder / 'package-verification.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
print(json.dumps(record, indent=2))
