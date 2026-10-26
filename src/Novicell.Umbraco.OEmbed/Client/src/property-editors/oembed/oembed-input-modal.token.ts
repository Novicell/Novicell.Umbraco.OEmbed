import {UmbModalToken} from '@umbraco-cms/backoffice/modal';

export interface OEmbedData {
    type?: string;
    version?: string;
    title?: string;
    url?: string;
    html?: string;
    width?: number;
    height?: number;
    author_name?: string;
    author_url?: string;
    provider_name?: string;
    provider_url?: string;
}

export interface OEmbedPropertyValue {
    url: string;
    oembed: OEmbedData | null;
}

export interface OEmbedInputModalData {
    value: OEmbedPropertyValue | null;
    config?: { type?: string };
}

export interface OEmbedInputModalValue {
    value: OEmbedPropertyValue | null;
}

export const NOVICELL_OEMBED_MODAL_TOKEN = new UmbModalToken<OEmbedInputModalData, OEmbedInputModalValue>(
    'Novicell.OEmbed.InputModal',
    {
        modal: {
            type: 'sidebar',
            size: 'small',
        },
    }
);

