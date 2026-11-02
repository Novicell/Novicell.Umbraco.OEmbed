import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { OEmbedValueModel } from "../../../property-editors/types.ts";

export interface OEmbedModalData {
    url?: string;
    allowedTypes?: string[];
}

export const OEMBED_MODAL = new UmbModalToken<OEmbedModalData, OEmbedValueModel>(
    'Novicell.Modal.OEmbedPicker',
    {
        modal: {
            type: 'sidebar',
            size: 'small',
        },
    },
);