const novicellOEmbedInputModal = {
  type: "modal",
  alias: "Novicell.OEmbed.InputModal",
  name: "Novicell OEmbed Input Modal",
  element: () => import("../property-editors/oembed/oembed-input-modal.element.js"),
}

export const manifests = [novicellOEmbedInputModal];
