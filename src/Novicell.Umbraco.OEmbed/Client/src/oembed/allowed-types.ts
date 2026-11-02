/**
 * Mirrors the server's OEmbedPickerPropertyValueEditor.AllowedTypeValidator: an empty or missing list allows every
 * type, and types are compared case-insensitively (the server uses OrdinalIgnoreCase).
 */
export function isOEmbedTypeAllowed(
  allowedTypes: readonly string[] | undefined,
  type: string | null | undefined,
): boolean {
  if (!allowedTypes?.length || !type) return true;

  const wanted = type.toLowerCase();
  return allowedTypes.some((allowed) => allowed.toLowerCase() === wanted);
}
