---
phase: 06-distribution
plan: 03
subsystem: samples
tags: [blazor, sample-app, server, wasm, auto-render, multi-tfm]

# Dependency graph
requires:
  - phase: 06-01
    provides: NuGet packaging configuration for Bustand and DevTools
provides:
  - Two-project sample app structure (Server + Client)
  - All three Blazor render modes configured (Server, WASM, Auto)
  - Navigation structure for demo pages
  - Basic layout and styling
affects: [06-04, 06-05, 06-06, 06-07, 06-09]

# Tech tracking
tech-stack:
  added: [Microsoft.AspNetCore.Components.WebAssembly.Server]
  patterns: [Blazor Web App with Auto render mode, samples/Directory.Build.props for TFM isolation]

key-files:
  created:
    - samples/Bustand.Sample/Bustand.Sample.csproj
    - samples/Bustand.Sample/Program.cs
    - samples/Bustand.Sample/Components/App.razor
    - samples/Bustand.Sample/Components/Routes.razor
    - samples/Bustand.Sample/Components/_Imports.razor
    - samples/Bustand.Sample/Components/Layout/MainLayout.razor
    - samples/Bustand.Sample/Components/Layout/NavMenu.razor
    - samples/Bustand.Sample/wwwroot/app.css
    - samples/Bustand.Sample.Client/Bustand.Sample.Client.csproj
    - samples/Bustand.Sample.Client/Program.cs
    - samples/Bustand.Sample.Client/_Imports.razor
    - samples/Directory.Build.props
  modified:
    - Bustand.sln
    - src/Bustand/Bustand.csproj
    - src/Bustand.DevTools/Bustand.DevTools.csproj

key-decisions:
  - "PrivateAssets=all on FrameworkReference prevents WASM consumer build failures"
  - "samples/Directory.Build.props isolates samples to net10.0 TFM"
  - "DevTools needs explicit FrameworkReference due to PrivateAssets propagation block"
  - "Server project needs WebAssembly.Server package for Auto render mode"

patterns-established:
  - "Multi-TFM library with WASM consumer requires PrivateAssets=all on FrameworkReference"
  - "Sample projects use isolated Directory.Build.props to avoid multi-TFM resolution issues"

# Metrics
duration: 10min
completed: 2026-01-25
---

# Phase 6 Plan 3: Sample App Structure Summary

**Blazor Web App sample with Server + Client projects supporting Server, WASM, and Auto render modes using isolated net10.0 targeting**

## Performance

- **Duration:** 10 min
- **Started:** 2026-01-25T09:18:16Z
- **Completed:** 2026-01-25T09:28:16Z
- **Tasks:** 3
- **Files created:** 12
- **Files modified:** 3

## Accomplishments

- Created two-project Blazor Web App structure (Bustand.Sample + Bustand.Sample.Client)
- Configured all three Blazor render modes (Server, WASM, Auto)
- Added navigation structure for all planned demo pages (Counter variants, TodoList, Shopping Cart, DevTools)
- Fixed multi-TFM library compatibility with WASM consumers
- Both sample projects added to solution and build successfully

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Server project (Bustand.Sample)** - `08d8bf7` (feat)
2. **Task 2: Create Client project (Bustand.Sample.Client)** - `9ab85a0` (feat)
3. **Task 3: Create layout components and add projects to solution** - `4e1984c` (feat)

## Files Created/Modified

**Created:**
- `samples/Bustand.Sample/Bustand.Sample.csproj` - Server project with WebAssembly.Server package
- `samples/Bustand.Sample/Program.cs` - Server entry point with all render modes
- `samples/Bustand.Sample/Components/App.razor` - Root HTML template
- `samples/Bustand.Sample/Components/Routes.razor` - Router configuration
- `samples/Bustand.Sample/Components/_Imports.razor` - Server-side usings
- `samples/Bustand.Sample/Components/Layout/MainLayout.razor` - Page layout with sidebar
- `samples/Bustand.Sample/Components/Layout/NavMenu.razor` - Navigation menu with demo links
- `samples/Bustand.Sample/wwwroot/app.css` - Basic application styling
- `samples/Bustand.Sample.Client/Bustand.Sample.Client.csproj` - Client project for WASM
- `samples/Bustand.Sample.Client/Program.cs` - Client entry point with Bustand registration
- `samples/Bustand.Sample.Client/_Imports.razor` - Client-side usings
- `samples/Directory.Build.props` - Isolated net10.0 targeting for samples

