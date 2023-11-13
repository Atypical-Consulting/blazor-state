using System;
// ReSharper disable UnusedMember.Local

namespace ObjectCalisthenics.Sample;

public class ExampleOC0001
{
    // OC0001: Method 'TestMethod' contains more than one level of indentation
    private void TestMethod()
    {
        for (var i = 0; i < 10; i++)
        {
            if (i == 5)
            {
                Console.WriteLine(@"More than one level of indentation");
            }
        }
    }
}