# Contributing

Contributions to Mutty are welcome! Whether you’ve encountered a bug, have an improvement in mind, or want to add a new feature, here are guidelines on how you can contribute to the project.

## Project Repository

The source code for Mutty is hosted on GitHub under the repository **phmatray/Mutty**. You can find it here: **<https://github.com/phmatray/Mutty>**. The repository includes the source generator implementation, examples, and the README documentation that this guide is based on.

Before contributing, it’s a good idea to familiarize yourself with the project structure and existing issues or discussions.

## Reporting Issues and Requesting Features

If you run into a problem using Mutty or have a suggestion for an enhancement, please open an issue on the GitHub repository. Include as much detail as possible:
- **Bug Reports:** Describe the issue, steps to reproduce, and the version of Mutty you’re using. If applicable, provide snippets of code or error messages. This will help us (and other contributors) verify and fix the issue more quickly.
- **Feature Requests:** Explain the feature or improvement, the motivation behind it, and (if you have ideas) how it might be implemented. Even if you’re not planning to implement it yourself, discussing it in an issue can help gauge interest and refine the idea.

Please search the existing issues first to see if your bug or request has already been noted. If so, you can add any additional information as a comment rather than creating a duplicate issue.

## Development Setup

To contribute code, you will need:
- A recent version of **Visual Studio** or the **.NET SDK** (the project is .NET based, likely requiring .NET 6 or higher to build because of incremental generator support).
- The repository cloned to your local machine.

Steps to set up:
1. **Fork the Repository:** Click “Fork” on the GitHub repo to create your own copy. This allows you to experiment freely.
2. **Clone the Fork:** `git clone https://github.com/phmatray/Mutty.git`
3. **Open the Solution:** Mutty should come with a Visual Studio solution (e.g., `Mutty.sln`). Open this in Visual Studio or VS Code. You may see multiple projects (possibly one for the source generator, one for example usage, etc.).
4. **Restore Dependencies:** Ensure NuGet restores any dependencies (though Mutty itself might not have many external dependencies, mostly just Roslyn which is provided by the .NET SDK).

You should be able to build the solution and run any included tests or example projects to verify everything is working.

## Code Style Guidelines

We aim to maintain a consistent coding style:
- Follow standard **.NET naming conventions** (PascalCase for public members/types, camelCase for private fields, etc.).
- Use descriptive names for classes and methods. This is especially important in a source generator for clarity.
- Keep the code **self-documenting** where possible; when not, use XML documentation comments for public APIs and important internal logic.
- The repository may include an `.editorconfig` or similar (there is a ReSharper/Rider settings file `.DotSettings` in the repo) which enforces certain style rules. Adhering to those will make it easier to merge your code. Common rules might include spacing, bracing style, etc.
- Before committing, consider running a code formatter (`dotnet format` or the IDE’s formatting) to ensure your changes match the project’s style.

If you’re adding new features or substantial changes, also consider updating documentation (the README or documentation files). Clear comments in the code or added XML docs are appreciated for any complex sections.

## Testing Changes

Ensure that any changes you make do not break existing functionality:
- Mutty may have an accompanying test suite or sample project. If there are **unit tests or integration tests**, run them (e.g., using `dotnet test`) and make sure all tests pass. Add new tests if you’re implementing a new feature or fixing a bug, to cover that scenario.
- If no formal test project exists, use the provided examples or create a small console app to simulate using Mutty. For instance, you can write a dummy record, run the generator, and verify that the generated class behaves as expected (e.g., via unit tests that use reflection to find the generated type, or by directly calling Mutty’s API in the example project).
- Manual testing can involve installing your modified Mutty package into a sample project to see it working end-to-end.

Keeping the project’s examples up to date with your changes (if you improve something that could be shown off in an example) is also a good practice.

## Submitting a Pull Request

Once you are satisfied with your changes:
1. **Commit** your changes with a clear message. For example: `git commit -m "Fix issue #X: handle ImmutableArray in AsMutable"` or `git commit -m "Add CreateDraft/FinishDraft extensions for finer control"`.
2. **Push** to your fork: `git push origin <your-branch-name>`.
3. **Open a Pull Request:** Go to the original Mutty repository and you should see a prompt to open a PR from your fork. In the PR description, describe the problem and solution, and mention the issue number if it fixes an existing issue. Provide context for maintainers: what changed and why? If it’s a new feature, explain how it’s useful. If it’s a bug fix, describe how you fixed it.
4. **Discuss and Iterate:** A project maintainer or other contributors might review your PR and provide feedback. You may be asked to adjust things (code style, approach, add a test, etc.). Collaborate in the PR by making additional commits to your branch – the PR will update automatically.

Be patient and responsive in the review process. Everyone involved shares the goal of improving Mutty.

## Community and Support

Keep an eye on the project’s issue tracker for any discussions. You can also reach out via any links the maintainer has provided (sometimes a Gitter/Discord or Twitter, etc., though none may be explicitly provided for Mutty). If you become a frequent contributor, you might get added as a collaborator.

## Code of Conduct

While not explicitly stated in the repository, it’s implied that contributors should behave professionally and respectfully. When giving feedback or discussing in issues/PRs, keep the tone constructive. We appreciate your contributions and aim to foster a welcoming open-source community.

By following these guidelines, you’ll make it easier for your contributions to be reviewed and accepted. Thank you for helping improve Mutty!
