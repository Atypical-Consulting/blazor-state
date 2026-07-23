# Phase 6: Distribution - Research

**Researched:** 2026-01-25
**Domain:** NuGet packaging, documentation generation, Blazor sample apps, test coverage
**Confidence:** HIGH

## Summary

This research covers the complete distribution requirements for Bustand: NuGet packaging with multi-targeting (.NET 8/.NET 10), API documentation generation, sample app architecture across rendering modes, and test coverage reporting. The .NET ecosystem has mature, well-documented tooling for all these needs.

The standard approach uses SDK-style project files with MSBuild properties for NuGet metadata, embedded PDBs with SourceLink for debugging, DefaultDocumentation (v1.2.2) for API reference generation, and Coverlet for test coverage collection. The sample app follows the Blazor Web App template pattern with separate pages demonstrating each rendering mode.

**Primary recommendation:** Configure NuGet packaging entirely through Directory.Build.props and project files using MSBuild properties. Use `TargetFrameworks` for multi-targeting, embedded symbols with SourceLink, and DefaultDocumentation for automated API reference generation.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.SourceLink.GitHub | 8.0.0+ | Source debugging support | Automatically embeds source control metadata; built into .NET SDK 8+ for GitHub |
| DefaultDocumentation | 1.2.2 | API reference generation | Converts XML docs to markdown; MSBuild integration; no runtime dependencies |
| coverlet.collector | 6.0.4 | Test coverage collection | Default in xUnit templates; cross-platform; integrates with dotnet test |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ReportGenerator | 5.x | Coverage report visualization | Generating HTML reports from coverage data |
| Microsoft.NET.Test.Sdk | 17.14.1 | Test platform | Already included; required for coverage collection |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| DefaultDocumentation | DocFX | DocFX is more powerful but heavier; DefaultDocumentation is simpler and sufficient for API reference |
| Embedded PDBs | Symbol packages (.snupkg) | .snupkg requires NuGet.org symbol server; embedded PDBs work with all feeds |
| README in NuGet | NuGet Description only | README provides richer documentation, significantly improves quality perception |

**Installation:**
```bash
# Already have coverlet.collector from xUnit template
# Add DefaultDocumentation for API docs generation
dotnet add package DefaultDocumentation --version 1.2.2
```

## Architecture Patterns

### Recommended Project Structure
```
Bustand/
├── src/
│   ├── Bustand/                    # Core NuGet package
│   │   ├── Bustand.csproj
│   │   └── ...
│   └── Bustand.DevTools/           # DevTools NuGet package
│       ├── Bustand.DevTools.csproj
│       └── ...
├── samples/
│   └── Bustand.Sample/             # Sample Blazor Web App
│       ├── Bustand.Sample/         # Server project
│       │   ├── Components/
│       │   │   ├── Layout/
│       │   │   └── Pages/
│       │   └── Program.cs
│       └── Bustand.Sample.Client/  # Client project (for WASM components)
│           ├── Pages/
│           └── Program.cs
├── tests/
│   └── Bustand.Tests/
├── docs/                           # Generated API documentation
├── Directory.Build.props           # Shared build properties
├── Directory.Build.targets         # Shared build targets (for DefaultDocumentation)
├── README.md                       # NuGet package README
├── NUGET-README.md                 # Alternative: NuGet-specific README
└── icon.png                        # Package icon (128x128 PNG)
```

### Pattern 1: Centralized NuGet Metadata in Directory.Build.props
**What:** Define common package metadata in Directory.Build.props to share across all packages
**When to use:** Always for multi-package solutions
**Example:**
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <!-- Existing properties -->
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- Package metadata -->
    <Authors>Philippe Matray</Authors>
    <Company>phmatray</Company>
    <Copyright>Copyright (c) Philippe Matray 2026</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/phmatray/Bustand</PackageProjectUrl>
    <RepositoryUrl>https://github.com/phmatray/Bustand.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- Symbols and debugging -->
    <DebugType>embedded</DebugType>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>

    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- Versioning -->
    <Version>0.1.0</Version>
    <PackageVersion>0.1.0</PackageVersion>
  </PropertyGroup>
