import {UmbLitElement} from '@umbraco-cms/backoffice/lit-element';
import {css, customElement, html, property, state, when} from '@umbraco-cms/backoffice/external/lit';
import {UmbModalExtensionElement} from '@umbraco-cms/backoffice/modal';
import type {UmbModalContext} from '@umbraco-cms/backoffice/modal';
import type {
    OEmbedInputModalData,
    OEmbedInputModalValue,
    OEmbedPropertyValue,
    OEmbedData,
} from './oembed-input-modal.token.js';
import {OEmbedService} from '../../api/index.js';
import './oembed-preview.element.js';

@customElement('novicell-oembed-input-modal')
export default class NovicellOEmbedInputModalElement
    extends UmbLitElement
    implements UmbModalExtensionElement<OEmbedInputModalData, OEmbedInputModalValue> {
    @property({attribute: false})
    modalContext?: UmbModalContext<OEmbedInputModalData, OEmbedInputModalValue>;

    @property({attribute: false})
    data?: OEmbedInputModalData;

    @state()
    private _url: string = '';

    @state()
    private _oembed: OEmbedData | null = null;

    @state()
    private _loading: boolean = false;

    @state()
    private _error: string | null = null;

    private _debounceTimer: ReturnType<typeof setTimeout> | null = null;

    override connectedCallback() {
        super.connectedCallback();
        if (this.data?.value) {
            this._url = this.data.value.url ?? '';
            this._oembed = this.data.value.oembed ?? null;
        }
    }

    private _handleUrlInput(e: Event) {
        this._url = (e.target as HTMLInputElement).value;
        this._error = null;

        if (this._debounceTimer) clearTimeout(this._debounceTimer);
        this._debounceTimer = setTimeout(() => this._fetchOEmbed(), 600);
    }

    private async _fetchOEmbed() {
        const url = this._url.trim();
        if (!url) {
            this._oembed = null;
            return;
        }

        this._loading = true;
        this._error = null;
        try {
            const res = await OEmbedService.getUmbracoOembedGet({query: {url}});
            if (res.data) {
                this._oembed = res.data as OEmbedData;
            } else {
                this._oembed = null;
                this._error = 'Could not load embed for the given URL.';
            }
        } catch {
            this._oembed = null;
            this._error = 'Failed to fetch OEmbed data.';
        } finally {
            this._loading = false;
        }
    }

    private _handleSubmit() {
        const value: OEmbedPropertyValue = {
            url: this._url,
            oembed: this._oembed,
        };
        this.modalContext?.setValue({value});
        this.modalContext?.submit();
    }

    private _handleClose() {
        this.modalContext?.destroy();
    }

    override render() {
        return html`
            <umb-body-layout headline=${this.localize.term('general_embed')}>
                <uui-box>
                    <div class="form-group">
                        <uui-label for="oembed-url">${this.localize.term('general_url')}</uui-label>
                        <uui-input
                                id="oembed-url"
                                type="text"
                                .value=${this._url}
                                @input=${this._handleUrlInput}
                                placeholder="https://..."
                        ></uui-input>
                    </div>

                    ${when(this._loading, () => html`
                        <uui-loader></uui-loader>`)}
                    ${when(!!this._error, () => html`
                        <uui-tag color="danger">${this._error}</uui-tag>`)}
                    ${when(
                            !!this._oembed,
                            () => html`
                                <div class="form-group">
                                    <uui-label>${this.localize.term('general_preview')}</uui-label>
                                    <novicell-oembed-preview .model=${this._oembed}></novicell-oembed-preview>
                                </div>
                            `
                    )}
                </uui-box>

                <div slot="actions">
                    <uui-button
                            look="default"
                            color="default"
                            label=${this.localize.term('general_close')}
                            @click=${this._handleClose}
                    ></uui-button>
                    <uui-button
                            look="primary"
                            color="positive"
                            label=${this.localize.term('general_submit')}
                            .disabled=${!this._url}
                            @click=${this._handleSubmit}
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = css`
        uui-box {
            margin-bottom: var(--uui-size-space-5, 16px);
        }

        .form-group {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-3, 8px);
            margin-bottom: var(--uui-size-space-5, 16px);
        }

        uui-input {
            width: 100%;
        }

        uui-loader {
            display: block;
            margin: var(--uui-size-space-3, 8px) auto;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'novicell-oembed-input-modal': NovicellOEmbedInputModalElement;
    }
}

