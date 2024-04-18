namespace SimpleBmpUtil.BaseClasses;

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
    private readonly int _yPixelsPerMeter;

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
                      int yPixelsPerMeter,
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
        _yPixelsPerMeter = yPixelsPerMeter;
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
        _yPixelsPerMeter,
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
    public int YPixelsPerMeter { get => _yPixelsPerMeter; init => _yPixelsPerMeter = value; }
    public uint ColorsUsed { get => _colorsUsed; init => _colorsUsed = value; }
    public uint ColorsImportant { get => _colorsImportant; init => _colorsImportant = value; }
}