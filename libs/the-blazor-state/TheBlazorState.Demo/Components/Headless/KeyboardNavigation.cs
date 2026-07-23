namespace TheBlazorState.Demo.Components.Headless;

public static class KeyboardNavigation
{
    public static int HandleArrowNavigation(string key, int currentIndex, int itemCount, bool vertical = true)
    {
        var prev = vertical ? "ArrowUp" : "ArrowLeft";
        var next = vertical ? "ArrowDown" : "ArrowRight";

        return key switch
        {
            _ when key == next => (currentIndex + 1) % itemCount,
            _ when key == prev => (currentIndex - 1 + itemCount) % itemCount,
            "Home" => 0,
            "End" => itemCount - 1,
            _ => currentIndex
        };
    }

    public static bool IsActivationKey(string key) => key is "Enter" or " ";
}
