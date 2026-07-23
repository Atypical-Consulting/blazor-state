![Reactif banner](.github/banner.png)

# Reactif — Reactive Programming Patterns in C#

<!-- portfolio-badges:start -->
<!-- Identity -->
[![phmatray - Reactif](https://img.shields.io/static/v1?label=phmatray&message=Reactif&color=blue&logo=github)](https://github.com/phmatray/Reactif)
![Top language](https://img.shields.io/github/languages/top/phmatray/Reactif)
[![Stars](https://img.shields.io/github/stars/phmatray/Reactif?style=social)](https://github.com/phmatray/Reactif/stargazers)
[![Forks](https://img.shields.io/github/forks/phmatray/Reactif?style=social)](https://github.com/phmatray/Reactif/network/members)

<!-- Activity -->
[![Issues](https://img.shields.io/github/issues/phmatray/Reactif)](https://github.com/phmatray/Reactif/issues)
[![Pull requests](https://img.shields.io/github/issues-pr/phmatray/Reactif)](https://github.com/phmatray/Reactif/pulls)
[![Last commit](https://img.shields.io/github/last-commit/phmatray/Reactif)](https://github.com/phmatray/Reactif/commits)
<!-- portfolio-badges:end -->


A collection of reactive programming patterns implemented in C# with Object Calisthenics discipline. Demonstrates clean reactive code with observable streams and functional composition.

## ✨ Features
- Reactive Observable patterns
- Object Calisthenics-compliant code
- Functional composition examples
- Console demo application
- Qodana code quality integration

## 📦 Installation
```bash
dotnet add package Reactif
```

## 🚀 Quick Start
```csharp
var stream = Observable.Range(1, 10)
    .Where(x => x % 2 == 0)
    .Select(x => x * x);
stream.Subscribe(Console.WriteLine);
```

<!-- portfolio-techstack:start -->

## Tech Stack

- **.NET 7 · .NET Standard 2.0**
- Shouldly
- Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit
- Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit
- xunit
- xunit.runner.visualstudio
- Microsoft.CodeAnalysis.CSharp
- Microsoft.CodeAnalysis.CSharp.Workspaces
- HtmlAgilityPack

<!-- portfolio-techstack:end -->

## 📄 License
MIT — see LICENSE

---

<!-- portfolio-sections:start -->

## Contributing

Contributions are welcome. Open an issue first to discuss any significant change.

1. Fork the repository and create your branch (`git checkout -b feat/my-feature`)
2. Commit your changes (`git commit -m 'feat: ...'`)
3. Push the branch and open a Pull Request

<!-- portfolio-sections:end -->
