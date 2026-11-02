// See scripts/README.md.
import { createRequire } from 'node:module';

// Playwright comes from the client's npm packages, so run this from src/Novicell.Umbraco.OEmbed/Client.
const require = createRequire(process.cwd() + '/package.json');
const { chromium } = require('playwright');

const base = process.argv[2] ?? 'https://localhost:44366';
const shots = process.argv[3] || undefined;
const umbracoVersion = process.argv[4];
const failures = [];
const check = (ok, message) => {
  console.log(`  ${ok ? 'ok  ' : 'FAIL'} ${message}`);
  if (!ok) failures.push(message);
};

const EMBEDS = [
  { name: 'YouTube video', url: 'https://www.youtube.com/watch?v=jNQXAC9IVRw', type: 'video', preview: 'iframe' },
  { name: 'Vimeo video', url: 'https://vimeo.com/371827688', type: 'video', preview: 'iframe' },
  { name: 'Spotify track', url: 'https://open.spotify.com/track/7ouMYWpwJ422jRcDASZB7P', type: 'rich', preview: 'iframe' },
  { name: 'SoundCloud track', url: 'https://soundcloud.com/forss/flickermood', type: 'rich', preview: 'iframe' },
  { name: 'Flickr photo', url: 'https://www.flickr.com/photos/pgregorynz/3692645578/', type: 'photo', preview: 'img' },
  // Umbraco's own X provider before 17.0.2 calls http://publish.twitter.com/oembed, which no longer answers.
  { name: 'X post', url: 'https://x.com/Interior/status/463440424141459456', type: 'rich', preview: 'srcdoc', since: '17.0.2' },
  { name: 'TikTok video', url: 'https://www.tiktok.com/@scout2015/video/6718335390845095173', type: 'video', preview: 'iframe' },
];

const browser = await chromium.launch({ channel: 'chrome' });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1400, height: 1000 } });
const page = await context.newPage();

const pageErrors = [];
let auth = null;
page.on('pageerror', (e) => pageErrors.push({ message: e.message, stack: e.stack ?? '' }));
// Errors from anywhere else come from a provider's script in a preview: reported, but not a failure.
const ownCode = /\/App_Plugins\/|\/umbraco\/backoffice\/|\/headless\//;
page.on('request', (r) => {
  const h = r.headers()['authorization'];
  if (h && r.url().includes('/management/api/')) auth = h;
});

const count = (text, pattern) => (text.match(pattern) ?? []).length;

