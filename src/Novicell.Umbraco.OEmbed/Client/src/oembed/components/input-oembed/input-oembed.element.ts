import {
  css,
  html,
  customElement,
  nothing,
  property,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { UmbPropertyEditorUiElement } from "@umbraco-cms/backoffice/property-editor";
import {
  UMB_VALIDATION_EMPTY_LOCALIZATION_KEY,
  UmbFormControlMixin,
} from "@umbraco-cms/backoffice/validation";

import { OEmbedValueModel } from "../../../property-editors/types.ts";
import { isOEmbedTypeAllowed } from "../../allowed-types.ts";
import "../oembed-preview/index.ts";
import { UmbOEmbedPickerContext } from "../../modals/oembed-picker/oembed-picker.context.ts";
import { UMB_OEMBED_PICKER_CONTEXT } from "../../modals/oembed-picker/oembed-picker.context.token.ts";

@customElement(InputOEmbedElement.tagName)
export class InputOEmbedElement
  extends UmbFormControlMixin<
    OEmbedValueModel[],
    typeof UmbLitElement,
    undefined
  >(UmbLitElement)
  implements UmbPropertyEditorUiElement
{
  static readonly tagName = "input-oembed";

  /**
   * Sets the input to required, meaning validation will fail if the value is empty.
   * @type {boolean}
   */
  @property({ type: Boolean })
  required?: boolean;

  @property({ type: String })
  requiredMessage?: string;

  /**
   * Allows picking more than one embed. When false, only a single embed is allowed.
   * @type {boolean}
   */
  @property({ type: Boolean })
  multiple = false;

  /**
   * This is a minimum amount of embeds in this input.
   * @type {number}
   */
  @property({ type: Number })
  min?: number;

  /**
   * This is a maximum amount of embeds in this input.
   * @type {number}
   */
  @property({ type: Number })
  max?: number;

  /**
   * Restricts which oEmbed types (video/photo/link/rich) can be picked. Empty/undefined
   * means no restriction.
   * @type {string[]}
   */
  @property({ type: Array })
  allowedTypes?: string[];

  /**
   * Sets the input to readonly mode, meaning value cannot be changed but still able to read and select its content.
   * @type {boolean}
   * @attr
   * @default
   */
  @property({ type: Boolean, reflect: true })
  readonly?: boolean;

  override get value(): OEmbedValueModel[] {
    return super.value ?? [];
  }
  override set value(value: OEmbedValueModel[] | OEmbedValueModel | undefined) {
    // Tolerate legacy content saved before multi-value support: Umbraco hands the
    // parsed stored JSON straight to the property editor UI, so pre-existing
    // properties still arrive as a single { url, oembed } object, not an array.
    if (!value) {
      super.value = [];
    } else if (Array.isArray(value)) {
      super.value = value;
    } else {
      super.value = [value];
    }
  }

  readonly #pickerContext = new UmbOEmbedPickerContext(this);

  constructor() {
    super();
    this.provideContext(UMB_OEMBED_PICKER_CONTEXT, this.#pickerContext);

    this.addValidator(
      "valueMissing",
      () => this.requiredMessage ?? UMB_VALIDATION_EMPTY_LOCALIZATION_KEY,
      () => !this.readonly && !!this.required && this.value.length === 0,
    );
    this.addValidator(
      "rangeUnderflow",
      () =>
        this.localize.term(
          "validation_entriesShort",
          this.min,
          (this.min ?? 0) - this.value.length,
        ),
      () => !this.readonly && !!this.min && this.value.length < this.min,
    );
    this.addValidator(
      "rangeOverflow",
      () =>
        this.localize.term(
          "validation_entriesExceed",
          this.max,
          this.value.length - (this.max ?? 0),
        ),
      () => !this.readonly && !!this.max && this.value.length > this.max,
    );
    // The server rejects both of these on save (OEmbedPickerPropertyValueEditor.AllowedTypeValidator and
    // OEmbedPickerPropertyValueEditor.MinMaxValidator), so surface them in the editor as well.
    this.addValidator(
      "typeMismatch",
      () =>
        this.localize.term(
          "oembed_errors_TypeNotAllowed",
          this.#disallowedTypes().join(", "),
          (this.allowedTypes ?? []).join(", "),
        ),
      () => !this.readonly && this.#disallowedTypes().length > 0,
    );
    this.addValidator(
      "customError",
      () => this.localize.term("oembed_errors_SingleValueOnly"),
      () => !this.readonly && !this.multiple && this.value.length > 1,
    );
  }

  async #openPicker(url?: string, editIndex?: number) {
    const result = await this.#pickerContext.openPicker(url, this.allowedTypes);
    if (!result) return;

    const items = [...this.value];
    if (editIndex !== undefined) {
      items[editIndex] = result;
    } else {
      items.push(result);
    }

    this.value = items;
    this.dispatchEvent(new UmbChangeEvent());
  }

  async #onRemove(index: number) {
    try {
      await this.requestRemoveItem(this.value[index]);
      const items = [...this.value];
      items.splice(index, 1);
      this.value = items;
      this.dispatchEvent(new UmbChangeEvent());
    } catch {
      // User cancelled the action
    }
  }

  async requestRemoveItem(value: OEmbedValueModel) {
    const title = value.oembed.title || value.url;
    await umbConfirmModal(this, {
      color: "danger",
      headline: `#actions_remove ${title}?`,
      content: `#defaultdialogs_confirmremove ${title}?`,
      confirmLabel: "#actions_remove",
    });
  }

  #renderActions(index: number) {
    if (this.readonly) return nothing;
    return html`
      <uui-action-bar slot="actions">
        <uui-button
          label=${this.localize.term("general_remove")}
          look="secondary"
          @click=${() => this.#onRemove(index)}
        >
          <uui-icon name="icon-trash"></uui-icon>
        </uui-button>
      </uui-action-bar>
    `;
  }

  #detail(oembed: OEmbedValueModel["oembed"]): string {
    return [oembed.provider_name, oembed.author_name].filter(Boolean).join(" · ");
  }

  #dimensions(oembed: OEmbedValueModel["oembed"]): {
    width?: number;
    height?: number;
  } {
    if (oembed.thumbnail_width && oembed.thumbnail_height) {
      return { width: oembed.thumbnail_width, height: oembed.thumbnail_height };
    }
    if (oembed.width && oembed.height) {
      return { width: oembed.width, height: oembed.height };
    }

    // Type can arrive in mixed casing from stored data, so normalise first.
    switch (oembed.type?.toLowerCase()) {
      case "video":
        return { width: 16, height: 9 };
      case "rich":
      case "link":
        return { width: 21, height: 9 };
      case "photo":
      default:
        return { width: 4, height: 3 };
    }
  }

  #renderPreview(value: OEmbedValueModel) {
    if (value.oembed.thumbnail_url) {
      return html`<img
        src=${value.oembed.thumbnail_url}
        alt=${value.oembed.title ?? value.url}
      />`;
    }
    return html`<oembed-preview
      contain
      .value="${value.oembed}"
      .sourceUrl="${value.url}"
    ></oembed-preview>`;
  }

  #isTypeAllowed(item: OEmbedValueModel): boolean {
    return isOEmbedTypeAllowed(this.allowedTypes, item.oembed.type);
  }

  #disallowedTypes(): string[] {
    return [
      ...new Set(
        this.value
          .filter((item) => !this.#isTypeAllowed(item))
          .map((item) => item.oembed.type),
      ),
    ];
  }

  #renderOEmbed(value: OEmbedValueModel, index: number) {
    const { width, height } = this.#dimensions(value.oembed);
    const aspectRatio = width && height ? width / height : 0;
    return html`
      <uui-card-media
        style=${aspectRatio >= 1 && aspectRatio <= 3
          ? `aspect-ratio: ${width} / ${height}`
          : ""}
        .name=${value.oembed.title}
        .detail=${this.#detail(value.oembed)}
        .selectonly="true"
        .selectable="false"
        @open="${() => this.#openPicker(value.url, index)}"
        ?readonly=${this.readonly}
        ?error=${!this.#isTypeAllowed(value)}
      >
        ${this.#renderPreview(value)} ${this.#renderActions(index)}
      </uui-card-media>
    `;
  }

  #renderItems() {
    return this.value.map((item, index) => this.#renderOEmbed(item, index));
  }

  #renderAddButton() {
    if (this.readonly) return nothing;
    if (!this.multiple && this.value.length >= 1) return nothing;
    if (this.max && this.value.length >= this.max) return nothing;

    return html`
      <uui-button
        id="btn-add"
        look="placeholder"
        @blur=${() => {
          this.pristine = false;
          this.checkValidity();
        }}
        @click=${() => this.#openPicker("")}
        label=${this.localize.term("general_choose")}
        ?disabled=${this.readonly}
      >
        <uui-icon name="icon-add"></uui-icon>
        ${this.localize.term("general_choose")}
      </uui-button>
    `;
  }

  override render() {
    const isEmpty = this.value.length === 0;
    return html`<div class="container ${isEmpty ? "container--empty" : ""}">
      ${this.#renderItems()} ${this.#renderAddButton()}
    </div>`;
  }

  static styles = [
    css`
      :host {
        position: relative;
      }

      /*
       * .container holds zero or more cards plus (optionally) the add-button.
       * Populated: flex-wrap, each card sized by its own aspect-ratio + fixed
       * row height, so cards of any ratio (16:9 video, 1:1 photo, etc.) keep
       * their natural size; the add-button (if shown) is just one more flex
       * item, sized as a fixed square cell.
       * Empty: fall back to Umbraco's own placeholder grid (auto-fill/minmax/
       * 1fr, same as Media Picker/Rich Media) so the lone add-button matches
       * their sizing exactly.
       */
      .container {
        display: flex;
        flex-wrap: wrap;
        gap: var(--uui-size-space-5);
        align-items: flex-start;
      }

      .container--empty {
        display: grid;
        grid-template-columns: repeat(
          auto-fill,
          minmax(var(--umb-card-medium-min-width), 1fr)
        );
        grid-auto-rows: var(--umb-card-medium-min-width);
      }

      uui-card-media {
        height: var(--umb-card-medium-min-width);
        width: auto;
        /* aspect-ratio override inline per card */
        aspect-ratio: 1/1;
      }

      #btn-add {
        text-align: center;
        /* Populated (flex) state: a fixed square cell, same size as a card's row height. */
        height: var(--umb-card-medium-min-width);
        width: var(--umb-card-medium-min-width);
      }

      .container--empty #btn-add {
        /* Empty (grid) state: fill the auto-fill/minmax(...)/1fr track exactly,
           matching Umbraco's Media Picker/Rich Media placeholder pixel-for-pixel. */
        height: 100%;
        width: 100%;
      }

      uui-icon {
        display: block;
        margin: 0 auto;
      }

      uui-card-media umb-icon {
        font-size: var(--uui-size-8);
      }

      uui-card-media[drag-placeholder] {
        opacity: 0.2;
      }

      img {
        display: block;
        width: 100%;
        height: 100%;
        object-fit: cover;
        background-image: url('data:image/svg+xml;charset=utf-8,<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" fill-opacity=".1"><path d="M50 0h50v50H50zM0 50h50v50H0z"/></svg>');
        background-size: 10px 10px;
        background-repeat: repeat;
      }
    `,
  ];
}

export default InputOEmbedElement;

declare global {
  interface HTMLElementTagNameMap {
    [InputOEmbedElement.tagName]: InputOEmbedElement;
  }
}
