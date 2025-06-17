// Copyright (c) 2020-2024 Atypical Consulting SRL. All rights reserved.
// Atypical Consulting SRL licenses this file to you under the Apache 2.0 license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Mutty.Tests.Analyzers;

/// <summary>
/// Default verifier for analyzer tests using NUnit assertions.
/// </summary>
public class DefaultVerifier : IVerifier
{
    /// <inheritdoc />
    public void Empty<T>(string collectionName, IEnumerable<T> collection)
    {
        Assert.That(collection, Is.Empty, $"{collectionName} is not empty.");
    }

    /// <inheritdoc />
    public void Equal<T>(T expected, T actual, string? message = null)
    {
        Assert.That(actual, Is.EqualTo(expected), message);
    }

    /// <inheritdoc />
    public void True([DoesNotReturnIf(false)] bool assert, string? message = null)
    {
        Assert.That(assert, Is.True, message);
    }

    /// <inheritdoc />
    public void False([DoesNotReturnIf(true)] bool assert, string? message = null)
    {
        Assert.That(assert, Is.False, message);
    }

    /// <inheritdoc />
    [DoesNotReturn]
    public void Fail(string? message = null)
    {
        Assert.Fail(message ?? "Test failed.");
        throw new InvalidOperationException("This method should not return");
    }

    /// <inheritdoc />
    public void LanguageIsSupported(string language)
    {
        Assert.That(
            language,
            Is.EqualTo(Microsoft.CodeAnalysis.LanguageNames.CSharp).Or
                .EqualTo(Microsoft.CodeAnalysis.LanguageNames.VisualBasic),
            $"Language '{language}' is not supported.");
    }

    /// <inheritdoc />
    public void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
    {
        Assert.That(collection, Is.Not.Empty, $"{collectionName} is empty.");
    }

    /// <inheritdoc />
    public void SequenceEqual<T>(
        IEnumerable<T> expected,
        IEnumerable<T> actual,
        IEqualityComparer<T>? equalityComparer = null,
        string? message = null)
    {
        if (equalityComparer is not null)
        {
            Assert.That(actual, Is.EqualTo(expected).Using(equalityComparer), message);
        }
        else
        {
            Assert.That(actual, Is.EqualTo(expected), message);
        }
    }

    /// <summary>
    /// Creates a new verifier for validation within a specific context.
    /// </summary>
    public IVerifier PushContext(string context)
    {
        // NUnit doesn't have a direct equivalent to xUnit's test context
        // For now, we just return the same instance
        return this;
    }
}
