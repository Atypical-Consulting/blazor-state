# Stack Research

**Domain:** Blazor state management library (.NET 10)
**Researched:** 2026-01-24
**Confidence:** HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended | Confidence |
|------------|---------|---------|-----------------|------------|
| .NET 10 | 10.0 | Target framework | LTS release (Nov 2025 - Nov 2028), required for Blazor unified rendering model, `[PersistentState]` attribute for state persistence | HIGH |
| Microsoft.NET.Sdk.Razor | (SDK) | Project SDK | Required for Blazor component compilation, supports `.razor` files, enables source generation | HIGH |
| Microsoft.AspNetCore.Components | 10.0.x | Blazor component framework | Core dependency for component lifecycle, render trees, and cascading parameters | HIGH |
| C# 13 | 13.0 | Language version | Ships with .NET 10, improved pattern matching, record improvements for immutable state | HIGH |

### Multi-Targeting Strategy

| Target Framework | Purpose | Why Include |
|------------------|---------|-------------|
| `net10.0` | Primary target | Full .NET 10 feature access, LTS support until 2028 |
| `net8.0` | Backward compatibility | Previous LTS (until Nov 2026), many projects still on .NET 8 |

**Recommendation:** Multi-target `net8.0;net10.0` for maximum adoption. Do NOT target `netstandard2.0` - Blazor components require framework-specific APIs.

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

### Supporting Libraries

| Library | Version | Purpose | When to Use | Confidence |
|---------|---------|---------|-------------|------------|
| Scrutor | 7.0.0 | Assembly scanning and auto-registration | Auto-discover and register stores via `IServiceCollection.Scan()` | HIGH |
| System.Text.Json | 10.0.x | JSON serialization | DevTools state serialization, persistence middleware, source generators for AOT | HIGH |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.x | DI abstractions | Service registration APIs (comes with Scrutor) | HIGH |

### Development Tools

| Tool | Version | Purpose | Notes | Confidence |
|------|---------|---------|-------|------------|
| bUnit | 2.5.3 | Blazor component testing | Supports .NET 8/9/10, works with xUnit/NUnit/MSTest, semantic HTML comparison | HIGH |
| xUnit | 2.9.x | Unit testing framework | Modern, fast, default for .NET Core, parallel test execution | HIGH |
| Microsoft.SourceLink.GitHub | 8.0.0 | Source debugging | Embeds source control metadata for debugging NuGet packages | HIGH |
| MinVer | 6.x | Semantic versioning | Git-based version generation, integrates with NuGet packaging | MEDIUM |

### Build & Packaging

| Tool | Purpose | Configuration |
|------|---------|---------------|
| `dotnet pack` | NuGet creation | Built into SDK, use `.csproj` properties |
| Directory.Build.props | Centralized build config | Share settings across multiple projects |
| GitHub Actions | CI/CD | Standard for .NET open source projects |

## Installation

```bash
# Create solution structure
dotnet new sln -n Bustand

# Core library (Razor class library)
dotnet new razorclasslib -n Bustand -f net10.0
dotnet sln add Bustand/Bustand.csproj

# DevTools library (separate package)
dotnet new razorclasslib -n Bustand.DevTools -f net10.0
dotnet sln add Bustand.DevTools/Bustand.DevTools.csproj

# Test project
dotnet new xunit -n Bustand.Tests -f net10.0
dotnet sln add Bustand.Tests/Bustand.Tests.csproj

# Sample app
dotnet new blazor -n Bustand.Sample -f net10.0 --interactivity Auto
dotnet sln add Bustand.Sample/Bustand.Sample.csproj
```

```bash
# Add dependencies to core library
cd Bustand
dotnet add package Scrutor --version 7.0.0

# Add dependencies to test project
cd ../Bustand.Tests
dotnet add package bunit --version 2.5.3
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
```

## Project Structure Recommendation

