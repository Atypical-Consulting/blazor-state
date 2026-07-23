# Blazor State

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

## License

MIT — see [`LICENSE`](LICENSE).
