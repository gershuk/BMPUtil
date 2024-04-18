namespace SimpleBmpUtil.Interpreter;

public static class Program
{
    public static async Task Main()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        await BmpEditorInterpreter.DefaultInterpreter.Run(cancellationToken);
    }
}