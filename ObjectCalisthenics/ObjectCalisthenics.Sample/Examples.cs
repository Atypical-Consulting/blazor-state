// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;

namespace ObjectCalisthenics.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    // some instance variables
    private int _intNumber = 42;
    private string _string = "Hello World";
    private bool _bool = true;
    private List<string> _list = new() { "Hello", "World" };
    
    public class MyCompanyClass // Try to apply quick fix using the IDE.
    {
    }
    
    // OC0009: NoGettersSettersAnalyzer
    public int IntNumber
    {
        // works for getters
        get => _intNumber;
        // works for setters
        set => _intNumber = value;
    }
    
    // works for auto properties too
    public int IntProperty { get; set; } // Try to apply quick fix using the IDE.

    public void ToStars()
    {
        var spaceship = new Spaceship();
        spaceship.SetSpeed(300000000); // Invalid value, it should be highlighted.
        spaceship.SetSpeed(42);
    }
    
    // OC0001 - MethodIndentationAnalyzer
    // method with more than one level of indentation
    private void TestMethod()
    {
        for (var i = 0; i < 10; i++)
        {
            if (i == 5)
            {
                System.Console.WriteLine("More than one level of indentation");
            }
        }
    }
    
    private void TestMethod2()
    {
        bool randomBool = Random.Shared.Next() % 2 == 0;
        if (randomBool)
        {
            Console.WriteLine($"Random bool is {randomBool}, string is {_string}");
        }
        else
        {
            Console.WriteLine("Random bool is false");
        }
    }
    
    private string TestMethod3()
    {
        return _string;
    }
}