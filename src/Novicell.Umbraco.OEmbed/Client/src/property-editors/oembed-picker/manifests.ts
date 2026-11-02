import type { ManifestPropertyEditorUi, ManifestPropertyEditorSchema } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorSchema: ManifestPropertyEditorSchema = {
    type: "propertyEditorSchema",
    name: "Novicell OEmbed Picker Schema",
    alias: "Novicell.OEmbed",
    meta: {
        defaultPropertyEditorUiAlias: 'Novicell.PropertyEditorUi.OEmbedPicker',
        settings: {
            properties: [
                {
                    alias: 'multiple',
                    label: 'Pick multiple items',
                    description: 'Allow picking more than one embed',
                    propertyEditorUiAlias: 'Umb.PropertyEditorUi.Toggle',
                },
                {
                    alias: 'validationLimit',
                    label: 'Amount',
                    description: 'Set a required range of embeds',
                    propertyEditorUiAlias: 'Umb.PropertyEditorUi.NumberRange',
                    config: [{ alias: 'validationRange', value: { min: 0, max: Infinity } }],
                },
                {
                    alias: 'allowedTypes',
                    label: 'Accepted types',
                    description: 'Limit to specific oEmbed types. Leave empty to allow any.',
                    propertyEditorUiAlias: 'Umb.PropertyEditorUi.CheckBoxList',
                    config: [
                        {
                            alias: 'items',
                            value: [
                                { name: 'Photo', value: 'photo' },
                                { name: 'Video', value: 'video' },
                                { name: 'Link', value: 'link' },
                                { name: 'Rich', value: 'rich' },
                            ],
                        },
                    ],
                },
            ],
        },
    },
};

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: 'Novicell.PropertyEditorUi.OEmbedPicker',
        name: 'OEmbed Picker Property Editor UI',
        element: () => import('./property-editor-ui-oembed-picker.element.ts'),
        meta: {
            label: 'OEmbed Picker',
            propertyEditorSchemaAlias: 'Novicell.OEmbed',
            icon: 'icon-embed',
            group: 'media',
        supportsReadOnly: true,
    },
}

export const oembedPickerPropertyEditorManifests = [propertyEditorSchema, propertyEditorUi];