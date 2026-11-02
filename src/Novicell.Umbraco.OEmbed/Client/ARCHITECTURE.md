# Novicell.Umbraco.OEmbed — Frontend Architecture

TypeScript/Lit web components for the Umbraco backoffice, following Umbraco.Web.UI.Client patterns.

---

## 1. Project Structure

```
Client/
├── src/
│   ├── api/                           # Generated OpenAPI client (hey-api), do not edit
│   ├── lang/                          # en.ts, da.ts and their manifests
│   ├── property-editors/
│   │   ├── oembed-picker/             # Property editor UI
│   │   │   ├── property-editor-ui-oembed-picker.element.ts
│   │   │   ├── manifests.ts
│   │   │   └── index.ts
│   │   ├── manifests.ts
│   │   └── types.ts                   # Shared value types (OEmbedValueModel)
│   ├── oembed/
│   │   ├── components/
│   │   │   ├── input-oembed/          # Picker input (list of embeds)
│   │   │   │   ├── input-oembed.element.ts
│   │   │   │   └── index.ts
│   │   │   └── oembed-preview/        # Preview rendering component
│   │   │       ├── oembed-preview.element.ts
│   │   │       └── index.ts
│   │   ├── modals/
│   │   │   ├── oembed-picker/         # Picker modal + context
│   │   │   │   ├── oembed-picker-modal.element.ts
│   │   │   │   ├── oembed-picker-modal.token.ts
│   │   │   │   ├── oembed-picker.context.ts
│   │   │   │   ├── oembed-picker.context.token.ts
│   │   │   │   ├── oembed-picker.errors.ts
│   │   │   │   ├── oembed-picker.store.ts
│   │   │   │   ├── manifests.ts
│   │   │   │   └── index.ts
│   │   │   └── manifests.ts
│   │   ├── repository/                # Data access layer
│   │   │   ├── oembed/
│   │   │   │   ├── oembed.repository.ts
│   │   │   │   ├── oembed.server.data-source.ts
│   │   │   │   ├── constants.ts
│   │   │   │   ├── manifests.ts
│   │   │   │   └── index.ts
│   │   │   └── manifests.ts
│   │   ├── components/index.ts
│   │   └── manifests.ts
│   ├── app.ts                         # OAuth/API token configuration
│   └── manifests.ts                   # Extension manifest aggregator
├── mocks/                             # Test data (data.ts) and MSW handlers
├── public/
│   └── umbraco-package.json           # Extension manifest
├── scripts/
│   └── generate-openapi.js            # Regenerates src/api from a running site
├── package.json
├── tsconfig.json
├── vite.config.ts                     # Builds to ../wwwroot/App_Plugins/NovicellUmbracoOEmbed
└── web-test-runner.config.mjs
```

---

## 2. Architecture Patterns

### Base Class Hierarchy

All components extend **`UmbLitElement`** (not `LitElement` directly):

```typescript
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

@customElement("input-oembed")
export class InputOEmbedElement extends UmbLitElement {
    // Built-in lifecycle, observe(), consumeContext(), etc.
}
```

**Benefits**:

- Automatic lifecycle management for observables
- `observe(observable, callback)` — auto-unsubscribes on disconnect
- `consumeContext(token, callback)` — subscribe to ancestor-provided contexts
- Consistent with Umbraco backoffice patterns

### State Management

Uses **`UmbStringState`**, **`UmbObjectState`** instead of Lit's `@state()` decorator.

**Example** (oembed-picker.store.ts):

```typescript
import {
    UmbStringState,
    UmbObjectState,
} from "@umbraco-cms/backoffice/observable-api";

class OEmbedPickerStore {
    #urlState = new UmbStringState<string>("");
    #previewState = new UmbObjectState<OEmbedResponse | undefined>(
        undefined,
    );

    // Public observables
    readonly url$ = this.#urlState.asObservable();
    readonly preview$ = this.#previewState.asObservable();

    setUrl(url: string): void {
        this.#urlState.setValue(url);
    }

    setPreview(data: OEmbedResponse): void {
        this.#previewState.setValue(data);
    }
}
```

**Why Umbraco states?**

- Automatic deduplication (same value = no re-emit)
- Deep equality for objects (`UmbObjectState`)
- `asObservablePart()` for projections with memoization
- Batch updates via `mute()`/`unmute()`

### Data Flow

Strict unidirectional flow:

```
Component
  ↓ observe()
Context (UmbControllerBase)
  ↓ uses
Repository (thin orchestrator)
  ↓ delegates to
DataSource (API calls via tryExecute)
  ↓ calls
Generated API Client (NovicellOEmbedService)
  ↓
Server (/umbraco/novicell-oembed/api/v1/oembed)
```

**Benefits**:

- Testable: mock at the DataSource layer
- Reusable: repositories can be injected into multiple components
- Type-safe: generated API client with full types

### Context API for Communication

**Define context** (oembed-picker.context.token.ts):

```typescript
import { createContext } from "@umbraco-cms/backoffice/context-api";
import type { UmbOEmbedPickerContext } from "./oembed-picker.context";

export const UMB_OEMBED_PICKER_CONTEXT = createContext<UmbOEmbedPickerContext>(
    "UmbOEmbedPickerContext",
);
```

**Provide context** (oembed-picker-modal.element.ts):

