"""Package a manually reviewed candidate pair, audit it, then commit and push that hero."""
import argparse
import csv
import json
import subprocess
import sys
from pathlib import Path

from importlib.util import module_from_spec, spec_from_file_location

ROOT = Path(__file__).resolve().parents[1]
spec = spec_from_file_location('portrait_assets', ROOT / 'tools/portrait-assets.py')
assets = module_from_spec(spec)
spec.loader.exec_module(assets)


def run(*args):
    subprocess.run(args, cwd=ROOT, check=True)


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('hero')
    p.add_argument('source', type=Path)
    p.add_argument('sheet', type=Path)
    args = p.parse_args()
    assets.split(args.hero, args.source, args.sheet)
    bible_path = ROOT / 'docs/HERO_ART_DIRECTION_ALL_41.json'
    bible = json.loads(bible_path.read_text(encoding='utf-8'))
    hero = next(h for h in bible['heroes'] if h['id'] == args.hero)
    hero['productionStatus'] = 'CANDIDATE_STATIC_AND_8_FRAMES_UNITY_REVIEW_PENDING'
    bible_path.write_text(json.dumps(bible, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    manifest_path = ROOT / 'docs/HERO_PORTRAITS_41_ASSET_MANIFEST.csv'
    with manifest_path.open(encoding='utf-8-sig', newline='') as f:
        reader = csv.DictReader(f)
        fields, rows = reader.fieldnames, list(reader)
    for row in rows:
        if row['hero_id'] == args.hero:
            row['portrait_asset_status'] = 'CANDIDATE_UNITY_REVIEW_PENDING'
    with manifest_path.open('w', encoding='utf-8', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fields, quoting=csv.QUOTE_ALL)
        writer.writeheader()
        writer.writerows(rows)
    note = ROOT / 'art-source' / args.hero / 'REVIEW.md'
    note.write_text(f'# {hero["name"]} candidate portrait\n\n'
        'Static and sheet visually checked against the identity, full art brief and game kit.\n'
        'Eight cells sliced without invented motion or artificial upscaling.\n'
        'Exact measured dimensions and file checks: docs/portrait-file-audit.json.\n'
        'Minor generative detail drift may remain. Unity import, loop playback,\n'
        'Android memory/performance and final visual approval are pending.\n'
        'Flat source sheet is not layered rigging source; no attack animation claimed.\n', encoding='utf-8')
    run(sys.executable, 'tools/portrait-assets.py', 'audit')
    run('node', 'tools/chronicles-source-audit.mjs')
    run('git', 'diff', '--check')
    run('git', 'add', hero['staticPath'], hero['staticPath']+'.meta', hero['framesFolder'],
        hero['framesFolder']+'.meta', f'art-source/{args.hero}',
        'docs/HERO_ART_DIRECTION_ALL_41.json', 'docs/HERO_PORTRAITS_41_ASSET_MANIFEST.csv',
        'docs/portrait-file-audit.json', 'tools/portrait-prompts.py', 'tools/deliver-portrait.py')
    run('git', '-c', 'user.name=Codex', '-c', 'user.email=codex@openai.com', 'commit',
        '-m', f'art(portraits): add {hero["name"]} static and eight candidate idle frames')
    run('git', 'push', 'origin', 'feature/egyptian-heroes-5v5')
