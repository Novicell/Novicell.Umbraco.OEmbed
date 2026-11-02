import { httpUrl, safeIframe } from './safe-embed.js';

// The same page as Views/embedPage.cshtml, built in the browser from the Delivery API, which is on with public
// access in the test site. The page is served from the same site, so no CORS setting is needed here; a site on
// another address needs the Delivery API's CORS to allow it.
const ENDPOINT = '/umbraco/delivery/api/v2/content?filter=contentType:embedPage&take=1';

// ?raw=1 writes the provider's html as it is, scripts included (Html.OEmbed with Scripts = Raw). Otherwise only
// one ordinary iframe is written (the default of Html.OEmbed).
const raw = new URLSearchParams(location.search).get('raw') === '1';

// The link to the page that Umbraco renders keeps the setting, so the two can be compared like for like.
document.querySelector('#server-rendered').href = raw ? '/?raw=1' : '/';

const element = (tag, props = {}, ...children) => {
  const node = Object.assign(document.createElement(tag), props);
  node.append(...children);
  return node;
};

/** The same markup as Views/Partials/oembed.cshtml. `{ url, oembed }` is one embed as the Delivery API returns it. */
function renderEmbed({ url, oembed }) {
  const figure = element('figure');
  figure.dataset.type = oembed.type;

  // Everything is set as text or as a checked address, never as markup, except what the raw mode is for.
  const image = oembed.type === 'photo' ? httpUrl(oembed.url) : null;

  if (image) {
    figure.append(element('img', { src: image, alt: oembed.title ?? '' }));
  } else if (raw && oembed.html) {
    // The provider's html as it is, scripts run: only for a site that trusts its editors with that.
    figure.append(document.createRange().createContextualFragment(oembed.html));
  } else {
    const frame = safeIframe(oembed.html);
    if (frame) figure.append(frame);
  }

  // The site decides what else to show; the link is also what a visitor gets when the html was left out.
  const entered = httpUrl(url);
  const label = oembed.title ?? url;
  const caption = element('figcaption', {}, entered ? element('a', { href: entered, textContent: label }) : label);

  if (oembed.providerName) caption.append(` from ${oembed.providerName}`);

  // A field only some providers return is in `extra`, with the provider's own name for it (Vimeo's video_id).
  if (oembed.extra?.video_id) {
    caption.append(' ', element('span', { textContent: `(Vimeo video ${oembed.extra.video_id})` }));
  }

  figure.append(caption);
  return figure;
}

async function main() {
  const response = await fetch(ENDPOINT);
  if (!response.ok) throw new Error(`The Delivery API answered ${response.status}`);

  const [page] = (await response.json()).items;
  if (!page) throw new Error('No Embed page in the Delivery API. Has the index been built?');

  const { title, video, embeds } = page.properties;
  document.title = `${title ?? page.name} (headless)`;
  document.querySelector('h1').textContent = title ?? page.name;

  // A data type for one embed and one for several both give an array in the Delivery API.
  if (video?.length) {
    document.body.append(element('h2', { textContent: 'Video' }), ...video.map(renderEmbed));
  }

  document.body.append(element('h2', { textContent: 'Embeds' }), ...(embeds ?? []).map(renderEmbed));
}

main().catch((error) => {
  document.body.append(element('p', { textContent: error.message, style: 'color:#b00020' }));
});
