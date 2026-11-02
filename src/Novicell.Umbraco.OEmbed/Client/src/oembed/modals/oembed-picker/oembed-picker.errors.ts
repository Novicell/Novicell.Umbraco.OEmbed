import { UmbApiError } from "@umbraco-cms/backoffice/resources";

const ERROR_KEY_BY_OPERATION_STATUS: Record<string, string> = {
  ProviderNotSupported: "oembed_errors_ProviderNotSupported",
  EmbedNotFound: "oembed_errors_EmbedNotFound",
  InvalidResponse: "oembed_errors_InvalidResponse",
};

/**
 * Maps a failed oEmbed fetch to a localization key, using the API's `operationStatus`
 * extension field rather than its (unlocalized) title/detail text.
 */
export function localizedOEmbedErrorKey(error: unknown): string {
  if (UmbApiError.isUmbApiError(error)) {
    const operationStatus = error.problemDetails.operationStatus;
    return ERROR_KEY_BY_OPERATION_STATUS[operationStatus ?? ""] ?? "oembed_errors_Unknown";
  }
  return "oembed_errors_Unknown";
}
