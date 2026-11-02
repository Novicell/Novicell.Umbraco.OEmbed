import { UmbOEmbedPickerContext } from "./oembed-picker.context";
import { UMB_OEMBED_PICKER_CONTEXT } from "./oembed-picker.context.token";
import type { OEmbedModalData } from "./oembed-picker-modal.token.ts";
import type { OEmbedValueModel } from "../../../property-editors/types.ts";
import {
  css,
  html,
  nothing,
  when,
  customElement,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import type {
  UUIButtonState,
  UUIInputEvent,
} from "@umbraco-cms/backoffice/external/uui";
import { umbFocus } from "@umbraco-cms/backoffice/lit-element";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

import "../../components/oembed-preview/oembed-preview.element.ts";
import { OEmbedResponse } from "../../../api";
import {
  umbBindToValidation,
  UmbValidationContext,
} from "@umbraco-cms/backoffice/validation";
import type { UmbApiError, UmbCancelError } from "@umbraco-cms/backoffice/resources";
import { localizedOEmbedErrorKey } from "./oembed-picker.errors.ts";
import { isOEmbedTypeAllowed } from "../../allowed-types.ts";
import { toHttpUrl } from "../../safe-url.ts";

@customElement(OembedPickerModalElement.tagName)
export class OembedPickerModalElement extends UmbModalBaseElement<
  OEmbedModalData,
  OEmbedValueModel
> {
  static readonly tagName = "oembed-modal";

  readonly #validation = new UmbValidationContext(this);
  readonly #pickerContext = new UmbOEmbedPickerContext(this);

  // Provide context to child components
  constructor() {
    super();
    this.provideContext(UMB_OEMBED_PICKER_CONTEXT, this.#pickerContext);
  }

  @state()
  private _submitButtonState: UUIButtonState;

  // Observables from context store (for rendering)
  private _currentUrl = "";
  private _currentLoading: string = "idle";
  private _currentPreview: OEmbedResponse | undefined = undefined;
  private _currentError: UmbApiError | UmbCancelError | undefined = undefined;

  override connectedCallback() {
    super.connectedCallback();

    // Subscribe to context observables for re-rendering
    this.observe(this.#pickerContext.url$, (url) => {
      this._currentUrl = url;
      this.requestUpdate();
    });

    this.observe(this.#pickerContext.loading$, (loading) => {
      this._currentLoading = loading;
      this.requestUpdate();
    });

    this.observe(this.#pickerContext.preview$, (preview) => {
      this._currentPreview = preview;
      this.requestUpdate();
    });

    this.observe(this.#pickerContext.error$, (error) => {
      this._currentError = error;
      this.requestUpdate();
    });

    // Initialize with data if provided
    if (this.data?.url) {
      this.#pickerContext.updateUrl(this.data.url);
      this.#getPreview();
    }
  }

  #close() {
    this.modalContext?.reject();
  }

  #isTypeAllowed(type?: string | null): boolean {
    return isOEmbedTypeAllowed(this.data?.allowedTypes, type);
  }

  async #submit() {
    this._submitButtonState = "waiting";

    try {
      await this.#validation.validate();

      if (!this._currentPreview || !this.#isTypeAllowed(this._currentPreview.type)) {
        this._submitButtonState = "failed";
        return;
      }

      this._submitButtonState = "success";

      // Get the current URL from context
      const url = this.#pickerContext.getUrl();
      this.value = {
        ...this.value,
        oembed: this._currentPreview,
        url: url,
      };

      this.modalContext?.submit();
    } catch {
      this._submitButtonState = "failed";
    }
  }

  async #getPreview() {
    await this.#pickerContext.fetchPreview();
  }

  #onUrlChange(e: UUIInputEvent) {
    this.#pickerContext.updateUrl(e.target.value as string);
  }

  #label(key: string, fallback: string) {
    const term = this.localize.term(key);
    return !term || term === key ? fallback : term;
  }

  #renderLink(url?: string | null, text?: string | null) {
    if (!url || !text) return nothing;

    // Provider-supplied URL: never render a javascript:, data: or similar link.
    const href = toHttpUrl(url);
    if (!href) return html`<span>${text}</span>`;

    return html`<a href="${href}" target="_blank" rel="noopener noreferrer"
      >${text}</a
    >`;
  }

  #renderDetailRow(
    label: string,
    value?: string | number | null,
    href?: string | null,
  ) {
    if (value === null || value === undefined || value === "") return nothing;
    const content = href
      ? this.#renderLink(href, String(value))
      : html`<span>${value}</span>`;
    return html`<div class="oembed-details__row">
      <span class="oembed-details__label">${label}</span>
      <span class="oembed-details__value">${content}</span>
    </div>`;
  }

  #renderDetails(oembed: OEmbedResponse) {
    const dimensions =
      oembed.width && oembed.height
        ? `${oembed.width} \u00d7 ${oembed.height}`
        : null;

    const rows = [
      this.#renderDetailRow(this.#label("oembed_details_title", "Title"), oembed.title),
      this.#renderDetailRow(this.#label("general_type", "Type"), oembed.type),
      this.#renderDetailRow(
        this.#label("oembed_details_author", "Author"),
        oembed.author_name,
        oembed.author_url,
      ),
      this.#renderDetailRow(
        this.#label("oembed_details_provider", "Provider"),
        oembed.provider_name,
        oembed.provider_url,
      ),
      this.#renderDetailRow(
        this.#label("general_dimensions", "Dimensions"),
        dimensions,
      ),
      this.#renderDetailRow(
        this.#label("general_url", "URL"),
        oembed.url,
        oembed.url,
      ),
    ];

    if (rows.every((row) => row === nothing)) return nothing;

    return html`<umb-property-layout
      label=${this.#label("general_details", "Details")}
      orientation="vertical"
    >
      <div slot="editor" class="oembed-details">${rows}</div>
    </umb-property-layout>`;
  }

  #renderThumbnail(
    url?: string | null,
    width?: number | null,
    height?: number | null,
    alt?: string | null,
  ) {
    if (!url) return nothing;
    return html`<umb-property-layout
      label=${this.#label("oembed_details_thumbnail", "Thumbnail")}
      orientation="vertical"
    >
      <div slot="editor">
        <img
          class="oembed-thumbnail"
          src="${url}"
          width="${width ?? nothing}"
          height="${height ?? nothing}"
          alt="${alt ?? ""}"
        />
      </div>
    </umb-property-layout>`;
  }

  #renderPreview(oembed: OEmbedResponse) {
    return html`
      <umb-property-layout
        label=${this.localize.term("general_preview")}
        orientation="vertical"
      >
        <div slot="editor">
          <oembed-preview live .value="${oembed}"></oembed-preview>
        </div>
      </umb-property-layout>
      ${this.#renderDetails(oembed)}
      ${this.#renderThumbnail(
        oembed.thumbnail_url,
        oembed.thumbnail_width,
        oembed.thumbnail_height,
        oembed.title,
      )}
    `;
  }

  renderInput() {
    return html`
      <uui-input
        id="url"
        data-mark="input:url"
        label=${this.localize.term("placeholders_enterUrl")}
        placeholder=${this.localize.term("placeholders_enterUrl")}
        .value=${this._currentUrl}
        @input=${this.#onUrlChange}
        required="true"
        ${umbFocus()}
        ${umbBindToValidation(this)}
      >
        <uui-button
          slot="append"
          look="primary"
          color="positive"
          @click=${this.#getPreview}
          label=${this.localize.term("general_retrieve")}
        ></uui-button>
      </uui-input>
    `;
  }

  override render() {
    return html`
      <umb-body-layout headline=${this.localize.term("general_embed")}>
        <uui-box>
          <umb-property-layout
            label=${this.localize.term("general_url")}
            orientation="vertical"
          >
            <div slot="editor">${this.renderInput()}</div>
          </umb-property-layout>
          ${when(
            this._currentLoading === "loading",
            () => html`<uui-loader-circle></uui-loader-circle>`,
          )}
          ${when(
            this._currentLoading === "success" &&
              this._currentPreview &&
              this.#isTypeAllowed(this._currentPreview.type),
            () => this.#renderPreview(this._currentPreview!),
          )}
          ${when(
            this._currentLoading === "success" &&
              this._currentPreview &&
              !this.#isTypeAllowed(this._currentPreview.type),
            () => html`<div class="oembed-error">
              ${this.localize.term(
                "oembed_errors_TypeNotAllowed",
                this._currentPreview!.type,
                this.data?.allowedTypes?.join(", ") ?? "",
              )}
            </div>`,
          )}
          ${when(
            this._currentLoading === "error",
            () => html`<div class="oembed-error">
              ${this.localize.term(localizedOEmbedErrorKey(this._currentError))}
            </div>`,
          )}
        </uui-box>
        <uui-button
          slot="actions"
          id="cancel"
          look="secondary"
          label=${this.localize.term("buttons_confirmActionCancel")}
          @click=${this.#close}
        ></uui-button>
        <uui-button
          slot="actions"
          id="submit"
          color="positive"
          look="primary"
          .state=${this._submitButtonState}
          label=${this.localize.term("buttons_confirmActionConfirm")}
          @click=${this.#submit}
        ></uui-button>
      </umb-body-layout>
    `;
  }

  static override styles = [
    UmbTextStyles,
    css`
      uui-input {
        width: 100%;
        --uui-button-border-radius: 0;
      }

      umb-property-layout:first-child {
        padding-top: 0;
      }

      umb-property-layout:last-child {
        padding-bottom: 0;
      }

      .oembed-details {
        display: flex;
        flex-direction: column;
        gap: var(--uui-size-space-2, 6px);
      }

      .oembed-details__row {
        display: grid;
        grid-template-columns: minmax(80px, 25%) 1fr;
        gap: var(--uui-size-space-3, 9px);
        align-items: baseline;
      }

      .oembed-details__label {
        color: var(--uui-color-text-alt, #515763);
        font-weight: var(--uui-font-weight-bold, 700);
      }

      .oembed-details__value {
        word-break: break-word;
      }

      .oembed-thumbnail {
        display: block;
        width: auto;
        height: auto;
        max-width: 100%;
        max-height: 40vh;
        border-radius: var(--uui-border-radius, 3px);
      }

      .oembed-error {
        color: var(--uui-color-danger, #d42054);
      }
    `,
  ];
}

export default OembedPickerModalElement;

declare global {
  interface HTMLElementTagNameMap {
    "oembed-modal": OembedPickerModalElement;
  }
}
