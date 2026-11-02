import { UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import type { OEmbedResponse } from "../../../api";
import { UmbApiError, UmbCancelError } from "@umbraco-cms/backoffice/resources";

/**
 * Store for managing oEmbed picker state.
 * Handles URL input, loading state, preview data, and error handling.
 */
export class OEmbedPickerStore {
  // State observables
  readonly #urlState = new UmbStringState<string>("");
  readonly #previewState = new UmbObjectState<OEmbedResponse | undefined>(
    undefined,
  );
  readonly #loadingState = new UmbStringState<
    "idle" | "loading" | "success" | "error"
  >("idle");
  readonly #errorState = new UmbObjectState<
    UmbApiError | UmbCancelError | undefined
  >(undefined);

  // Public observables
  readonly url$ = this.#urlState.asObservable();
  readonly preview$ = this.#previewState.asObservable();
  readonly loading$ = this.#loadingState.asObservable();
  readonly error$ = this.#errorState.asObservable();

  /**
   * Update the URL input.
   */
  setUrl(url: string): void {
    this.#urlState.setValue(url);
    this.#previewState.setValue(undefined);
    this.#errorState.setValue(undefined);
    this.#loadingState.setValue("idle");
  }

  /**
   * Get the current URL value.
   */
  getUrl(): string {
    return this.#urlState.value;
  }

  /**
   * Set the preview data (successful fetch).
   */
  setPreview(data: OEmbedResponse): void {
    this.#previewState.setValue(data);
    this.#errorState.setValue(undefined);
    this.#loadingState.setValue("success");
  }

  /**
   * Set an error state.
   */
  setError(error: UmbApiError | UmbCancelError | undefined): void {
    this.#errorState.setValue(error);
    this.#previewState.setValue(undefined);
    this.#loadingState.setValue(error ? "error" : "idle");
  }

  /**
   * Set loading state.
   */
  setLoading(isLoading: boolean): void {
    this.#loadingState.setValue(isLoading ? "loading" : "idle");
  }

  /**
   * Get the current loading state.
   */
  getLoading(): string {
    return this.#loadingState.value;
  }

  /**
   * Reset all state to initial values.
   */
  reset(): void {
    this.#urlState.setValue("");
    this.#previewState.setValue(undefined);
    this.#errorState.setValue(undefined);
    this.#loadingState.setValue("idle");
  }
}
