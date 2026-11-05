"""Build per-hero production briefs from the checked-in art bible and actual kit."""
import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FEMALE = set('isis sekhmet nephthys bastet maat athena artemis persephone nyx freyja hel skadi amaterasu guanyin morgana cleopatra joan izanami nuwa'.split())
SPECIAL = {
    'sobek': 'Adult male crocodile-headed guardian, no human face; massive protective build.',
    'set': 'Adult masculine human desert storm warlord with red hair; angular storm crown.',
    'sekhmet': 'Adult FEMALE golden LIONESS-headed warrior, feminine powerful humanoid body, no mane or male lion face.',
    'bastet': 'Adult FEMALE black CAT-headed agile guardian, feminine humanoid body, no human face.',
    'sunwukong': 'Adult MALE MONKEY king, expressive simian face, golden eyes, circlet, red scarf; no plain human face.',
    'nezha': 'Young MALE divine martial warrior, youthful masculine face, fully covered red-gold armored silk; show both fire wheels in lower composition.',
    'hel': 'Adult female queen, one half of face living and the other pale spectral blue shadow, elegant and non-graphic, no gore.',
    'fenrir': 'A gigantic four-legged WOLF, black fur with silver-blue highlights, not humanoid; powerful jaws, paws and broken chains clearly visible. No hands, clothes, sword or human face.',
    'nuwa': 'Adult FEMALE Chinese creation goddess with human upper body and elegant jade SERPENT lower body visible curling into composition, five-color stones. Not a human-legged woman.',
    'hundun': 'Primordial FACELESS cosmic creature of wings, rounded dark nebula form and floating shards; no eyes, mouth, humanoid head, clothes or hands. No invented human gender.',
    'apep': 'Enormous primordial black-violet COBRA SERPENT, nonhumanoid, colossal coils encircling devoured sun, no human head/hands/clothes/weapons.',
    'odin': 'Adult MALE elder with long silver beard, exactly ONE functional eye and an eye patch, Gungnir spear and exactly TWO ravens.',
    'thor': 'Adult MALE powerful RED-BEARDED Norse warrior, weathered iron, runic hammer; no blond clean-shaven superhero likeness.',
    'merlin': 'Adult MALE ELDER wizard, long silver beard, indigo scholarly robe, oak astral staff and old spellbook.',
    'joan': 'Young adult FEMALE medieval French commander, short brown hair, modest full steel plate and white banner; courageous human, no goddess crown.',
    'amun': 'Adult MALE HUMAN hidden Egyptian creator, tall TWO-PLUMED feather crown and layered gold-white veil, mysterious human face distinct from Ra.',
}


def brief(hid):
    bible = json.loads((ROOT / 'docs/HERO_ART_DIRECTION_ALL_41.json').read_text(encoding='utf-8'))
    hero = next(h for h in bible['heroes'] if h['id'] == hid)
    src = (ROOT / 'Assets/Chronicles/HeroAbilityCatalog.cs').read_text(encoding='utf-8')
    kit = src.split(f'new HeroKit("{hid}"', 1)[1].split('),', 1)[0]
    strings = re.findall(r'"([^"\n]*)"', kit)
    identity = SPECIAL.get(hid, 'Adult FEMALE woman.' if hid in FEMALE else 'Adult MALE man, masculine face.')
    anatomy = ('Show complete defining creature anatomy clearly.' if hid in ('fenrir','apep','hundun') else
               'BOTH forearms, wrists and hands fully inside canvas and readable, anatomically natural fingers gripping the named props; no hidden/missing hand. Place large named props within composition.')
    static = (f'Use case: stylized-concept. ONE individual original luxury painterly ANIME mythology game portrait of {hero["name"]} '
              f'from {hero["origin"]}, {hero["rarity"]} {hero["element"]} {hero["role"]}. {identity} '
              f'Exact design brief: {hero["silhouette"]}. Distinctive weapon/props: {hero["weapon"]}. '
              f'Setting: {hero["setting"]}. Actual game abilities: {"; ".join(strings)}. '
              f'Express role and divine/heroic presence through a restrained scene motif inspired by: {hero["ultimateVfx"]}. '
              f'Imposing cinematic low-angle three-quarter composition, readable face/silhouette at thumbnail, ornate convincing materials, powerful dignified character-specific presence. '
              f'{anatomy} Preserve specified gender/species/culture; fully covered appropriate ceremonial outfit or armor for humanoids. '
              'Premium polished original anime finish, not copied from any game, distinct from other roster heroes. '
              'Full-bleed vertical2:3 target1024x1536. No writing, runic letters, hieroglyph text, numbers, logos, watermark, card frame, UI, panel borders, collage, extra people or extra limbs.')
    animation = (f'Edit target: supplied {hero["name"]} portrait. EXACTLY8 sequential subtle idle frames in a STRICT regular4-column x2-row grid, '
                 'zero gaps or margins, requested2048x1536 sheet; every equal cell repeats COMPLETE2:3 portrait with identical framing. '
                 f'Preserve same {identity} Lock identity, face/anatomy, gender/species, costume patterns, colors, named props ({hero["weapon"]}), both hands/limbs and their grips. '
                 f'Fixed camera and identical background ({hero["setting"]}). Subtle character idle inspired by {hero["idle"]}, '
                 'reduced to tiny organic motion returning to start, no changing pose or spinning weapon. Eyes if present: open1-2, halfclosed3, closed4, halfopen5, open6-8. '
                 'Only small hair/fur/cloth/feather movement and gentle character-specific glow pulse; no camera zoom, large particles or attack scene. '
                 'First/last nearly identical for looping. Preserve both hands and all props fully visible in every frame; for nonhumanoid creatures no added human hands. '
                 'No labels, typography, UI, numbers, borders, dividers, watermark, new objects or extra limbs. Consistent original premium anime details.')
    if hid == 'apep':
        animation = animation.replace('Eyes if present: open1-2, halfclosed3, closed4, halfopen5, open6-8.',
            'Snake eyes stay OPEN and identical throughout: NO blinking, no eyelids. Only slight coil breathing and glow.')
    return dict(hero=hero, static=static, animation=animation)


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('hero')
    p.add_argument('--save', action='store_true')
    args = p.parse_args()
    result = brief(args.hero)
    if args.save:
        dest = ROOT / 'art-source' / args.hero
        dest.mkdir(parents=True, exist_ok=True)
        (dest / 'prompts.json').write_text(json.dumps(result, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))
