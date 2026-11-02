import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
  oembed_details: {
    title: "Titel",
    author: "Forfatter",
    provider: "Udbyder",
    thumbnail: "Miniature",
  },
  oembed_errors: {
    ProviderNotSupported:
      "Denne kilde er ikke understøttet.",
    EmbedNotFound: "Indhold blev ikke fundet på denne URL.",
    InvalidResponse:
      "Kunne ikke hente indholdet — kontrollér at URL'en er korrekt.",
    Unknown: "Der opstod en uventet fejl.",
    TypeNotAllowed:
      "Denne embed-type (%0%) er ikke tilladt for dette felt. Tilladte typer er: %1%",
    SingleValueOnly: "Dette felt tillader kun én embed.",
  },
} satisfies UmbLocalizationDictionary;
