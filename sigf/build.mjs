// ULTRA R.E.P.O. (roryk, MIT): ULTRAKILL's V1 played inside R.E.P.O. A BepInEx 5 plugin (UltraHaul) in R.E.P.O. that
// loads V1, the guns and three sounds at runtime from the player's own ULTRAKILL folder (Addressables bundles, read
// only). ULTRAKILL is never started. Rehosted on SIGFAI/ultra-repo (standard upstream fusion, source.hosted) with the
// official BepInEx 5 x64 build, as library/slimecraft does.
//
// Upstream tag is v0.1.0-dev (a GitHub prerelease); the catalog's version format is x.y.z, so our release is v0.1.0.
// Layout is upstream's own recipe (melty.json): plugins/UltraHaul/ -> {game}/BepInEx/plugins/UltraHaul and docs/ ->
// {game}/BepInEx/plugins/UltraHaul/docs. Our zip holds the same three files unchanged under that one folder.
// The plugin finds ULTRAKILL itself (Source/Assets.cs:52-73: env ULTRAHAUL_ULTRAKILL, its config, the sibling folder of
// R.E.P.O., then the Steam libraries), so no env var is needed from the app.
//   node library/ultra-repo/build.mjs [--fixture]       (outputs: library/lib.mjs)
import { unzip } from '../../orchestrator/src/recipe.js';
import { BEPINEX, asset, card, dl, emit, pinned, player, zipAsset } from '../lib.mjs';

const UP = {
  repo: 'https://github.com/roryekay/ULTRA-REPO', tag: 'v0.1.0-dev', commit: '376a84f0ae8286b95410a872e192270a59cd5f4b',
  license: 'MIT', authors: ['roryk'],
  zip: { file: 'ULTRA-REPO-0.1.0-dev.zip', sha256: '15916e5d4b5a70ec8175b4a39cb7f76a92bf099faf7fdfed46c2fbe0ef67bbb6' }, // = GitHub digest, 2026-10-06
  dll: { path: 'plugins/UltraHaul/UltraHaul.dll', sha256: '96894ce639e4969a0e1215e6ea8bc7ab65779f0a1443ddcc0094a9e4d0864b50' }, // = MODLOG.md build hash
};
const ID = 'ultra-repo', VERSION = '0.1.0', NAME = 'ULTRA R.E.P.O.';
const TAGLINE = 'Move like V1 in R.E.P.O.: dash, slide, slam and shoot, then bring the loot home intact.';

const up = new Map(unzip(await pinned(`${UP.repo}/releases/download/${UP.tag}/${UP.zip.file}`, UP.zip.sha256)).map(e => [e.name.replace(/\\/g, '/'), e.data]));
for (const f of [UP.dll.path, 'docs/README.md', 'docs/LICENSE']) if (!up.has(f)) throw new Error(`${UP.zip.file} has no ${f}`);
const bepinex = asset(BEPINEX.file, await pinned(BEPINEX.url, BEPINEX.sha256), { zipped: true });
const plugin = zipAsset(`${ID}-repo.zip`, [
  { name: 'UltraHaul.dll', data: up.get(UP.dll.path) },
  { name: 'docs/README.md', data: up.get('docs/README.md') },
  { name: 'docs/LICENSE', data: up.get('docs/LICENSE') },
]);
if (!plugin.contents.some(c => c.path === 'UltraHaul.dll' && c.sha256 === UP.dll.sha256)) throw new Error('UltraHaul.dll hash differs from the reviewed build');
const assets = [bepinex, plugin];

const make = (urls, set) => ({
  id: `sigf/${ID}`,
  version: VERSION,
  name: NAME,
  tagline: player(ID).tagline ?? TAGLINE,
  how_to_play: player(ID).howToPlay,
  kind: 'mashup', // ULTRAKILL does not run: its models and sounds are read from the player's install
  games: [
    { game: 'repo', role: 'host', label: 'R.E.P.O.', engine: 'R.E.P.O. (Unity 2022.3, Mono, x64) + BepInEx 5 plugin UltraHaul (C#)', apps: { steam: '3241660' }, runtime: 'v0.4.4.3, Steam build 23363152 (checked by the author)' },
    { game: 'ultrakill', role: 'guest', label: 'ULTRAKILL', apps: { steam: '1229490' }, uses: 'models, animations and sounds of the player\'s own install, not launched', runtime: 'Steam build 22957324 (checked by the author)' },
  ],
  requires: [
    { id: BEPINEX.id, version: BEPINEX.version, license: `${BEPINEX.license}, shipped unchanged`, page: `${BEPINEX.repo}/releases/tag/v${BEPINEX.version}`,
      note: 'installed into the R.E.P.O. folder by the app', source: { url: urls[bepinex.name], sha256: bepinex.sha256 } },
    { id: 'ultrakill', page: 'https://store.steampowered.com/app/1229490',
      note: 'ULTRAKILL installed on Steam (never started): the mod finds it next to R.E.P.O. or in your Steam libraries and reads V1, the guns and sounds from it' },
  ],
  install: [
    { game: 'repo', strategy: 'game-dir-snapshot', loader: 'bepinex', files: [
      { src: bepinex.name, dst: '{game}', unpack: true, contents: bepinex.contents, ...dl(bepinex, urls) },
      { src: plugin.name, dst: '{game}/BepInEx/plugins/UltraHaul', unpack: true, contents: plugin.contents, ...dl(plugin, urls) },
    ] },
  ],
  launch: [{ game: 'repo', args: [] }],
  files: set.map(a => ({ name: a.name, ...dl(a, urls) })),
  source: {
    repo: UP.repo, license: 'MIT AND LGPL-2.1', upstream_license: UP.license, tag: UP.tag, commit: UP.commit,
    hosted: `https://github.com/SIGFAI/${ID}`,
    bundled: [{ name: 'BepInEx', version: BEPINEX.version, repo: BEPINEX.repo, commit: BEPINEX.commit, license: BEPINEX.license }],
  },
  media: { cover: `${UP.repo}/releases/download/${UP.tag}/ULTRA-REPO-gameplay.png` },
  built_by: { author: UP.authors[0], authors: UP.authors, packaged_by: 'SIGF' },
  idea_by: UP.authors[0],
  built_at: '2026-10-06T00:00:00.000Z',
  ...card(UP.repo),
  notes: player(ID).notes,
});

emit({ slug: ID, version: VERSION, assets, fixtureAssets: assets, make });
