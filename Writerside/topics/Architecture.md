# Architecture

This page delves into how Mutty works under the hood. We’ll explain how the Roslyn Source Generator in Mutty processes your code and generates new source files, and how those files fit into your project’s compilation. Understanding the architecture can help you trust the tool and even troubleshoot or extend it if needed.

## Incremental Source Generation via Roslyn

Mutty is implemented as an **Incremental Source Generator** using the Roslyn compiler APIs ([GitHub - dotnet/roslyn](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)). This means it runs at compile time and produces new source code that gets compiled together with your own code. “Incremental” indicates that the generator is optimized to run efficiently as your code changes – it will only recompute outputs for the parts of the code that changed, rather than regenerating everything on each build.

Here’s a simplified breakdown of what happens when you build a project that uses Mutty:

1. **Initialization:** When the compiler starts, it loads the Mutty analyzer (source generator). The generator registers a pipeline of tasks with the compiler – for example, one task might be “find all record declarations with the `[MutableGeneration]` attribute”.

2. **Scanning for Annotations:** The generator uses the syntax and semantic analysis provided by Roslyn to find candidate types. Specifically, it looks for any record definition that has the `MutableGeneration` attribute on it. This attribute itself is defined in the Mutty package (likely as an internal or public attribute class). Because you reference Mutty, the attribute is available for you to use and for the generator to detect.

3. **Generating Code for Each Record:** For each record tagged with `[MutableGeneration]`, Mutty’s generator creates the source code for the corresponding mutable class and any related extension methods. It likely uses the record’s metadata (property names and types, namespace, etc.) to fill in a code template. This generation step is incremental per record – if you add or modify one annotated record, only that record’s generated code is affected.

4. **Output Integration:** The Roslyn compiler then “adds” the generated source to the compilation. The mutable classes and extension methods become part of your assembly, just as if you had written them by hand. They are invisible in your source code (except perhaps via IntelliSense or the Visual Studio Analyzers node), but they get compiled and you can use them in your code naturally.

Because this is done at compile time, **there is no runtime reflection or code generation happening**. All the heavy lifting is at build time. At runtime, your application is simply calling normal compiled methods.

**Why incremental?** Incremental generators (using Roslyn’s `IIncrementalGenerator` interface) are a refined form of source generators that were introduced to improve performance and reliability. They allow the generator to declare what information it depends on (like syntax nodes or semantic models) and cache results between compilations. For example, if you have 10 annotated records and you edit one property in one record, an incremental generator can detect that only one record’s generated output needs updating, and reuse the already-generated code for the other nine. This makes the build process much faster than a naive approach that regenerates code for all 10 records every time.

Mutty benefits from this by providing a smooth experience even in large projects – you should notice that adding Mutty doesn’t significantly slow down your builds, thanks to this incremental architecture.

## Wrapper Generation Process

For each annotated record, Mutty’s generation logic produces two main categories of output: the **mutable wrapper class** and the **helper extension methods** (like `Produce`, etc.). Let’s break down the content of these outputs:

- **Mutable Class Template:** Mutty generates a partial class with a name derived from the record (usually simply prefixing “Mutable”). The generator uses the record’s definition to determine what fields/properties to include. For each property in the record:
    - It determines the type. If the type is one of the record types marked for mutation, it will use the corresponding mutable type. If it’s an `ImmutableList<T>` or similar known immutable collection, it will translate it to a `List<TMutable>` or `List<T>` accordingly. Otherwise, it uses the type as-is (for value types or types that aren’t handled by Mutty).
    - It writes a public auto-property in the mutable class with that type and the same name.
    - In the constructor of the mutable class, it assigns each of those properties from the original record. If conversion is needed (for nested records or collections), it calls the appropriate helper (`AsMutable` or implicit cast).
    - It writes the `Build()` method, which creates the new immutable record. This often uses the expression `return _record with { ... }`, filling in the changed properties. The generator knows which properties are collections and calls `.ToImmutable()` on them, and which are mutable records and lets implicit conversion handle them. The `_record` is typically the original record stored to preserve any values that weren’t changed (so that the `with` expression can copy unchanged parts).
    - It adds the implicit operators for conversion both ways, as described in the API section.

  The resulting class is valid C# source code that gets compiled along with your code.

- **Extension Methods and Helpers:** Mutty also generates some static extension methods to improve the developer experience:
    - For each record, an extension `Produce(this RecordType, Action<MutableRecordType>)` is generated (or similarly named method) so you can call it on instances of the record. Internally, this method likely calls `CreateDraft`, passes it to the lambda, then calls `FinishDraft` – or does equivalent inline logic – to produce the new record.
    - If configured, it may generate `CreateDraft(this RecordType)` and `FinishDraft(this MutableRecordType)` extensions as well. These would be straightforward: `CreateDraft` does `=> (MutableRecordType)record;` using the implicit conversion, and `FinishDraft` does `=> (RecordType)mutable;`. This gives the two-step mutation ability.
    - `AsMutable` and `ToImmutable` for collections might be generated either generally or for specific element types. Often, source generators either generate a generic method that can handle any `ImmutableList<T>` where T is an annotated record (using some runtime checks or simply assuming the implicit conversion exists for T), or generate specific overloads for each needed T. It’s possible Mutty generates a generic implementation using LINQ: e.g., `public static List<TMutable> AsMutable<T,TMutable>(this ImmutableList<T> list) ...` but more likely it explicitly knows about each relevant T from your records and outputs a method for each (to keep it simple and fully compile-time typed). In any case, these methods use standard library calls like `ToList()` and `ToImmutableList()` to do the conversion, inserting conversions for each element if necessary.
    - All these helpers are typically put in a static class (maybe something like `MuttyExtensions` or even in a nested static class inside the generated output for organizational purposes). They are marked as `partial` or in a specific namespace so as not to collide with other code.

