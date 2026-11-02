import type { ManifestModal } from '@umbraco-cms/backoffice/modal';

const modals: Array<ManifestModal> = [
    {
        type: 'modal',
        alias: 'Novicell.Modal.OEmbedPicker',
        name: 'OEmbed Picker Modal',
        js: () => import('./oembed-picker-modal.element.js'),
    },
];

export const manifests = [...modals];