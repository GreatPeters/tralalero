# Quaternius Universal Animation Library

- Source: https://quaternius.com/packs/universalanimationlibrary.html
- Download: https://quaternius.itch.io/universal-animation-library
- Package: `Universal Animation Library[Standard].zip`
- Downloaded: 2026-08-30
- License: CC0 1.0 Universal
- Original archive SHA-256: `CC73FC4E495B82958207316596317A3F40B9FA38065BDE1027937452DA537724`
- Retained file: `Unity/UAL1_Standard.fbx`
- Retained FBX SHA-256: `21B32D912DA3CB93426D974FB945E86F5B2E86970ACD2CE89905E0FBF9F1DCC2`

Only the non-root-motion Unity FBX is retained. The Forward enemy setup uses
locomotion clips from this source for `walk` and `run`; existing enemy idle,
attack, and death clips remain character-specific and unchanged.

Unity imports only `Armature|Walk_Loop` and `Armature|Sprint_Loop` from the
source FBX. Both are configured as looping Humanoid clips. The other source
animations remain inside the original FBX file but are excluded from Unity's
clip import list to keep the Library cache and reimport work narrow.