</Project>
```

### Pattern 2: Package-Specific Metadata in csproj
**What:** Package-specific properties like PackageId, Description, Tags in individual csproj files
**When to use:** Properties that differ between packages
**Example:**
```xml
<!-- src/Bustand/Bustand.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <PackageId>Bustand</PackageId>
    <Description>Zustand-inspired state management for Blazor. Minimal boilerplate, immutable state with records, works across Server, WebAssembly, and Auto rendering modes.</Description>
    <PackageTags>blazor;state-management;zustand;store;flux;wasm;blazor-server;dotnet</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReleaseNotes>Initial release with core store functionality, middleware architecture, and persistence support.</PackageReleaseNotes>
  </PropertyGroup>

  <ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath=""/>
    <None Include="../../icon.png" Pack="true" PackagePath=""/>
  </ItemGroup>
</Project>
```

### Pattern 3: Blazor Sample App with Multiple Render Modes
**What:** Demonstrate all rendering modes via separate pages
**When to use:** Sample/demo applications teaching render mode concepts
**Example:**
```razor
@* Counter-Server.razor - Server rendering *@
@page "/counter-server"
@rendermode InteractiveServer

<h1>Counter (Server)</h1>
<!-- This component uses Interactive Server rendering.
     State changes are processed on the server via SignalR. -->
```

```razor
@* Counter-Wasm.razor - WebAssembly rendering *@
@page "/counter-wasm"
@rendermode InteractiveWebAssembly

<h1>Counter (WebAssembly)</h1>
<!-- This component uses Interactive WebAssembly rendering.
     State changes are processed in the browser. -->
```

```razor
@* Counter-Auto.razor - Auto rendering *@
@page "/counter-auto"
@rendermode InteractiveAuto

<h1>Counter (Auto)</h1>
<!-- This component uses Interactive Auto rendering.
     Initially uses Server, switches to WASM when runtime downloads. -->
```

### Pattern 4: DefaultDocumentation Integration
**What:** Auto-generate markdown API docs post-build
**When to use:** For API reference generation
**Example:**
```xml
<!-- Directory.Build.targets or individual csproj -->
<Project>
  <ItemGroup>
    <PackageReference Include="DefaultDocumentation" Version="1.2.2" PrivateAssets="all" />
  </ItemGroup>

  <PropertyGroup>
    <DefaultDocumentationFolder>$(MSBuildThisFileDirectory)docs/api</DefaultDocumentationFolder>
    <DefaultDocumentationGeneratedAccessModifiers>Public</DefaultDocumentationGeneratedAccessModifiers>
    <DefaultDocumentationIncludeUndocumentedItems>false</DefaultDocumentationIncludeUndocumentedItems>
  </PropertyGroup>
</Project>
```

### Anti-Patterns to Avoid
- **Separate .nuspec files:** Don't use .nuspec for SDK-style projects; use MSBuild properties in csproj
- **IconUrl property:** Deprecated; embed icon with PackageIcon instead
- **LicenseUrl property:** Deprecated; use PackageLicenseExpression or PackageLicenseFile
- **Global version without synchronization:** DevTools must depend on exact same version of Bustand core
- **Hardcoded paths in sample app:** Use relative paths and configuration for portability

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| API documentation | Custom markdown generator | DefaultDocumentation 1.2.2 | Parses XML docs, handles all member types, supports plugins |
| Test coverage collection | Custom coverage tool | coverlet.collector | Already in test project; integrates with dotnet test |
| Coverage HTML reports | Custom report generator | ReportGenerator | Produces styled HTML, supports multiple input formats |
| Source debugging | Manual source packaging | SourceLink | Automatically embeds source control info; IDE integration |
| Version management | Manual version editing | Central Package Management or props file | Single source of truth for version numbers |
| Multi-targeting builds | Separate project files | TargetFrameworks property | SDK handles assembly layout automatically |

**Key insight:** The .NET ecosystem has mature tooling for all distribution concerns. Hand-rolling any of these creates maintenance burden and misses edge cases the tools handle.

## Common Pitfalls

### Pitfall 1: Forgetting to Set IsPackable for Test Projects
**What goes wrong:** Test projects get packaged and uploaded to NuGet
**Why it happens:** Default IsPackable is true for SDK-style projects
**How to avoid:** Set `<IsPackable>false</IsPackable>` in test project csproj (already done in Bustand.Tests)
**Warning signs:** NuGet push creates unexpected .nupkg files

### Pitfall 2: Version Mismatch Between Core and DevTools
**What goes wrong:** DevTools doesn't work with installed Bustand version
**Why it happens:** Packages have independent versions; user installs mismatched versions
**How to avoid:** DevTools csproj must have exact version dependency: `<PackageReference Include="Bustand" Version="[$(Version)]" />`
**Warning signs:** Runtime errors about missing types or methods

### Pitfall 3: Missing Framework-Specific Code for Multi-Targeting
**What goes wrong:** Compile errors when targeting multiple frameworks
**Why it happens:** APIs differ between .NET 8 and .NET 10
**How to avoid:** Use `#if NET8_0` / `#if NET10_0` preprocessor directives for framework-specific code
**Warning signs:** Build failures in one target framework

