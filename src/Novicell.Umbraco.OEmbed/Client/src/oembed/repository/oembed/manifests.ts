import { OEMBED_REPOSITORY_ALIAS } from './constants.js';

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: 'repository',
        alias: OEMBED_REPOSITORY_ALIAS,
        name: 'OEmbed Repository',
        api: () => import('./oembed.repository.js'),
    },
];