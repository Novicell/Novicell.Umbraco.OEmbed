import {
  html,
  customElement,
  property,
  state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_PROPERTY_CONTEXT } from "@umbraco-cms/backoffice/property";
import type {
  UmbPropertyEditorConfigCollection,
  UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";
import type { UmbNumberRangeValueType } from "@umbraco-cms/backoffice/models";
import {
  UMB_VALIDATION_EMPTY_LOCALIZATION_KEY,
  UmbFormControlMixin,
} from "@umbraco-cms/backoffice/validation";

import { OEmbedValueModel } from "../types.ts";
import "../../oembed/components/input-oembed/input-oembed.element.js";

@customElement(PropertyEditorUiOEmbedPickerElement.tagName)
export default class PropertyEditorUiOEmbedPickerElement
  extends UmbFormControlMixin<
    OEmbedValueModel[],
    typeof UmbLitElement,
    undefined
  >(UmbLitElement)
  implements UmbPropertyEditorUiElement
{
  static readonly tagName = "oembed-property-editor";

  public set config(config: UmbPropertyEditorConfigCollection | undefined) {
    if (!config) return;

    this._multiple = config.getValueByAlias<boolean>("multiple") ?? false;

    const minMax = config.getValueByAlias<UmbNumberRangeValueType>("validationLimit");
    this._min = minMax?.min ?? undefined;
    this._max = minMax?.max ?? undefined;

    this._allowedTypes = config.getValueByAlias<string[]>("allowedTypes") ?? [];
  }

  /**
   * Sets the input to mandatory, meaning validation will fail if the value is empty.
   * @type {boolean}
   */
  @property({ type: Boolean })
  mandatory?: boolean;

  @property({ type: String })
  mandatoryMessage = UMB_VALIDATION_EMPTY_LOCALIZATION_KEY;

  /**
   * Sets the input to readonly mode, meaning value cannot be changed but still able to read and select its content.
   * @type {boolean}
   * @attr
   * @default false
   */
  @property({ type: Boolean, reflect: true })
  readonly = false;

  @state()
  private _alias?: string;

  @state()
  private _variantId?: string;

  @state()
  private _multiple = false;

  @state()
  private _min?: number;

  @state()
  private _max?: number;

  @state()
  private _allowedTypes: string[] = [];

  constructor() {
    super();

    this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
      this.observe(context?.alias, (alias) => (this._alias = alias));
      this.observe(
        context?.variantId,
        (variantId) => (this._variantId = variantId?.toString() || "invariant"),
      );
    });
  }

  override firstUpdated() {
    // Registers the inner <input-oembed> as this control's form control element, so
    // this wrapper's validity (and therefore server-side validation messages bound to
    // this property) delegates to the actual input, matching how Umbraco's own picker
    // wrappers (e.g. Media Picker's umb-property-editor-ui-media-picker) do it.
    this.addFormControlElement(this.shadowRoot!.querySelector("input-oembed")!);
  }

  #onChange(
    event: CustomEvent & {
      target: HTMLInputElement & { value?: OEmbedValueModel[] };
    },
  ) {
    const isEmpty = !event.target.value?.length;
    this.value = isEmpty ? undefined : event.target.value;
    this.dispatchEvent(new UmbChangeEvent());
  }

  override render() {
    return html`
      <input-oembed
        .alias=${this._alias}
        .value=${this.value}
        .variantId=${this._variantId}
        .required=${this.mandatory}
        .requiredMessage=${this.mandatoryMessage}
        ?multiple=${this._multiple}
        .min=${this._min}
        .max=${this._max}
        .allowedTypes=${this._allowedTypes}
        @change=${this.#onChange}
        ?readonly=${this.readonly}
      >
      </input-oembed>
    `;
  }
}

export { PropertyEditorUiOEmbedPickerElement as element };

declare global {
  interface HTMLElementTagNameMap {
    [PropertyEditorUiOEmbedPickerElement.tagName]: PropertyEditorUiOEmbedPickerElement;
  }
}
