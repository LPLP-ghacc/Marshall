namespace MarshallApp.Services;

public static class MarshallExtension
{
    public static void Log(this string message)
    {
        MainWindow.Instance?.Log(message);
    }
}