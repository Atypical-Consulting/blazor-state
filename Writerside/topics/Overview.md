# Overview

Mutty is a C# Incremental Source Generator that provides a convenient way to work with immutable records by automatically generating **mutable wrappers** for them ([GitHub - phmatray/Mutty: Immutable Record Mutation Made Easy](https://github.com/phmatray/mutty)). These wrappers let you modify the properties of an immutable record in a controlled manner and then convert those changes back into a new immutable record. Mutty enables a cleaner workflow for state updates, eliminating the tedious boilerplate of manually copying records.

![Mutty Overview](mutty-overview.png)

*Figure: Conceptual overview of Mutty's operation – the current immutable state is copied into a **Draft** (mutable proxy) where you apply edits, and then Mutty produces the **Next** immutable state with those edits incorporated. This approach preserves the benefits of immutability while allowing straightforward mutations.*

**Badges:**  
[![NuGet](https://img.shields.io/nuget/v/Mutty.svg)](https://www.nuget.org/packages/Mutty)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://github.com/phmatray/Mutty/blob/main/LICENSE)

## Features

- **Automated Mutable Wrappers:** Mark your record types with a `[MutableGeneration]` attribute, and Mutty uses Roslyn’s incremental source generation to produce corresponding `Mutable<RecordName>` classes at compile-time. Each mutable class mirrors the original record’s structure and provides setters for each property.
- **Deep Nesting Support:** Mutty handles complex, nested immutable structures without hassle. If your record contains other records or immutable collections, Mutty generates wrappers for those as well (provided they are also annotated), so you can mutate deeply nested data in one go.
- **Implicit Conversion:** Mutty leverages C# implicit operators to allow seamless conversion between an immutable record and its mutable wrapper. You can assign a record to a `MutableRecord` variable (creating a draft copy) or assign a `MutableRecord` back to the record type (producing an updated immutable instance) without explicit casting.
- **Immutable Collections Integration:** Provides extension methods like `AsMutable()` and `ToImmutable()` to bridge between immutable collection types (e.g. `ImmutableList<T>`) and standard .NET collections. This makes it easy to mutate lists of records: you get a `List<MutableT>` to work with, and convert it back to an `ImmutableList<T>` when done.
- **Flux-Friendly Architecture:** Mutty is ideal for Flux/Redux-like state management. It enables an immutable state store with convenient mutable draft updates. You can apply changes in a reducer-style function using Mutty’s helpers, and still maintain predictability and immutability of state transitions.

## How It Works (Summary)

Mutty uses a custom attribute `[MutableGeneration]` to mark the record types you want to make mutable. At build time, a Roslyn **Incremental Source Generator** scans the compilation for these attributes and generates a partial class “Mutable*YourRecord*” for each annotated record. Each mutable class acts as a proxy to the original record: you modify the proxy’s properties, and when you're done, Mutty produces a new instance of the record with those modifications applied. In essence, Mutty gives you a temporary editable draft of your data and then creates the next immutable state from it, ensuring that your original data remains untouched and thread-safe during the mutation process.

For a deeper dive into the code generation process, see the [](Architecture.md) page, and check the [](API-Reference.md) for specifics of the generated classes and methods.

## Getting Started

- **Installation:** Install the NuGet package and annotate your record types. See the [Installation guide](Installation.md) for details on adding Mutty to your project.
- **Usage Examples:** Use the `[MutableGeneration]` attribute on your records and call the provided `Produce` method or draft methods to update them. The [](Usage.md) section provides examples of mutating nested records, compares Mutty with traditional `with` expressions, and shows how to integrate Mutty into a Flux-style workflow.
- **API Reference:** Refer to the [](API-Reference.md) for a detailed listing of the generated wrapper classes and helper methods like `Produce`, `CreateDraft`, etc.

## Contributing

Mutty is an open-source project, and contributions are welcome! If you encounter any issues or have feature suggestions, please let us know on GitHub. To contribute code or documentation, see the [Contributing guidelines](Contributing.md) for instructions on how to get started. We appreciate all forms of feedback and help.

## License

Mutty is licensed under the **Apache License 2.0**, which means it’s free to use in both personal and commercial projects. See the [LICENSE](https://github.com/phmatray/Mutty/blob/main/LICENSE) for the full license text. By contributing to this project, you agree that your contributions will be licensed under the same Apache-2.0 license.
