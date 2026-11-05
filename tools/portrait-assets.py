"""Inspect PNG payloads; split an AI-authored regular animation sheet (no motion synthesis).

Requires Pillow. Run from repository root. Outputs never overwrite existing art.
"""
import argparse
import csv
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/Resources/Chronicles/Portraits'


def audit():
    rows, errors = [], []
    manifest = ROOT / 'docs/HERO_PORTRAITS_41_ASSET_MANIFEST.csv'
    for hero in csv.DictReader(manifest.open(encoding='utf-8-sig')):
        hid = hero['hero_id']
        still = ART / 'Static' / f'{hid}.png'
        frames = sorted((ART / 'Animated' / hid).glob('frame_*.png'))
        paths = ([still] if still.exists() else []) + frames
        dimensions, hashes = [], []
        for p in paths:
            try:
                with Image.open(p) as im:
                    assert im.format == 'PNG', 'PNG extension with non-PNG payload'
                    im.load()
                    dimensions.append(im.size)
                    hashes.append(hashlib.sha256(im.convert('RGB').tobytes()).hexdigest())
            except Exception as ex:
                errors.append(f'{p.relative_to(ROOT)}: {ex}')
        if frames:
            expected = [f'frame_{i:03}.png' for i in range(len(frames))]
            if [p.name for p in frames] != expected or len(frames) not in (8, 12):
                errors.append(f'{hid}: expected 8 or 12 consecutive frames')
            frame_sizes = dimensions[1:] if still.exists() else dimensions
            if len(set(frame_sizes)) != 1:
                errors.append(f'{hid}: inconsistent frame sizes')
            frame_hashes = hashes[1:] if still.exists() else hashes
            if len(set(frame_hashes)) != len(frames):
                errors.append(f'{hid}: duplicate decoded frames')
        rows.append(dict(hero_id=hid, static=still.exists(), frames=len(frames),
                         dimensions=dimensions, visual_approval='MANUAL_REVIEW_REQUIRED'))
    report = dict(heroes=rows, errors=errors,
                  note='File validation only. Not identity approval, Unity import, or Android QA.')
    out = ROOT / 'docs/portrait-file-audit.json'
    out.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f'{len(rows)} heroes; {sum(r["static"] for r in rows)} stills; '
          f'{sum(r["frames"] >= 8 for r in rows)} frame sets; {len(errors)} file errors')
    for error in errors:
        print(error)
    raise SystemExit(bool(errors))


def split(hero, source, sheet):
    known = {h['hero_id'] for h in csv.DictReader(
        (ROOT / 'docs/HERO_PORTRAITS_41_ASSET_MANIFEST.csv').open(encoding='utf-8-sig'))}
    if hero not in known:
        raise ValueError('Unknown hero ID')
    still = ART / 'Static' / f'{hero}.png'
    folder = ART / 'Animated' / hero
    if still.exists() or folder.exists():
        raise FileExistsError('Review existing artwork before replacing it')
    with Image.open(source) as original, Image.open(sheet) as grid:
        w, h = grid.size
        if w % 4 or h % 2:
            raise ValueError(f'Sheet {w}x{h} cannot split evenly into 4x2 cells')
        cw, ch = w // 4, h // 2
        if abs(cw / ch - 2 / 3) > .01:
            raise ValueError('Animation cells must be 2:3 portraits')
        frames = [grid.crop((x*cw, y*ch, (x+1)*cw, (y+1)*ch)).convert('RGB')
                  for y in range(2) for x in range(4)]
        still.parent.mkdir(parents=True, exist_ok=True)
        folder.mkdir(parents=True)
        original.save(still, format='PNG')
        for i, frame in enumerate(frames):
            frame.save(folder / f'frame_{i:03}.png')
        preview = ROOT / 'art-source' / hero
        preview.mkdir(parents=True, exist_ok=True)
        # Preview only, outside Resources; PNGs are the runtime assets.
        frames[0].save(preview / 'idle-preview.gif', save_all=True,
                       append_images=frames[1:], duration=125, loop=0)
        metrics = [sum(ImageStat.Stat(ImageChops.difference(frames[i], frames[(i+1)%8])).mean)/3
                   for i in range(8)]
        (preview / 'frame-differences.json').write_text(json.dumps(dict(
            cell_size=[cw, ch], adjacent_mean_absolute_rgb_difference=metrics,
            note='Includes final-to-first seam. Visual review remains required.'), indent=2)+'\n')
        print(f'{hero}: {original.size} static; 8 frames {cw}x{ch}; preview {preview}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest='command', required=True)
    sub.add_parser('audit')
    p = sub.add_parser('split')
    p.add_argument('hero')
    p.add_argument('source', type=Path)
    p.add_argument('sheet', type=Path)
    args = parser.parse_args()
    if args.command == 'audit':
        audit()
    else:
        split(args.hero, args.source, args.sheet)
