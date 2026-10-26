import {UmbLitElement} from '@umbraco-cms/backoffice/lit-element';
import {css, customElement, html, property, state, when} from '@umbraco-cms/backoffice/external/lit';
import type {OEmbedData} from './oembed-input-modal.token.js';

@customElement('novicell-oembed-preview')
export class NovicellOEmbedPreviewElement extends UmbLitElement {
    @property({attribute: false})
    model: OEmbedData | null = null;

    @state()
    private _srcdoc: string | null = null;

    @state()
    private _aspectRatio: number | null = null;

    override updated(changed: Map<string, unknown>) {
        super.updated(changed);
        if (changed.has('model')) {
            this._updatePreview();
        }
    }

    private _updatePreview() {
        const m = this.model;
        if (!m) {
            this._srcdoc = null;
            this._aspectRatio = null;
            return;
        }

        this._aspectRatio =
            m.width && m.height && !isNaN(m.width) && !isNaN(m.height)
                ? Math.round((m.height / m.width) * 10000) / 10000
                : null;

        if (m.html) {
            const div = document.createElement('div');
            div.insertAdjacentHTML('afterbegin', m.html);
            // Must set explicit dimensions on the wrapper div so that `height:100%` on
            // inner elements (html, body, iframe) resolves correctly inside the srcdoc.
            if (this._aspectRatio) {
                div.style.width = '100%';
                div.style.height = '100%';
            }
            const styleTag = this._aspectRatio ? '<style>html,body,iframe{width:100%;height:100%;}</style>' : '';
            this._srcdoc = `<html><head>${styleTag}</head><body>${div.outerHTML}</body></html>`;
        } else {
            this._srcdoc = null;
        }
    }

    // Only used when there is NO fixed aspect ratio, to auto-size the iframe to its content.
    private _handleFrameLoad(e: Event) {
        if (this._aspectRatio) return; // aspect-ratio container controls height via CSS — don't override

        const frame = e.target as HTMLIFrameElement;
        const contentWindow = frame.contentWindow;
        if (!contentWindow) return;

        const update = () => {
            const el = contentWindow.document.documentElement;
            frame.style.height = el.offsetHeight + 'px';
        };

        const observer = new MutationObserver(() => setTimeout(update, 1));
        const body = contentWindow.document.querySelector('body');
        if (body) observer.observe(body, {childList: true, subtree: true, attributes: true, characterData: true});
        update();
    }

    override render() {
        const m = this.model;
        if (!m) return html``;

        const isPhoto = m.type?.toLowerCase() === 'photo';
        const containerStyle = this._aspectRatio
            ? `position:relative;padding-top:${this._aspectRatio * 100}%;width:100%;`
            : `width:100%;`;

        return html`
            <div class="novicell-oembed-preview">
                <div class="preview-container" style="${containerStyle}">
                    ${when(
                            isPhoto && m.url,
                            () => html`
                                <img
                                        class="preview-frame"
                                        src="${m!.url}"
                                        width="${m!.width}"
                                        height="${m!.height}"
                                        alt=""
                                        title="${m!.title ?? ''}"
                                />
                            `,
                            () =>
                                    when(
                                            !!this._srcdoc,
                                            () => html`
                                                <iframe
                                                        class="preview-frame ${this._aspectRatio ? 'preview-frame--absolute' : ''}"
                                                        srcdoc="${this._srcdoc!}"
                                                        sandbox="allow-scripts allow-same-origin allow-presentation"
                                                        marginheight="0"
                                                        marginwidth="0"
                                                        scrolling="no"
                                                        @load=${this._handleFrameLoad}
                                                ></iframe>
                                            `
                                    )
                    )}
                </div>
                ${when(
                        !!m.title || !!m.author_name || !!m.provider_name,
                        () => html`
                            <div class="preview-info">
                                ${when(!!m!.title, () => html`
                                    <div class="preview-title">${m!.title}</div>`)}
                                <div class="preview-meta">
                                    ${when(
                                            !!m!.author_name,
                                            () => html`
                                                <span>
                      ${this.localize.term('general_by')}
                      ${m!.author_url
                              ? html`<a href="${m!.author_url}" target="_blank"
                                        rel="noopener noreferrer">${m!.author_name}</a>`
                              : html`${m!.author_name}`}
                    </span>
                                            `
                                    )}
                                    ${when(
                                            !!m!.provider_name,
                                            () => html`
                                                <span>
                      (${m!.provider_url
                                                            ? html`<a href="${m!.provider_url}" target="_blank"
                                                                      rel="noopener noreferrer">${m!.provider_name}</a>`
                                                            : html`${m!.provider_name}`})
                    </span>
                                            `
                                    )}
                                </div>
                            </div>
                        `
                )}
            </div>
        `;
    }

    static override styles = css`
        .novicell-oembed-preview {
            width: 100%;
        }

        .preview-container {
            max-width: 100%;
            overflow: hidden;
        }

        .preview-frame {
            max-width: 100%;
            width: 100%;
            height: auto;
            border: none;
        }

        .preview-frame--absolute {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            height: 100%;
        }

        .preview-info {
            margin-top: var(--uui-size-space-3, 8px);
        }

        .preview-title {
            font-weight: bold;
        }

        .preview-meta {
            color: var(--uui-color-text-alt, #333);
            font-size: 0.85em;
        }

        .preview-meta span + span {
            margin-left: 4px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'novicell-oembed-preview': NovicellOEmbedPreviewElement;
    }
}
