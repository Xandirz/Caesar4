public struct PlacementResult
{
    public bool ok;
    public string message;
    public MessagePopUp.Style style;

    public static PlacementResult Ok()
    {
        return new PlacementResult { ok = true };
    }

    public static PlacementResult Fail(string msg, MessagePopUp.Style style = MessagePopUp.Style.Error)
    {
        return new PlacementResult
        {
            ok = false,
            message = msg,
            style = style
        };
    }
}