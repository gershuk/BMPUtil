﻿namespace SimpleBmpUtil.Interpreter;

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

    public static unsafe Command MakeCreateBmpCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("create", "Create bmp in pool");

        command.SetHandler((bmpName, bitsPerPixel, colorsUsed, width, height, xPixelsPerMeter, yPixelsPerMeter, palette) =>
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
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true },
        new Option<ushort>(["-b", "--bits-per-pixel"], "Bits per pixel in data array") { IsRequired = true },
        new Option<uint>(["-c", "--colors-used"], "Color used in bmp"),
        new Option<int>(["-w", "--width"], "Width") { IsRequired = true },
        new Option<int>(["-h", "--height"], "Height") { IsRequired = true },
        new Option<int>(["-xP", "--x-pixels-per-meter"], "Horizontal pixels per meter"),
        new Option<int>(["-yP", "--y-pixels-per-meter"], "Vertical pixels per meter"),
        new Option<uint[]>(["-p", "--palette"], "Palette"));

        return command;
    }

    public static Command MakeGetBmpListCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("bmpList", "Get names in pool");

        command.SetHandler(() =>
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
        });

        return command;
    }

    public static Command MakeGetPixelColorCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("getPixelColor", "Get pixel color");

        command.SetHandler((bmpName, width, height) =>
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
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true },
        new Option<int>(["-w", "--width"], "Width") { IsRequired = true },
        new Option<int>(["-h", "--height"], "Height") { IsRequired = true });

        return command;
    }

    public static Command MakeInversColorsCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("inversColors", "Invers colors");

        command.SetHandler<string>(bmpName =>
        {
            try
            {
                bitmaps[bmpName].InversColors();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine(nameof(FileNotFoundException));
            }
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true });

        return command;
    }

    public static Command MakeMirrorCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("mirror", "Mirror the image");

        command.SetHandler((bmpName, axis) =>
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
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true },
        new Option<AxisMirror>(["-a", "--axis"], "Rotation direction") { IsRequired = true });

        return command;
    }

    public static Command MakeReadCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("read", "Read bmp from file");

        command.SetHandler((bmpName, filePath) =>
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
        },
        new Option<string>(["-n", "--bmp-name"], "The name that is assigned when creating the BMP") { IsRequired = true },
        new Option<string>(["-p", "--file-path"], "File path") { IsRequired = true });

        return command;
    }

    public static Command MakeRotateCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("rotate", "Rotate the image");

        command.SetHandler((bmpName, direction) =>
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
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true },
        new Option<RotationDirection>(["-d", "--direction"], "Rotation direction and angle") { IsRequired = true });

        return command;
    }

    public static Command MakeSaveCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("save", "Save bmp to file");

        command.SetHandler((bmpName, filePath) =>
        {
            try
            {
                BitmapFactory.WriteBmp(filePath, bitmaps[bmpName]);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(nameof(KeyNotFoundException));
            }
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true },
        new Option<string>(["-p", "--file-path"], "File path") { IsRequired = true });

        return command;
    }

    public static Command MakeSetPixelColorCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("setPixelColor", "Set pixel color");

        command.SetHandler((bmpName, width, height, b, g, r) =>
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
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true },
        new Option<int>(["-w", "--width"], "Width") { IsRequired = true },
        new Option<int>(["-h", "--height"], "Height") { IsRequired = true },
        new Option<byte>(["-b", "--blue"], "Blue component value") { IsRequired = true },
        new Option<byte>(["-g", "--green"], "Green component value") { IsRequired = true },
        new Option<byte>(["-r", "--red"], "Red component value") { IsRequired = true });

        return command;
    }

    public static Command MakeUnloadCommand(Dictionary<string, IBitmap> bitmaps)
    {
        Command command = new("unload", "Delete bmp from pool");

        command.SetHandler(bmpName =>
        {
            try
            {
                _ = bitmaps.Remove(bmpName);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(nameof(KeyNotFoundException));
            }
        },
        new Option<string>(["-n", "--bmp-name"], "Name in image pool") { IsRequired = true });

        return command;
    }
}