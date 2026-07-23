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

## 📄 License
MIT — see LICENSE
