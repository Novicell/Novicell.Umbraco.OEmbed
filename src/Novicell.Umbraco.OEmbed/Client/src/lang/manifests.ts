import type { ManifestLocalization } from "@umbraco-cms/backoffice/localization";

export const localizationManifests: Array<ManifestLocalization> = [
  {
    type: "localization",
    alias: "Novicell.OEmbed.Localization.EN",
    name: "Novicell OEmbed English Localization",
    meta: { culture: "en" },
    js: () => import("./en.ts"),
  },
  {
    type: "localization",
    alias: "Novicell.OEmbed.Localization.DA",
    name: "Novicell OEmbed Danish Localization",
    meta: { culture: "da" },
    js: () => import("./da.ts"),
  },
];
