import { http, HttpResponse } from "msw";
import { mockOEmbedResponses } from "./data";

/**
 * MSW request handlers for oEmbed API.
 * Intercepts HTTP requests and returns mock responses for testing.
 */
export const handlers = [
  // GET /umbraco/management/api/v1.0/novicell-oembed/oembed
  http.get(
    "*/umbraco/management/api/v1.0/novicell-oembed/oembed",
    ({ request }) => {
      const url = new URL(request.url);
      const urlParam = url.searchParams.get("url");

      if (!urlParam) {
        return HttpResponse.json(
          { error: "Missing URL parameter" },
          { status: 400 },
        );
      }

      // Mock successful response for YouTube URLs
      if (urlParam.includes("youtube.com")) {
        return HttpResponse.json(mockOEmbedResponses.youtube);
      }

      // Mock successful response for Vimeo URLs
      if (urlParam.includes("vimeo.com")) {
        return HttpResponse.json(mockOEmbedResponses.vimeo);
      }

      // Mock response for image URLs
      if (urlParam.endsWith(".jpg") || urlParam.endsWith(".png")) {
        return HttpResponse.json(mockOEmbedResponses.image);
      }

      // Default 404 for unsupported URLs
      return HttpResponse.json(
        { error: "Provider not supported" },
        { status: 404 },
      );
    },
  ),
];

/**
 * Export setup function for test files to use in beforeEach.
 * Usage: import { setupServer } from 'msw/node'; import { handlers } from './mocks/handlers';
 * const server = setupServer(...handlers);
 * beforeEach(() => server.listen());
 * afterEach(() => server.resetHandlers());
 * after(() => server.close());
 */
export { setupServer } from "msw/node";
