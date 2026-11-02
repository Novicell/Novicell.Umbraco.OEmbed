// The rule of Html.OEmbed, for a site that is not rendered by Umbraco.
//
// The Delivery API returns the `html` of an embed as it was saved, and the backoffice posts it without it being
// verified, so a headless site must not put it in the page with innerHTML. This only accepts what is exactly one
// ordinary iframe (YouTube, Vimeo, Spotify, SoundCloud and the like) and gives that iframe back as it is.
// Markup with a script of its own (X, TikTok, Reddit) is refused.
//
// The markup is read by the browser's own parser (DOMParser, which runs nothing and loads nothing), and the
// elements and attributes it produced are checked. A check on the text could read it differently from the browser.

const REFERRER_POLICIES = new Set([
  'no-referrer', 'origin', 'origin-when-cross-origin', 'same-origin', 'strict-origin',
  'strict-origin-when-cross-origin', 'unsafe-url', 'no-referrer-when-downgrade',
]);

// Look and size only: nothing that can move the frame over the rest of the page. That leaves out position, top,
// inset, z-index and transform, and also margin and vertical-align, where a negative length shifts the frame
// (margin:-100vh 0 0 puts a full-page frame over the controls above it).
const STYLE_PROPERTIES = new Set([
  'border', 'border-radius', 'border-width', 'border-style', 'border-color', 'width', 'height', 'max-width',
  'max-height', 'min-width', 'min-height', 'aspect-ratio', 'display', 'overflow',
]);

// scheme://host and nothing odd: not https:/media/x or https:media/x (a browser reads those as a path on this
// site), and no backslash, third slash or user name before the host. Checked on the text, so it does not depend on
// how a parser reads them.
const ADDRESS_SHAPE = /^https?:\/\/[^/\\?#@\s][^/\\?#@\s]*(?:[/?#][^\\\s]*)?$/i;

const isHttpUrl = (value) => {
  if (!ADDRESS_SHAPE.test(value)) return false;
  try {
    const url = new URL(value, document.baseURI);
    return url.protocol === 'https:' || url.protocol === 'http:';
  } catch {
    return false;
  }
};

// A frame of the page's own host is not isolated from the page, so a script in what it loads (an SVG uploaded to
// the site, say) could reach it. Any scheme or port of the host is refused. This cannot see where another address
// redirects to: for that, set a frame-src Content-Security-Policy (see the README).
// The URL parser gives the host in punycode and lower case; a trailing dot or the brackets of an IPv6 address are
// the same host.
const hostOf = (host) => host.toLowerCase().replace(/^\[|\]$/g, '').replace(/\.$/, '');
const isOtherHost = (value) => hostOf(new URL(value, document.baseURI).hostname) !== hostOf(location.hostname);

const isSafeStyle = (value) =>
  value
    .split(';')
    .map((declaration) => declaration.trim())
    .filter(Boolean)
    .every((declaration) => {
      const colon = declaration.indexOf(':');
      return (
        colon > 0 &&
        STYLE_PROPERTIES.has(declaration.slice(0, colon).trim().toLowerCase()) &&
        /^[a-z0-9.%#/\- ]+$/i.test(declaration.slice(colon + 1).trim())
      );
    });

// Each check gets the value the browser read, so entities are already decoded.
const VALUE_CHECKS = {
  src: (v) => isHttpUrl(v) && isOtherHost(v),
  width: (v) => /^\d{1,4}%?$/.test(v),
  height: (v) => /^\d{1,4}%?$/.test(v),
  frameborder: (v) => /^(0|1|no|yes)$/i.test(v),
  scrolling: (v) => /^(yes|no|auto)$/i.test(v),
  loading: (v) => /^(lazy|eager)$/i.test(v),
  referrerpolicy: (v) => REFERRER_POLICIES.has(v.toLowerCase()),
  allowfullscreen: (v) => v === '' || /^(allowfullscreen|true)$/i.test(v),
  title: () => true,
  allow: (v) => /^[A-Za-z0-9\-; ]*$/.test(v),
  sandbox: (v) => /^[A-Za-z\- ]*$/.test(v),
  'data-testid': (v) => /^[A-Za-z0-9_-]*$/.test(v),
  style: isSafeStyle,
};

/**
 * The iframe of the markup, ready to put in the page, or null when the markup is anything but one safe iframe.
 * @param {unknown} html The `html` of an embed from the Delivery API.
 * @returns {HTMLIFrameElement | null}
 */
export function safeIframe(html) {
  try {
    return checkedIframe(html);
  } catch {
    return null;
  }
}

function checkedIframe(html) {
  if (typeof html !== 'string' || !html.trim()) return null;

  const doc = new DOMParser().parseFromString(html, 'text/html');

  // One element in the whole document: the iframe, in the body, with nothing before, after or inside it but
  // white space (the parser keeps the line break a provider writes after its iframe).
  if (doc.childNodes.length !== 1 || doc.head.childNodes.length > 0) return null;

  const content = [...doc.body.childNodes].filter(
    (node) => !(node.nodeType === Node.TEXT_NODE && /^[\t\n\f\r ]*$/.test(node.data)),
  );
  if (content.length !== 1) return null;

  const frame = content[0];
  if (!(frame instanceof HTMLIFrameElement) || frame.childNodes.length > 0) return null;

  for (const { name, value } of frame.attributes) {
    // Own properties only: constructor, __proto__ and toString are on every object.
    if (!Object.hasOwn(VALUE_CHECKS, name) || !VALUE_CHECKS[name](value)) return null;
  }

  // Without a source there is nothing to show.
  if (!frame.hasAttribute('src')) return null;

  // An element is returned, not the text, so how the markup was closed does not matter: the C# check refuses an
  // iframe that is not closed because it writes the text, where an open iframe would swallow the page after it.
  return document.importNode(frame, false);
}

/** The address when it is an absolute http(s) one, otherwise null. For a link or an image from the response. */
export function httpUrl(value) {
  return typeof value === 'string' && isHttpUrl(value) ? value : null;
}
