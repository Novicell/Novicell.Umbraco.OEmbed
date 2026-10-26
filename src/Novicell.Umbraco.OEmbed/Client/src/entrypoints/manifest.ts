export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "OEmbed Entrypoint",
    alias: "OEmbed.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
