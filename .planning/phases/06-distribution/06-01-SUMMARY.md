---
phase: 06-distribution
plan: 01
subsystem: infra
tags: [nuget, multi-targeting, packaging, net8, net10]

# Dependency graph
requires:
  - phase: 05-devtools
    provides: DevTools package requiring NuGet packaging
  - phase: 01-foundation
    provides: Project structure with Directory.Build.props
provides:
  - Multi-targeting for net8.0 and net10.0
  - NuGet package metadata configuration
  - Version 0.1.0 for both packages
  - Version-locked dependency pattern for DevTools
affects: [06-distribution (remaining plans)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Multi-target packaging via TargetFrameworks
    - Centralized NuGet metadata in Directory.Build.props
    - Conditional PackageReference for version-locked dependency

key-files:
  created: []
  modified:
    - Directory.Build.props
    - src/Bustand/Bustand.csproj
    - src/Bustand.DevTools/Bustand.DevTools.csproj
    - tests/Bustand.Tests/Bustand.Tests.csproj

key-decisions:
  - "Suppress CS1591/CS0419/CS1574 for now (missing XML docs)"
  - "Test project targets net10.0 to avoid static web asset conflicts"
  - "MSBuild target removes duplicate WebAssembly.Authentication assets"
  - "DevTools uses ProjectReference in Debug, PackageReference in Release"

patterns-established:
  - "Version-locked dependency: [$(Version)] exact match pattern"
  - "Multi-target library: TargetFrameworks with FrameworkReference"

# Metrics
duration: 8min
completed: 2026-01-25
---

# Phase 6 Plan 1: NuGet Package Configuration Summary

**Multi-targeted NuGet packages (net8.0/net10.0) with centralized metadata, version 0.1.0, and version-locked DevTools dependency**

## Performance

- **Duration:** 8 min
- **Started:** 2026-01-25T09:07:45Z
- **Completed:** 2026-01-25T09:15:20Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments
- Both Bustand and Bustand.DevTools build for net8.0 and net10.0
- NuGet packages created with complete metadata (description, tags, license, repository URL)
- Version 0.1.0 set in Directory.Build.props for centralized version management
- DevTools has version-locked dependency on Bustand core using [$(Version)] pattern

## Task Commits

Each task was committed atomically:

1. **Task 1: Update Directory.Build.props with centralized NuGet metadata** - `bfa9793` (chore)
2. **Task 2: Update Bustand.csproj with package-specific metadata** - `beb0780` (feat)
3. **Task 3: Update Bustand.DevTools.csproj with package-specific metadata and version lock** - `42b79fe` (feat)
4. **Fix: Update test project for multi-targeted dependencies** - `678e6e7` (fix)

## Files Created/Modified
- `Directory.Build.props` - Added TargetFrameworks (net8.0;net10.0), Version (0.1.0), NuGet metadata, symbol config
- `src/Bustand/Bustand.csproj` - Added PackageId, Description, PackageTags, README/icon references
- `src/Bustand.DevTools/Bustand.DevTools.csproj` - Added package metadata, version-locked dependency pattern
- `tests/Bustand.Tests/Bustand.Tests.csproj` - Fixed for multi-target compatibility

## Decisions Made
- **Suppress XML doc warnings (CS1591/CS0419/CS1574):** XML documentation generation is enabled but warnings suppressed until full docs are added. This allows GenerateDocumentationFile without blocking the build.
- **Test project on net10.0:** Static web asset conflicts from multi-targeted libraries are resolved by targeting net10.0 for tests. This is simpler than removing assets manually.
- **MSBuild workaround for duplicate assets:** Added target to remove conflicting Microsoft.AspNetCore.Components.WebAssembly.Authentication static web assets that appear from both framework versions.
- **xunit.runner.visualstudio 2.8.2:** Downgraded from 3.1.4 which requires xunit v3. Version 2.8.2 is compatible with xunit 2.9.3.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] XML documentation warnings became errors**
- **Found during:** Task 1 (Build verification)
- **Issue:** TreatWarningsAsErrors + GenerateDocumentationFile caused CS1591 errors for missing XML docs
- **Fix:** Added NoWarn for CS1591, CS0419, CS1574 in Directory.Build.props
- **Files modified:** Directory.Build.props
- **Verification:** Build succeeds
- **Committed in:** bfa9793 (Task 1 commit)

**2. [Rule 3 - Blocking] Static web asset conflicts from multi-targeted libraries**
- **Found during:** Task 1 (Build verification)
- **Issue:** Test project referencing multi-targeted libraries got duplicate static web assets from both net8.0 and net10.0 versions of Microsoft.AspNetCore.Components.WebAssembly.Authentication
- **Fix:** Added MSBuild target to remove conflicting assets, changed test target to net10.0
- **Files modified:** tests/Bustand.Tests/Bustand.Tests.csproj
- **Verification:** Build succeeds with 0 errors
- **Committed in:** 678e6e7 (Test fix commit)

**3. [Rule 3 - Blocking] xunit.runner.visualstudio v3 incompatible with xunit v2**
- **Found during:** Task verification (Test run)
- **Issue:** Tests not discovered because xunit.runner.visualstudio 3.1.4 only works with xunit v3
- **Fix:** Downgraded to xunit.runner.visualstudio 2.8.2
- **Files modified:** tests/Bustand.Tests/Bustand.Tests.csproj
- **Verification:** 201 tests discovered and pass
- **Committed in:** 678e6e7 (Test fix commit)

---

**Total deviations:** 3 auto-fixed (all Rule 3 - Blocking)
**Impact on plan:** All auto-fixes necessary for build to succeed. No scope creep.

## Issues Encountered
- DevTools cannot pack in Release mode without Bustand published to NuGet first (expected behavior for version-locked pattern)
- Test output suppressed in some terminal environments (exit code 0 confirms tests pass)

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Package configuration complete, ready for README and icon creation (Plan 02)
- Packages can be created locally with `dotnet pack`
- Release builds of DevTools require Bustand to be published first or local NuGet feed

---
*Phase: 06-distribution*
*Completed: 2026-01-25*
