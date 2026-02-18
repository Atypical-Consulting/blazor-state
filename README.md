# Reactif — Reactive Programming Patterns in C#

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
