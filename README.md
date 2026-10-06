# ULTRA R.E.P.O. — development prototype

**Experimental build: multiplayer has not been tested with two players.** Solo movement, gun controls and staged combat have passed native-game checks. Private hosting was checked with one client; joining, combat replication and remote V1 visuals remain unverified.

A real R.E.P.O. BepInEx 5 mashup, using V1, weapon models/animations, coins and sounds from the player's installed ULTRAKILL copy.

Start with R.E.P.O.'s normal looting objective. Fragile valuables keep their native physics, damage, value and extraction rules. Draw a revolver or shotgun to protect the haul; holster to grab valuables again. Dash, slide and slam through rooms. Coin ricochets, shotgun projectile boosts and timed close-range parries reward style. Style refills dash energy and briefly improves damage and firing speed; it never adds money or shields loot.

## Requirements and current scope

Both R.E.P.O. and ULTRAKILL must be installed. R.E.P.O. is the running host game; ULTRAKILL supplies local assets and is never redistributed. Tested installations: R.E.P.O. v0.4.4.3 / Steam build 23363152, ULTRAKILL Steam build 22957324. Other versions are unverified. BepInEx 5.4.23.5 is the loader Melty can install automatically. No Python, toolkit, asset export or additional runtime is needed by players.

Solo and experimental private co-op are implemented. Native Steam/Photon lobbies support up to six players; every player needs both games and the same mod. TWO-CLIENT HOST/JOIN AND COMBAT ARE STILL UNVERIFIED. Joining uses the game's private lobby/invite menus; automatic joining is not enabled. Do not infer working co-op from successful compilation. This development build is being shared with that limitation explicitly disclosed.

## Controls

Defaults: G draw/holster, R switch weapons, left mouse fire, right mouse toss a revolver coin, F parry or boost a freshly fired shotgun shot. Dash uses the player's saved R.E.P.O. Sprint binding; slide/slam uses the saved Crouch binding. The HUD displays actual bindings. Custom mod actions are remappable in BepInEx/config/local.ultrahaul.cfg. Ammunition is unlimited.

Parries currently counter native enemy hits inside three metres while facing the attacker, within a 0.22-second window. Shotgun boosts turn a traveling pellet packet into an explosive shot; punch before it hits anything. Intentional slams reset the player's fall distance; ordinary falls and loot keep native damage rules. Hard impacts can still cause native knockdowns, including during slams. Native loot remains breakable, including from combat impacts.

## Development and verification

JSON sheets in Sheets are the source of truth. Build.ps1 runs a complete cell/reference preflight and generates one C# struct per row before compilation. Tools/self_check.py rejects missing cells, unresolved references and nonfinite values. Tools/runtime_check.py drives the actual installed game and checks real native movement, enemy damage and timed incoming-hit hooks; enter a fresh development solo run through the native menus before running it. Development commands require ULTRAHAUL_DEV=1 and stay inactive during normal play. test-results.json records passes and partial evidence on failure; only passed=true means the full selected check completed. Natural AI attack timing, two-player replication and other game versions need separate playtests.

Current build checks passed separately: native dash (22.46 m/s), slide input, jump (0.61 m rise), slam (-27.40 m/s), G draw/holster, R weapon swap, F parry input and mouse firing, all with god mode disabled. Native enemy damage, coin ricochet, projectile boost and style were checked in a staged solo combat sequence. God mode isolated weapon checks; the timed native incoming-hit check ran with it disabled. These receipts do not establish natural AI attack timing, a full extraction run, two-player joining/replication or the Melty app installation. The Melty recipe checker returned oneClick=yes; an actual Melty installation is still required before claiming it plays in one click.

## Credits and publishing choices

Concept and gameplay choices: roryk.
ULTRAKILL assets: Arsi “Hakita” Patala, the ULTRAKILL team and New Blood Interactive, loaded only from the player's copy.
R.E.P.O.: semiwork; native worlds, enemies, physics, valuables, extraction and Steam/Photon networking.
BepInEx and Harmony: their respective open-source contributors, provided by the loader.
Development toolkit: rehan-remade/universal-modder. Built with Codex assistance. No paid asset generation used.

Original mod code and documentation: MIT, copyright 2026 roryk. Melty remixes allowed. The MIT license does not cover either game's assets; those remain in each player's own installation under their respective terms. This is an experimental development release with untested multiplayer. Melty installation must be tested before the listing goes live.


Source and experimental downloads: https://github.com/roryekay/ULTRA-REPO
Melty listing: https://melty.gg/m/ultra-r-e-p-o (app Test required before it goes live).
