import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  build: {
    lib: {
      entry: {
        "novicell-umbraco-oembed-manifests": resolve(__dirname, "src/manifests.ts"),
        "novicell-umbraco-oembed-app": resolve(__dirname, "src/app.ts"),
      },
      formats: ["es"],
    },
    outDir: "../wwwroot/App_Plugins/NovicellUmbracoOEmbed", // your web component will be saved in this location
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
