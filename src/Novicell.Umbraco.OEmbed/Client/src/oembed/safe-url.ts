/**
 * Returns the normalized URL when `value` is an absolute http(s) URL, otherwise undefined.
 *
 * URLs in an oEmbed response come from the provider. The server accepts any absolute URI, including `javascript:` and `data:`, so
 * anything rendered as a link in the backoffice has to be checked here first.
 */
export function toHttpUrl(value: string | null | undefined): string | undefined {
  if (!value) return undefined;

  try {
    const url = new URL(value.trim());
    return url.protocol === "http:" || url.protocol === "https:" ? url.href : undefined;
  } catch {
    // Not an absolute URL (relative or malformed).
    return undefined;
  }
}
