# ULTRA R.E.P.O. development journal

User-approved loot-first R.E.P.O. mashup: V1 character and movement, revolver/coins, shotgun/projectile boosts, close-range parries and style that fuels combat. Both solo and private co-op are required for the first release. Native fragile loot damage, value, physics and extraction stay active.

## Current state — 2026-10-05

Title ULTRA R.E.P.O.; tagline Move like V1. Bring the loot home intact. Concept/gameplay credit roryk. Original code/documentation MIT; Melty remixes allowed. Assets belong to the game creators and are read from the player's installed copies, never distributed.

Melty modId 74bbc5a5-0a4d-45f5-a6ca-317116c0c45f, slug ultra-r-e-p-o. Studio https://melty.gg/studio/74bbc5a5-0a4d-45f5-a6ca-317116c0c45f. Metadata draft only: no release package uploaded or submitted, nothing published. Screenshot and 20.166-second silent native gameplay clip uploads finished. Screenshot mediaId 945c47b6-d6c8-4215-8e50-5a3962a5aac4; clip mediaId bf221ff6-2684-4afb-af95-9fca7b21dfee.

REPO install C:/Program Files (x86)/Steam/steamapps/common/REPO; v0.4.4.3, Steam build 23363152, Unity 2022.3.67f2 Mono/C#. ULTRAKILL install C:/Program Files (x86)/Steam/steamapps/common/ULTRAKILL; Steam build 22957324, Unity 2022.3.29f1 Mono/C#. Both games required. Tested BepInEx 5.4.23.5 is installed automatically by Melty. No anti-cheat detected in recon; private agreed modded lobbies only. No exact listed mashup was found; launched recipe example ultrakill-risk-of-rain imports local ULTRAKILL assets.

Native BepInEx/Harmony plugin, Photon PUN host authority using existing Steam/Photon relay. 12 source-of-truth sheets, 282 defined cells, 35 generated row structs. Definition preflight verifies populated cells, types, finite values and references; it is not proof that every behavior works in game. Latest game DLL SHA256 96894ce639e4969a0e1215e6ea8bc7ab65779f0a1443ddcc0094a9e4d0864b50, built with zero warnings/errors. Production behavior is unchanged since the recorded titled build; subsequent changes concern comments, checks and documentation.

Separate native movement check passed with god mode off: dash 22.46045 m/s, slide input logged, jump rise 0.6123 m, slam -27.3962 m/s. Native knockdown false in this check; hard impacts can still knock the player down. Keyboard check was paused by user, then explicitly resumed; native G draw/holster, R swap, F parry and LMB fire now passed. RMB input was sent but its action result was not independently observed. Combat check passed against native Huntsman: parry 250 to 215, revolver to 191, boosted shot to 131, coin to 73, shotgun to zero. Timed native Hurt inside parry window kept health 100; after 0.35 seconds health fell to 95. Weapons isolated with god mode; incoming-hit hook checked with it disabled. Style reached 468.75 and combat momentum activated. Natural AI attack timing and a complete loot extraction remain unverified.

Native private hosting succeeded. Native six-player limit; every client needs both games and matching mod. Same-account second client was rejected by Photon because UserId already joined the game. No bypass attempted. User has no second player available yet. TWO-ACCOUNT JOIN/REPLICATION/REMOTE V1 BODIES UNVERIFIED. Recipe multiplayer.maxPlayers=6, connect omitted. Keep the first multiplayer release unpublished until hosting/joining is verified with two genuine accounts, hosting through Melty is observed, the user runs Melty app Test, and the user authorizes publishing.

Recipe inspection includes every package entry and README/LICENSE. All three entries map; no unmapped files. one_click_check returned yes with no blockers/toFinish. This checks the declared recipe/files only; actual Melty install/launch has not been tested. Secondary game folder is passed in ULTRAHAUL_ULTRAKILL={game:ultrakill}. Only our DLL and docs are packaged; loader comes from Melty, assets load locally. No Python/toolkit runtime needed by players.

## Lab and root causes

Installed official BepInEx 5.4.23.5, ILSpy 9.1.0.7988, UnityPy 1.25.4 locally and BtbN FFmpeg via universal-modder. Existing .NET SDK 9.0.203 and bundled Python used. No paid services/FAL generation. universal-modder clone ../universal-modder; SDK excerpts ../reference, outside this repo; never package them.

Saves backup ../tool-state/backups/repo-saves/20261005-183725.zip. Loader backup ../original-loader. Global HideManagerGameObject stays false; plugin hides only its own manager. Native game PID 31828 at time of writing, local development flag ULTRAHAUL_DEV=1; bridge under LocalAppData/UltraHaul/dev. No input held after checks, gun holstered, god mode disabled. Never kill by wildcard.

Fixed native startup by avoiding Photon initialization in Awake/singleplayer. Subscribe only after native multiplayer initialization. Fixed weapon switching by enforcing the previous accepted shot's cooldown centrally. Shared enemy health resolver follows EnemyParent child health rather than collider parent only. Parry uses native EnemyDirector index. Native enemy activation has a random 2–5-second delay; tests wait six seconds. Stunned ragdolls still move physically, so aim immediately before firing. Native tumble moves controller back to its body; test positioning explicitly ends tumble and synchronizes collision fall bookkeeping. Own slams reset falling but preserve other native impacts. Restore native slide tuning with movement tuning on incompatible sessions. Gate all assets, animations/textures/audio before gameplay and handshake.

Current assets include V1 rig/idle/run, revolver and shotgun rigs/idle/fire, coin Cylinder mesh and three sounds; no placeholders. Native coin FBX hash fb46724754ff9044b80f6df9d583440d. Shotgun deliberately uses one traveling pellet packet; full-flight spread would need independent projectiles.

Failed movement runs were interrupted by Windows focus returning to Explorer; WinDrive refused foreign-window input. User resumed and the guarded check then passed. A mouse check initially used mdown without required coordinates; corrected to the existing click helper, then passed. Do not turn either interrupted check into a claimed pass.

## Remaining before publication

Two-account host/join and combat/visual replication, Melty session pickup, natural AI parry/player playtest and complete extraction, user Melty Test/install, explicit publication approval. Keep draft status and reuse the existing modId. Do not infer working multiplayer or one-click installation from static validation.

## Sharing authorization update

User explicitly requested GitHub upload followed by Melty upload, with multiplayer marked untested on both. This replaces the earlier requirement to withhold the development release until two-player co-op is tested. Share as experimental 0.1.0-dev; retain multiplayer metadata and omit connect. Melty app Test remains necessary before publication. Existing Melty modId is reused. Target GitHub account confirmed by authenticated gh API: roryekay; new public repository ULTRA-REPO. No secrets or game assets belong in the repository/release. Validated recipe saved as melty.json with user authorization to upload the project.
