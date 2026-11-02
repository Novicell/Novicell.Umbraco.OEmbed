import type { OEmbedResponse } from "../src/api";

/**
 * Mock oEmbed responses for testing.
 */
export const mockOEmbedResponses = {
  youtube: {
    type: "video",
    version: "1.0",
    provider_name: "YouTube",
    provider_url: "https://www.youtube.com/",
    title: "Me at the zoo",
    author_name: "jawed",
    author_url: "https://www.youtube.com/user/jawed",
    height: 338,
    width: 600,
    length: 25,
    description: "The first video uploaded to YouTube",
    thumbnail_height: 360,
    thumbnail_width: 480,
    thumbnail_url: "https://i.ytimg.com/vi/jNQXAC9IVRw/hqdefault.jpg",
    html: '<iframe width="600" height="338" src="https://www.youtube.com/embed/jNQXAC9IVRw" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>',
  } as OEmbedResponse,

  vimeo: {
    type: "video",
    version: "1.0",
    provider_name: "Vimeo",
    provider_url: "https://vimeo.com/",
    title: "Vimeo Example Video",
    author_name: "Vimeo",
    author_url: "https://vimeo.com/vimeo",
    height: 360,
    width: 640,
    description: "A sample Vimeo video for testing",
    thumbnail_height: 360,
    thumbnail_width: 640,
    thumbnail_url: "https://i.vimeocdn.com/video/123456.jpg",
    html: '<iframe src="https://player.vimeo.com/video/123456" width="640" height="360" frameborder="0" webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>',
  } as OEmbedResponse,

  image: {
    type: "photo",
    version: "1.0",
    title: "Sample Image",
    url: "https://example.com/image.jpg",
    width: 800,
    height: 600,
    thumbnail_url: "https://example.com/image-thumb.jpg",
    thumbnail_width: 200,
    thumbnail_height: 150,
  } as OEmbedResponse,

  link: {
    type: "link",
    version: "1.0",
    title: "Example Link",
    url: "https://example.com",
    description: "This is a sample link embed",
  } as OEmbedResponse,
};
