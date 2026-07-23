# Roslyn Analyzers Sample

A set of three sample projects that includes Roslyn analyzers with code fix providers. Enjoy this template to learn from
and modify analyzers for your own needs.

## Content

### ObjectCalisthenics

A .NET Standard project with implementations of sample analyzers and code fix providers.
**You must build this project to see the results (warnings) in the IDE.**

- [SampleSemanticAnalyzer.cs](SampleSemanticAnalyzer.cs): An analyzer that reports invalid values used for the `speed`
  parameter of the `SetSpeed` function.
- [SampleSyntaxAnalyzer.cs](SampleSyntaxAnalyzer.cs): An analyzer that reports the company name used in class
  definitions.
- [SampleCodeFixProvider.cs](SampleCodeFixProvider.cs): A code fix that renames classes with company name in their
  definition. The fix is linked to [SampleSyntaxAnalyzer.cs](SampleSyntaxAnalyzer.cs).

### ObjectCalisthenics.Sample

A project that references the sample analyzers. Note the parameters of `ProjectReference`
in [ObjectCalisthenics.Sample.csproj](../ObjectCalisthenics.Sample/ObjectCalisthenics.Sample.csproj), they make sure
that the project is referenced as a set of analyzers.

### ObjectCalisthenics.Tests

Unit tests for the sample analyzers and code fix provider. The easiest way to develop language-related features is to
start with unit tests.

## How To?

### How to debug?

- Use the [launchSettings.json](Properties/launchSettings.json) profile.
- Debug tests.

### How can I determine which syntax nodes I should expect?

Consider installing the Roslyn syntax tree viewer plugin [Rossynt](https://plugins.jetbrains.com/plugin/16902-rossynt/).

### Learn more about wiring analyzers

The complete set of information is available
at [roslyn github repo wiki](https://github.com/dotnet/roslyn/blob/main/docs/wiki/README.md).

### OC Analyzers categories

The categories in the markdown array are derived from the principles of Object Calisthenics, each aiming to address
specific areas of code quality:

1. **Readability:** This category includes rules that make the code easier to read and understand. For instance, having
   one level of indentation helps in making the code flow clear and linear, making it easier to follow the logic.
2. **Size:** These rules are about keeping the size of components small. Small methods and classes are generally easier
   to understand, test, and maintain. Examples are rules against using the `else` keyword to keep methods short and
   enforcing small class sizes.
3. **Names:** Good naming is crucial for understandable code. This category emphasizes the importance of descriptive and
   unambiguous names for variables, methods, and classes.
4. **Complexity:** These rules focus on reducing the complexity of the code. By limiting the number of instance
   variables or using first-class collections, the aim is to create more cohesive and less coupled classes.
5. **Testing:** This category includes rules that make the code more testable. Avoiding getters and setters can
   encourage better encapsulation and make the code less dependent on its internal structure, which can simplify
   testing.

Each of these categories targets an aspect of software development that contributes to code quality, maintainability,
and the ability to evolve and refactor without introducing errors. The Object Calisthenics rules are meant to guide
programmers in writing code that adheres to good object-oriented design principles.



# ObjectCalisthenics Analyzers 📏🖋️

Welcome to the ObjectCalisthenics project, your go-to toolkit for enforcing clean code practices with a .NET Standard implementation featuring a suite of Roslyn analyzers based on Object Calisthenics principles. 🌟

## Content

### ObjectCalisthenics Analyzers 🛠️

This project houses a robust collection of Roslyn analyzers and code fix providers crafted to instill the Object Calisthenics rules within your code. To harness these tools, build the project and heed the warnings and suggestions that surface in your IDE.

### ObjectCalisthenics.Sample 📚

A reference project that demonstrates the analyzers in action. The project references ensure that the analyzers are employed effectively to scrutinize your code.

### ObjectCalisthenics.Tests 🔍

Unit tests for the analyzers and code fix providers make up this section. These tests are crucial for developing and ensuring the reliability of language-related features.

## How To Use

### Debugging 🐞

- Apply the `launchSettings.json` profile to streamline your debugging process.
- Develop and refine features by initially writing unit tests, enabling iterative refinement and problem-solving.

### Understanding Syntax Nodes 🌳

For insights into the structure of your code's syntax trees, a syntax tree viewer like Rossynt can be invaluable, revealing the intricacies of your code's structure.

### Learning More 📖

For a detailed guide on creating and using analyzers, the [Roslyn GitHub repository's wiki](https://github.com/dotnet/roslyn/blob/main/docs/wiki/README.md) is an excellent resource.

## OC Analyzers Rules 📜

Our analyzers are geared towards ensuring that your codebase complies with the following Object Calisthenics rules:

1. **One Level of Indentation per Method** - Enhance readability and reduce complexity.
2. **Don’t Use the ELSE Keyword** - Foster single entry, single exit functions.
3. **Wrap All Primitives and Strings** - Add meaningful behavior and context to primitive types.
4. **First Class Collections** - Collections get their own classes, simplifying their management.
5. **One Dot Per Line** - Prevent Law of Demeter violations and promote encapsulation.
6. **Don’t Abbreviate** - Prioritize clear and expressive variable names.
7. **Keep All Entities Small** - Limit classes to 50 lines and packages to 10 files.
8. **No Classes with More Than Two Instance Variables** - Encourage single responsibility and cohesion.
9. **No Getters/Setters/Properties** - Protect the internal structure of objects to ease refactoring.

Incorporating the ObjectCalisthenics analyzers into your development process commits you to a codebase that not only meets rigorous standards but is also in line with the highest principles of clean code and object-oriented design. Get ready to elevate your code with ObjectCalisthenics! 🚀👩‍💻👨‍💻