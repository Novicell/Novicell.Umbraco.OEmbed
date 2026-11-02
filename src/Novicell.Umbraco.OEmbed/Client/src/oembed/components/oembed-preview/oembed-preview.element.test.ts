import { expect } from "chai";
import "./oembed-preview.element.ts";
import type { OEmbedPreviewElement } from "./oembed-preview.element.ts";

/**
 * Tests for OEmbedPreviewElement.
 * Verifies it renders the correct markup per oEmbed type and normalizes casing.
 */
describe("OEmbedPreviewElement", () => {
  let element: OEmbedPreviewElement;

  beforeEach(() => {
    element = document.createElement(
      "oembed-preview",
    ) as OEmbedPreviewElement;
    // Provider markup only runs in live mode (the picker modal's fresh server response).
    element.live = true;
    document.body.appendChild(element);
  });

  afterEach(() => {
    element.remove();
  });

  it("renders nothing when no value is set", async () => {
    await element.updateComplete;
    expect(element.shadowRoot!.querySelector(".oembed-frame")).to.be.null;
  });

  it("renders an <img> for a photo type", async () => {
    element.value = {
      type: "photo" as any,
      url: "https://example.com/photo.jpg",
      width: 800,
      height: 600,
    };
    await element.updateComplete;

    const img = element.shadowRoot!.querySelector("img.oembed-media");
    expect(img).to.not.be.null;
    expect(img!.getAttribute("src")).to.equal(
      "https://example.com/photo.jpg",
    );
  });

  it("falls back to the thumbnail card for a photo with no url", async () => {
    element.value = {
      type: "photo" as any,
      thumbnail_url: "https://example.com/thumb.jpg",
      thumbnail_width: 200,
      thumbnail_height: 100,
    };
    await element.updateComplete;

    const img = element.shadowRoot!.querySelector("img.oembed-media");
    expect(img!.getAttribute("src")).to.equal(
      "https://example.com/thumb.jpg",
    );
  });

  it("renders the thumbnail card for a link type", async () => {
    element.value = {
      type: "link" as any,
      thumbnail_url: "https://example.com/thumb.jpg",
      thumbnail_width: 200,
      thumbnail_height: 100,
    };
    await element.updateComplete;

    const img = element.shadowRoot!.querySelector("img.oembed-media");
    expect(img).to.not.be.null;
  });

  it("renders a provider's lone iframe directly, sandboxed, with its own origin", async () => {
    element.value = {
      type: "video" as any,
      html: '<iframe src="https://example.com/embed" allow="autoplay" allowfullscreen title="Clip"></iframe>',
      width: 16,
      height: 9,
    };
    await element.updateComplete;

    const iframe = element.shadowRoot!.querySelector(
      "iframe.oembed-media",
    ) as HTMLIFrameElement;
    expect(iframe).to.not.be.null;
    expect(iframe.getAttribute("src")).to.equal("https://example.com/embed");
    expect(iframe.hasAttribute("srcdoc")).to.be.false;
    expect(iframe.getAttribute("sandbox")).to.contain("allow-scripts");
    expect(iframe.getAttribute("sandbox")).to.contain("allow-same-origin");
    expect(iframe.getAttribute("allow")).to.equal("autoplay");
    expect(iframe.hasAttribute("allowfullscreen")).to.be.true;
    expect(iframe.getAttribute("title")).to.equal("Clip");
  });

  it("never gives a provider's iframe more sandbox permissions than it asked for", async () => {
    const sandboxFor = async (markup: string) => {
      element.value = { type: "video" as any, html: markup, width: 16, height: 9 };
      await element.updateComplete;
      return element.shadowRoot!.querySelector("iframe")!.getAttribute("sandbox");
    };

    expect(
      await sandboxFor('<iframe src="https://example.com/embed" sandbox="allow-scripts"></iframe>'),
    ).to.equal("allow-scripts");
    expect(
      await sandboxFor('<iframe src="https://example.com/embed" sandbox></iframe>'),
    ).to.equal("");
  });

  it("uses an inline pixel height from the provider iframe's style", async () => {
    element.value = {
      type: "rich" as any,
      html: '<iframe src="https://example.com/embed" style="height:320px"></iframe>',
    };
    await element.updateComplete;

    expect(element.shadowRoot!.querySelector("iframe")!.style.height).to.equal(
      "320px",
    );
  });

  it("replaces the iframe when its effective sandbox changes, so a stricter sandbox takes effect", async () => {
    const markup = (sandbox: string) =>
      `<iframe src="https://example.com/embed" sandbox="${sandbox}"></iframe>`;

    element.value = { type: "video" as any, html: markup("allow-scripts allow-popups"), width: 16, height: 9 };
    await element.updateComplete;
    const before = element.shadowRoot!.querySelector("iframe")!;

    element.value = { type: "video" as any, html: markup("allow-scripts"), width: 16, height: 9 };
    await element.updateComplete;
    const after = element.shadowRoot!.querySelector("iframe")!;

    expect(after).to.not.equal(before);
    expect(after.getAttribute("sandbox")).to.equal("allow-scripts");
  });

  it("passes on the provider iframe's referrer policy", async () => {
    element.value = {
      type: "video" as any,
      html: '<iframe src="https://example.com/embed" referrerpolicy="strict-origin-when-cross-origin"></iframe>',
      width: 16,
      height: 9,
    };
    await element.updateComplete;

    expect(
      element.shadowRoot!.querySelector("iframe")!.getAttribute("referrerpolicy"),
    ).to.equal("strict-origin-when-cross-origin");
  });

  it("renders an iframe that has both src and srcdoc through the srcdoc path", async () => {
    element.value = {
      type: "video" as any,
      html: '<iframe src="https://example.com/embed" srcdoc="<p>inline</p>"></iframe>',
      width: 16,
      height: 9,
    };
    await element.updateComplete;

    const iframe = element.shadowRoot!.querySelector("iframe")!;
    expect(iframe.hasAttribute("src")).to.be.false;
    expect(iframe.hasAttribute("srcdoc")).to.be.true;
  });

  it("renders other markup in a sandboxed srcdoc iframe", async () => {
    element.value = {
      type: "video" as any,
      html: '<blockquote class="embed">x</blockquote><script async src="https://example.com/widgets.js"></script>',
      width: 16,
      height: 9,
    };
    await element.updateComplete;

    const iframe = element.shadowRoot!.querySelector(
      "iframe.oembed-media",
    ) as HTMLIFrameElement;
    expect(iframe.getAttribute("sandbox")).to.equal(
      "allow-scripts allow-presentation",
    );
    expect(iframe.getAttribute("sandbox")).to.not.contain("allow-same-origin");
    expect(iframe.getAttribute("srcdoc")).to.contain("widgets.js");
    expect(iframe.hasAttribute("src")).to.be.false;
  });

  it("runs provider scripts without granting access to the backoffice", async () => {
    const token = "oembed-preview-isolated-script";
    const message = new Promise<{ token: string; canReadParent: boolean }>(
      (resolve, reject) => {
        const onMessage = (event: MessageEvent) => {
          if (event.data?.token !== token) return;
          window.removeEventListener("message", onMessage);
          resolve(event.data);
        };
        window.addEventListener("message", onMessage);
        window.setTimeout(() => {
          window.removeEventListener("message", onMessage);
          reject(new Error("Provider script did not run"));
        }, 2000);
      },
    );

    element.value = {
      type: "rich" as any,
      html: `<script>let canReadParent = false; try { parent.document.body; canReadParent = true; } catch {} parent.postMessage({ token: "${token}", canReadParent }, "*");</script>`,
    };
    await element.updateComplete;

    const result = await message;
    expect(result.canReadParent).to.be.false;
  });

  it("sizes an opaque-origin provider preview from its height message", async () => {
    let iframe: HTMLIFrameElement | undefined;
    const heightMessage = new Promise<boolean>((resolve, reject) => {
      const onMessage = (event: MessageEvent) => {
        if (
          event.data?.type !== "novicell-oembed-height" ||
          event.data.height !== 800
        ) {
          return;
        }
        window.removeEventListener("message", onMessage);
        resolve(event.source === iframe?.contentWindow);
      };
      window.addEventListener("message", onMessage);
      window.setTimeout(() => {
        window.removeEventListener("message", onMessage);
        reject(new Error("Provider height message was not received"));
      }, 2000);
    });
    element.value = {
      type: "rich" as any,
      html: '<script>parent.postMessage({ type: "novicell-oembed-height", height: 800 }, "*");</script>',
    };
    await element.updateComplete;

    iframe = element.shadowRoot!.querySelector(
      "iframe.oembed-media--auto",
    ) as HTMLIFrameElement;
    expect(await heightMessage).to.be.true;
    await new Promise<void>((resolve, reject) => {
      const deadline = Date.now() + 2000;
      const checkHeight = () => {
        if (iframe.style.height === "600px") return resolve();
        if (Date.now() >= deadline) {
          return reject(new Error("Provider height message was not applied"));
        }
        window.setTimeout(checkHeight, 10);
      };
      checkHeight();
    });
    expect(iframe.getAttribute("sandbox")).to.not.contain("allow-same-origin");
    expect(iframe.style.height).to.equal("600px");
  });

  it("does not render an iframe that points back at the backoffice directly", async () => {
    element.value = {
      type: "video" as any,
      html: `<iframe src="${window.location.origin}/some/page"></iframe>`,
      width: 16,
      height: 9,
    };
    await element.updateComplete;

    const iframe = element.shadowRoot!.querySelector(
      "iframe.oembed-media",
    ) as HTMLIFrameElement;
    expect(iframe.hasAttribute("src")).to.be.false;
    expect(iframe.hasAttribute("srcdoc")).to.be.true;
  });

  it("sizes a dimensionless provider iframe from its own height attribute, capped", async () => {
    element.value = {
      type: "rich" as any,
      html: '<iframe src="https://example.com/embed" height="400"></iframe>',
    };
    await element.updateComplete;
    const frame = () =>
      element.shadowRoot!.querySelector("iframe") as HTMLIFrameElement;
    expect(frame().style.height).to.equal("400px");

    element.value = {
      type: "rich" as any,
      html: '<iframe src="https://example.com/embed" height="5000"></iframe>',
    };
    await element.updateComplete;
    expect(frame().style.height).to.equal("600px");
  });

  it("normalizes mixed-case types from the Management API (e.g. 'Photo')", async () => {
    element.value = {
      type: "Photo" as any,
      url: "https://example.com/photo.jpg",
    };
    await element.updateComplete;

    expect(element.shadowRoot!.querySelector("img.oembed-media")).to.not.be
      .null;
  });

  it("falls back to the thumbnail card when a rich/video type has no html", async () => {
    element.value = {
      type: "rich" as any,
      thumbnail_url: "https://example.com/thumb.jpg",
    };
    await element.updateComplete;

    expect(element.shadowRoot!.querySelector("iframe")).to.be.null;
    expect(element.shadowRoot!.querySelector("img.oembed-media")).to.not.be
      .null;
  });

  describe("saved values (not live)", () => {
    // A saved value comes from the client and is shown to every editor who opens the content,
    // so its markup must never run in the backoffice.
    let saved: OEmbedPreviewElement;

    beforeEach(() => {
      saved = document.createElement("oembed-preview") as OEmbedPreviewElement;
      document.body.appendChild(saved);
    });

    afterEach(() => {
      saved.remove();
      delete (window as any).__oembedSaved;
    });

    it("is not live by default", () => {
      expect(saved.live).to.be.false;
    });

    it("renders no iframe for script-based markup, only a card", async () => {
      saved.value = {
        type: "rich" as any,
        html: '<img src="x-does-not-exist.png" onerror="window.__oembedSaved = true"><script>window.__oembedSaved = true</script>',
        title: "A tweet",
        url: "https://example.com/tweet",
      };
      await saved.updateComplete;
      await new Promise((resolve) => setTimeout(resolve, 300));

      expect(saved.shadowRoot!.querySelector("iframe")).to.be.null;
      expect(saved.shadowRoot!.querySelector(".oembed-link-card")).to.not.be.null;
      expect((window as any).__oembedSaved).to.be.undefined;
    });

    it("falls back to the saved source URL as plain text when the response has no title or URL", async () => {
      saved.sourceUrl = "https://example.com/saved-tweet";
      saved.value = {
        type: "rich" as any,
        html: "<blockquote>x</blockquote>",
      };
      await saved.updateComplete;

      const card = saved.shadowRoot!.querySelector(".oembed-link-card")!;
      expect(card.querySelector(".oembed-link-title")!.textContent).to.equal(
        "https://example.com/saved-tweet",
      );
      expect(card.querySelector(".oembed-link-host")!.textContent).to.equal(
        "example.com",
      );
      expect(saved.shadowRoot!.querySelector("a")).to.be.null;
    });

    it("prefers the response's own title and URL over the saved source URL", async () => {
      saved.sourceUrl = "https://example.com/saved";
      saved.value = {
        type: "rich" as any,
        html: "<blockquote>x</blockquote>",
        title: "A real title",
      };
      await saved.updateComplete;

      expect(
        saved.shadowRoot!.querySelector(".oembed-link-title")!.textContent,
      ).to.equal("A real title");
    });

    it("renders no iframe even for a lone provider iframe", async () => {
      saved.value = {
        type: "video" as any,
        html: '<iframe src="https://example.com/embed"></iframe>',
        thumbnail_url: "https://example.com/thumb.jpg",
        thumbnail_width: 200,
        thumbnail_height: 100,
      };
      await saved.updateComplete;

      expect(saved.shadowRoot!.querySelector("iframe")).to.be.null;
      expect(saved.shadowRoot!.querySelector("img.oembed-media")).to.not.be.null;
    });
  });

  describe("untrusted provider markup", () => {
    afterEach(() => {
      delete (window as any).__oembedXss;
    });

    it("does not run event handlers from the markup in the backoffice document", async () => {
      element.value = {
        type: "video" as any,
        html: '<img src="x-does-not-exist.png" onerror="window.__oembedXss = true">',
        width: 16,
        height: 9,
      };
      await element.updateComplete;
      // The failed image load fires asynchronously.
      await new Promise((resolve) => setTimeout(resolve, 300));

      expect((window as any).__oembedXss).to.be.undefined;
    });

    it("still passes the markup to the iframe unchanged", async () => {
      element.value = {
        type: "video" as any,
        html: '<img src="x-does-not-exist.png" onerror="window.__oembedXss = true">',
        width: 16,
        height: 9,
      };
      await element.updateComplete;

      const srcdoc = element
        .shadowRoot!.querySelector("iframe")!
        .getAttribute("srcdoc");
      expect(srcdoc).to.contain('onerror="window.__oembedXss = true"');
    });
  });
});
