"""Package the existing web game without Unity assets or obsolete iPhone bundles."""
from pathlib import Path
import hashlib
import json
import zipfile

root = Path(__file__).resolve().parent.parent
out = root / 'artifacts/building-sweep'
release = json.loads((root / 'dist/iphone/release.json').read_text(encoding="utf-8"))
active = Path(release['url']).name
files = []
for file in (root / 'dist').rglob('*'):
    if not file.is_file():
        continue
    if file.parent == root / 'dist/iphone' and file.name.startswith('game-') and file.name != active:
        continue
    if file.suffix.lower() in ['.html', '.js', '.css', '.json', '.png', '.webmanifest', '.svg']:
        files.append(file)
for directory in ['server', 'tests', 'node_modules/ws']:
    files.extend(p for p in (root / directory).rglob('*') if p.is_file())
for name in ['README.md', 'BUILDING_SWEEP.md', 'package.json', 'package-lock.json', 'Start-BuildingSweep.cmd', 'Start-BuildingSweep.ps1']:
    files.append(root / name)
for name in ['build-mobile.cjs', 'build-iphone.cjs', 'check.cjs', 'validate-buildings.cjs', 'test-building-browser.cjs', 'test-building-lifecycle.cjs', 'test-building-mobile.cjs', 'package-building.py']:
    files.append(root / 'scripts' / name)
files.extend(p for p in (root / 'mobile').iterdir() if p.is_file() and p.suffix in ['.js', '.html', '.md'])
for name in ['TEST_REPORT.md', 'browser-report.json', 'lifecycle-report.json', 'mobile-report.json', '01-loadout.png', '02-briefing.png', '03-building.png', '04-result.png', '05-office.png']:
    files.append(out / name)
archive = out / 'TacticalZero-4.0-BuildingSweep-WebSource.zip'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as bundle:
    for file in sorted(set(files)):
        bundle.write(file, file.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as bundle:
    assert bundle.testzip() is None
    for name in ['dist/src/building-game.js', 'dist/src/building-systems.js', 'dist/src/building-data.js', 'dist/src/pwa-update.js', 'dist/iphone/' + active, 'server/index.mjs', 'node_modules/ws/package.json', 'BUILDING_SWEEP.md']:
        assert name in bundle.namelist(), name
manifest = dict(version='4.0.0', archive=archive.name, files=len(set(files)), bytes=archive.stat().st_size,
                sha256=hashlib.sha256(archive.read_bytes()).hexdigest(), iphone=release,
                browserChecks=len(json.loads((out / 'browser-report.json').read_text(encoding="utf-8"))['checks']),
                mobileChecks=len(json.loads((out / 'mobile-report.json').read_text(encoding="utf-8"))['results']))
(out / 'DELIVERY.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(manifest, ensure_ascii=False, indent=2))

