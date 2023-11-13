using System;

namespace ObjectCalisthenics.Sample;

public class ExampleOC0002
{
    private void TestMethod()
    {
        var now = DateTime.Now;
        if (now.Second % 2 == 0)
        {
            Console.WriteLine("Yes, we can.");
        }
        // OC0002: 'else' keyword detected in method 'TestMethod'
        else
        {
            Console.WriteLine("No, we can't.");
        }
    }
}