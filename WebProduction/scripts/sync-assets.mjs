import { cp, mkdir, readdir, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const webRoot = path.resolve(scriptDir, '..');
const repoRoot = path.resolve(webRoot, '..');
const outputRoot = path.join(webRoot, 'assets');
const allowed = new Set(['.png', '.jpg', '.jpeg', '.webp', '.svg', '.gif']);

const sources = [
  { root: path.join(repoRoot, 'DominoMajlisPRO', 'Resources', 'Images'), prefix: 'maui-images' },
  { root: path.join(repoRoot, 'DominoMajlisPRO', 'GalleryEngine', 'Assets'), prefix: 'gallery-assets' }
];

async function exists(directory) {
  try { await readdir(directory); return true; } catch { return false; }
}

async function walk(directory) {
  const result = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) result.push(...await walk(absolute));
    else if (allowed.has(path.extname(entry.name).toLowerCase())) result.push(absolute);
  }
  return result;
}

await rm(outputRoot, { recursive: true, force: true });
await mkdir(outputRoot, { recursive: true });

const entries = [];
const aliases = {};
for (const source of sources) {
  if (!await exists(source.root)) continue;
  for (const absolute of await walk(source.root)) {
    const relative = path.relative(source.root, absolute).split(path.sep).join('/');
    const destinationRelative = `${source.prefix}/${relative}`;
    const destination = path.join(outputRoot, ...destinationRelative.split('/'));
    await mkdir(path.dirname(destination), { recursive: true });
    await cp(absolute, destination);

    const webPath = `/assets/${destinationRelative}`;
    const sourceRelative = path.relative(repoRoot, absolute).split(path.sep).join('/');
    entries.push({ source: sourceRelative, path: webPath, fileName: path.basename(relative) });

    const keys = [relative, relative.toLowerCase(), path.basename(relative), path.basename(relative).toLowerCase(), sourceRelative, sourceRelative.toLowerCase()];
    for (const key of keys) if (!aliases[key]) aliases[key] = webPath;
  }
}

const manifest = {
  version: 1,
  generatedAt: new Date().toISOString(),
  count: entries.length,
  entries,
  aliases
};
await writeFile(path.join(outputRoot, 'asset-manifest.json'), JSON.stringify(manifest, null, 2), 'utf8');
console.log(`Synced ${entries.length} web assets.`);
