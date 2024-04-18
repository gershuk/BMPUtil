namespace SimpleBmpUtil.BaseClasses;

public static class BitmapFactory
{
    public static IBitmap CreateBitmap(ushort bitPerPixel,
                                       uint colorsUsed,
                                       int width,
                                       int height,
                                       int xPixelsPerMeter = default,
                                       int yPixelsPerMeter = default,
                                       byte[]? data = default,
                                       Pixel32[]? palette = default
                                       ) =>
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
        }), data ?? new byte[width * height * bitPerPixel / 8], width, height, palette, xPixelsPerMeter, yPixelsPerMeter) ?? throw new Exception()),
        _ => throw new NotImplementedException(),
    };

    public static unsafe (IBitmap bitmap, FileHeader fileHeader, InfoHeader infoHeader, Pixel32[] palette) ReadBmpFromFile(string path)
    {
        FileHeader fileHeader;
        InfoHeader infoHeader;
        Pixel32[] palette;
        byte[] data;

        using var fs = File.OpenRead(path);

        _ = fs.Read(new(&fileHeader, sizeof(FileHeader)));
        _ = fs.Read(new(&infoHeader, sizeof(InfoHeader)));

        fixed (void* palettePtr = palette)
        {
            checked
            {
                var paletteSize = (int)infoHeader.StructSize - sizeof(InfoHeader);
                palette = paletteSize is 0 ? Array.Empty<Pixel32>() : new Pixel32[paletteSize / sizeof(InfoHeader)];
                _ = fs.Read(new(palettePtr, paletteSize));
            }
        }

        _ = fs.Seek(fileHeader.OffsetData, SeekOrigin.Begin);
        data = new byte[fileHeader.FileSize - fileHeader.OffsetData];
        _ = fs.Read(data);

        return
        (
            CreateBitmap(infoHeader.BitPerPixelCount, infoHeader.ColorsUsed, infoHeader.ImageWidth, infoHeader.ImageHeight, infoHeader.XPixelsPerMeter, infoHeader.YPixelsPerMeter, data, palette),
            fileHeader,
            infoHeader,
            palette
        );
    }

    public static unsafe void WriteBmp(string path, IBitmap bitmap, Pixel32[]? explicitPalette = null, int? explicitDataOffset = default)
    {
        var infoHeader = bitmap.MakeInfoHeader();
        var palette = explicitPalette ?? bitmap.Palette?.ToArray() ?? Array.Empty<Pixel32>();
        var data = bitmap.AlignedData;
        var fileHeader = new FileHeader((uint)(Marshal.SizeOf<FileHeader>() + infoHeader.StructSize + data.Length),
            (uint)(explicitDataOffset ?? Marshal.SizeOf<FileHeader>() + infoHeader.StructSize));

        using var fs = File.OpenWrite(path);

        fs.Write(new(&fileHeader, sizeof(FileHeader)));
        fs.Write(new(&infoHeader, sizeof(InfoHeader)));

        fixed (void* palettePtr = palette)
            fs.Write(new(palettePtr, palette.Length * Marshal.SizeOf<Pixel32>()));

        _ = fs.Seek(fileHeader.OffsetData, SeekOrigin.Begin);
        fs.Write(data);
    }
}