### Pitfall 4: Coverage Not Collected from Referenced Projects
**What goes wrong:** Coverage report shows 0% for core library
**Why it happens:** Coverlet only collects from test project by default
**How to avoid:** Configure `<CoverletCollectCoverage>true</CoverletCollectCoverage>` and proper includes
**Warning signs:** Missing assemblies in coverage report

### Pitfall 5: Sample App WASM Components in Wrong Project
**What goes wrong:** InteractiveWebAssembly or InteractiveAuto components fail to load
**Why it happens:** WASM-capable components must be in .Client project
**How to avoid:** Put all components using InteractiveWebAssembly or InteractiveAuto in Bustand.Sample.Client
**Warning signs:** "Could not find component" errors in browser console

### Pitfall 6: README Images Not Loading on NuGet.org
**What goes wrong:** Images in README show as broken links
**Why it happens:** NuGet.org only allows images from trusted domains
**How to avoid:** Host images on GitHub (raw.githubusercontent.com) or other allowed domains
**Warning signs:** Broken image icons in NuGet.org package page

### Pitfall 7: TargetFramework vs TargetFrameworks (Singular vs Plural)
**What goes wrong:** Only one framework is targeted despite listing multiple
**Why it happens:** Using singular `<TargetFramework>net8.0;net10.0</TargetFramework>` instead of plural
**How to avoid:** Use `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>` (note the 's')
**Warning signs:** Package only contains one lib folder

## Code Examples

Verified patterns from official sources:

### Complete NuGet Package csproj Configuration
```xml
<!-- Source: Microsoft Learn NuGet documentation -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <!-- Multi-targeting -->
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>

    <!-- Package identity -->
    <PackageId>Bustand</PackageId>
    <Version>0.1.0</Version>
    <Authors>Philippe Matray</Authors>
    <Company>phmatray</Company>

    <!-- Package metadata -->
    <Description>Zustand-inspired state management for Blazor...</Description>
    <PackageTags>blazor;state-management;zustand;store</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/phmatray/Bustand</PackageProjectUrl>
    <RepositoryUrl>https://github.com/phmatray/Bustand.git</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- README and Icon -->
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>

    <!-- Debugging -->
    <DebugType>embedded</DebugType>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>

    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <None Include="../../README.md" Pack="true" PackagePath=""/>
    <None Include="../../icon.png" Pack="true" PackagePath=""/>
  </ItemGroup>

  <!-- InternalsVisibleTo for testing -->
  <ItemGroup>
    <InternalsVisibleTo Include="Bustand.Tests" />
  </ItemGroup>
</Project>
```

### Running Test Coverage with Threshold Check
```bash
# Source: Microsoft Learn code coverage documentation
# Collect coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"TestResults/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:Html

# Check coverage threshold (example script)
# The coverage.cobertura.xml contains line-rate and branch-rate attributes
```

### DefaultDocumentation Configuration
```xml
<!-- Source: DefaultDocumentation GitHub -->
<Project>
  <ItemGroup>
    <PackageReference Include="DefaultDocumentation" Version="1.2.2" PrivateAssets="all" />
  </ItemGroup>

  <PropertyGroup>
    <!-- Output directory -->
    <DefaultDocumentationFolder>$(MSBuildThisFileDirectory)../../docs/api</DefaultDocumentationFolder>

    <!-- Only document public API -->
    <DefaultDocumentationGeneratedAccessModifiers>Public</DefaultDocumentationGeneratedAccessModifiers>

    <!-- Skip undocumented items -->
    <DefaultDocumentationIncludeUndocumentedItems>false</DefaultDocumentationIncludeUndocumentedItems>

    <!-- Generate pages for these elements -->
    <DefaultDocumentationGeneratedPages>Namespaces,Types,Members</DefaultDocumentationGeneratedPages>
  </PropertyGroup>
</Project>
```

### Blazor Web App Program.cs for All Render Modes
```csharp
// Source: Microsoft Learn Blazor render modes documentation
var builder = WebApplication.CreateBuilder(args);

// Add Razor component services for all render modes
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

// Map Razor components with all render modes enabled
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bustand.Sample.Client._Imports).Assembly);

app.Run();
```

