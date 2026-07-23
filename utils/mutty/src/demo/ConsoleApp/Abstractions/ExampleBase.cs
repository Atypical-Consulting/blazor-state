// Copyright (c) 2020-2024 Atypical Consulting SRL. All rights reserved.
// Atypical Consulting SRL licenses this file to you under the Apache 2.0 license.
// See the LICENSE file in the project root for full license information.

namespace Mutty.ConsoleApp.Abstractions;

/// <summary>
/// Base class for all examples.
/// </summary>
public abstract class ExampleBase
{
    /// <summary>
    /// Abstract method that each derived class must implement.
    /// </summary>
    public abstract void Run();

    /// <summary>
    /// Common method to display a header for the example.
    /// </summary>
    /// <param name="header">The header to display.</param>
    protected static void DisplayHeader(string header)
    {
        MarkupLine($"[bold yellow]{header}[/]\n");
    }
}
