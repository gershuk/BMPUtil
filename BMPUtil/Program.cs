#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

Bitmap bmp = new(Console.ReadLine());
bmp.InversColors().Save(@"C:\Users\vladi\Desktop\Test2.bmp");

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

    public FileHeader(uint fileSize, uint offsetData, ushort reserved1, ushort reserved2, ushort type)
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
    public int YpixelsPerMeter { get => _ypixelsPerMeter; init => _ypixelsPerMeter = value; }
    public uint ColorsUsed { get => _colorsUsed; init => _colorsUsed = value; }
    public uint ColorsImportant { get => _colorsImportant; init => _colorsImportant = value; }
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

public static class PixelArrayExtension
{
    public static TPixel[] GetInversPixelArray<TPixel>(this TPixel[] pixels) where TPixel : struct, IPixel
    {
        var newPixels = (TPixel[])pixels.Clone();
        foreach (var pixel in newPixels)
            pixel.Invers();
        return newPixels;
    }

    public static unsafe byte[] GetInversPixelArray<TPixel>(this byte[] data, int bytesPerLine, int width, int height) where TPixel : struct, IPixel
    {
        var newData = (byte[])data.Clone();
        fixed (void* dataPtr = newData)
        {
            void* lineStartPosition = dataPtr;
            for (var h = 0; h < height; ++h)
            {
                Span<TPixel> pixels = new(lineStartPosition, width);
                for (var i = 0; i < pixels.Length; ++i)
                {
                    pixels[i].Invers();
                }

                lineStartPosition = (byte*)lineStartPosition + bytesPerLine;
            }
        }
        return newData;
    }
}

public sealed class Bitmap
{
    private readonly byte[] _data;
    private readonly FileHeader _fileHeader;
    private readonly InfoHeader _infoHeader;
    private readonly Pixel32[] _palette;
    private int? _bitsPerLine;
    private int? _bytesForLineAlignment;

    private int BytesForLineAlignment => _bytesForLineAlignment ??
            (int)(_bytesForLineAlignment =
           _infoHeader.BitPerPixelCount switch
           {
               8 => _infoHeader.ImageWidth * 3 % 4,
               16 => _infoHeader.ImageWidth * 2 % 4,
               24 => _infoHeader.ImageWidth % 4,
               32 => 0,
               _ => throw new ArgumentOutOfRangeException()
           });

    public int BitsPerLine => _bitsPerLine ?? (int)(_bitsPerLine = _infoHeader.ImageWidth * _infoHeader.BitPerPixelCount + 8 * BytesForLineAlignment);

    public FileHeader FileHeader { get => _fileHeader; init => _fileHeader = value; }

    public IReadOnlyList<Pixel32> GetPalette => _palette;

    public InfoHeader InfoHeader { get => _infoHeader; init => _infoHeader = value; }

    public Pixel32[] Palette { private get => _palette; init => _palette = value; }

    public unsafe Bitmap([NotNull] string path)
    {
        using var fs = File.OpenRead(path);
        fixed (void* fileHeaderPtr = &_fileHeader, infoHeaderPtr = &_infoHeader, _palettePtr = Palette)
        {
            fs.Read(new(fileHeaderPtr, sizeof(FileHeader)));
            fs.Read(new(infoHeaderPtr, sizeof(InfoHeader)));

            checked
            {
                var paletteSize = (int)_infoHeader.StructSize - sizeof(InfoHeader);
                Palette = paletteSize is 0 ? Array.Empty<Pixel32>() : new Pixel32[paletteSize / sizeof(InfoHeader)];
                fs.Read(new(_palettePtr, paletteSize));
            }
        }
        fs.Seek(_fileHeader.OffsetData, SeekOrigin.Begin);
        _data = new byte[_fileHeader.FileSize - _fileHeader.OffsetData];
        fs.Read(_data);
    }

    public Bitmap(in FileHeader fileHeader, in InfoHeader infoHeader, Pixel32[] palette, byte[] data = null)
    {
        Palette = palette ?? throw new ArgumentNullException(nameof(palette));
        FileHeader = fileHeader;
        InfoHeader = infoHeader;
        _data = data ?? new byte[infoHeader.ImageHeight * BitsPerLine / 8];
    }

