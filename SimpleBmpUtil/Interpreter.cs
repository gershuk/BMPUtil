using System.CommandLine;
using System.CommandLine.Invocation;

namespace SimpleBmpUtil
{
    #region Interpreter

    public static class CommandFactory
    {
        private enum AxisMirror
        {
            Vertical = 0,
            Horizontal = 1,
        }

        private enum RotationDirection
        {
            Left90 = 0,
            Right90 = 1,
            Rotate180 = 2,
        }

        public static unsafe RootCommand MakeCreateBmpCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Create bmp in pool")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
                new Option<ushort>(new[] { "-b", "--bits-per-pixel" }, "Bits per pixel in data array") { IsRequired = true },
                new Option<uint>(new[] { "-c", "--colors-used" }, "Color used in bmp"),
                new Option<int>(new[] { "-w", "--width" }, "Width") { IsRequired = true },
                new Option<int>(new[] { "-h", "--height" }, "Height") { IsRequired = true },
                new Option<int>(new[] { "-xP", "--x-pixels-per-meter" }, "Horizontal pixels per meter"),
                new Option<int>(new[] { "-yP", "--y-pixels-per-meter" }, "Vertical pixels per meter"),
                new Option<uint[]>(new[] { "-p", "--palette" }, "Palette"),
            };

