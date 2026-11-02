import { expect } from "chai";
import { toHttpUrl } from "./safe-url.ts";

/**
 * Provider-supplied URLs are rendered as links in the backoffice, so only http(s) is allowed.
 */
describe("toHttpUrl", () => {
  it("returns http and https URLs in normalized form", () => {
    expect(toHttpUrl("https://example.com/a?b=1")).to.equal("https://example.com/a?b=1");
    expect(toHttpUrl("http://example.com")).to.equal("http://example.com/");
    expect(toHttpUrl("HTTPS://EXAMPLE.COM/x")).to.equal("https://example.com/x");
    expect(toHttpUrl("  https://example.com/  ")).to.equal("https://example.com/");
  });

  it("rejects script-capable and other non-http schemes", () => {
    expect(toHttpUrl("javascript:alert(1)")).to.be.undefined;
    expect(toHttpUrl("JaVaScRiPt:alert(1)")).to.be.undefined;
    expect(toHttpUrl("java\nscript:alert(1)")).to.be.undefined;
    expect(toHttpUrl("data:text/html,<script>alert(1)</script>")).to.be.undefined;
    expect(toHttpUrl("vbscript:msgbox(1)")).to.be.undefined;
    expect(toHttpUrl("file:///etc/passwd")).to.be.undefined;
    expect(toHttpUrl("ftp://example.com/file")).to.be.undefined;
  });

  it("rejects relative and malformed URLs", () => {
    expect(toHttpUrl("//example.com/x")).to.be.undefined;
    expect(toHttpUrl("/just/a/path")).to.be.undefined;
    expect(toHttpUrl("not a url")).to.be.undefined;
  });

  it("returns undefined for empty values", () => {
    expect(toHttpUrl("")).to.be.undefined;
    expect(toHttpUrl(null)).to.be.undefined;
    expect(toHttpUrl(undefined)).to.be.undefined;
  });
});
