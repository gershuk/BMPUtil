namespace SimpleBmpUtil.Interpreter;

public class BmpEditorInterpreter
{
    private static readonly Lazy<BmpEditorInterpreter> _defaultInterpreter;
    private readonly Dictionary<string, IBitmap> _bitmaps;
    private readonly RootCommand _rootCommand;

    public static BmpEditorInterpreter DefaultInterpreter => _defaultInterpreter.Value;
    public IReadOnlyDictionary<string, IBitmap> Bitmaps => _bitmaps;

    static BmpEditorInterpreter() => _defaultInterpreter = new(static () => new(CommandFactory.MakeCreateBmpCommand,
                                                                                CommandFactory.MakeReadCommand,
                                                                                CommandFactory.MakeSaveCommand,
                                                                                CommandFactory.MakeRotateCommand,
                                                                                CommandFactory.MakeMirrorCommand,
                                                                                CommandFactory.MakeGetBmpListCommand,
                                                                                CommandFactory.MakeUnloadCommand,
                                                                                CommandFactory.MakeInversColorsCommand,
                                                                                CommandFactory.MakeSetPixelColorCommand,
                                                                                CommandFactory.MakeGetPixelColorCommand));

    public BmpEditorInterpreter(params Func<Dictionary<string, IBitmap>, Command>[] fabricators)
    {
        _bitmaps = [];
        _rootCommand = [];

        foreach (var rootCommandGenerator in fabricators)
            _rootCommand.Add(rootCommandGenerator(_bitmaps));
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested)
        {
            _ = await _rootCommand.InvokeAsync(Console.ReadLine() ?? string.Empty);
        }
    }
}