```
Bustand/
├── src/
│   ├── Bustand/                    # Core library
│   │   ├── Bustand.csproj
│   │   ├── ZustandStore.cs         # Base store class
│   │   ├── ZustandScope.razor      # Cascading component
│   │   ├── Middleware/             # Middleware interfaces
│   │   └── Extensions/             # DI extension methods
│   └── Bustand.DevTools/           # DevTools (separate package)
│       ├── Bustand.DevTools.csproj
│       ├── DevToolsPage.razor      # /bustand-devtools page
│       ├── Components/             # DevTools UI components
│       └── Extensions/             # AddBustandDevTools()
├── tests/
│   └── Bustand.Tests/
│       ├── Bustand.Tests.csproj
│       ├── StoreTests.cs
│       └── ComponentTests.cs
├── samples/
│   └── Bustand.Sample/
├── Directory.Build.props           # Shared build config
├── Directory.Packages.props        # Central package management
└── Bustand.sln
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| xUnit | NUnit | Team has existing NUnit expertise; NUnit has richer assertions |
| xUnit | MSTest | Deep Microsoft/Visual Studio integration required |
| Scrutor | Manual registration | Very few stores (<5), want zero dependencies |
| System.Text.Json | Newtonsoft.Json | Need JSON.NET-specific features (JToken, complex converters) |
| bUnit | Playwright | Need E2E testing with real browser JavaScript |
| MinVer | GitVersion | Need more complex versioning schemes |

## What NOT to Use

| Avoid | Why | Use Instead | Confidence |
|-------|-----|-------------|------------|
| MediatR | Overkill for simple state management; adds unnecessary abstraction | Direct middleware pattern via interfaces | HIGH |
| Fluxor | Competitor library; heavy Flux/Redux pattern when Zustand-style is goal | Build custom lightweight solution | HIGH |
| Blazored.LocalStorage | Adds dependency for persistence; trivial to implement | Custom persistence middleware with `IJSRuntime` | MEDIUM |
| UI component frameworks (MudBlazor, Radzen) | DevTools should be dependency-free, plain HTML/CSS | Plain Blazor components with CSS | HIGH |
| Autofac/other DI containers | Forces container choice on consumers | Microsoft.Extensions.DependencyInjection only | HIGH |
| SignalR (direct) | Already available in Blazor Server, unnecessary dependency | Use Blazor's built-in rendering for DevTools | MEDIUM |

## Stack Patterns by Variant

**If targeting only .NET 10:**
- Use single target `net10.0`
- Leverage `[PersistentState]` attribute for prerendering
- Use C# 13 features freely

**If targeting .NET 8 + .NET 10:**
- Multi-target `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`
- Use `#if NET10_0_OR_GREATER` for .NET 10-specific features
- Avoid APIs not in .NET 8

**If NativeAOT support required:**
- Use System.Text.Json source generators
- Avoid reflection-based serialization
- Test trimming with `<PublishTrimmed>true</PublishTrimmed>`

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| Scrutor 7.0.0 | .NET 8, .NET 10, .NET Standard 2.0 | Requires Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0+ |
| bUnit 2.5.3 | .NET 8, .NET 9, .NET 10 | Works with all Blazor rendering modes |
| Microsoft.AspNetCore.Components 10.0.x | .NET 10 only | Use version matching target framework |
| Microsoft.AspNetCore.Components 8.0.x | .NET 8 only | Use conditional package references |

## NuGet Package Configuration

```xml
<!-- In Bustand.csproj -->
<PropertyGroup>
  <!-- Package metadata -->
  <PackageId>Bustand</PackageId>
  <Description>Zustand-inspired state management for Blazor with minimal boilerplate and exceptional DevTools</Description>
  <Authors>Your Name</Authors>
  <PackageTags>blazor;state-management;zustand;flux;devtools</PackageTags>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/yourname/Bustand</PackageProjectUrl>
  <RepositoryUrl>https://github.com/yourname/Bustand</RepositoryUrl>
  <RepositoryType>git</RepositoryType>

  <!-- Enable Source Link -->
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>

  <!-- README in NuGet -->
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>

<ItemGroup>
  <None Include="../../README.md" Pack="true" PackagePath="/" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
</ItemGroup>
```

## Render Mode Compatibility

Bustand must work across all Blazor render modes. Key considerations:

| Render Mode | State Location | Considerations |
|-------------|----------------|----------------|
| Static SSR | Server (no interactivity) | Components should render read-only state |
| Interactive Server | Server memory | State lives in circuit; SignalR manages updates |
| Interactive WebAssembly | Browser memory | State isolated per tab; persistence via localStorage |
| Interactive Auto | Server then client | State must handle prerendering + hydration |

**Library design principles:**
1. Never specify `@rendermode` in library components - let consumers decide
2. Use `RendererInfo.IsInteractive` for graceful degradation
3. DevTools page should work in any interactive mode
4. Persistence middleware must detect render mode for storage API access

## Sources

- [Blazor in .NET 10: What's New](https://dev.to/mashrulhaque/blazor-in-net-10-the-features-that-actually-matter-nc1) - .NET 10 Blazor features (HIGH)
- [.NET 10.0 is Ready](https://www.heise.de/en/news/NET-10-0-is-Ready-11075047.html) - .NET 10 release info (HIGH)
- [Scrutor GitHub](https://github.com/khellang/Scrutor) - Scrutor 7.0.0 features (HIGH)
- [NuGet: Scrutor 7.0.0](https://www.nuget.org/packages/Scrutor) - Scrutor version and TFMs (HIGH)
- [NuGet: bUnit 2.5.3](https://www.nuget.org/packages/bunit) - bUnit version and compatibility (HIGH)
- [ASP.NET Core Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0) - Official render mode guidance (HIGH)
- [ASP.NET Core Razor SDK](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/sdk?view=aspnetcore-10.0) - SDK configuration (HIGH)
- [Package authoring best practices](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices) - NuGet packaging (HIGH)
- [Source Link and .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) - Source debugging (HIGH)
- [Cross-platform targeting](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting) - Multi-targeting guidance (HIGH)
- [xUnit vs NUnit vs MSTest comparison](https://daily.dev/blog/nunit-vs-xunit-vs-mstest-net-unit-testing-framework-comparison) - Testing frameworks (MEDIUM)
- [System.Text.Json source generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation) - JSON serialization (HIGH)

---
*Stack research for: Blazor state management library (Bustand)*
*Researched: 2026-01-24*
