import { toHttpUrl } from "./safe-url.ts";

/** The parts of a provider's `<iframe>` embed that are safe to render directly. */
export interface ProviderFrame {
  src: string;
  allow?: string;
  allowFullscreen: boolean;
  title?: string;
  width?: number;
  height?: number;
  /** The sandbox tokens the provider asked for. Undefined when the iframe had no sandbox attribute. */
  sandbox?: string;
  referrerPolicy?: string;
}

/** Referrer policies a provider may request. The ones that send the full backoffice URL are left out. */
const ALLOWED_REFERRER_POLICIES = new Set([
  "no-referrer",
  "origin",
  "origin-when-cross-origin",
  "same-origin",
  "strict-origin",
  "strict-origin-when-cross-origin",
]);

/**
 * The sandbox to apply to a provider's iframe: the package policy, narrowed to what the provider
 * asked for. A provider that requests a sandbox never ends up with more permissions than it
 * asked for, and an empty sandbox stays fully restricted.
 */
export function effectiveSandbox(
  requested: string | undefined,
  policy: readonly string[],
): string {
  if (requested === undefined) return policy.join(" ");

  // Browsers split sandbox tokens on ASCII whitespace only and compare them ASCII
  // case-insensitively. \s and toLowerCase() are broader (non-breaking space, other Unicode
  // spaces and case folds), which could grant a token the browser would not.
  const asked = new Set(
    requested
      .split(/[\t\n\f\r ]+/)
      .filter(Boolean)
      .map((token) => token.replace(/[A-Z]/g, (c) => c.toLowerCase())),
  );
  return policy.filter((token) => asked.has(token)).join(" ");
}

const positiveInteger = (value: string | null): number | undefined => {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : undefined;
};

/**
 * The pixel height from an inline style, for example `style="height:315px"`.
 *
 * The effective `height` declaration is worked out the way CSS does it: a declaration the browser
 * cannot parse is dropped and overrides nothing, a later declaration overrides an earlier one,
 * and a normal declaration never overrides an `!important` one. Only when the winner is a plain
 * pixel value is it returned. Percentages, calc() and the like cannot be reproduced safely, so
 * they give undefined and the height attribute is used instead. `max-height`, `min-height` and
 * `line-height` are different properties and are ignored.
 */
const inlinePixelHeight = (style: string | null): number | undefined => {
  let effective: { value: string; important: boolean } | undefined;

  for (const declaration of (style ?? "").split(";")) {
    const match = declaration.match(
      /^\s*height\s*:\s*(.*?)\s*(!\s*important)?\s*$/i,
    );
    if (!match) continue;

    const value = match[1];
    if (!value || !CSS.supports("height", value)) continue;

    const important = Boolean(match[2]);
    if (effective?.important && !important) continue;

    effective = { value, important };
  }

  const pixels = effective?.value.match(/^(\d+(?:\.\d+)?)px$/i);
  const height = pixels ? Math.round(Number(pixels[1])) : 0;
  return height > 0 ? height : undefined;
};

/**
 * Returns the iframe attributes when the provider's markup is exactly one `<iframe>` that
 * points at an absolute http(s) URL on another origin than the backoffice, otherwise undefined.
 *
 * Such an embed can be rendered as a normal iframe. It is cross-origin from the backoffice, so
 * it keeps its own origin (providers such as YouTube need storage access, and a sandbox is
 * inherited by nested frames) without being able to reach the backoffice.
 *
 * The markup is parsed with DOMParser, which creates an inert document: nothing runs and no
 * resources load.
 */
export function parseProviderFrame(
  markup: string,
  backofficeOrigin: string,
): ProviderFrame | undefined {
  const doc = new DOMParser().parseFromString(markup, "text/html");

  // Leading <script>, <style> or <link> elements end up in <head>: not a lone iframe.
  if (doc.head.children.length > 0) return undefined;

  const nodes = [...doc.body.childNodes].filter(
    (node) =>
      node.nodeType !== Node.COMMENT_NODE &&
      !(node.nodeType === Node.TEXT_NODE && !node.textContent?.trim()),
  );
  if (nodes.length !== 1) return undefined;

  const element = nodes[0] as Element;
  if (element.nodeType !== Node.ELEMENT_NODE || element.localName !== "iframe") {
    return undefined;
  }

  // Browsers load srcdoc instead of src, so such an iframe is not what its src says.
  if (element.hasAttribute("srcdoc")) return undefined;

  const src = toHttpUrl(element.getAttribute("src"));
  if (!src || new URL(src).origin === backofficeOrigin) return undefined;

  const referrerPolicy = element.getAttribute("referrerpolicy")?.trim().toLowerCase();

  return {
    src,
    allow: element.getAttribute("allow") ?? undefined,
    allowFullscreen: element.hasAttribute("allowfullscreen"),
    title: element.getAttribute("title") ?? undefined,
    width: positiveInteger(element.getAttribute("width")),
    height:
      inlinePixelHeight(element.getAttribute("style")) ??
      positiveInteger(element.getAttribute("height")),
    sandbox: element.getAttribute("sandbox") ?? undefined,
    referrerPolicy:
      referrerPolicy && ALLOWED_REFERRER_POLICIES.has(referrerPolicy)
        ? referrerPolicy
        : undefined,
  };
}
