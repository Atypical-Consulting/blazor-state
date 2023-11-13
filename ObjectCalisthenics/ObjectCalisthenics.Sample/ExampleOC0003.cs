#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace ObjectCalisthenics.Sample;

public record CustomType(int Value);

public class ExampleOC0003
{
    // OC0003: 
    private int _a = 5;
    private string _b = "hello";
    private bool _c = true;
    private byte _d = 0;
    private char _e = 'a';
    private decimal _f = 0.0m;
    private double _g = 0.0;
    private float _h = 0.0f;
    private long _i = 0;
    private sbyte _j = 0;
    private CustomType _k = new CustomType(5);
}