### Version Synchronization for DevTools Dependency
```xml
<!-- src/Bustand.DevTools/Bustand.DevTools.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <PackageId>Bustand.DevTools</PackageId>
    <Description>DevTools for Bustand state management. Inspect state, view action history, and time-travel debug.</Description>
    <PackageTags>blazor;state-management;zustand;devtools;debugging</PackageTags>
  </PropertyGroup>

  <!-- CRITICAL: Exact version match required -->
  <ItemGroup Condition="'$(Configuration)' == 'Release'">
    <PackageReference Include="Bustand" Version="[$(Version)]" />
  </ItemGroup>

  <!-- For development, use ProjectReference -->
  <ItemGroup Condition="'$(Configuration)' != 'Release'">
    <ProjectReference Include="..\Bustand\Bustand.csproj" />
  </ItemGroup>
</Project>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| .nuspec files | MSBuild properties in csproj | .NET Core 2.0+ | Simpler, no separate manifest |
| IconUrl | PackageIcon (embedded) | NuGet 5.3+ | More reliable, works offline |
| LicenseUrl | PackageLicenseExpression | NuGet 4.9+ | SPDX expressions, clearer licensing |
| Symbol packages (.symbols.nupkg) | Embedded PDBs or .snupkg | 2019+ | Better debugging experience |
| Separate Blazor Server/WASM projects | Unified Blazor Web App | .NET 8 | Single project with per-component render modes |
| DocFX for simple API docs | DefaultDocumentation | 2020+ | Lighter weight, simpler setup |

**Deprecated/outdated:**
- **IconUrl property**: Deprecated; use PackageIcon with embedded image instead
- **LicenseUrl property**: Deprecated; use PackageLicenseExpression (SPDX) or PackageLicenseFile
- **Blazor Server standalone template**: Deprecated in .NET 8+; use Blazor Web App template
- **Blazor WebAssembly standalone template**: Still available but Blazor Web App is preferred
- **coverlet.msbuild**: Prefer coverlet.collector for data collection

## Open Questions

Things that couldn't be fully resolved:

1. **Icon Design Specifics**
   - What we know: 128x128 PNG with transparent background required
   - What's unclear: Exact icon design (bear theme? zustand-inspired? abstract?)
   - Recommendation: Create simple, recognizable icon; can iterate in future versions

2. **GitHub Wiki vs GitHub Pages for Documentation**
   - What we know: Context decision says "wiki/GitHub Pages"
   - What's unclear: Which primary; wiki for guides, Pages for API reference?
   - Recommendation: README + wiki for guides; generated API docs can go in docs/ folder or Pages

3. **.NET 10 Specific Considerations**
   - What we know: .NET 10 is current; multi-targeting with .NET 8 decided
   - What's unclear: Any .NET 10-specific APIs that require conditional compilation
   - Recommendation: Start without conditionals; add #if NET10_0 if needed during implementation

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn: Multi-targeting for NuGet Packages](https://learn.microsoft.com/en-us/nuget/create-packages/multiple-target-frameworks-project-file) - Multi-targeting configuration
- [Microsoft Learn: NuGet Package Authoring Best Practices](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices) - Metadata, licensing, icons
- [Microsoft Learn: Unit Testing Code Coverage](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage) - Coverlet integration
- [Microsoft Learn: Blazor Project Structure](https://learn.microsoft.com/en-us/aspnet/core/blazor/project-structure) - Sample app architecture
- [Microsoft Learn: Blazor Render Modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes) - Render mode configuration
- [NuGet.org: DefaultDocumentation 1.2.2](https://www.nuget.org/packages/DefaultDocumentation) - Version confirmation

### Secondary (MEDIUM confidence)
- [DefaultDocumentation GitHub](https://github.com/Doraku/DefaultDocumentation) - Configuration options, MSBuild integration
- [Coverlet GitHub](https://github.com/coverlet-coverage/coverlet) - Coverage collection details
- [.NET Blog: Write a high-quality README for NuGet packages](https://devblogs.microsoft.com/dotnet/write-a-high-quality-readme-for-nuget-packages/) - README best practices

### Tertiary (LOW confidence)
- Various blog posts on NuGet packaging patterns (verified with Microsoft docs)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All tools verified via official documentation and NuGet.org
- Architecture: HIGH - Patterns from official Microsoft documentation
- Pitfalls: MEDIUM - Mix of official docs and community experience

**Research date:** 2026-01-25
**Valid until:** 90 days (stable domain, well-established tooling)