**Modified:**
- `Bustand.sln` - Added sample projects
- `src/Bustand/Bustand.csproj` - Added PrivateAssets=all on FrameworkReference
- `src/Bustand.DevTools/Bustand.DevTools.csproj` - Added explicit FrameworkReference

## Decisions Made

- **PrivateAssets=all on FrameworkReference:** Prevents FrameworkReference from propagating to WASM consumer projects, which would fail with "no runtime pack for browser-wasm"
- **samples/Directory.Build.props:** Isolates sample projects to net10.0-only targeting, preventing multi-TFM resolution issues where NuGet tries to resolve packages for all TFMs of project references
- **Explicit FrameworkReference in DevTools:** Required because PrivateAssets=all blocks transitive propagation of AspNetCore types
- **Microsoft.AspNetCore.Components.WebAssembly.Server:** Server project needs this package to host WASM components and provide AddInteractiveWebAssemblyComponents/AddInteractiveWebAssemblyRenderMode

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added PrivateAssets=all to Bustand FrameworkReference**
- **Found during:** Task 2 (Client project creation)
- **Issue:** WASM consumer builds failed with "no runtime pack for Microsoft.AspNetCore.App available for browser-wasm"
- **Fix:** Added PrivateAssets="all" to FrameworkReference in Bustand.csproj to prevent transitive propagation
- **Files modified:** src/Bustand/Bustand.csproj
- **Verification:** Client project builds successfully
- **Committed in:** 9ab85a0 (Task 2 commit)

**2. [Rule 3 - Blocking] Added samples/Directory.Build.props for TFM isolation**
- **Found during:** Task 2 (Client project creation)
- **Issue:** NuGet resolved packages against all TFMs of Bustand (net8.0 + net10.0), causing version incompatibility errors
- **Fix:** Created samples/Directory.Build.props that overrides TargetFrameworks to net10.0 only
- **Files modified:** samples/Directory.Build.props (created)
- **Verification:** Sample projects build without multi-TFM resolution errors
- **Committed in:** 9ab85a0 (Task 2 commit)

**3. [Rule 3 - Blocking] Added explicit FrameworkReference to DevTools**
- **Found during:** Task 3 (Layout components)
- **Issue:** DevTools couldn't find AspNetCore types because PrivateAssets=all on Bustand's FrameworkReference blocked propagation
- **Fix:** Added explicit FrameworkReference Include="Microsoft.AspNetCore.App" to DevTools.csproj
- **Files modified:** src/Bustand.DevTools/Bustand.DevTools.csproj
- **Verification:** Solution builds successfully
- **Committed in:** 4e1984c (Task 3 commit)

**4. [Rule 1 - Bug] Fixed Program.cs API usage**
- **Found during:** Task 1/2 (Project creation)
- **Issue:** Plan specified AddBustand(Assembly) overload which doesn't exist
- **Fix:** Used correct API: AddBustand(options => options.ScanAssemblyContaining<T>())
- **Files modified:** samples/Bustand.Sample/Program.cs, samples/Bustand.Sample.Client/Program.cs
- **Verification:** Projects compile with correct Bustand registration
- **Committed in:** 08d8bf7, 9ab85a0

---

**Total deviations:** 4 auto-fixed (3 blocking, 1 bug)
**Impact on plan:** All fixes necessary for WASM compatibility in multi-TFM library scenario. No scope creep.

## Issues Encountered

- **Multi-TFM + WASM compatibility:** The Bustand library targets net8.0 and net10.0, and the FrameworkReference to Microsoft.AspNetCore.App caused build failures when consumed by WASM projects. This required multiple coordinated changes (PrivateAssets, Directory.Build.props, explicit FrameworkReference in DevTools).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Sample app structure complete and builds successfully
- All 201 existing tests continue to pass
- Ready for 06-04 (Counter Store demo), 06-05 (Counter Pages), 06-06 (TodoList demo), 06-07 (ShoppingCart demo), 06-09 (Final polish)
- Navigation structure prepared for all demo pages

---
*Phase: 06-distribution*
*Completed: 2026-01-25*
