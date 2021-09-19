#nullable enable

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

Interpreter.DefaultInterpreter.Run();

#region PixelStructs

public interface IPixel
{
    public byte Blue { get; set; }
    public byte Green { get; set; }
    public byte Red { get; set; }

    public void Invers();
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct Pixel16
{
    [FieldOffset(0x00)]
    private ushort _data;

    public enum ComponentId
    {
        B = 0,
        G = 1,
        R = 2,
    }

    public enum SchemeType
    {
        FiveFiveFive = 0,
        FiveSixFive = 1,
    }

    private static int[,] masks = { { 0x001F, 0x03E0, 0x7C00 }, { 0x001F, 0x07E0, 0xF800 } };

    private static int[,] offsets = { { 0, 5, 10 }, { 0, 5, 11 } };

    public byte GetColorComponent(ComponentId id, SchemeType type) => (byte)((_data & masks[(int)type, (int)id]) >> offsets[(int)type, (int)id]);

    public ushort SetColorComponent(byte value, ComponentId id, SchemeType type) => (type is SchemeType.FiveFiveFive || id is ComponentId.B or ComponentId.R) && value < 32 ||
        type is SchemeType.FiveSixFive && id is ComponentId.R && value < 64 ?
        _data = (ushort)((_data & masks[(int)type, 0] + masks[(int)type, 1] + masks[(int)type, 2] - masks[(int)type, (int)id]) +
        (value << offsets[(int)type, (int)id]) & masks[(int)type, (int)id]) : throw new ColorValueOutOfRangeException();

    public void Invers(SchemeType type)
    {
        SetColorComponent((byte)(0x001F - GetColorComponent(ComponentId.B, type)), ComponentId.B, type);
        SetColorComponent((byte)(type switch
        {
            SchemeType.FiveFiveFive => 0x001F,
            SchemeType.FiveSixFive => 0x003F,
            _ => throw new NotImplementedException(),
        } - GetColorComponent(ComponentId.G, type)), ComponentId.G, type);
        SetColorComponent((byte)(0x001F - GetColorComponent(ComponentId.R, type)), ComponentId.R, type);
    }
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct Pixel16FiveFiveFive : IPixel
{
    [FieldOffset(0x00)]
    public Pixel16 _pixel16;

    public byte Blue
    {
        get => _pixel16.GetColorComponent(Pixel16.ComponentId.B, Pixel16.SchemeType.FiveFiveFive);
        set => _pixel16.SetColorComponent(value, Pixel16.ComponentId.B, Pixel16.SchemeType.FiveFiveFive);
    }

    public byte Green
    {
        get => _pixel16.GetColorComponent(Pixel16.ComponentId.G, Pixel16.SchemeType.FiveFiveFive);
        set => _pixel16.SetColorComponent(value, Pixel16.ComponentId.G, Pixel16.SchemeType.FiveFiveFive);
    }

    public byte Red
    {
        get => _pixel16.GetColorComponent(Pixel16.ComponentId.R, Pixel16.SchemeType.FiveFiveFive);
        set => _pixel16.SetColorComponent(value, Pixel16.ComponentId.R, Pixel16.SchemeType.FiveFiveFive);
    }

    public void Invers() => _pixel16.Invers(Pixel16.SchemeType.FiveFiveFive);
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct Pixel16FiveSixFive : IPixel
{
    [FieldOffset(0x00)]
    public Pixel16 _pixel16;

    public byte Blue
    {
        get => _pixel16.GetColorComponent(Pixel16.ComponentId.B, Pixel16.SchemeType.FiveSixFive);
        set => _pixel16.SetColorComponent(value, Pixel16.ComponentId.B, Pixel16.SchemeType.FiveSixFive);
    }

    public byte Green
    {
        get => _pixel16.GetColorComponent(Pixel16.ComponentId.G, Pixel16.SchemeType.FiveSixFive);
        set => _pixel16.SetColorComponent(value, Pixel16.ComponentId.G, Pixel16.SchemeType.FiveSixFive);
    }

    public byte Red
    {
        get => _pixel16.GetColorComponent(Pixel16.ComponentId.R, Pixel16.SchemeType.FiveSixFive);
        set => _pixel16.SetColorComponent(value, Pixel16.ComponentId.R, Pixel16.SchemeType.FiveSixFive);
    }

    public void Invers() => _pixel16.Invers(Pixel16.SchemeType.FiveSixFive);
}

[StructLayout(LayoutKind.Explicit, Size = 3)]
public struct Pixel24 : IPixel
{
    [FieldOffset(0x00)]
    private byte _blue;

    [FieldOffset(0x01)]
    private byte _green;

    [FieldOffset(0x02)]
    private byte _red;

    public byte Blue { get => _blue; set => _blue = value; }
    public byte Green { get => _green; set => _green = value; }
    public byte Red { get => _red; set => _red = value; }

    public void Invers()
    {
        _blue = (byte)(255 - _blue);
        _green = (byte)(255 - _green);
        _red = (byte)(255 - _red);
    }
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct Pixel32 : IPixel
{
    [FieldOffset(0x00)]
    private Pixel24 _pixel24;

    [FieldOffset(0x03)]
    private byte _reserved;

    public byte Blue { get => _pixel24.Blue; set => _pixel24.Blue = value; }
    public byte Green { get => _pixel24.Green; set => _pixel24.Green = value; }
    public byte Red { get => _pixel24.Red; set => _pixel24.Red = value; }
    public byte Reserved { get => _reserved; set => _reserved = value; }

    public void Invers() => _pixel24.Invers();
}

public class ColorValueOutOfRangeException : Exception
{
    protected ColorValueOutOfRangeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ColorValueOutOfRangeException()
    {
    }

    public ColorValueOutOfRangeException(string? message) : base(message)
    {
    }

    public ColorValueOutOfRangeException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

#endregion PixelStructs

#region BitmapTypes

public interface IBitmap
{
    public byte[] AlignedData { get; }
    public int Height { get; }
    public int LineBitsLength { get; }
    public IReadOnlyList<Pixel32> Palette { get; }
    public int Width { get; }

    public void HorizontalMirror();

    public void InversColors();

    public InfoHeader MakeInfoHeader();

    public void Rotate180();

    public void RotateLeft90();

    public void RotateRight90();

    public void VerticalMirror();
}

public interface IBitmapWithoutPalette : IBitmap
{
    public IPixel this[int w, int h] { get; set; }

    public IPixel this[int index]
    {
        get => this[index / Width, index % Height];
        set => this[index / Width, index % Height] = value;
    }
}

public interface IBitmapWithPalette : IBitmap
{
    public byte this[int w, int h] { get; set; }

    public byte this[int index]
    {
        get => this[index / Width, index % Height];
        set => this[index / Width, index % Height] = value;
    }
}

public sealed class BitmapWithoutPalette<TPixel> : IBitmapWithoutPalette where TPixel : struct, IPixel
{
    private Pixel32[] _palette;

    private TPixel[] _pixels;

    private int BytesForLineAlignment =>
            Marshal.SizeOf<TPixel>() switch
            {
                1 => Width * 3 % 4,
                2 => Width * 2 % 4,
                3 => Width % 4,
                4 => 0,
                _ => throw new ArgumentOutOfRangeException()
            };

    public byte[] AlignedData
    {
        get
        {
            unsafe
            {
                var data = new byte[LineBitsLength / 8 * Height];
                var handle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
                var _pixelsPtr = handle.AddrOfPinnedObject().ToPointer();

                fixed (void* dataPtr = data)
                {
                    var linePtr = dataPtr;
                    for (var h = 0; h < Height; ++h)
                    {
                        Buffer.MemoryCopy(_pixelsPtr, linePtr, Width * Marshal.SizeOf<TPixel>(), Width * Marshal.SizeOf<TPixel>());
                        linePtr = (byte*)linePtr + LineBitsLength / 8;
                        _pixelsPtr = (byte*)_pixelsPtr + Width * Marshal.SizeOf<TPixel>();
                    }
                }

                handle.Free();

                return data;
            }
        }
    }

    public int Height { get; set; }
    public unsafe int LineBitsLength => (Width * Marshal.SizeOf<TPixel>() + BytesForLineAlignment) * 8;
    public IReadOnlyList<Pixel32> Palette => _palette;
    public int Width { get; set; }
    public int XPixelsPerMeter { get; set; }

    public int YPixelsPerMeter { get; set; }

    private BitmapWithoutPalette(int width, int height, Pixel32[] palette, int xPixelsPerMeter = 0, int yPixelsPerMeter = 0) =>
        (Width, Height, _palette, XPixelsPerMeter, YPixelsPerMeter) = (width, height, palette, xPixelsPerMeter, yPixelsPerMeter);

    public BitmapWithoutPalette(TPixel[] pixels, int width, int height, Pixel32[] palette, int xPixelsPerMeter = 0, int yPixelsPerMeter = 0) :
        this(width, height, palette, xPixelsPerMeter, yPixelsPerMeter) =>
        _pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));

    public unsafe BitmapWithoutPalette(byte[] data, int width, int height, Pixel32[] palette, int xPixelsPerMeter = 0, int yPixelsPerMeter = 0) :
        this(width, height, palette, xPixelsPerMeter, yPixelsPerMeter)
    {
        _pixels = new TPixel[width * height];
        var handle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
        var _pixelsPtr = handle.AddrOfPinnedObject().ToPointer();

        fixed (void* dataPtr = data)
        {
            var linePtr = dataPtr;
            for (var h = 0; h < height; ++h)
            {
                Buffer.MemoryCopy(linePtr, _pixelsPtr, width * Marshal.SizeOf<TPixel>(), width * Marshal.SizeOf<TPixel>());
                linePtr = (byte*)linePtr + LineBitsLength / 8;
                _pixelsPtr = (byte*)_pixelsPtr + width * Marshal.SizeOf<TPixel>();
            }
        }

        handle.Free();
    }

    IPixel IBitmapWithoutPalette.this[int w, int h] { get => _pixels[h * Width + w]; set => _pixels[h * Width + w] = (TPixel)value; }

    public TPixel this[int w, int h] { get => _pixels[h * Width + w]; set => _pixels[h * Width + w] = value; }

    private void SwapDimensions() => (Width, Height, XPixelsPerMeter, YPixelsPerMeter) = (Height, Width, YPixelsPerMeter, XPixelsPerMeter);

    public unsafe void HorizontalMirror()
    {
        for (var h = 0; h < Height; ++h)
        {
            Array.Reverse(_pixels, h * Width, Width);
        }
    }

    public void InversColors()
    {
        for (var i = 0; i < _pixels.Length; ++i)
        {
            _pixels[i].Invers();
        }
    }

    public InfoHeader MakeInfoHeader() =>
    new InfoHeader
    (
        (uint)(Marshal.SizeOf<InfoHeader>() + _palette.Length * Marshal.SizeOf<Pixel32>()),
        Width,
        Height,
        1,
        (ushort)(Marshal.SizeOf<TPixel>() * 8),
        0,
        0,
        XPixelsPerMeter,
        YPixelsPerMeter,
        0,
        0
    );

    public void Rotate180() => Array.Reverse(_pixels);

    public void RotateLeft90()
    {
        var newData = new TPixel[Width * Height];
        for (var h = 0; h < Height; ++h)
        {
            for (var w = 0; w < Width; ++w)
            {
                newData[h + Height * w] = this[w, Height - 1 - h];
            }
        }
        _pixels = newData;
        SwapDimensions();
    }

    public void RotateRight90()
    {
        var newData = new TPixel[Width * Height];
        for (var h = 0; h < Height; ++h)
        {
            for (var w = 0; w < Width; ++w)
            {
                newData[h + Height * w] = this[Width - 1 - w, h];
            }
        }
        _pixels = newData;
        SwapDimensions();
    }

    public unsafe void VerticalMirror()
    {
        var oldHandle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
        var oldPixelsPtr = oldHandle.AddrOfPinnedObject().ToPointer();

        var newPixels = new TPixel[Width * Height];
        var newHandle = GCHandle.Alloc(newPixels, GCHandleType.Pinned);
        var newPixelsPtr = newHandle.AddrOfPinnedObject().ToPointer();
        newPixelsPtr = (byte*)newPixelsPtr + (Height - 1) * Width * Marshal.SizeOf<TPixel>();

        for (var h = 0; h < Height; ++h)
        {
            Buffer.MemoryCopy(oldPixelsPtr, newPixelsPtr, Width * Marshal.SizeOf<TPixel>(), Width * Marshal.SizeOf<TPixel>());
            oldPixelsPtr = (byte*)oldPixelsPtr + Width * Marshal.SizeOf<TPixel>();
            newPixelsPtr = (byte*)newPixelsPtr - Width * Marshal.SizeOf<TPixel>();
        }

        oldHandle.Free();
        newHandle.Free();

        _pixels = newPixels;
    }
}

public class WrongBmpFormatException : Exception
{
    protected WrongBmpFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public WrongBmpFormatException()
    {
    }

    public WrongBmpFormatException(string? message) : base(message)
    {
    }

    public WrongBmpFormatException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

#endregion BitmapTypes

#region BmpHeader

[StructLayout(LayoutKind.Explicit, Size = 14)]
public readonly struct FileHeader
{
    [FieldOffset(0x02)]
    private readonly uint _fileSize;

    [FieldOffset(0x0A)]
    private readonly uint _offsetData;

    [FieldOffset(0x06)]
    private readonly ushort _reserved1;

    [FieldOffset(0x08)]
    private readonly ushort _reserved2;

    [FieldOffset(0x00)]
    private readonly ushort _type;

    public FileHeader(uint fileSize, uint offsetData, ushort reserved1 = 0, ushort reserved2 = 0, ushort type = 19778)
    {
        _fileSize = fileSize;
        _offsetData = offsetData;
        _reserved1 = reserved1;
        _reserved2 = reserved2;
        _type = type;
    }

    public uint FileSize { get => _fileSize; init => _fileSize = value; }
    public uint OffsetData { get => _offsetData; init => _offsetData = value; }
    public ushort Reserved1 { get => _reserved1; init => _reserved1 = value; }
    public ushort Reserved2 { get => _reserved2; init => _reserved2 = value; }
    public ushort Type { get => _type; init => _type = value; }
}

[StructLayout(LayoutKind.Explicit, Size = 40)]
public readonly struct InfoHeader
{
    [FieldOffset(0x00)]
    private readonly uint _structSize;

    [FieldOffset(0x04)]
    private readonly int _imageWidth;

    [FieldOffset(0x08)]
    private readonly int _imageHeight;

    [FieldOffset(0x0C)]
    private readonly ushort _planes;

    [FieldOffset(0x0E)]
    private readonly ushort _bitPerPixelCount;

    [FieldOffset(0x10)]
    private readonly uint _compression;

    [FieldOffset(0x14)]
    private readonly uint _sizeImage;

    [FieldOffset(0x18)]
    private readonly int _xPixelsPerMeter;

    [FieldOffset(0x1C)]
    private readonly int _ypixelsPerMeter;

    [FieldOffset(0x20)]
    private readonly uint _colorsUsed;

    [FieldOffset(0x24)]
    private readonly uint _colorsImportant;

    public InfoHeader(uint structSize,
                      int imageWidth,
                      int imageHeight,
                      ushort planes,
                      ushort bitPerPixelCount,
                      uint compression,
                      uint sizeImage,
                      int xPixelsPerMeter,
                      int ypixelsPerMeter,
                      uint colorsUsed,
                      uint colorsImportant)
    {
        _structSize = structSize;
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;
        _planes = planes;
        _bitPerPixelCount = bitPerPixelCount;
        _compression = compression;
        _sizeImage = sizeImage;
        _xPixelsPerMeter = xPixelsPerMeter;
        _ypixelsPerMeter = ypixelsPerMeter;
        _colorsUsed = colorsUsed;
        _colorsImportant = colorsImportant;
    }

    public InfoHeader GetRotatedHeader() =>
    new
    (
        _structSize,
        _imageHeight,
        _imageWidth,
        _planes,
        _bitPerPixelCount,
        _compression,
        _sizeImage,
        _ypixelsPerMeter,
        _xPixelsPerMeter,
        _colorsUsed,
        _colorsImportant
    );

    public uint StructSize { get => _structSize; init => _structSize = value; }
    public int ImageWidth { get => _imageWidth; init => _imageWidth = value; }
    public int ImageHeight { get => _imageHeight; init => _imageHeight = value; }
    public ushort Planes { get => _planes; init => _planes = value; }
    public ushort BitPerPixelCount { get => _bitPerPixelCount; init => _bitPerPixelCount = value; }
    public uint Compression { get => _compression; init => _compression = value; }
    public uint SizeImage { get => _sizeImage; init => _sizeImage = value; }
    public int XPixelsPerMeter { get => _xPixelsPerMeter; init => _xPixelsPerMeter = value; }
    public int YPixelsPerMeter { get => _ypixelsPerMeter; init => _ypixelsPerMeter = value; }
    public uint ColorsUsed { get => _colorsUsed; init => _colorsUsed = value; }
    public uint ColorsImportant { get => _colorsImportant; init => _colorsImportant = value; }
}

#endregion BmpHeader

#region BitmapFactory

public static class BitmapFactory
{
    public static IBitmap CreateBitmap(ushort bitPerPixel,
                                       uint colorsUsed,
                                       int width,
                                       int height,
                                       int xPixelsPerMeter,
                                       int yPixelsPerMeter,
                                       Pixel32[]? palette = null,
                                       byte[]? data = null) =>
    bitPerPixel switch
    {
        8 => throw new NotImplementedException(),
        16 or 24 or 32 =>
        (IBitmap)(Activator.CreateInstance(typeof(BitmapWithoutPalette<>).MakeGenericType(
        bitPerPixel switch
        {
            16 when colorsUsed is 32768 => typeof(Pixel16FiveFiveFive),
            16 when colorsUsed is 65536 => typeof(Pixel16FiveSixFive),
            24 => typeof(Pixel24),
            32 => typeof(Pixel32),
            _ => throw new NotImplementedException(),
        }), data ?? new byte[width * height * bitPerPixel / 8], width, height, palette ?? Array.Empty<Pixel32>(), xPixelsPerMeter, yPixelsPerMeter) ?? throw new Exception()),
        _ => throw new NotImplementedException(),
    };

    public static unsafe (IBitmap bitmap, FileHeader fileHeader, InfoHeader infoHeader, Pixel32[] palette) ReadBmpFromFile(string path)
    {
        FileHeader fileHeader;
        InfoHeader infoHeader;
        Pixel32[] palette;
        byte[] data;

        using var fs = File.OpenRead(path);

        fs.Read(new(&fileHeader, sizeof(FileHeader)));
        fs.Read(new(&infoHeader, sizeof(InfoHeader)));

        fixed (void* palettePtr = palette)
        {
            checked
            {
                var paletteSize = (int)infoHeader.StructSize - sizeof(InfoHeader);
                palette = paletteSize is 0 ? Array.Empty<Pixel32>() : new Pixel32[paletteSize / sizeof(InfoHeader)];
                fs.Read(new(palettePtr, paletteSize));
            }
        }

        fs.Seek(fileHeader.OffsetData, SeekOrigin.Begin);
        data = new byte[fileHeader.FileSize - fileHeader.OffsetData];
        fs.Read(data);

        return
        (
            CreateBitmap(infoHeader.BitPerPixelCount, infoHeader.ColorsUsed, infoHeader.ImageWidth, infoHeader.ImageHeight, infoHeader.XPixelsPerMeter, infoHeader.YPixelsPerMeter, palette, data),
            fileHeader,
            infoHeader,
            palette
        );
    }

    public static unsafe void WriteBmp(string path, IBitmap bitmap, Pixel32[]? explicitPalette = null, int? explicitDataOffset = default)
    {
        var infoHeader = bitmap.MakeInfoHeader();
        var palette = explicitPalette ?? bitmap.Palette.ToArray();
        var data = bitmap.AlignedData;
        FileHeader fileHeader = new FileHeader((uint)(Marshal.SizeOf<FileHeader>() + infoHeader.StructSize + data.Length),
            (uint)(explicitDataOffset ?? Marshal.SizeOf<FileHeader>() + infoHeader.StructSize));

        using var fs = File.OpenWrite(path);

        fs.Write(new(&fileHeader, sizeof(FileHeader)));
        fs.Write(new(&infoHeader, sizeof(InfoHeader)));

        fixed (void* palettePtr = palette)
            fs.Write(new(palettePtr, palette.Length * Marshal.SizeOf<Pixel32>()));

        fs.Seek(fileHeader.OffsetData, SeekOrigin.Begin);
        fs.Write(data);
    }
}

#endregion BitmapFactory

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
        var rootCommand = new RootCommand("Create bmp in pool")
        {
            new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
            new Option<ushort>(new[] { "-b","--bits-per-pixel"}, "Bits per pixel in data array") { IsRequired = true},
            new Option<uint>(new[] { "-c","--colors-used"}, "Color used in bmp"),
            new Option<int>(new[] { "-w","--width"}, "Width") { IsRequired = true},
            new Option<int>(new[] { "-h","--height"}, "Height") { IsRequired = true},
            new Option<int>(new[] { "-xP","--x-pixels-per-meter"}, "Horizontal pixels per meter"),
            new Option<int>(new[] { "-yP","--y-pixels-per-meter"}, "Vertical pixels per meter"),
            new Option<uint[]>(new[] { "-p","--palette"}, "Palette"),
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
                }

                bitmaps.Add(bmpName, BitmapFactory.CreateBitmap(bitsPerPixel, colorsUsed, width, height, xPixelsPerMeter, yPixelsPerMeter, convertedPalette));
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
        var rootCommand = new RootCommand("Get names in pool");
        rootCommand.Handler = CommandHandler.Create(() =>
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

        return rootCommand;
    }

    public static RootCommand MakeGetPixelColorCommand(Dictionary<string, IBitmap> bitmaps)
    {
        var rootCommand = new RootCommand("Delete bmp from pool")
        {
            new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
            new Option<string>(new[] { "-w","--width"}, "Width") { IsRequired = true},
            new Option<string>(new[] { "-h","--height"}, "Height") { IsRequired = true},
        };
        rootCommand.Handler = CommandHandler.Create<string, int, int>((bmpName, width, height) =>
        {
            try
            {
                Console.WriteLine(bitmaps[bmpName] is IBitmapWithoutPalette bitmapWithoutPalette ?
                $"b:{bitmapWithoutPalette[width, height].Blue} g:{bitmapWithoutPalette[width, height].Green} r:{bitmapWithoutPalette[width, height].Red}" :
                nameof(NotSupportedException));
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine(nameof(IndexOutOfRangeException));
            }
        });

        return rootCommand;
    }

    public static RootCommand MakeInversColorsCommand(Dictionary<string, IBitmap> bitmaps)
    {
        var rootCommand = new RootCommand("Delete bmp from pool")
            {
                new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
            };
        rootCommand.Handler = CommandHandler.Create<string>(bmpName =>
        {
            try
            {
                bitmaps[bmpName].InversColors();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine(nameof(KeyNotFoundException));
            }
        });

        return rootCommand;
    }

    public static RootCommand MakeMirrorCommand(Dictionary<string, IBitmap> bitmaps)
    {
        var rootCommand = new RootCommand("Mirror the image")
            {
                new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
                new Option<AxisMirror>(new[] { "-a","--axis"}, "Rotation direction") { IsRequired = true},
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
        var rootCommand = new RootCommand("Read bmp from file")
            {
                new Option<string>(new[] { "-n","--bmp-name"}, "The name that is assigned when creating the BMP") { IsRequired = true},
                new Option<string>(new[] { "-p","--file-path"}, "File path") { IsRequired = true},
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
        var rootCommand = new RootCommand("Mirror the image")
            {
                new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
                new Option<RotationDirection>(new[] { "-d","--direction"}, "Rotation direction and angle") { IsRequired = true},
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
        var rootCommand = new RootCommand("Save bmp to file")
            {
                new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
                new Option<string>(new[] { "-p","--file-path"}, "File path") { IsRequired = true},
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
        var rootCommand = new RootCommand("Delete bmp from pool")
            {
                new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
                new Option<int>(new[] { "-w","--width"}, "Width") { IsRequired = true},
                new Option<int>(new[] { "-h","--height"}, "Height") { IsRequired = true},
                new Option<byte>(new[] { "-b","--blue"}, "Blue component value") { IsRequired = true },
                new Option<byte>(new[] { "-g","--green"}, "Green component value") { IsRequired = true },
                new Option<byte>(new[] { "-r","--red"}, "Red component value") { IsRequired = true },
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
                    Console.WriteLine(nameof(NotSupportedException));
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
        var rootCommand = new RootCommand("Delete bmp from pool")
            {
                new Option<string>(new[] { "-n","--bmp-name"}, "Name in image pool") { IsRequired = true},
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
    private static Lazy<Interpreter> _defaultInterpreter;
    private readonly Dictionary<string, IBitmap> _bitmaps;

    private readonly Dictionary<string, RootCommand> _commands;

    public static Interpreter DefaultInterpreter => _defaultInterpreter.Value;
    public IReadOnlyDictionary<string, IBitmap> Bitmaps => _bitmaps;

    static Interpreter()
    {
        _defaultInterpreter = new(static () => new
        (
            ("create", CommandFactory.MakeCreateBmpCommand),
            ("read", CommandFactory.MakeReadCommand),
            ("save", CommandFactory.MakeSaveCommand),
            ("rotate", CommandFactory.MakeRotateCommand),
            ("mirror", CommandFactory.MakeMirrorCommand),
            ("bmpList", CommandFactory.MakeGetBmpListCommand),
            ("unload", CommandFactory.MakeUnloadCommand),
            ("inversColors", CommandFactory.MakeInversColorsCommand),
            ("setPixelColor", CommandFactory.MakeSetPixelColorCommand),
            ("getPixelColor", CommandFactory.MakeGetPixelColorCommand))
        );
    }

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