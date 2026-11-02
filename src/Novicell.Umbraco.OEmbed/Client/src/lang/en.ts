import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
  oembed_details: {
    title: "Title",
    author: "Author",
    provider: "Provider",
    thumbnail: "Thumbnail",
  },
  oembed_errors: {
    ProviderNotSupported: "This source isn't supported.",
    EmbedNotFound: "Content not found at this URL.",
    InvalidResponse:
      "Unable to retrieve the content — please check that the URL is correct.",
    Unknown: "An unexpected error occurred.",
    TypeNotAllowed:
      "This embed's type (%0%) isn't allowed for this field. Allowed types are: %1%",
    SingleValueOnly: "This field only allows one embed.",
  },
} satisfies UmbLocalizationDictionary;