try {
  const safe = await (await page.request.get(`${base}/?raw=0`)).text();
  const raw = await (await page.request.get(`${base}/?raw=1`)).text();
  check(count(safe, /<iframe\b/g) >= 3, `the page renders provider iframes (${count(safe, /<iframe\b/g)})`);
  check(!/<script\b/i.test(safe), 'the page renders no provider script by default');
  check(/<script\b/i.test(raw), 'the page renders provider markup with scripts when asked to (?raw=1)');

  const delivery = await page.request.get(`${base}/umbraco/delivery/api/v2/content?filter=contentType:embedPage&take=1`);
  const item = (await delivery.json()).items?.[0];
  const embeds = item?.properties?.embeds ?? [];
  check(delivery.status() === 200 && embeds.length >= 3, `the Delivery API returns the embeds (${embeds.length})`);
  check(embeds.every((e) => e.url && e.oembed?.type), 'each delivered embed has its url and oEmbed type');

  // Not networkidle: embedded players keep connections open.
  await page.goto(`${base}/headless/`, { waitUntil: 'load' });
  await page.locator('iframe').first().waitFor({ state: 'attached', timeout: 30000 }).catch(() => {});
  const headlessFrames = await page.locator('iframe').count();
  check(headlessFrames >= 3, `the headless page renders the embeds (${headlessFrames} iframes)`);

  await page.goto(`${base}/umbraco`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[name=username]').first().waitFor({ timeout: 60000 });
  await page.locator('input[name=username]').first().fill('admin@example.com');
  await page.locator('input[name=password]').first().fill('1234567890');
  await page.locator('uui-button[type=submit]').first().click();
  await page.waitForURL(/\/umbraco\/section\//, { timeout: 60000 });
  await page.waitForTimeout(3000);
  check(auth !== null, 'logged in to the backoffice');

  const manifest = await page.request.get(`${base}/App_Plugins/NovicellUmbracoOEmbed/umbraco-package.json`);
  check(manifest.status() === 200, `package manifest is served (${manifest.status()})`);

  const root = await page.request.get(`${base}/umbraco/management/api/v1/tree/document/root?take=10`, {
    headers: { authorization: auth },
  });
  const doc = ((await root.json()).items ?? [])[0];
  check(doc !== undefined, `a document to open (${doc?.variants?.[0]?.name ?? 'none'})`);

  await page.goto(`${base}/umbraco/section/content/workspace/document/edit/${doc.id}`, { waitUntil: 'domcontentloaded' });
  const choose = page.getByRole('button', { name: /choose|add/i }).first();
  await choose.waitFor({ timeout: 60000 });
  check(true, 'the embed property editor is shown');

  const savedLive = await page.locator('oembed-preview:not([live]) iframe').count();
  check(savedLive === 0, `saved embeds are shown without running provider markup (${savedLive} iframes)`);

  async function retrieve(url, shot) {
    await choose.click();
    const retrieveButton = page.getByRole('button', { name: /retrieve/i }).first();
    await retrieveButton.waitFor({ timeout: 15000 });
    await page.keyboard.type(url);
    const responsePromise = page.waitForResponse((r) => r.url().includes('/novicell-oembed/api/v1/oembed'), { timeout: 30000 });
    await retrieveButton.click();
    const response = await responsePromise;
    const body = await response.json().catch(() => ({}));
    await page.waitForTimeout(4000);
    if (shots) await page.screenshot({ path: `${shots}/${shot}.png` });
    return { status: response.status(), body };
  }

  async function close() {
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1500);
  }

  // The modal renders in shadow roots; Playwright's CSS and text locators look inside them.
  const preview = ':is(uui-modal-sidebar, umb-backoffice-modal-container) oembed-preview';
  const older = (a, b) => {
    const [x, y] = [a, b].map((v) => v.split(/[.-]/).map((p) => parseInt(p, 10) || 0));
    for (let i = 0; i < 3; i++) if (x[i] !== y[i]) return x[i] < y[i];
    return false;
  };
  for (const embed of EMBEDS) {
    if (embed.since && umbracoVersion && older(umbracoVersion, embed.since)) {
      console.log(`  skip ${embed.name}: Umbraco ${umbracoVersion}'s own provider does not work, fixed in ${embed.since}`);
      continue;
    }
    try {
      const { status, body } = await retrieve(embed.url, embed.name.replace(/\W+/g, '-').toLowerCase());
      const type = String(body.type ?? '').toLowerCase();
      let shown = false;
      if (embed.preview === 'img') shown = (await page.locator(`${preview} img`).count()) > 0;
      if (embed.preview === 'iframe') shown = (await page.locator(`${preview} iframe[src]:not([srcdoc])`).count()) > 0;
      if (embed.preview === 'srcdoc') shown = (await page.locator(`${preview} iframe[srcdoc]`).count()) > 0;
      const found = shown
        ? ''
        : ` (found: ${await page.locator(`${preview} :is(iframe, img)`).evaluateAll((els) =>
            els.map((el) => `${el.tagName.toLowerCase()}${el.hasAttribute('srcdoc') ? '[srcdoc]' : ''}${el.getAttribute('src') ? '[src]' : ''}`).join(', ') || 'nothing')})`;
      const titled = body.title ? (await page.getByText(body.title, { exact: false }).count()) > 0 : true;
      check(
        status === 200 && type === embed.type && shown && titled,
        `${embed.name}: ${status}, type ${type || '-'}, ${embed.preview} preview ${shown ? 'shown' : 'missing' + found}${titled ? '' : ', title missing'}`,
      );
    } catch (e) {
      check(false, `${embed.name}: ${e.message.split('\n')[0]}`);
    }
    await close();
  }

  const unsupported = await retrieve('https://example.com/not-a-provider', 'unsupported');
  check(unsupported.status === 400, `an unsupported URL is refused (${unsupported.status})`);
  check((await page.getByText(/isn.t supported/).count()) > 0, 'the picker says it is not supported');
  await close();

  const own = pageErrors.filter((e) => ownCode.test(e.stack));
  const other = pageErrors.filter((e) => !ownCode.test(e.stack));
  check(own.length === 0, `no script errors from the site's own code${own.length ? ': ' + own.map((e) => `${e.message} @ ${e.stack.split('\n')[1]?.trim()}`).join(' | ') : ''}`);
  if (other.length) {
    console.log(`  info ${other.length} error(s) from provider scripts: ${other.map((e) => `${e.message} @ ${e.stack.split('\n')[1]?.trim() || 'no stack'}`).join(' | ')}`);
  }
} catch (e) {
  if (shots) await page.screenshot({ path: `${shots}/stopped.png` }).catch(() => {});
  check(false, `smoke test stopped: ${e.message.split('\n')[0]}`);
} finally {
  await browser.close();
}

process.exit(failures.length ? 1 : 0);