    public uint this[int w, int h]
    {
        get =>
            w >= _infoHeader.ImageWidth ? throw new ArgumentOutOfRangeException(nameof(w)) :
            h >= _infoHeader.ImageHeight ? throw new ArgumentOutOfRangeException(nameof(h)) :
            _infoHeader.BitPerPixelCount switch
            {
                8 => _data[PixelBitPosition(w, h) / 8],
                16 => BitConverter.ToUInt16(_data, PixelBitPosition(w, h) / 8),
                24 => BitConverter.ToUInt32(_data, PixelBitPosition(w, h) / 8) & 0xFFFFFF,
                32 => BitConverter.ToUInt32(_data, PixelBitPosition(w, h) / 8),
                _ => throw new ArgumentOutOfRangeException()
            };
        set
        {
            if (w >= _infoHeader.ImageWidth)
                throw new ArgumentOutOfRangeException(nameof(w));
            if (h >= _infoHeader.ImageHeight)
                throw new ArgumentOutOfRangeException(nameof(h));

            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, _data, PixelBitPosition(w, h) / 8, _infoHeader.BitPerPixelCount / 8);
        }
    }

    public uint this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[index / _infoHeader.ImageWidth, index % _infoHeader.ImageWidth];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => this[index / _infoHeader.ImageWidth, index % _infoHeader.ImageWidth] = value;
    }

    private static void Swap<T>(ref T lhs, ref T rhs) => (lhs, rhs) = (rhs, lhs);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int PixelBitPosition(int w, int h) => BitsPerLine * h + w * _infoHeader.BitPerPixelCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int PixelBitPosition(int index) => PixelBitPosition(index / _infoHeader.ImageWidth, index % _infoHeader.ImageWidth);

    private unsafe Bitmap VerticalMirror<TPixel>()
    {
        var newData = (byte[])_data.Clone();
        Bitmap newBitmap = new(FileHeader, InfoHeader, Palette, newData);
        fixed (void* dataPtr = newData)
        {
            void* lineStartPosition = dataPtr;
            for (var h = 0; h < newBitmap.InfoHeader.ImageHeight; ++h)
            {
                Span<TPixel> pixels = new(lineStartPosition, newBitmap.InfoHeader.ImageWidth);
                pixels.Reverse();
                lineStartPosition = (byte*)lineStartPosition + newBitmap.BitsPerLine / 8;
            }
        }

        return newBitmap;
    }

    public unsafe Bitmap HorizontalMirror()
    {
        var newData = new byte[_data.Length];
        Bitmap newBitmap = new(FileHeader, InfoHeader, Palette, newData);
        for (var h = 0; h < _infoHeader.ImageHeight; ++h)
        {
            Buffer.BlockCopy(_data, BitsPerLine / 8 * h, newData, _data.Length - BitsPerLine / 8 * (h + 1), BitsPerLine / 8);
        }
        return newBitmap;
    }

    public Bitmap InversColors() =>
    _infoHeader.BitPerPixelCount switch
    {
        8 => new Bitmap(FileHeader, InfoHeader, Palette.GetInversPixelArray(), _data),
        16 => new Bitmap(FileHeader, InfoHeader, Palette, _data.GetInversPixelArray<Pixel16FiveFiveFive>(BitsPerLine / 8, InfoHeader.ImageWidth, InfoHeader.ImageHeight)),
        24 => new Bitmap(FileHeader, InfoHeader, Palette, _data.GetInversPixelArray<Pixel24>(BitsPerLine / 8, InfoHeader.ImageWidth, InfoHeader.ImageHeight)),
        32 => new Bitmap(FileHeader, InfoHeader, Palette, _data.GetInversPixelArray<Pixel32>(BitsPerLine / 8, InfoHeader.ImageWidth, InfoHeader.ImageHeight)),
        _ => throw new ArgumentOutOfRangeException()
    };

    public Bitmap Rotate180()
    {
        Bitmap newBitmap = new(FileHeader, InfoHeader, (Pixel32[])Palette.Clone());

        var length = InfoHeader.ImageHeight * InfoHeader.ImageWidth;

        for (var i = 0; i < length; ++i)
        {
            newBitmap[length - 1 - i] = this[i];
        }

        return newBitmap;
    }

    public Bitmap RotateLeft90()
    {
        Bitmap newBitmap = new(FileHeader, InfoHeader.GetRotatedHeader(), (Pixel32[])Palette.Clone());

        for (var w = 0; w < InfoHeader.ImageWidth; ++w)
        {
            for (var h = 0; h < InfoHeader.ImageHeight; ++h)
            {
                newBitmap[h, w] = this[InfoHeader.ImageWidth - 1 - w, h];
            }
        }

        return newBitmap;
    }

    public Bitmap RotateRight90()
    {
        Bitmap newBitmap = new(FileHeader, InfoHeader.GetRotatedHeader(), (Pixel32[])Palette.Clone());

        for (var w = 0; w < InfoHeader.ImageWidth; ++w)
        {
            for (var h = 0; h < InfoHeader.ImageHeight; ++h)
            {
                newBitmap[h, w] = this[w, InfoHeader.ImageHeight - 1 - h];
            }
        }

        return newBitmap;
    }

    public unsafe void Save([NotNull] string path)
    {
        using var fs = File.OpenWrite(path);
        fixed (void* fileHeaderPtr = &_fileHeader, infoHeaderPtr = &_infoHeader, _palettePtr = Palette)
        {
            fs.Write(new(fileHeaderPtr, sizeof(FileHeader)));
            fs.Write(new(infoHeaderPtr, sizeof(InfoHeader)));
            fs.Write(new(_palettePtr, Palette.Length * Palette.Length > 0 ? Marshal.SizeOf(Palette[0]) : 0));
        }
        fs.Seek(_fileHeader.OffsetData, SeekOrigin.Begin);
        fs.Write(_data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bitmap VerticalMirror() =>
    _infoHeader.BitPerPixelCount switch
    {
        8 => VerticalMirror<byte>(),
        16 => VerticalMirror<Pixel16>(),
        24 => VerticalMirror<Pixel24>(),
        32 => VerticalMirror<Pixel32>(),
        _ => throw new ArgumentOutOfRangeException()
    };
}

public class NotSupportedOperationException : Exception
{
    public NotSupportedOperationException(string message) : base(message)
    {
    }
}

public interface IPixel
{
    public byte Blue { get; set; }
    public byte Green { get; set; }
    public byte Red { get; set; }

    public void Invers();
}