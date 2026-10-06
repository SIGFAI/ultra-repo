# ULTRA R.E.P.O.

Move like V1 in R.E.P.O.: dash, slide, slam and shoot, then bring the loot home intact.

**ULTRA R.E.P.O. is made by [roryk](https://github.com/roryk).** All credit for the mod goes to them.

- Original project: https://github.com/roryekay/ULTRA-REPO
- Report bugs and ask questions there: https://github.com/roryekay/ULTRA-REPO/issues
- Upstream release packaged here: [v0.1.0-dev](https://github.com/roryekay/ULTRA-REPO/releases/tag/v0.1.0-dev) (commit [`376a84f`](https://github.com/roryekay/ULTRA-REPO/tree/376a84f0ae8286b95410a872e192270a59cd5f4b))

> **Beta.** Nobody at SIGF has played this build yet. Back up your saves.
> Bugs in the mod itself go to the author's issue tracker above; problems with the one-click install go to this repository's issues.

## What you need

- **R.E.P.O.** ([Steam](https://store.steampowered.com/app/3241660/)): v0.4.4.3, Steam build 23363152 (checked by the author).
- **ULTRAKILL** ([Steam](https://store.steampowered.com/app/1229490/)): Steam build 22957324 (checked by the author) (models, animations and sounds of the player's own install, not launched).
- ultrakill: ULTRAKILL installed on Steam (never started): the mod finds it next to R.E.P.O. or in your Steam libraries and reads V1, the guns and sounds from it (https://store.steampowered.com/app/1229490).
- Windows and the [SIGF app](https://sigf.ai). The app installs bepinex 5.4.23.5 for you.

## Install

In the SIGF app, open **ULTRA R.E.P.O.** in the catalog, press **Install**, then **Play**. **Restore** puts your game folders back exactly as they were.
The app follows `mashup.json` in this repository: every download is pinned by sha256. The files come from the release [`v0.1.0`](../../releases/tag/v0.1.0).

### How to play

- R.E.P.O. as usual, but your player is ULTRAKILL's V1: grab fragile valuables, extract them, and fight off monsters with V1's revolver and shotgun.
- Press Play and start Singleplayer or a Private Game. The ULTRA R.E.P.O. panel shows on the left once your ULTRAKILL models have loaded.
- G draw/holster the gun (holster to grab loot again), R swap revolver/shotgun, left mouse fire, right mouse toss a coin to ricochet revolver shots.
- Your Sprint key dashes (3 charges), Crouch slides on the ground or slams in the air. F parries a hit up close or boosts a fresh shotgun shot.
- Style from hits, coins and parries refills dash and briefly boosts damage. It never adds money, and your shots can still break loot.

### Good to know

- You need R.E.P.O. and ULTRAKILL on Steam (Windows). ULTRAKILL never runs: the mod reads V1, guns and sounds from your ULTRAKILL folder. Made for R.E.P.O. v0.4.4.3 and ULTRAKILL build 22957324; a game update can break it.
- Experimental development build (0.1.0-dev). Solo was checked by the author; multiplayer has not been tested with two players. Every co-op player needs both games and this same version, or the mod stays off for the whole lobby.
- Use Singleplayer or Private Game with friends who have the mod; joining is through R.E.P.O.'s own invite menu. Keys can be changed in BepInEx\config\local.ultrahaul.cfg.
- BepInEx 5.4.23.5 comes with the mod and Restore removes both. Beta: report bugs to the author on the upstream issue tracker with BepInEx\LogOutput.log.

## What this repository holds

1. The upstream source tree at tag `v0.1.0-dev`, commit [`376a84f0ae8286b95410a872e192270a59cd5f4b`](https://github.com/roryekay/ULTRA-REPO/tree/376a84f0ae8286b95410a872e192270a59cd5f4b), every file unchanged (same git blobs). Upstream's own `README.md` is there, unchanged; GitHub shows this file (`.github/README.md`) first.
2. Added by SIGF in the same commit: this file, `THIRD-PARTY.md` (licenses and sources of the third-party files in the release), and `sigf/` (the scripts that built the release assets, for reference: they run inside the SIGF repository).
3. `mashup.json`, the SIGF app recipe (the next commit).
4. The release `v0.1.0` (its tag is the first commit):

| Asset | Size | sha256 | What it is |
|---|---|---|---|
| `BepInEx_win_x64_5.4.23.5.zip` | 639118 B | `82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4` | BepInEx 5.4.23.5 x64, the official build, unchanged (see THIRD-PARTY.md); unpacked into the R.E.P.O. folder. |
| `ultra-repo-repo.zip` | 39474 B | `3616ad675874b46f9bf5a01660814e15e735cb0a937d0fd61dd4e26dc4455bd8` | upstream's `UltraHaul.dll` from release `v0.1.0-dev`, unchanged (sha256 `96894ce6...4b50`, the build hash in upstream's MODLOG.md), with upstream's `docs/README.md` and `docs/LICENSE`; unpacked into `BepInEx/plugins/UltraHaul`. |

The sha256 of every file inside the zips is in `mashup.json` (`contents`).

## Licenses

| Part | License | Where |
|---|---|---|
| ULTRA R.E.P.O. (all of the upstream tree) | MIT, Copyright 2026 roryk | `LICENSE` |
| BepInEx 5.4.23.5 and what its zip bundles (release asset) | MIT; UnityDoorstop LGPL-2.1 | `THIRD-PARTY.md` |

## Why this repository exists

The SIGF app (https://sigf.ai) installs mods from recipes (`mashup.json`) whose downloads are pinned release files. This repository makes ULTRA R.E.P.O. installable in one click, credited to roryk. If you are the author and want anything changed or taken down, open an issue here.
