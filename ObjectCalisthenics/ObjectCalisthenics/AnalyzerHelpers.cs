namespace ObjectCalisthenics;

public static class AnalyzerHelpers
{
    public static LocalizableResourceString CreateLocalizableResourceString(string resourceName)
        => new(resourceName, Resources.ResourceManager, typeof(Resources));
}