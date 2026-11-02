import { expect } from "chai";
import sinon from "sinon";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbOEmbedPickerContext } from "./oembed-picker.context.ts";
import { OEmbedRepository } from "../../repository/oembed/oembed.repository.ts";
import type { OEmbedResponse } from "../../../api";

class OEmbedPickerContextTestHost extends UmbLitElement {}
customElements.define("oembed-picker-context-test-host", OEmbedPickerContextTestHost);

const deferred = <T>() => {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((r) => (resolve = r));
  return { promise, resolve };
};

const response = (title: string): OEmbedResponse => ({ type: "video", title });

/**
 * Tests for UmbOEmbedPickerContext.fetchPreview().
 * The modal saves the preview together with the URL currently in the store, so a response
 * for an earlier URL must never end up as the preview of the current one.
 */
describe("UmbOEmbedPickerContext", () => {
  let host: OEmbedPickerContextTestHost;
  let context: UmbOEmbedPickerContext;
  let requestOEmbed: sinon.SinonStub;
  let preview: OEmbedResponse | undefined;
  let loading: string;

  beforeEach(() => {
    requestOEmbed = sinon.stub(OEmbedRepository.prototype, "requestOEmbed");
    host = document.createElement("oembed-picker-context-test-host") as OEmbedPickerContextTestHost;
    document.body.appendChild(host);
    context = new UmbOEmbedPickerContext(host);
    preview = undefined;
    loading = "idle";
    host.observe(context.preview$, (value) => (preview = value));
    host.observe(context.loading$, (value) => (loading = value));
  });

  afterEach(() => {
    sinon.restore();
    host.remove();
  });

  it("applies the response for the current URL", async () => {
    requestOEmbed.resolves({ data: response("A") });

    context.updateUrl("https://example.com/a");
    await context.fetchPreview();

    expect(preview?.title).to.equal("A");
    expect(loading).to.equal("success");
  });

  it("ignores a response that arrives after the URL was changed", async () => {
    const first = deferred<{ data: OEmbedResponse }>();
    requestOEmbed.onFirstCall().returns(first.promise);

    context.updateUrl("https://example.com/a");
    const pending = context.fetchPreview();
    context.updateUrl("https://example.com/b");

    first.resolve({ data: response("A") });
    await pending;

    expect(context.getUrl()).to.equal("https://example.com/b");
    expect(preview).to.be.undefined;
    expect(loading).to.equal("idle");
  });

  it("keeps the latest preview when responses arrive out of order", async () => {
    const first = deferred<{ data: OEmbedResponse }>();
    const second = deferred<{ data: OEmbedResponse }>();
    requestOEmbed.onFirstCall().returns(first.promise);
    requestOEmbed.onSecondCall().returns(second.promise);

    context.updateUrl("https://example.com/a");
    const pendingA = context.fetchPreview();
    context.updateUrl("https://example.com/b");
    const pendingB = context.fetchPreview();

    second.resolve({ data: response("B") });
    await pendingB;
    first.resolve({ data: response("A") });
    await pendingA;

    expect(preview?.title).to.equal("B");
  });

  it("ignores a stale error so it cannot clear or replace the current state", async () => {
    const first = deferred<{ error: unknown }>();
    requestOEmbed.onFirstCall().returns(first.promise);
    requestOEmbed.onSecondCall().resolves({ data: response("B") });

    context.updateUrl("https://example.com/a");
    const pendingA = context.fetchPreview();
    context.updateUrl("https://example.com/b");
    await context.fetchPreview();
    first.resolve({ error: new Error("stale") });
    await pendingA;

    expect(preview?.title).to.equal("B");
    expect(loading).to.equal("success");
  });

  it("ignores a response that arrives after reset()", async () => {
    const first = deferred<{ data: OEmbedResponse }>();
    requestOEmbed.onFirstCall().returns(first.promise);

    context.updateUrl("https://example.com/a");
    const pending = context.fetchPreview();
    context.reset();
    first.resolve({ data: response("A") });
    await pending;

    expect(preview).to.be.undefined;
    expect(context.getUrl()).to.equal("");
  });
});
