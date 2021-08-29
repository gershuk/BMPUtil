#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

BMP bmp = new(Console.ReadLine());

[StructLayout(LayoutKind.Explicit, Size = 14)]
public struct FileHeader
{
    [FieldOffset(0x00)]
    private ushort _type;

    [FieldOffset(0x02)]
    private uint _fileSize;

    [FieldOffset(0x06)]
    private ushort _reserved1;

    [FieldOffset(0x08)]
    private ushort _reserved2;

    [FieldOffset(0x0A)]
    private uint _offsetData;

    public ushort Type { get => _type; set => _type = value; }
    public uint FileSize { get => _fileSize; set => _fileSize = value; }
    public ushort Reserved1 { get => _reserved1; set => _reserved1 = value; }
    public ushort Reserved2 { get => _reserved2; set => _reserved2 = value; }
    public uint OffsetData { get => _offsetData; set => _offsetData = value; }
}

[StructLayout(LayoutKind.Explicit, Size = 40)]
public struct InfoHeader
{
    [FieldOffset(0x00)]
    private uint _structSize;

    [FieldOffset(0x04)]
    private int _imageWidth;

    [FieldOffset(0x08)]
    private int _imageHeight;

    [FieldOffset(0x0C)]
    private ushort _planes;

    [FieldOffset(0x0E)]
    private ushort _bitPerPixelCount;

    [FieldOffset(0x10)]
    private uint _compression;

    [FieldOffset(0x14)]
    private uint _sizeImage;

    [FieldOffset(0x18)]
    private int _xPixelsPerMeter;

    [FieldOffset(0x1C)]
    private int _ypixelsPerMeter;

    [FieldOffset(0x20)]
    private uint _colorsUsed;

    [FieldOffset(0x24)]
    private uint _colorsImportant;

    public uint StructSize { get => _structSize; set => _structSize = value; }
    public int ImageWidth { get => _imageWidth; set => _imageWidth = value; }
    public int ImageHeight { get => _imageHeight; set => _imageHeight = value; }
    public ushort Planes { get => _planes; set => _planes = value; }
    public ushort BitPerPixelCount { get => _bitPerPixelCount; set => _bitPerPixelCount = value; }
    public uint Compression { get => _compression; set => _compression = value; }
    public uint SizeImage { get => _sizeImage; set => _sizeImage = value; }
    public int XPixelsPerMeter { get => _xPixelsPerMeter; set => _xPixelsPerMeter = value; }
    public int YpixelsPerMeter { get => _ypixelsPerMeter; set => _ypixelsPerMeter = value; }
    public uint ColorsUsed { get => _colorsUsed; set => _colorsUsed = value; }
    public uint ColorsImportant { get => _colorsImportant; set => _colorsImportant = value; }
}

[StructLayout(LayoutKind.Explicit, Size = 3)]
public struct Pixel24
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
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct Pixel32
{
    [FieldOffset(0x00)]
    private byte _blue;

    [FieldOffset(0x01)]
    private byte green;

    [FieldOffset(0x02)]
    private byte red;

    [FieldOffset(0x03)]
    private byte reserved;

    public byte Blue { get => _blue; set => _blue = value; }
    public byte Green { get => green; set => green = value; }
    public byte Red { get => red; set => red = value; }
    public byte Reserved { get => reserved; set => reserved = value; }
}

public class BMP
{
    private int? _bitsPerLine;
    private int? _bytesForLineAlignment;
    private byte[] _data;
    private FileHeader _fileHeader;
    private InfoHeader _infoHeader;
    private Pixel32[] _palette;

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
    public InfoHeader InfoHeader { get => _infoHeader; init => _infoHeader = value; }

    public unsafe BMP([NotNull] string path)
    {
        using var fs = File.OpenRead(path);
        fixed (void* fileHeaderPtr = &_fileHeader, infoHeaderPtr = &_infoHeader, _palettePtr = _palette)
        {
            fs.Read(new(fileHeaderPtr, sizeof(FileHeader)));
            fs.Read(new(infoHeaderPtr, sizeof(InfoHeader)));

            checked
            {
                var paletteSize = (int)_infoHeader.StructSize - sizeof(InfoHeader);
                _palette = paletteSize is 0 ? Array.Empty<Pixel32>() : new Pixel32[paletteSize / sizeof(InfoHeader)];
                fs.Read(new(_palettePtr, paletteSize));
            }
        }
        fs.Seek(_fileHeader.OffsetData, SeekOrigin.Begin);
        _data = new byte[_fileHeader.FileSize - _fileHeader.OffsetData];
        fs.Read(_data);
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

    private int PixelBitPosition(int w, int h) => BitsPerLine * h + w * _infoHeader.BitPerPixelCount;

    private int PixelBitPosition(int index) => PixelBitPosition(index / _infoHeader.ImageWidth, index % _infoHeader.ImageWidth);
}