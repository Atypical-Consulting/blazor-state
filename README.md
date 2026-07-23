# Blazor State

<!-- portfolio-badges:start -->
<!-- Identity -->
[![Atypical-Consulting - blazor-state](https://img.shields.io/static/v1?label=Atypical-Consulting&message=blazor-state&color=blue&logo=github)](https://github.com/Atypical-Consulting/blazor-state)
![Top language](https://img.shields.io/github/languages/top/Atypical-Consulting/blazor-state)
[![Stars](https://img.shields.io/github/stars/Atypical-Consulting/blazor-state?style=social)](https://github.com/Atypical-Consulting/blazor-state/stargazers)
[![Forks](https://img.shields.io/github/forks/Atypical-Consulting/blazor-state?style=social)](https://github.com/Atypical-Consulting/blazor-state/network/members)
[![License](https://img.shields.io/github/license/Atypical-Consulting/blazor-state)](https://github.com/Atypical-Consulting/blazor-state/blob/HEAD/LICENSE)

<!-- Activity -->
[![Issues](https://img.shields.io/github/issues/Atypical-Consulting/blazor-state)](https://github.com/Atypical-Consulting/blazor-state/issues)
[![Pull requests](https://img.shields.io/github/issues-pr/Atypical-Consulting/blazor-state)](https://github.com/Atypical-Consulting/blazor-state/pulls)
[![Last commit](https://img.shields.io/github/last-commit/Atypical-Consulting/blazor-state)](https://github.com/Atypical-Consulting/blazor-state/commits)
<!-- portfolio-badges:end -->


> **State management for Blazor — four paradigms, one home.** Pick the approach
> that fits how you think: Elm-style MVU, source-generated state, Redux, or Zustand.

Consolidated from separate repositories (full git history preserved). The libraries
are independent packages — you use one, not all.

## Libraries

| Lib | Paradigm | Pitch | From |
|---|---|---|---|
| [`libs/mvu`](libs/mvu) | **MVU** (Elm) | Model-View-Update pattern for Blazor | `Atypical-Consulting/BlazorMVU` ★ |
| [`libs/the-blazor-state`](libs/the-blazor-state) | **Source generators** | `[Persist]` / `[Shared]` attributes, pluggable storage, cross-tab sync | `Atypical-Consulting/TheBlazorState` |
| [`libs/ducky`](libs/ducky) | **Redux** | Predictable, centralized state container for .NET | `phmatray/Ducky` ★ |
| [`libs/bustand`](libs/bustand) | **Zustand** | Minimal boilerplate, immutable records, time-travel DevTools | `phmatray/Bustand` |

## Utility

| Path | What it is | From |
|---|---|---|
| [`utils/mutty`](utils/mutty) | **Immutable record mutation made easy** — a helper the state libs can build on | `phmatray/Mutty` ★ |

## Samples

| Path | Shows | From |
|---|---|---|
| [`samples/mvvm-todo`](samples/mvvm-todo) | MVVM Todo app with CommunityToolkit.Mvvm | `phmatray/BlazorMvvmApp` |
| [`samples/reactive-patterns`](samples/reactive-patterns) | Reactive programming patterns with Object Calisthenics | `phmatray/Reactif` |

## Which should I use?

- **mvu** — you like Elm's single-message-loop model.
- **the-blazor-state** — you want state that persists and syncs across tabs with minimal code.
- **ducky** — you're coming from Redux and want actions/reducers.
- **bustand** — you want the smallest possible store with great DevTools.

## History

Each folder was merged with **full git history preserved** (`git subtree`). The
original repositories are archived and redirect here.

<!-- portfolio-techstack:start -->

## Tech Stack

- **.NET 10**
- Microsoft.AspNetCore.Components.WebAssembly
- Microsoft.AspNetCore.Components.WebAssembly.Server
- Bustand
- CompareNETObjects
- Scrutor
- bunit
- NSubstitute
- xunit.v3

<!-- portfolio-techstack:end -->

## License

MIT — see [`LICENSE`](LICENSE).

---

<!-- portfolio-sections:start -->

## Contributing

Contributions are welcome. Open an issue first to discuss any significant change.

1. Fork the repository and create your branch (`git checkout -b feat/my-feature`)
2. Commit your changes (`git commit -m 'feat: ...'`)
3. Push the branch and open a Pull Request

<!-- portfolio-sections:end -->
