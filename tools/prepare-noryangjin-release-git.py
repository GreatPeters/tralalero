"""Stage explicit project paths; retain local caches and existing unrelated index entries."""
from pathlib import Path
import subprocess

roots = ['.gitignore', 'ARCHITECTURE.md', 'BALANCE_OVERVIEW.md', 'DEVELOPMENT_OVERVIEW.md',
         'MAP_DESIGN_OVERVIEW.md', 'Assets', 'docs', 'tools', 'map-concepts']
def paths(args):
    return [p for p in subprocess.check_output(['git', '-c', 'core.quotepath=false', *args]).split(b'\0') if p]
candidates = set(paths(['diff', '--name-only', '-z', 'HEAD', '--', *roots]))
candidates.update(paths(['ls-files', '--others', '--exclude-standard', '-z', '--', *roots]))
selected = sorted(p for p in candidates if not p.startswith((b'Assets/_Recovery/', b'tmp/'))
                  and p != b'Assets/_Recovery.meta' and b'/node_modules/' not in p and b'/__pycache__/' not in p)
destination = Path('tmp/backups/sr18-release-20260910/publish-pathspec.nul')
destination.write_bytes(b'\0'.join(selected) + b'\0')
subprocess.run(['git', 'add', '--pathspec-from-file=' + str(destination), '--pathspec-file-nul'], check=True)
print(f'Staged {len(selected)} explicit project paths; local tmp and recovery preserved.')
