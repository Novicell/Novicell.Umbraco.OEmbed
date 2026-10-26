import {UmbLitElement} from '@umbraco-cms/backoffice/lit-element';
import {css, customElement, html, property, when} from '@umbraco-cms/backoffice/external/lit';
import {UmbChangeEvent} from '@umbraco-cms/backoffice/event';
import type {UmbPropertyEditorUiElement} from '@umbraco-cms/backoffice/property-editor';
import type {UmbPropertyEditorConfigCollection} from '@umbraco-cms/backoffice/property-editor';
import {UMB_MODAL_MANAGER_CONTEXT} from '@umbraco-cms/backoffice/modal';
import {NOVICELL_OEMBED_MODAL_TOKEN} from './oembed-input-modal.token.js';
import type {OEmbedPropertyValue} from './oembed-input-modal.token.js';
import './oembed-preview.element.js';

@customElement('novicell-oembed-property-editor-ui')
export default class NovicellOEmbedPropertyEditorUiElement
    extends UmbLitElement
    implements UmbPropertyEditorUiElement {
    @property({attribute: false})
    value: OEmbedPropertyValue | null = null;

    @property({attribute: false})
    config?: UmbPropertyEditorConfigCollection;

    private get _typeConfig(): string | undefined {
        return this.config?.getValueByAlias<string>('type');
    }

    private async _openModal(existing: OEmbedPropertyValue | null) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modalHandler = modalManager.open(this, NOVICELL_OEMBED_MODAL_TOKEN, {
            data: {
                value: existing,
                config: {type: this._typeConfig},
            },
        });

        const result = await modalHandler.onSubmit().catch(() => null);
        if (result?.value !== undefined) {
            this.value = result.value;
            this.dispatchEvent(new UmbChangeEvent());
        }
    }

    private _handleAdd() {
        this._openModal(null);
    }

    private _handleEdit() {
        this._openModal(this.value);
    }

    private _handleRemove() {
        this.value = null;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        const hasValue = !!this.value?.oembed;

        return html`
            ${when(
                    hasValue,
                    () => html`
                        <div class="oembed-preview-wrapper">
                            <div class="oembed-preview-content">
                                <novicell-oembed-preview .model=${this.value!.oembed}></novicell-oembed-preview>
                            </div>
                            <div class="oembed-preview-actions">
                                <uui-button
                                        look="secondary"
                                        label=${this.localize.term('general_edit')}
                                        @click=${this._handleEdit}
                                ></uui-button>
                                <uui-button
                                        look="secondary"
                                        color="danger"
                                        label=${this.localize.term('general_remove')}
                                        @click=${this._handleRemove}
                                ></uui-button>
                            </div>
                        </div>
                    `,
                    () => html`
                        <uui-button
                                look="placeholder"
                                label=${this.localize.term('general_add')}
                                @click=${this._handleAdd}
                        >
                            <uui-icon name="icon-add"></uui-icon>
                            ${this.localize.term('general_add')}
                        </uui-button>
                    `
            )}
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }

        uui-button[look='placeholder'] {
            width: 100%;
        }

        .oembed-preview-wrapper {
            border: 1px solid var(--uui-color-border, #d4d4d4);
            border-radius: var(--uui-border-radius, 3px);
            overflow: hidden;
            max-width: 800px;
        }

        .oembed-preview-content {
            padding: var(--uui-size-space-4, 12px);
            overflow: hidden;
        }

        .oembed-preview-actions {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2, 6px);
            padding: var(--uui-size-space-2, 6px) var(--uui-size-space-4, 12px);
            border-top: 1px solid var(--uui-color-border, #d4d4d4);
            background: var(--uui-color-surface-alt, #f5f5f5);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'novicell-oembed-property-editor-ui': NovicellOEmbedPropertyEditorUiElement;
    }
}



