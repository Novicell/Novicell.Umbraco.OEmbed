import { expect } from "chai";
import { isOEmbedTypeAllowed } from "./allowed-types.ts";

/**
 * Mirrors the server-side OEmbedPickerPropertyValueEditor.AllowedTypeValidator, which compares types
 * case-insensitively and treats an empty list as "no restriction".
 */
describe("isOEmbedTypeAllowed", () => {
  it("allows every type when no restriction is configured", () => {
    expect(isOEmbedTypeAllowed(undefined, "video")).to.be.true;
    expect(isOEmbedTypeAllowed([], "video")).to.be.true;
  });

  it("allows a type in the list", () => {
    expect(isOEmbedTypeAllowed(["photo", "video"], "video")).to.be.true;
  });

  it("rejects a type that is not in the list", () => {
    expect(isOEmbedTypeAllowed(["photo"], "video")).to.be.false;
  });

  it("compares case-insensitively in both directions", () => {
    expect(isOEmbedTypeAllowed(["Video"], "video")).to.be.true;
    expect(isOEmbedTypeAllowed(["video"], "VIDEO")).to.be.true;
  });

  it("does not block when the type is missing", () => {
    expect(isOEmbedTypeAllowed(["photo"], undefined)).to.be.true;
    expect(isOEmbedTypeAllowed(["photo"], null)).to.be.true;
  });
});
