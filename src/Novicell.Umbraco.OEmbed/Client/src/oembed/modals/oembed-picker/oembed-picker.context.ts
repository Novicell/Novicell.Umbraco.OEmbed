import { OEmbedRepository } from "../../repository/oembed";
import { OEmbedPickerStore } from "./oembed-picker.store";
import { OEMBED_MODAL } from "./oembed-picker-modal.token";
import type { OEmbedValueModel } from "../../../property-editors/types";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";

/**
 * Context for composing a single oEmbed value (URL entry, preview fetch, error
 * state). Used both by the picker modal (its own instance) and by the input
 * element (to open the modal and await the composed result) — it does not
 * hold or own the property's committed value(s); the input element manages
 * its own array of embeds directly.
 *
 * Consumed by the picker modal and child input components via
 * consumeContext(UMB_OEMBED_PICKER_CONTEXT, callback).
 */
export class UmbOEmbedPickerContext extends UmbControllerBase {
  readonly #store = new OEmbedPickerStore();
  readonly #repository = new OEmbedRepository(this);

  /**
   * Bumped whenever the URL changes or a new fetch starts. A response is only applied if it
   * still belongs to the latest request, so a slow response for an earlier URL can never
   * overwrite the preview (or error) of the URL that is currently entered.
   */
  #requestId = 0;

  // Public observables
  readonly url$ = this.#store.url$;
  readonly preview$ = this.#store.preview$;
  readonly loading$ = this.#store.loading$;
  readonly error$ = this.#store.error$;

  constructor(host: UmbControllerHost) {
    super(host);
  }

  /**
   * Update the URL and fetch preview data.
   */
  async updateUrl(url: string): Promise<void> {
    this.#requestId++;
    this.#store.setUrl(url);
  }

  /**
   * Fetch oEmbed preview data for the current URL.
   */
  async fetchPreview(
    url?: string,
    maxWidth?: number,
    maxHeight?: number,
  ): Promise<void> {
    const urlToFetch = url || this.#store.getUrl();
    if (!urlToFetch) return;

    // The preview is saved together with getUrl(), so keep the two in sync.
    if (urlToFetch !== this.#store.getUrl()) {
      this.#requestId++;
      this.#store.setUrl(urlToFetch);
    }

    const requestId = ++this.#requestId;
    this.#store.setLoading(true);

    const { data, error } = await this.#repository.requestOEmbed({
      url: urlToFetch,
      maxWidth,
      maxHeight,
    });

    // A newer request or a URL change happened while this one was in flight.
    if (requestId !== this.#requestId) return;

    if (data) {
      this.#store.setPreview(data);
    } else {
      this.#store.setError(error);
    }
  }

  /**
   * Get the current URL value (for form submission).
   */
  getUrl(): string {
    return this.#store.getUrl();
  }

  /**
   * Open the picker modal and resolve the composed value.
   * @param url Optional URL to pre-fill the modal with.
   * @param allowedTypes Optional oEmbed type restriction; empty/undefined allows any type.
   * @returns The chosen value, or undefined if cancelled.
   */
  async openPicker(url?: string, allowedTypes?: string[]): Promise<OEmbedValueModel | undefined> {
    const result = await umbOpenModal(this, OEMBED_MODAL, {
      data: { url, allowedTypes },
    }).catch(() => undefined);

    if (!result) return undefined;

    return { ...result };
  }

  /**
   * Reset all state to initial values.
   */
  reset(): void {
    this.#requestId++;
    this.#store.reset();
  }
}
