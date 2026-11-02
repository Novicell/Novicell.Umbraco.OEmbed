import { expect } from "chai";
import "./input-oembed.element.ts";
import type { InputOEmbedElement } from "./input-oembed.element.ts";
import type { OEmbedValueModel } from "../../../property-editors/types.ts";

const video = (url: string): OEmbedValueModel => ({
  url,
  oembed: { type: "video", width: 16, height: 9 } as any,
});

/**
 * Tests for InputOEmbedElement.
 * Verifies the empty-state add-button sizing, that populated cards keep their
 * own aspect ratio, and multi-value add/remove/max behavior.
 */
describe("InputOEmbedElement", () => {
  let element: InputOEmbedElement;

  beforeEach(() => {
    element = document.createElement("input-oembed") as InputOEmbedElement;
    document.body.appendChild(element);
  });

  afterEach(() => {
    element.remove();
  });

  it("sizes the empty-state add-button like Umbraco's own placeholder grid (auto-fill/minmax/1fr)", async () => {
    // Mirrors Umbraco's input-rich-media/input-media .container exactly, so the
    // add-button's width matches theirs pixel-for-pixel at any container width.
    element.style.display = "block";
    element.style.width = "400px";
    element.style.setProperty("--umb-card-medium-min-width", "160px");
    element.style.setProperty("--uui-size-space-5", "12px");
    await element.updateComplete;

    const button = element.shadowRoot!.querySelector("#btn-add");
    expect(button).to.not.be.null;
    expect(element.shadowRoot!.querySelector("uui-card-media")).to.be.null;

    // 400px container, minmax(160px, 1fr), 12px gap -> 2 columns of 194px each
    // (same auto-fill/minmax math Umbraco's Media Picker/Rich Media use).
    const style = getComputedStyle(button!);
    expect(style.height).to.equal("160px");
    expect(style.width).to.equal("194px");
  });

  it("wraps a legacy single-object value (pre-multi-value content) in an array", async () => {
    // Umbraco hands the parsed stored JSON straight to the property editor UI, so
    // content saved before multi-value support still arrives as a single object.
    element.value = video("https://example.com/legacy") as any;
    await element.updateComplete;

    expect(element.value).to.deep.equal([video("https://example.com/legacy")]);
    expect(element.shadowRoot!.querySelectorAll("uui-card-media")).to.have.length(1);
  });

  it("renders a wide 16:9 card for a video embed", async () => {
    element.value = [video("https://example.com/video")];
    await element.updateComplete;

    const card = element.shadowRoot!.querySelector("uui-card-media");
    expect(card).to.not.be.null;
    expect(card!.getAttribute("style")).to.contain("aspect-ratio: 16 / 9");
  });

  it("renders a square card for a photo embed", async () => {
    element.value = [
      { url: "https://example.com/photo", oembed: { type: "photo", width: 1, height: 1 } as any },
    ];
    await element.updateComplete;

    const card = element.shadowRoot!.querySelector("uui-card-media");
    expect(card!.getAttribute("style")).to.contain("aspect-ratio: 1 / 1");
  });

  it("renders a proportional card for a non-standard ratio (e.g. rich/tweet)", async () => {
    element.value = [
      { url: "https://example.com/tweet", oembed: { type: "rich", width: 2, height: 3 } as any },
    ];
    await element.updateComplete;

    const card = element.shadowRoot!.querySelector("uui-card-media");
    // aspect-ratio is only applied inline when between 1 and 2 (see #renderOEmbed);
    // outside that range the card falls back to no inline aspect-ratio and relies
    // on its fixed height + auto width, so it must not be forced to a fixed width.
    expect(card!.getAttribute("style")).to.equal("");
  });

  it("renders multiple cards plus an add-button alongside them", async () => {
    element.multiple = true;
    element.value = [video("https://example.com/1"), video("https://example.com/2")];
    await element.updateComplete;

    expect(element.shadowRoot!.querySelectorAll("uui-card-media")).to.have.length(2);
    expect(element.shadowRoot!.querySelector("#btn-add")).to.not.be.null;
  });

  it("hides the add-button once the item count reaches max", async () => {
    element.multiple = true;
    element.max = 2;
    element.value = [video("https://example.com/1"), video("https://example.com/2")];
    await element.updateComplete;

    expect(element.shadowRoot!.querySelectorAll("uui-card-media")).to.have.length(2);
    expect(element.shadowRoot!.querySelector("#btn-add")).to.be.null;
  });

  it("hides the add-button once one item exists when multiple is false", async () => {
    element.value = [video("https://example.com/1")];
    await element.updateComplete;

    expect(element.shadowRoot!.querySelector("#btn-add")).to.be.null;
  });

  it("fails valueMissing validation when required and empty", async () => {
    element.required = true;
    element.value = [];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.false;
  });

  it("passes valueMissing validation when required and populated", async () => {
    element.required = true;
    element.value = [video("https://example.com/1")];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.true;
  });

  it("fails rangeUnderflow validation when below min", async () => {
    element.min = 2;
    element.value = [video("https://example.com/1")];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.false;
  });

  it("fails rangeOverflow validation when above max", async () => {
    element.max = 1;
    element.value = [video("https://example.com/1"), video("https://example.com/2")];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.false;
  });

  it("accepts and exposes an allowedTypes restriction", async () => {
    element.allowedTypes = ["photo", "video"];
    await element.updateComplete;

    expect(element.allowedTypes).to.deep.equal(["photo", "video"]);
  });

  it("marks only the disallowed card's uui-card-media with the error state", async () => {
    element.allowedTypes = ["photo"];
    element.value = [
      video("https://example.com/video"), // not allowed
      { url: "https://example.com/photo", oembed: { type: "photo", width: 1, height: 1 } as any }, // allowed
    ];
    await element.updateComplete;

    const cards = element.shadowRoot!.querySelectorAll("uui-card-media");
    expect(cards).to.have.length(2);
    expect((cards[0] as HTMLElement).hasAttribute("error")).to.be.true;
    expect((cards[1] as HTMLElement).hasAttribute("error")).to.be.false;
  });

  it("shows a saved value without a thumbnail in a preview that never runs its markup", async () => {
    element.value = [
      {
        url: "https://example.com/tweet",
        oembed: {
          type: "rich",
          html: '<blockquote>x</blockquote><script>window.__oembedCard = true</script>',
        } as any,
      },
    ];
    await element.updateComplete;

    const preview = element.shadowRoot!.querySelector(
      "oembed-preview",
    ) as any;
    expect(preview).to.not.be.null;
    expect(preview.live).to.be.false;
    await preview.updateComplete;
    expect(preview.shadowRoot.querySelector("iframe")).to.be.null;
    // The saved source URL keeps otherwise empty cards telling each other apart.
    expect(
      preview.shadowRoot.querySelector(".oembed-link-title").textContent,
    ).to.equal("https://example.com/tweet");
  });

  it("passes type validation regardless of the casing of the allowed types", async () => {
    element.allowedTypes = ["Video"];
    element.value = [video("https://example.com/1")];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.true;
  });

  it("fails type validation when an item's type is not allowed", async () => {
    element.allowedTypes = ["photo"];
    element.value = [video("https://example.com/1")];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.false;
  });

  it("fails validation when more than one item exists and multiple is false", async () => {
    element.multiple = false;
    element.value = [video("https://example.com/1"), video("https://example.com/2")];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.false;
  });

  it("passes validation with more than one item when multiple is true", async () => {
    element.multiple = true;
    element.value = [video("https://example.com/1"), video("https://example.com/2")];
    await element.updateComplete;

    expect(element.checkValidity()).to.be.true;
  });
});
