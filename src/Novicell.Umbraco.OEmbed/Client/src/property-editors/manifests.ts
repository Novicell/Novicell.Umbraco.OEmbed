import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";

import { oembedPickerPropertyEditorManifests } from './oembed-picker/manifests.js';

export const propertyEditorManifests : Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    ...oembedPickerPropertyEditorManifests
];