- **Conditional Generation:** The source generator likely includes logic to avoid generating duplicate helpers. For example, it should generate `AsMutable`/`ToImmutable` extension for a given collection type only once, even if multiple records use the same collection type. Similarly, if multiple records are annotated, it ensures each gets its own `Produce`/`CreateDraft`/`FinishDraft`, but those are distinct by parameter types, so that’s fine. Mutty might also check if a record is generic or has other unusual characteristics and either handle those or ignore them (e.g., it might not support generic records out-of-the-box, depending on implementation complexity).

- **Error Handling:** If you misuse the attribute (say, put `[MutableGeneration]` on something that’s not a record), Mutty’s generator might emit a compile-time diagnostic (warning or error). The architecture of a source generator allows it to report diagnostics. For instance, it could warn you if a record’s property is of an unsupported type for which it can’t generate a wrapper (though in most cases it can always fall back to treating it as an immutable black box). Checking Mutty’s documentation or source code would clarify this, but it’s good to be aware that some edge cases might not be handled and could result in a compile-time message.

## Example of Generated Code (Simplified)

To illustrate, let’s use a very simple record and show roughly what Mutty generates:

**Input (your code):**
```C#
[MutableGeneration]
public record Person(string Name, int Age);
```

**Output (generated code):**
```C#
public partial class MutablePerson
{
    private Person _record;
    
    public MutablePerson(Person record)
    {
        _record = record;
        Name = record.Name;
        Age = record.Age;
    }

    public Person Build()
    {
        // Produce a new Person record with updated properties
        return _record with 
        {
            Name = this.Name,
            Age  = this.Age
        };
    }

    public string Name { get; set; }
    public int Age { get; set; }

    public static implicit operator MutablePerson(Person record)
        => new MutablePerson(record);
    
    public static implicit operator Person(MutablePerson mutable)
        => mutable.Build();
}

public static class PersonExtensions
{
    public static Person Produce(
        this Person record, Action<MutablePerson> mutator)
    {
        var draft = new MutablePerson(record);
        mutator(draft);
        return draft.Build();
    }
    
    public static MutablePerson CreateDraft(this Person record)
        => new MutablePerson(record);
    
    public static Person FinishDraft(this MutablePerson draft)
        => draft.Build();
}
```

*(Note: The above is a conceptual simplification. Actual generated code might differ in naming or minor details, but it captures the essence.)*

The architecture is such that all of this is generated behind the scenes when you compile. You’ll see a reference to Mutty’s analyzer in your build output indicating it added sources. If using an IDE like Visual Studio, you can often find the generated files under **Analyzers** or in the Intermediate Output directory.

## Benefits and Trade-offs

- **No Runtime Penalty:** Because Mutty operates at compile time, there’s no reflection or code generation happening during runtime. You pay a small cost at build time (in exchange for writing far less code yourself), and at runtime your code is just normal compiled code using lists and object initializations.

- **Compile-time Safety:** The generated code is strongly typed. If Mutty generates something inconsistent with your records (which would be a bug in Mutty), the compiler would catch it. Also, if you remove or change a record/property, the generator will adapt or the compiler will show an error if something doesn’t match. This is safer than approaches that rely on string property names or runtime decisions.

- **Learning Curve:** The architecture means you, as a user, don’t see the code you’re calling. Developers should trust the generator or inspect the generated code if curious. Once comfortable, using Mutty feels natural, but initially one might wonder “where is this `Produce` method coming from?”. Understanding that it’s generated by the source generator (as explained here) helps demystify that.

- **Roslyn Integration:** Mutty’s incremental generator likely uses Roslyn’s `GeneratorExecutionContext` or newer `IncrementalGeneratorInitializationContext` to register its steps. If you ever looked into Mutty’s source code (since it’s Apache 2.0 licensed, you can), you’d find code that constructs source text strings or uses syntax factory APIs to output classes. The architecture section in Mutty’s docs (this section) abstracts those details and focuses on the outcome, but it’s built on standard Roslyn features.

In summary, Mutty’s architecture follows the modern source generator pattern: you annotate your code, and the generator produces new code at compile time to extend your program. It leverages incremental generation for efficiency. The result is that you write minimal boilerplate (just an attribute) and get a lot of code generated for you, integrated as if you wrote it yourself. This design encapsulates complex logic (like deep cloning of records) into a compile-time tool, keeping your runtime lean and your source code clean.
