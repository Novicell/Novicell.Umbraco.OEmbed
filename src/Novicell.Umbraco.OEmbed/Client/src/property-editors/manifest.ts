const novicellOEmbedPropertyEditor = {
    type: "propertyEditorUi",
    alias: "Novicell.OEmbed",
    name: "Novicell OEmbed",
    element: () => import("./oembed/oembed-property-editor-ui.element.js"),
    meta: {
        label: "OEmbed",
        icon: "icon-movie-alt",
        group: "media",
        propertyEditorSchemaAlias: "Novicell.OEmbed",
        settings: {
            properties: [
                {
                    alias: "type",
                    label: "OEmbed Type",
                    description: "Filter for a specific OEmbed type (e.g. Video)",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
            ],
        },
    },
};

export const manifests = [novicellOEmbedPropertyEditor];
