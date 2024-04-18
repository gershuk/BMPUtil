namespace SimpleBmpUtil.BaseClasses;

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

    private static readonly int[,] _masks = { { 0x001F, 0x03E0, 0x7C00 }, { 0x001F, 0x07E0, 0xF800 } };

    private static readonly int[,] _offsets = { { 0, 5, 10 }, { 0, 5, 11 } };

    public byte GetColorComponent(ComponentId id, SchemeType type) => (byte)((_data & _masks[(int)type, (int)id]) >> _offsets[(int)type, (int)id]);

    private ushort CalcNewData(byte value, ComponentId id, SchemeType type) =>
        (ushort)((_data & (_masks[(int)type, 0] + _masks[(int)type, 1] + _masks[(int)type, 2] - _masks[(int)type, (int)id])) +
        ((value << _offsets[(int)type, (int)id]) & _masks[(int)type, (int)id]));

    private static bool IsParametersValid(byte value, ComponentId id, SchemeType type) =>
        ((type is SchemeType.FiveFiveFive || id is ComponentId.B or ComponentId.R) && value is >= 0 and < 32) ||
        (type is SchemeType.FiveSixFive && id is ComponentId.G && value is >= 0 and < 64);

    public ushort SetColorComponent(byte value, ComponentId id, SchemeType type) =>
        IsParametersValid(value, id, type) ?
        _data = CalcNewData(value, id, type) :
        throw new ColorValueOutOfRangeException();

    public void Invers(SchemeType type)
    {
        _ = SetColorComponent((byte)(0x001F - GetColorComponent(ComponentId.B, type)), ComponentId.B, type);
        _ = SetColorComponent((byte)(type switch
        {
            SchemeType.FiveFiveFive => 0x001F,
            SchemeType.FiveSixFive => 0x003F,
            _ => throw new NotImplementedException(),
        } - GetColorComponent(ComponentId.G, type)), ComponentId.G, type);
        _ = SetColorComponent((byte)(0x001F - GetColorComponent(ComponentId.R, type)), ComponentId.R, type);
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