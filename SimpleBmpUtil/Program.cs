#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

(var bmp, _, _, _) = BitmapFactory.ReadBmpFromFile(@"C:\Users\vladi\Desktop\Test.bmp");
bmp.VerticalInvers();
bmp.InversColors();
bmp.InversColors();
BitmapFactory.WriteBmp(@"C:\Users\vladi\Desktop\Test2.bmp", bmp);

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

    public void SetColorComponent(byte value, ComponentId id, SchemeType type) =>
        _data = (ushort)((_data & masks[(int)type, 0] + masks[(int)type, 1] + masks[(int)type, 2] - masks[(int)type, (int)id])
        + (value << offsets[(int)type, (int)id]) & masks[(int)type, (int)id]);

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

#endregion PixelStructs

#region BitmapTypes

public interface IBitmap
{
    public byte[] AlignedData { get; }
    public int Height { get; }
    public int LineBitsLength { get; }
    public IReadOnlyList<Pixel32> Palette { get; }
    public int Width { get; }

    public void HorizontalInvers();

    public void InversColors();

    public InfoHeader MakeInfoHeader();

    public void Rotate180();

    public void RotateLeft90();

    public void RotateRight90();

    public void VerticalInvers();
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

    public unsafe void HorizontalInvers()
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

    public unsafe void VerticalInvers()
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

        Activator.CreateInstance(typeof(BitmapWithoutPalette<>).MakeGenericType(
           infoHeader.BitPerPixelCount switch
           {
               16 when infoHeader.ColorsUsed is 32768 => typeof(Pixel16FiveFiveFive),
               16 when infoHeader.ColorsUsed is 65536 => typeof(Pixel16FiveSixFive),
               24 => typeof(Pixel24),
               32 => typeof(Pixel32),
               _ => throw new NotImplementedException(),
           }), data, infoHeader.ImageWidth, infoHeader.ImageHeight, palette, infoHeader.XPixelsPerMeter, infoHeader.YPixelsPerMeter);

        return
        (
            infoHeader.BitPerPixelCount switch
            {
                8 => throw new NotImplementedException(),
                16 or 24 or 32 =>
                (IBitmap)(Activator.CreateInstance(typeof(BitmapWithoutPalette<>).MakeGenericType(
                infoHeader.BitPerPixelCount switch
                {
                    16 when infoHeader.ColorsUsed is 32768 => typeof(Pixel16FiveFiveFive),
                    16 when infoHeader.ColorsUsed is 65536 => typeof(Pixel16FiveSixFive),
                    24 => typeof(Pixel24),
                    32 => typeof(Pixel32),
                    _ => throw new NotImplementedException(),
                }), data, infoHeader.ImageWidth, infoHeader.ImageHeight, palette, infoHeader.XPixelsPerMeter, infoHeader.YPixelsPerMeter) ?? throw new Exception()),
                _ => throw new NotImplementedException(),
            },
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

public interface ICommand
{
    public void Execute();

    public void TryParse(Dictionary<string, string> args)
    {
        var fieldInfos = GetType().GetFields();

        foreach (var fieldInfo in fieldInfos)
        {
            var value = args[(fieldInfo.GetCustomAttributes(typeof(CommandParamNameAttribute), true)[0] as CommandParamNameAttribute).Name];
            fieldInfo.SetValue(this, Convert.ChangeType(value, fieldInfo.FieldType));
        }
    }
}

public static class Interpreter
{
    private static readonly Dictionary<string, IBitmap> _bitmaps;

    public static Dictionary<string, IBitmap> Bitmaps => _bitmaps;

    static Interpreter()
    {
        _bitmaps = new Dictionary<string, IBitmap>();
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class CommandParamNameAttribute : Attribute
{
    public string Name { get; set; }

    public CommandParamNameAttribute(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
}

public class CreateBmpCommand : ICommand
{
    private int _bitsPerPixel;
    private int _colorUsedParm;
    private int _height;
    private string _name;
    private Pixel32[] _palette;
    private int _width;
    private int _xPixelsPerMeter;
    private int _yPixelsPerMeter;

    public void Execute()
    {
        IBitmap bitmap = (IBitmap)(Activator.CreateInstance(typeof(BitmapWithoutPalette<>).MakeGenericType(
           _bitsPerPixel switch
           {
               16 when Convert.ToInt32(_colorUsedParm) is 32768 => typeof(Pixel16FiveFiveFive),
               16 when Convert.ToInt32(_colorUsedParm) is 65536 => typeof(Pixel16FiveSixFive),
               24 => typeof(Pixel24),
               32 => typeof(Pixel32),
               _ => throw new NotImplementedException(),
           }), _width, _height, _palette, _xPixelsPerMeter, _yPixelsPerMeter) ?? throw new Exception());
        Interpreter.Bitmaps.Add(_name, bitmap);
    }

    //public bool TryParse(Dictionary<string, string> args)
    //{
    //    const string bitsPerPixelParm = "-bits";
    //    const string colorUsedParm = "-colors";
    //    const string heightParm = "-height";
    //    const string nameParm = "-name";
    //    const string paletteParm = "-palette";
    //    const string widthParm = "-width";

    //    _bitsPerPixel = args[bitsPerPixelParm;
    //    _colorUsedParm;
    //    _height;
    //    _name;
    //    _palette;
    //    _width;
    //    _xPixelsPerMeter;
    //    _yPixelsPerMeter;
    //}
}

public class LoadBmpFromFileCommand : ICommand
{
    private string _name;
    private string _path;

    public void Execute() => Interpreter.Bitmaps.Add(_name, BitmapFactory.ReadBmpFromFile(_path).bitmap);

    //public bool TryParse(Dictionary<string, string> args)
    //{
    //    const string bmpNameParm = "-name";
    //    const string pathNameParm = "-path";

    //    _name = args[bmpNameParm];
    //    _path = args[pathNameParm];

    //    return true;
    //}
}

public class SaveBmpToFileCommand : ICommand
{
    private string _name;
    private string _path;

    public void Execute() => BitmapFactory.WriteBmp(_path, Interpreter.Bitmaps[_name]);

    //public bool TryParse(Dictionary<string, string> args)
    //{
    //    const string bmpNameParm = "-name";
    //    const string pathNameParm = "-path";

    //    _name = args[bmpNameParm];
    //    _path = args[pathNameParm];

    //    return true;
    //}
}

public class UnloadBmpFromPull : ICommand
{
    private const string bmpNameParm = "-name";
    private string _name;

    public void Execute() => Interpreter.Bitmaps.Remove(_name);

    //public bool TryParse(Dictionary<string, string> args)
    //{
    //    const string bmpNameParm = "-name";
    //    _name = args[bmpNameParm];

    //    return true;
    //}
}

#endregion Interpreter