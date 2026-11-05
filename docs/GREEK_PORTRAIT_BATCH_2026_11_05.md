# Greek portrait continuation during Unity installation

All new images are original built-in image_gen outputs, individually prompted.
No source reference attachments were available from the old conversation.
Runtime language remains English. Portraits contain no baked text/UI/borders.

| Hero | Static | Frames | Visual review | Unity/device status |
|---|---|---|---|---|
| Artemis | 1024x1536 PNG | 8 x 362x543 | Adult female, crescent circlet, green cloak, silver bow, quiver; blink present; minor generative drift | Awaiting Unity installation |
| Persephone | 1024x1536 PNG | 8 x 362x543 | Adult female, pomegranate, auburn hair, blossom/ivy crown; blink present; minor detail drift | Awaiting Unity installation |

These are candidate idle loops, not final production animation approval.
Full source sheets, exact prompts, GIF previews and frame-difference metrics are
under `art-source/<hero>/`. Flat images are not layered rigging source.
Existing importer handles Texture2D loading with mipmaps/readback disabled and
Android ASTC. Files and .meta are committed; actual Unity import/compile pending.

Run `python tools/portrait-assets.py audit` and
`node tools/chronicles-source-audit.mjs` after every addition.
