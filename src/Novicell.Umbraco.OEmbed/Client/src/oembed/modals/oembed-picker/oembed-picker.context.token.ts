import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbOEmbedPickerContext } from "./oembed-picker.context";

/**
 * Context token for the oEmbed picker.
 * Used to provide/consume picker state and methods across components.
 *
 * Provider: oembed-picker-modal.element.ts
 * Consumers: input-oembed.element.ts (or child components that need picker interaction)
 */
export const UMB_OEMBED_PICKER_CONTEXT =
  new UmbContextToken<UmbOEmbedPickerContext>("UmbOEmbedPickerContext");