            rootCommand.Handler = CommandHandler.Create<string, ushort, uint, int, int, int, int, uint[]>((bmpName, bitsPerPixel, colorsUsed, width, height, xPixelsPerMeter, yPixelsPerMeter, palette) =>
            {
                try
                {
                    if (bitsPerPixel is 16 && colorsUsed is 0)
                        colorsUsed = 32768;

                    if (colorsUsed is not (0 or 32768 or 65536))
                    {
                        throw new WrongBmpFormatException();
                    }

                    var convertedPalette = new Pixel32[palette?.Length ?? 0];
                    if (palette != null && palette.Length > 0)
                    {
                        var paletteHandle = GCHandle.Alloc(palette, GCHandleType.Pinned);
                        var palettePtr = paletteHandle.AddrOfPinnedObject().ToPointer();
                        var convertedPaletteHandle = GCHandle.Alloc(convertedPalette, GCHandleType.Pinned);
                        var convertedPalettePtr = convertedPaletteHandle.AddrOfPinnedObject().ToPointer();
                        Buffer.MemoryCopy(palettePtr, convertedPalettePtr, Buffer.ByteLength(palette), Buffer.ByteLength(palette));

                        paletteHandle.Free();
                        convertedPaletteHandle.Free();
                    }

                    bitmaps.Add(bmpName, BitmapFactory.CreateBitmap(bitsPerPixel, colorsUsed, width, height, xPixelsPerMeter, yPixelsPerMeter, palette: convertedPalette));
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("KeyAlreadyExistsException");
                }
                catch (WrongBmpFormatException)
                {
                    Console.WriteLine(nameof(WrongBmpFormatException));
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeGetBmpListCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Get names in pool")
            {
                Handler = CommandHandler.Create(() =>
                {
                    if (bitmaps.Keys.Count is 0)
                    {
                        Console.WriteLine("Pool is empty");
                    }
                    else
                    {
                        Console.WriteLine("Bmps in pool:");
                        foreach (var commandName in bitmaps.Keys)
                            Console.WriteLine($"  {commandName}");
                    }
                })
            };

            return rootCommand;
        }

        public static RootCommand MakeGetPixelColorCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Delete bmp from pool")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
                new Option<string>(new[] { "-w", "--width" }, "Width") { IsRequired = true },
                new Option<string>(new[] { "-h", "--height" }, "Height") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string, int, int>((bmpName, width, height) =>
            {
                try
                {
                    Console.WriteLine(bitmaps[bmpName] is IBitmapWithoutPalette bitmapWithoutPalette ?
                    $"b:{bitmapWithoutPalette[width, height].Blue} g:{bitmapWithoutPalette[width, height].Green} r:{bitmapWithoutPalette[width, height].Red}" :
                    throw new NotSupportedException());
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine(nameof(IndexOutOfRangeException));
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine(nameof(NotSupportedException));
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeInversColorsCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Delete bmp from pool")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string>(bmpName =>
            {
                try
                {
                    bitmaps[bmpName].InversColors();
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine(nameof(FileNotFoundException));
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeMirrorCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Mirror the image")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
                new Option<AxisMirror>(new[] { "-a", "--axis" }, "Rotation direction") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string, AxisMirror>((bmpName, axis) =>
            {
                try
                {
                    switch (axis)
                    {
                        case AxisMirror.Vertical:
                        bitmaps[bmpName].VerticalMirror();
                        break;

                        case AxisMirror.Horizontal:
                        bitmaps[bmpName].HorizontalMirror();
                        break;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine(nameof(KeyNotFoundException));
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeReadCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Read bmp from file")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "The name that is assigned when creating the BMP") { IsRequired = true },
                new Option<string>(new[] { "-p", "--file-path" }, "File path") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string, string>((bmpName, filePath) =>
            {
                try
                {
                    bitmaps.Add(bmpName, BitmapFactory.ReadBmpFromFile(filePath).bitmap);
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine(nameof(FileNotFoundException));
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("KeyAlreadyExistsException");
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeRotateCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Mirror the image")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
                new Option<RotationDirection>(new[] { "-d", "--direction" }, "Rotation direction and angle") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string, RotationDirection>((bmpName, direction) =>
            {
                try
                {
                    switch (direction)
                    {
                        case RotationDirection.Left90:
                        bitmaps[bmpName].RotateLeft90();
                        break;

                        case RotationDirection.Right90:
                        bitmaps[bmpName].RotateRight90();
                        break;

                        case RotationDirection.Rotate180:
                        bitmaps[bmpName].Rotate180();
                        break;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine(nameof(KeyNotFoundException));
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeSaveCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Save bmp to file")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
                new Option<string>(new[] { "-p", "--file-path" }, "File path") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string, string>((bmpName, filePath) =>
            {
                try
                {
                    BitmapFactory.WriteBmp(filePath, bitmaps[bmpName]);
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine(nameof(KeyNotFoundException));
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeSetPixelColorCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Delete bmp from pool")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
                new Option<int>(new[] { "-w", "--width" }, "Width") { IsRequired = true },
                new Option<int>(new[] { "-h", "--height" }, "Height") { IsRequired = true },
                new Option<byte>(new[] { "-b", "--blue" }, "Blue component value") { IsRequired = true },
                new Option<byte>(new[] { "-g", "--green" }, "Green component value") { IsRequired = true },
                new Option<byte>(new[] { "-r", "--red" }, "Red component value") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string, int, int, byte, byte, byte>((bmpName, width, height, b, g, r) =>
            {
                try
                {
                    if (bitmaps[bmpName] is IBitmapWithoutPalette bitmapWithoutPalette)
                    {
                        var pixel = bitmapWithoutPalette[width, height];
                        (pixel.Blue, pixel.Green, pixel.Red) = (b, g, r);
                        bitmapWithoutPalette[width, height] = pixel;
                    }
                    else
                    {
                        Console.WriteLine(nameof(NotSupportedException));
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine(nameof(IndexOutOfRangeException));
                }
                catch (ColorValueOutOfRangeException)
                {
                    Console.WriteLine(nameof(ColorValueOutOfRangeException));
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine(nameof(KeyNotFoundException));
                }
            });

            return rootCommand;
        }

        public static RootCommand MakeUnloadCommand(Dictionary<string, IBitmap> bitmaps)
        {
            RootCommand rootCommand = new("Delete bmp from pool")
            {
                new Option<string>(new[] { "-n", "--bmp-name" }, "Name in image pool") { IsRequired = true },
            };

            rootCommand.Handler = CommandHandler.Create<string>(bmpName =>
            {
                try
                {
                    bitmaps.Remove(bmpName);
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine(nameof(KeyNotFoundException));
                }
            });

            return rootCommand;
        }
    }

    public class Interpreter
    {
        private static readonly Lazy<Interpreter> _defaultInterpreter;
        private readonly Dictionary<string, IBitmap> _bitmaps;

        private readonly Dictionary<string, RootCommand> _commands;

        public static Interpreter DefaultInterpreter => _defaultInterpreter.Value;
        public IReadOnlyDictionary<string, IBitmap> Bitmaps => _bitmaps;

        static Interpreter() => _defaultInterpreter = new(static () => new(("create", CommandFactory.MakeCreateBmpCommand),
                                                                           ("read", CommandFactory.MakeReadCommand),
                                                                           ("save", CommandFactory.MakeSaveCommand),
                                                                           ("rotate", CommandFactory.MakeRotateCommand),
                                                                           ("mirror", CommandFactory.MakeMirrorCommand),
                                                                           ("bmpList", CommandFactory.MakeGetBmpListCommand),
                                                                           ("unload", CommandFactory.MakeUnloadCommand),
                                                                           ("inversColors", CommandFactory.MakeInversColorsCommand),
                                                                           ("setPixelColor", CommandFactory.MakeSetPixelColorCommand),
                                                                           ("getPixelColor", CommandFactory.MakeGetPixelColorCommand)));

        public Interpreter(params (string name, RootCommand rootCommand)[] commands)
        {
            _bitmaps = new();
            _commands = new(commands.Length);

            foreach (var (name, rootCommand) in commands)
                RegisterCommand(name, rootCommand);
        }

        public Interpreter(params (string name, Func<Dictionary<string, IBitmap>, RootCommand> rootCommandGenerator)[] fabricators)
        {
            _bitmaps = new();
            _commands = new(fabricators.Length);

            foreach (var (name, rootCommandGenerator) in fabricators)
                RegisterCommand(name, rootCommandGenerator(_bitmaps));
        }

        public void RegisterCommand(string name, RootCommand rootCommand) => _commands.Add(name, rootCommand);

        public void Run()
        {
            while (true)
            {
                var strs = Console.ReadLine()?.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (strs != null && strs.Length > 0 && _commands.TryGetValue(strs[0], out var command))
                {
                    if (command.Invoke(strs.Length >= 2 ? strs[1] : string.Empty) is 0)
                        Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Commands:");
                    foreach (var commandName in _commands.Keys)
                        Console.WriteLine($"  {commandName}");
                    Console.WriteLine();
                }
            }
        }
    }

    #endregion Interpreter
}
