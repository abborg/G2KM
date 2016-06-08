namespace RawInput
{
    public enum MouseClickState
    {
        Mouse1Down = 1,
        Mouse1Up = 2,
        Mouse2Down = 4,
        Mouse2Up = 8,
        Mouse3Down = 16,
        Mouse3Up = 32,
        Mouse4Down = 64,
        Mouse4Up = 128,
        Mouse5Down = 256,
        Mouse5Up = 512,
        MouseWheel = 1024,
    }

    public enum MouseLocationState
    {
        MouseMoveRelative = 0,
        MouseMoveAbsolute = 1,
        MouseVirtualDesktop = 2,
        MouseAttributesChanged = 4,
    }
}