# Phase 6: Distribution - Context

**Gathered:** 2026-01-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Package Bustand as installable NuGet packages (Bustand and Bustand.DevTools) with comprehensive documentation (wiki/GitHub Pages) and a teaching-focused sample app demonstrating all features across all rendering modes. Distribution artifacts include icon/branding, complete package metadata, and dual getting-started paths (quick start in README, detailed guide in docs).

</domain>

<decisions>
## Implementation Decisions

### Sample app scope
- **Teaching tool** with comprehensive examples and extensive comments explaining concepts
- **Three example stores**: Counter (simple), TodoList (intermediate), ShoppingCart (advanced/nested objects)
- **All rendering modes** demonstrated via separate pages (Counter-Server, Counter-WASM, Counter-Auto, etc.)
- **DevTools prominently featured**: Link in nav, explain usage in comments
- **All stores persist**: Demonstrate that persistence is standard practice
- **Built-in middleware**: Show logging and persistence in examples
- **Selectors explicitly demonstrated**: Components use selectors with explanatory comments
- **Home page with overview**: Landing page introduces each example and what it demonstrates
- **Blazor default template styling**: Focus on code, not design aesthetics
- Counter demonstrates basic state, TodoList shows list management, ShoppingCart shows nested objects

### Documentation structure
- **Primary location**: README + wiki/GitHub Pages
- **README content**: Marketing pitch (Why Bustand, key features, links to docs and samples)
- **Wiki/Pages sections** (comprehensive): Getting Started, Core Concepts, Middleware Guide, Persistence, DevTools, Advanced Topics, API Reference
- **API reference**: Auto-generated using DefaultDocumentation (PackageVersion 1.2.2)
- **No migration guides**: Stand on own merits, no comparison migration docs
- **Troubleshooting**: Comprehensive FAQ with common issues, error messages, debugging tips
- **Code examples**: Mix of focused snippets inline and complete files linked
- **Philosophy page**: Explain why Bustand takes its approach (not feature matrix comparison)

### Package organization
- **Two packages**: Bustand (core) and Bustand.DevTools (current split)
- **Versioning**: 0.x until stable (start at 0.1.0, allow breaking changes until 1.0.0)
- **Custom icon**: Design a logo/icon for NuGet package listing
- **Complete metadata**: Description, tags, project URL, license, repository, release notes
- **NuGet tags**: Both Blazor-focused (blazor, state-management, wasm, blazor-server) and Zustand-related (zustand, store, flux)
- **Symbol packages**: Embedded symbols (embed PDB in NuGet package)
- **License**: MIT (permissive, same as Zustand)
- **Version synchronization**: Both packages share same version number (0.1.0 requires 0.1.0)

### Getting started flow
- **Install + 3 code snippets**: Add package, create store, register DI, use in component
- **Dual location**: Quick start in README, detailed guide in wiki/docs
- **TodoList example**: More realistic than Counter, shows list management
- **DevTools part of setup**: Install both packages, configure DevTools from the start (not optional)

### Claude's Discretion
- Exact icon design (branding consistent with Bustand name/theme)
- Specific wording for README marketing pitch
- Detailed troubleshooting FAQ content
- Advanced Topics section structure in docs

</decisions>

<specifics>
## Specific Ideas

- DefaultDocumentation package (version 1.2.2) for API reference generation
- Philosophy page should explain "why Bustand" rather than feature matrix
- Sample app should feel tutorial-like with comments explaining every pattern
- Getting started should get from zero to working TodoList + DevTools in under 5 minutes

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 06-distribution*
*Context gathered: 2026-01-25*
