namespace Payroll.Desktop.Bootstrapping;

public static class StartupArguments
{
    private static string[] _args = [];

    public static void Set(string[] args)
    {
        _args = args ?? [];
    }

    public static IReadOnlyList<string> Current => _args;
}
