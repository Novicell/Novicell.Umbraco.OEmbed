// Stamps a release version into the backoffice manifest (umbraco-package.json): the manifest's own
// version, and the ?v= cache-buster on its script URLs, so browsers fetch the files of a new release
// instead of serving the previous release's from cache.
//
// Usage: node stamp-manifest.mjs <version> <path to umbraco-package.json>
import fs from 'node:fs';

const [version, file] = process.argv.slice(2);

if (!version || !file) {
  console.error('Usage: node stamp-manifest.mjs <version> <path to umbraco-package.json>');
  process.exit(1);
}

const manifest = JSON.parse(fs.readFileSync(file, 'utf8'));
manifest.version = version;

let stamped = 0;
for (const extension of manifest.extensions ?? []) {
  if (typeof extension.js === 'string' && /\?v=[^?#]*$/.test(extension.js)) {
    extension.js = extension.js.replace(/\?v=[^?#]*$/, `?v=${encodeURIComponent(version)}`);
    stamped++;
  }
}

fs.writeFileSync(file, JSON.stringify(manifest, null, 2) + '\n');
console.log(`Stamped ${version} into ${file} (${stamped} script URL${stamped === 1 ? '' : 's'})`);
