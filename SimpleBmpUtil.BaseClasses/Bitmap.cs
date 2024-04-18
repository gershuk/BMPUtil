namespace SimpleBmpUtil.BaseClasses;

public interface IBitmap
{
    public byte[] AlignedData { get; }
    public int Height { get; }
    public int LineBitsLength { get; }
    public IReadOnlyList<Pixel32> Palette { get; }
    public int Width { get; }
    public int XPixelsPerMeter { get; set; }

    public int YPixelsPerMeter { get; set; }

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
    private readonly Pixel32[] _palette;

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
                var pixelsPtr = handle.AddrOfPinnedObject().ToPointer();

                fixed (void* dataPtr = data)
                {
                    var linePtr = dataPtr;
                    for (var h = 0; h < Height; ++h)
                    {
                        Buffer.MemoryCopy(pixelsPtr, linePtr, Width * Marshal.SizeOf<TPixel>(), Width * Marshal.SizeOf<TPixel>());
                        linePtr = (byte*)linePtr + (LineBitsLength / 8);
                        pixelsPtr = (byte*)pixelsPtr + (Width * Marshal.SizeOf<TPixel>());
                    }
                }

                handle.Free();

                return data;
            }
        }
    }

    public int Height { get; set; }
    public unsafe int LineBitsLength => ((Width * Marshal.SizeOf<TPixel>()) + BytesForLineAlignment) * 8;
    public IReadOnlyList<Pixel32> Palette => _palette;
    public int Width { get; set; }
    public int XPixelsPerMeter { get; set; }
    public int YPixelsPerMeter { get; set; }

    private BitmapWithoutPalette(int width, int height, Pixel32[]? palette, int xPixelsPerMeter = 0, int yPixelsPerMeter = 0) =>
        (Width, Height, _palette, XPixelsPerMeter, YPixelsPerMeter) = (width, height, palette ?? Array.Empty<Pixel32>(), xPixelsPerMeter, yPixelsPerMeter);

    public BitmapWithoutPalette(TPixel[] pixels, int width, int height, Pixel32[]? palette, int xPixelsPerMeter = 0, int yPixelsPerMeter = 0) :
        this(width, height, palette, xPixelsPerMeter, yPixelsPerMeter) =>
        _pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));

    public unsafe BitmapWithoutPalette(byte[] data, int width, int height, Pixel32[]? palette, int xPixelsPerMeter = 0, int yPixelsPerMeter = 0) :
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
                linePtr = (byte*)linePtr + (LineBitsLength / 8);
                _pixelsPtr = (byte*)_pixelsPtr + (width * Marshal.SizeOf<TPixel>());
            }
        }

        handle.Free();
    }

    IPixel IBitmapWithoutPalette.this[int w, int h] { get => this[w, h]; set => this[w, h] = (TPixel)value; }

    public TPixel this[int w, int h] { get => _pixels[(h * Width) + w]; set => _pixels[(h * Width) + w] = value; }

    private void SwapDimensions() => (Width, Height, XPixelsPerMeter, YPixelsPerMeter) = (Height, Width, YPixelsPerMeter, XPixelsPerMeter);

    public unsafe void HorizontalMirror()
    {
        for (var h = 0; h < Height; ++h)
            Array.Reverse(_pixels, h * Width, Width);
    }

    public void InversColors()
    {
        for (var i = 0; i < _pixels.Length; ++i)
            _pixels[i].Invers();
    }

    public InfoHeader MakeInfoHeader() =>
    new(
        (uint)(Marshal.SizeOf<InfoHeader>() + (_palette.Length * Marshal.SizeOf<Pixel32>())),
        Width,
        Height,
        1,
        (ushort)(Marshal.SizeOf<TPixel>() * 8),
        0,
        0,
        XPixelsPerMeter,
        YPixelsPerMeter,
        this switch
        {
            BitmapWithoutPalette<Pixel16FiveFiveFive> => 32768,
            BitmapWithoutPalette<Pixel16FiveSixFive> => 65536,
            BitmapWithoutPalette<Pixel24> or BitmapWithoutPalette<Pixel32> => 0,
            _ => throw new NotImplementedException(),
        },
        0
    );

    public void Rotate180() => Array.Reverse(_pixels);

    public void RotateLeft90()
    {
        var newData = new TPixel[Width * Height];
        for (var h = 0; h < Height; ++h)
        {
            for (var w = 0; w < Width; ++w)
                newData[h + (Height * w)] = this[w, Height - 1 - h];
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
                newData[h + (Height * w)] = this[Width - 1 - w, h];
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
        newPixelsPtr = (byte*)newPixelsPtr + ((Height - 1) * Width * Marshal.SizeOf<TPixel>());

        for (var h = 0; h < Height; ++h)
        {
            Buffer.MemoryCopy(oldPixelsPtr, newPixelsPtr, Width * Marshal.SizeOf<TPixel>(), Width * Marshal.SizeOf<TPixel>());
            oldPixelsPtr = (byte*)oldPixelsPtr + (Width * Marshal.SizeOf<TPixel>());
            newPixelsPtr = (byte*)newPixelsPtr - (Width * Marshal.SizeOf<TPixel>());
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