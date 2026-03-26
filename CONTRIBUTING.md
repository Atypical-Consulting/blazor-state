# 🤝 Contributing to BlazorMVU

Thank you for considering a contribution to BlazorMVU!  
This guide will help you get from "I want to help" to "my PR is merged" as smoothly as possible.

---

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Ways to Contribute](#ways-to-contribute)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Issue Labels](#issue-labels)

---

## Code of Conduct

Be kind. Be constructive. We're all here to build something useful together.  
Discriminatory, harassing, or otherwise harmful behaviour will not be tolerated.

---

## Ways to Contribute

| Type | Description |
|---|---|
| 🐛 Bug report | Found something broken? Open an issue |
| 💡 Feature request | Have an idea? Open an issue with `[feature]` in the title |
| 📝 Documentation | Fix typos, improve examples, add guides |
| ✅ Tests | Improve coverage, add edge cases |
| 🔧 Code | Fix bugs, implement new features from the backlog |
| 🎨 Demo | Add a new MVU example to the demo app |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git

### Clone & Build

```bash
git clone https://github.com/Atypical-Consulting/BlazorMVU.git
cd BlazorMVU
dotnet build BlazorMVU.sln
```

### Run the Demo

```bash
cd src/BlazorMVU.Demo
dotnet run
# Open https://localhost:7xxx in your browser
```

### Run the Tests

```bash
dotnet test BlazorMVU.sln
```

---

## Development Workflow

1. **Fork** the repository
2. **Create a branch** from `dev` (not `main`):
   ```bash
   git checkout dev
   git pull origin dev
   git checkout -b feat/my-feature
   ```
3. **Make your changes** (see coding standards below)
4. **Run the tests** to verify nothing is broken
5. **Commit** using [Conventional Commits](#conventional-commits)
6. **Push** to your fork and open a PR against `dev`

> ⚠️ PRs targeting `main` directly will be redirected to `dev`.

---

## Conventional Commits

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.

```
feat: add Sub.Debounce subscription type
fix: resolve double-render on rapid dispatch
docs: improve MvuComponent XML summary
test: add bUnit test for Cmd.Batch ordering
chore: update .NET SDK to 10.0.201
```

This keeps the changelog clean and makes releases easier to generate.

---

## Coding Standards

- **Language version:** C# 13 (as specified in csproj)
- **Nullable:** enabled — no `#nullable disable` without a good reason
- **Namespaces:** `BlazorMVU` (root) — match the folder structure
- **Records over classes** for Model state (immutability first)
- **No mutable state** in the `Update` function
- **XML doc comments** on all public API surface

### Style

We use the default .NET `EditorConfig` style. Run `dotnet format` before committing:

```bash
dotnet format BlazorMVU.sln
```

---

## Testing

Tests live in `src/BlazorMVU.Tests` and use:
- **xUnit v3** for unit tests
- **bUnit** for component tests
- **Shouldly** for readable assertions

### What to test

- **New feature?** Add at least one happy-path test and one edge-case test.
- **Bug fix?** Add a regression test that would have caught the bug.
- **New `Cmd` or `Sub` type?** Test that the side effect fires and returns the expected message.

---

## Submitting a Pull Request

### Checklist

- [ ] Branch is based on `dev`
- [ ] `dotnet build` passes with no warnings
- [ ] `dotnet test` passes (all tests green)
- [ ] `dotnet format --verify-no-changes` passes
- [ ] PR description explains *what* and *why* (not just *how*)
- [ ] New public APIs have XML doc comments

### PR Title Format

```
feat: add Cmd.Race for competing async commands
fix: correct Sub.Timer cancellation on dispose
docs: add time-travel debugging tutorial
```

### What Happens After You Submit

1. CI runs automatically (build + tests)
2. A maintainer reviews your PR (usually within a few days)
3. We may request changes — please don't take this personally, it's how we keep quality high
4. Once approved, we squash-merge into `dev`

---

## Issue Labels

| Label | Meaning |
|---|---|
| `good first issue` | Small, well-scoped — great for newcomers |
| `help wanted` | We want community input on this |
| `bug` | Something is broken |
| `enhancement` | New feature or improvement |
| `documentation` | Docs-only change |
| `question` | Needs clarification before work starts |

---

## Questions?

Open an issue or start a [Discussion](https://github.com/Atypical-Consulting/BlazorMVU/discussions).  
We're friendly, we promise. 🙂

---

_Maintained by [Atypical Consulting](https://atypical.garry-ai.cloud) — opinionated, production-grade open source._