```typescript
export class OembedPickerModalElement extends UmbModalBaseElement {
    #pickerContext = new UmbOEmbedPickerContext(this);

    constructor() {
        super();
        this.provideContext(UMB_OEMBED_PICKER_CONTEXT, this.#pickerContext);
    }
}
```

**Consume context** (child component):

```typescript
export class InputOEmbedElement extends UmbLitElement {
    #pickerContext?: UmbOEmbedPickerContext;

    constructor() {
        super();
        this.consumeContext(UMB_OEMBED_PICKER_CONTEXT, (context) => {
            this.#pickerContext = context;
            // Now you can call context methods and observe context state
        });
    }
}
```

### Error Handling with `tryExecute`

All API calls use `tryExecute()` wrapper — never throws, always returns `{ data, error }`:

```typescript
const { data, error } = await tryExecute(
    this, // controller host (for notifications)
    NovicellOEmbedService.getOEmbed({ query: { url } }),
);

if (data) {
    store.setPreview(data);
} else {
    store.setError(error);
}
```

---

## 3. Key Components

| Component                    | Purpose                                     | Base                | State                          |
| ---------------------------- | ------------------------------------------- | ------------------- | ------------------------------ |
| **InputOEmbedElement**       | URL input + picker button                   | UmbLitElement       | Form control value             |
| **OEmbedPreviewElement**     | Render preview (image, video, iframe, link) | UmbLitElement       | @property (read-only)          |
| **OembedPickerModalElement** | Modal for entering URL and preview          | UmbModalBaseElement | UmbOEmbedPickerContext         |
| **OembedPickerModalElement** | Provides picker context                     | UmbControllerBase   | UmbStringState, UmbObjectState |

---

## 4. Testing

### Test Framework: @web/test-runner

**Config**: `web-test-runner.config.ts`

- Playwright browser automation
- Fast in-memory test runner
- Supports ESM modules
- Coverage tracking

### MSW (Mock Service Worker)

**Handlers**: `mocks/handlers.ts`

- Intercepts HTTP requests
- Returns mock responses without real API calls
- Reusable across all tests

**Usage**:

```typescript
import { setupServer } from "msw/node";
import { handlers } from "./mocks/handlers";

const server = setupServer(...handlers);

describe("MyComponent", () => {
    before(() => server.listen());
    afterEach(() => server.resetHandlers());
    after(() => server.close());

    it("should load preview", async () => {
        // Test code here
    });
});
```

### Running Tests

```bash
npm test              # Run once
npm run test:watch   # Watch mode
```

### Test Targets (70%+ Coverage)

**Priority 1** (critical):

- `OEmbedPickerStore` — state management
- `UmbOEmbedPickerContext` — context lifecycle and methods
- `OEmbedRepository` → `OEmbedServerDataSource` — API integration

**Priority 2** (high):

- `OembedPickerModalElement` — modal rendering and user interaction
- `InputOEmbedElement` — form integration and binding

**Priority 3** (standard):

- `OEmbedPreviewElement` — preview rendering for different oEmbed types

---

## 5. Adding New Features

### Add a New Component

1. **Create element** in `src/oembed/components/{name}/`
2. **Extend `UmbLitElement`** (not `LitElement`)
3. **Consume context** via `consumeContext(token, callback)` if needed
4. **Add manifest** in `{name}/manifests.ts`
5. **Write tests** in `{name}/{name}.element.test.ts`

### Add a New Store

1. **Create store** class extending Umbraco state patterns
2. **Use `UmbStringState` / `UmbObjectState`** for typed state
3. **Export observables** via `asObservable()`
4. **Write tests** for state transitions

### Refactor to Use Context

1. **Create context** (`{feature}.context.ts`) extending `UmbControllerBase`
2. **Create token** (`{feature}.context.token.ts`) via `createContext()`
3. **Provide in parent** via `this.provideContext(token, instance)`
4. **Consume in child** via `this.consumeContext(token, callback)`

---

## 6. Key Files Reference

| File                                                             | Purpose                       |
| ---------------------------------------------------------------- | ----------------------------- |
| `src/oembed/modals/oembed-picker/oembed-picker.store.ts`         | State management              |
| `src/oembed/modals/oembed-picker/oembed-picker.context.ts`       | Context logic                 |
| `src/oembed/repository/oembed/oembed.server.data-source.ts`      | API calls                     |
| `src/oembed/modals/oembed-picker/oembed-picker-modal.element.ts` | Picker UI                     |
| `mocks/handlers.ts`                                              | MSW request mocking           |
| `package.json`                                                   | Dependencies and test scripts |

---

## 7. Umbraco Patterns Used

- **UmbLitElement** — Component base class with lifecycle management
- **UmbStringState / UmbObjectState** — Reactive state with deduplication
- **Context API** — Component communication via DI tokens
- **Repository + DataSource** — Data access pattern with separation of concerns
- **tryExecute()** — Error handling for API calls (no exceptions)
- **Generated API Client** — Type-safe OpenAPI integration
- **Extension Manifests** — Registration system for backoffice UI

---

## 8. Next Steps

### Completed ✅

- State management (UmbStringState)
- Context API (UmbOEmbedPickerContext)
- Testing framework (@web/test-runner, MSW)
- Data source layer (existing)

### Future Enhancements

- Increase test coverage to 70%+ (currently at ~40%)
- Add repository tests
- Add component integration tests
- Consider adding preview caching
- Add support for custom provider plugins

---

**This frontend implementation follows all Umbraco.Web.UI.Client conventions and is ready for production.**
