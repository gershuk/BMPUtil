using Microsoft.VisualStudio.TestTools.UnitTesting;

using SimpleBmpUtil;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static SimpleBmpUtil.Pixel16;

namespace BmpUtilTests
{
    public static class Ext
    {
        private static int BytesForLineAlignment<TPixel>(int width) where TPixel : struct, IPixel =>
                Marshal.SizeOf<TPixel>() switch
                {
                    1 => width * 3 % 4,
                    2 => width * 2 % 4,
                    3 => width % 4,
                    4 => 0,
                    _ => throw new ArgumentOutOfRangeException()
                };

        public static unsafe int LineBitsLength<TPixel>(int width) where TPixel : struct, IPixel =>
            (width * Marshal.SizeOf<TPixel>() + BytesForLineAlignment<TPixel>(width)) * 8;
    }

    [TestClass]
    public class BitmapFactoryTests
    {
        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(10, 10)]
        [DataRow(10, 200)]
        [DataRow(200, 10)]
        [DataRow(568, 1239)]
        [DataRow(1239, 568)]
        public unsafe void TestBitmapCreation(int width, int height)
        {
            TestBitmapCreationGeneric<Pixel16FiveFiveFive>(width, height, 32768);
            TestBitmapCreationGeneric<Pixel16FiveSixFive>(width, height, 65536);
            TestBitmapCreationGeneric<Pixel24>(width, height);
            TestBitmapCreationGeneric<Pixel32>(width, height);
        }

        public unsafe void TestBitmapCreationGeneric<TPixel>(int width, int height, uint colorUsed = default) where TPixel : struct, IPixel
        {
            var data = new byte[height * Ext.LineBitsLength<TPixel>(width) / 8];
            TPixel pixel = new() { Blue = 2, Green = 5, Red = 7 };
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pixelsPtr = handle.AddrOfPinnedObject().ToPointer();
            for (var j = 0; j < height; ++j)
            {
                for (var i = 0; i < width; ++i)
                {
                    Unsafe.Write((byte*)pixelsPtr + i * Marshal.SizeOf<TPixel>(), pixel);
                }
                pixelsPtr = (byte*)pixelsPtr + Ext.LineBitsLength<TPixel>(width) / 8;
            }
            handle.Free();

            var bitmap = (IBitmapWithoutPalette)BitmapFactory.CreateBitmap((ushort)(Marshal.SizeOf<TPixel>() * 8), colorUsed, width, height, width, height, data);
            Assert.AreEqual(bitmap.GetType(), typeof(BitmapWithoutPalette<TPixel>));
            Assert.AreEqual(bitmap.Width, width);
            Assert.AreEqual(bitmap.Height, height);
            Assert.AreEqual(bitmap.XPixelsPerMeter, width);
            Assert.AreEqual(bitmap.YPixelsPerMeter, height);

            for (var h = 0; h < height; ++h)
            {
                for (var w = 0; w < width; ++w)
                {
                    Assert.AreEqual(bitmap[w, h], pixel);
                }
            }
        }
    }

    [TestClass]
    public class BitmapWithoutPalette
    {
        public static void TestCreationFromPixelArrayGeneric<TPixel>(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter) where TPixel : struct, IPixel
        {
            var pixels = new TPixel[width * height];
            TPixel pixel = new() { Blue = b, Green = g, Red = r };
            for (var i = 0; i < pixels.Length; ++i)
                pixels[i] = pixel;
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, xPixelsPerMeter, yPixelsPerMeter);

            Assert.AreEqual(bitmapWithoutPalette.Width, width);
            Assert.AreEqual(bitmapWithoutPalette.Height, height);
            Assert.AreEqual(bitmapWithoutPalette.XPixelsPerMeter, xPixelsPerMeter);
            Assert.AreEqual(bitmapWithoutPalette.YPixelsPerMeter, yPixelsPerMeter);
            Assert.AreEqual(bitmapWithoutPalette.Palette.Count, 0);
            Assert.AreEqual(bitmapWithoutPalette.LineBitsLength, Ext.LineBitsLength<TPixel>(width));

            for (var j = 0; j < height; ++j)
                for (var i = 0; i < width; ++i)
                    Assert.AreEqual(bitmapWithoutPalette[i, j], pixel);
        }

        public static void TestHorizontalMirrorGeneric<TPixel>(int width, int height) where TPixel : struct, IPixel
        {
            Random random = new(6987);
            var pixels = new TPixel[width * height];
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i].Blue = (byte)random.Next(31);
                pixels[i].Green = (byte)random.Next(31);
                pixels[i].Red = (byte)random.Next(31);
            }
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, width, height);

            var mirroredPixels = new TPixel[width * height];
            for (var h = 0; h < height; ++h)
            {
                for (var w = 0; w < width; ++w)
                {
                    mirroredPixels[h * width + w] = pixels[h * width + width - w - 1];
                }
            }
            BitmapWithoutPalette<TPixel> mirroredBitmapWithoutPalette = new(mirroredPixels, width, height, null, width, height);

            bitmapWithoutPalette.HorizontalMirror();

            for (var j = 0; j < height; ++j)
                for (var i = 0; i < width; ++i)
                    Assert.AreEqual(mirroredBitmapWithoutPalette[i, j], bitmapWithoutPalette[i, j]);
        }

        public static void TestIndexersGeneric<TPixel>(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter) where TPixel : struct, IPixel
        {
            var pixels = new TPixel[width * height];
            TPixel pixel = new() { Blue = b, Green = g, Red = r };
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, xPixelsPerMeter, yPixelsPerMeter);

            for (var j = 0; j < height; ++j)
                for (var i = 0; i < width; ++i)
                    bitmapWithoutPalette[i, j] = pixel;

            for (var j = 0; j < height; ++j)
                for (var i = 0; i < width; ++i)
                    Assert.AreEqual(bitmapWithoutPalette[i, j], pixel);
        }

        public static void TestInterfaceIndexersGeneric<TPixel>(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter) where TPixel : struct, IPixel
        {
            var pixels = new TPixel[width * height];
            TPixel pixel = new() { Blue = b, Green = g, Red = r };
            IBitmapWithoutPalette bitmapWithoutPalette = new BitmapWithoutPalette<TPixel>(pixels, width, height, null, xPixelsPerMeter, yPixelsPerMeter);

            for (var j = 0; j < height; ++j)
                for (var i = 0; i < width; ++i)
                    bitmapWithoutPalette[i, j] = pixel;

            for (var j = 0; j < height; ++j)
                for (var i = 0; i < width; ++i)
                    Assert.AreEqual(bitmapWithoutPalette[i, j], pixel);
        }

        public static void TestInversColorsGeneric<TPixel>(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter) where TPixel : struct, IPixel
        {
            var pixels = new TPixel[width * height];
            TPixel pixel = new() { Blue = b, Green = g, Red = r };
            for (var i = 0; i < pixels.Length; ++i)
                pixels[i] = pixel;
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, xPixelsPerMeter, yPixelsPerMeter);
            bitmapWithoutPalette.InversColors();

            pixel.Invers();
            for (var i = 0; i < width; ++i)
                for (var j = 0; j < height; ++j)
                    Assert.AreEqual(bitmapWithoutPalette[i, j], pixel);
        }

        public static void TestMakingInfoHeaderGeneric<TPixel>(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter) where TPixel : struct, IPixel
        {
            var pixels = new TPixel[width * height];
            TPixel pixel = new() { Blue = b, Green = g, Red = r };
            for (var i = 0; i < pixels.Length; ++i)
                pixels[i] = pixel;
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, xPixelsPerMeter, yPixelsPerMeter);
            var infoHeader = bitmapWithoutPalette.MakeInfoHeader();

            Assert.AreEqual(infoHeader.StructSize, (uint)(Marshal.SizeOf<InfoHeader>() + bitmapWithoutPalette.Palette.Count * Marshal.SizeOf<Pixel32>()));
            Assert.AreEqual(infoHeader.ImageWidth, width);
            Assert.AreEqual(infoHeader.ImageHeight, height);
            Assert.AreEqual(infoHeader.Planes, 1);
            Assert.AreEqual(infoHeader.BitPerPixelCount, (ushort)(Marshal.SizeOf<TPixel>() * 8));
            Assert.AreEqual(infoHeader.Compression, (uint)0);
            Assert.AreEqual(infoHeader.SizeImage, (uint)0);
            Assert.AreEqual(infoHeader.XPixelsPerMeter, xPixelsPerMeter);
            Assert.AreEqual(infoHeader.YPixelsPerMeter, yPixelsPerMeter);
            Assert.AreEqual(infoHeader.ColorsUsed, bitmapWithoutPalette switch
            {
                BitmapWithoutPalette<Pixel16FiveFiveFive> => (uint)32768,
                BitmapWithoutPalette<Pixel16FiveSixFive> => (uint)65536,
                BitmapWithoutPalette<Pixel24> or BitmapWithoutPalette<Pixel32> => (uint)0,
                _ => throw new NotImplementedException(),
            });
            Assert.AreEqual(infoHeader.ColorsImportant, (uint)0);
        }

        public static void TestRotate180Generic<TPixel>(int width, int height) where TPixel : struct, IPixel
        {
            Random random = new(6987);
            var pixels = new TPixel[width * height];
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i].Blue = (byte)random.Next(31);
                pixels[i].Green = (byte)random.Next(31);
                pixels[i].Red = (byte)random.Next(31);
            }

            var rotatedPixels = (TPixel[])pixels.Clone();

            Array.Reverse(rotatedPixels);
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, width, height);
            BitmapWithoutPalette<TPixel> rotatedBitmapWithoutPalette = new(rotatedPixels, width, height, null, width, height);
            bitmapWithoutPalette.Rotate180();

            Assert.AreEqual(rotatedBitmapWithoutPalette.Width, bitmapWithoutPalette.Width);
            Assert.AreEqual(rotatedBitmapWithoutPalette.Height, bitmapWithoutPalette.Height);
            Assert.AreEqual(rotatedBitmapWithoutPalette.XPixelsPerMeter, bitmapWithoutPalette.XPixelsPerMeter);
            Assert.AreEqual(rotatedBitmapWithoutPalette.YPixelsPerMeter, bitmapWithoutPalette.YPixelsPerMeter);

            for (var i = 0; i < width; ++i)
                for (var j = 0; j < height; ++j)
                    Assert.AreEqual(rotatedBitmapWithoutPalette[i, j], bitmapWithoutPalette[i, j]);
        }

        public static void TestRotateLeft90Generic<TPixel>(int width, int height) where TPixel : struct, IPixel
        {
            Random random = new(6987);
            var pixels = new TPixel[width * height];
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i].Blue = (byte)random.Next(31);
                pixels[i].Green = (byte)random.Next(31);
                pixels[i].Red = (byte)random.Next(31);
            }

            var rotatedPixels = (TPixel[])pixels.Clone();

            for (var h = 0; h < height; ++h)
            {
                for (var w = 0; w < width; ++w)
                    rotatedPixels[h + height * w] = pixels[w + (height - 1 - h) * width];
            }

            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, width, height);
            BitmapWithoutPalette<TPixel> rotatedBitmapWithoutPalette = new(rotatedPixels, height, width, null, height, width);
            bitmapWithoutPalette.RotateLeft90();

            Assert.AreEqual(rotatedBitmapWithoutPalette.Width, bitmapWithoutPalette.Width);
            Assert.AreEqual(rotatedBitmapWithoutPalette.Height, bitmapWithoutPalette.Height);
            Assert.AreEqual(rotatedBitmapWithoutPalette.XPixelsPerMeter, bitmapWithoutPalette.XPixelsPerMeter);
            Assert.AreEqual(rotatedBitmapWithoutPalette.YPixelsPerMeter, bitmapWithoutPalette.YPixelsPerMeter);

            for (var i = 0; i < width; ++i)
                for (var j = 0; j < height; ++j)
                    Assert.AreEqual(rotatedBitmapWithoutPalette[j, i], bitmapWithoutPalette[j, i]);
        }

        public static void TestRotateRight90Generic<TPixel>(int width, int height) where TPixel : struct, IPixel
        {
            Random random = new(6987);
            var pixels = new TPixel[width * height];
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i].Blue = (byte)random.Next(31);
                pixels[i].Green = (byte)random.Next(31);
                pixels[i].Red = (byte)random.Next(31);
            }

            var rotatedPixels = (TPixel[])pixels.Clone();

            for (var h = 0; h < height; ++h)
            {
                for (var w = 0; w < width; ++w)
                    rotatedPixels[h + height * w] = pixels[width - 1 - w + width * h];
            }

            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, width, height);
            BitmapWithoutPalette<TPixel> rotatedBitmapWithoutPalette = new(rotatedPixels, height, width, null, height, width);
            bitmapWithoutPalette.RotateRight90();

            Assert.AreEqual(rotatedBitmapWithoutPalette.Width, bitmapWithoutPalette.Width);
            Assert.AreEqual(rotatedBitmapWithoutPalette.Height, bitmapWithoutPalette.Height);
            Assert.AreEqual(rotatedBitmapWithoutPalette.XPixelsPerMeter, bitmapWithoutPalette.XPixelsPerMeter);
            Assert.AreEqual(rotatedBitmapWithoutPalette.YPixelsPerMeter, bitmapWithoutPalette.YPixelsPerMeter);

            for (var i = 0; i < width; ++i)
                for (var j = 0; j < height; ++j)
                    Assert.AreEqual(rotatedBitmapWithoutPalette[j, i], bitmapWithoutPalette[j, i]);
        }

        public static void TestVerticalMirrorGeneric<TPixel>(int width, int height) where TPixel : struct, IPixel
        {
            Random random = new(6987);
            var pixels = new TPixel[width * height];
            for (var i = 0; i < pixels.Length; ++i)
            {
                pixels[i].Blue = (byte)random.Next(31);
                pixels[i].Green = (byte)random.Next(31);
                pixels[i].Red = (byte)random.Next(31);
            }
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(pixels, width, height, null, width, height);

            var mirroredPixels = new TPixel[width * height];
            for (var h = 0; h < height; ++h)
            {
                for (var w = 0; w < width; ++w)
                {
                    mirroredPixels[h * width + w] = pixels[(height - h - 1) * width + w];
                }
            }
            BitmapWithoutPalette<TPixel> mirroredBitmapWithoutPalette = new(mirroredPixels, width, height, null, width, height);

            bitmapWithoutPalette.VerticalMirror();

            for (var j = 0; j < height; ++j)
                for (var i = 0; i < width; ++i)
                    Assert.AreEqual(mirroredBitmapWithoutPalette[i, j], bitmapWithoutPalette[i, j]);
        }

        [DataTestMethod]
        [DataRow(0, 0, (byte)0, (byte)0, (byte)0, 0, 0)]
        [DataRow(10, 10, (byte)0, (byte)0, (byte)0, 12, 12)]
        [DataRow(10, 200, (byte)31, (byte)0, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)31, 10, 200)]
        public void TestCreationAlignedData(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter)
        {
            TestCreationAlignedDataGeneric<Pixel16FiveFiveFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationAlignedDataGeneric<Pixel16FiveSixFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationAlignedDataGeneric<Pixel24>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationAlignedDataGeneric<Pixel32>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
        }

        public unsafe void TestCreationAlignedDataGeneric<TPixel>(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter) where TPixel : struct, IPixel
        {
            var data = new byte[height * Ext.LineBitsLength<TPixel>(width) / 8];
            TPixel pixel = new() { Blue = b, Green = g, Red = r };
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pixelsPtr = handle.AddrOfPinnedObject().ToPointer();
            for (var j = 0; j < height; ++j)
            {
                for (var i = 0; i < width; ++i)
                {
                    Unsafe.Write((byte*)pixelsPtr + i * Marshal.SizeOf<TPixel>(), pixel);
                }
                pixelsPtr = (byte*)pixelsPtr + Ext.LineBitsLength<TPixel>(width) / 8;
            }
            handle.Free();

            var pixels = new TPixel[width * height];
            for (var i = 0; i < pixels.Length; ++i)
                pixels[i] = pixel;

            var alignedData = new BitmapWithoutPalette<TPixel>(pixels, width, height, null, xPixelsPerMeter, yPixelsPerMeter).AlignedData;

            Assert.AreEqual(alignedData.Length, data.Length);

            for (var i = 0; i < alignedData.Length; i++)
                Assert.AreEqual(data[i], alignedData[i]);
        }

        [DataTestMethod]
        [DataRow(0, 0, (byte)0, (byte)0, (byte)0, 0, 0)]
        [DataRow(10, 10, (byte)0, (byte)0, (byte)0, 12, 12)]
        [DataRow(10, 200, (byte)31, (byte)0, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)31, 10, 200)]
        public void TestCreationFromDataArray(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter)
        {
            TestCreationFromDataArrayGeneric<Pixel16FiveFiveFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationFromDataArrayGeneric<Pixel16FiveSixFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationFromDataArrayGeneric<Pixel24>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationFromDataArrayGeneric<Pixel32>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
        }

        public unsafe void TestCreationFromDataArrayGeneric<TPixel>(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter) where TPixel : struct, IPixel
        {
            var data = new byte[height * Ext.LineBitsLength<TPixel>(width) / 8];
            TPixel pixel = new() { Blue = b, Green = g, Red = r };
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pixelsPtr = handle.AddrOfPinnedObject().ToPointer();
            for (var j = 0; j < height; ++j)
            {
                for (var i = 0; i < width; ++i)
                {
                    Unsafe.Write((byte*)pixelsPtr + i * Marshal.SizeOf<TPixel>(), pixel);
                }
                pixelsPtr = (byte*)pixelsPtr + Ext.LineBitsLength<TPixel>(width) / 8;
            }
            handle.Free();
            BitmapWithoutPalette<TPixel> bitmapWithoutPalette = new(data, width, height, null, xPixelsPerMeter, yPixelsPerMeter);

            Assert.AreEqual(bitmapWithoutPalette.Width, width);
            Assert.AreEqual(bitmapWithoutPalette.Height, height);
            Assert.AreEqual(bitmapWithoutPalette.XPixelsPerMeter, xPixelsPerMeter);
            Assert.AreEqual(bitmapWithoutPalette.YPixelsPerMeter, yPixelsPerMeter);
            Assert.AreEqual(bitmapWithoutPalette.Palette.Count, 0);
            Assert.AreEqual(bitmapWithoutPalette.LineBitsLength, Ext.LineBitsLength<TPixel>(width));

            for (var i = 0; i < width; ++i)
                for (var j = 0; j < height; ++j)
                    Assert.AreEqual(bitmapWithoutPalette[i, j], pixel);
        }

        [DataTestMethod]
        [DataRow(0, 0, (byte)0, (byte)0, (byte)0, 0, 0)]
        [DataRow(10, 10, (byte)0, (byte)0, (byte)0, 12, 12)]
        [DataRow(10, 200, (byte)31, (byte)0, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)31, 10, 200)]
        public void TestCreationFromPixelArray(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter)
        {
            TestCreationFromPixelArrayGeneric<Pixel16FiveFiveFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationFromPixelArrayGeneric<Pixel16FiveSixFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationFromPixelArrayGeneric<Pixel24>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestCreationFromPixelArrayGeneric<Pixel32>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(10, 10)]
        [DataRow(10, 200)]
        [DataRow(200, 10)]
        [DataRow(568, 1239)]
        [DataRow(1239, 568)]
        public void TestHorizontalMirror(int width, int height)
        {
            TestHorizontalMirrorGeneric<Pixel16FiveFiveFive>(width, height);
            TestHorizontalMirrorGeneric<Pixel16FiveSixFive>(width, height);
            TestHorizontalMirrorGeneric<Pixel24>(width, height);
            TestHorizontalMirrorGeneric<Pixel32>(width, height);
        }

        [DataTestMethod]
        [DataRow(0, 0, (byte)0, (byte)0, (byte)0, 0, 0)]
        [DataRow(10, 10, (byte)0, (byte)0, (byte)0, 12, 12)]
        [DataRow(10, 200, (byte)31, (byte)0, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)31, 10, 200)]
        public void TestIndexers(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter)
        {
            TestIndexersGeneric<Pixel16FiveFiveFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestIndexersGeneric<Pixel16FiveSixFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestIndexersGeneric<Pixel24>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestIndexersGeneric<Pixel32>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
        }

        [DataTestMethod]
        [DataRow(0, 0, (byte)0, (byte)0, (byte)0, 0, 0)]
        [DataRow(10, 10, (byte)0, (byte)0, (byte)0, 12, 12)]
        [DataRow(10, 200, (byte)31, (byte)0, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)31, 10, 200)]
        public void TestInterfaceIndexers(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter)
        {
            TestInterfaceIndexersGeneric<Pixel16FiveFiveFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestInterfaceIndexersGeneric<Pixel16FiveSixFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestInterfaceIndexersGeneric<Pixel24>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestInterfaceIndexersGeneric<Pixel32>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
        }

        [DataTestMethod]
        [DataRow(0, 0, (byte)0, (byte)0, (byte)0, 0, 0)]
        [DataRow(10, 10, (byte)0, (byte)0, (byte)0, 12, 12)]
        [DataRow(10, 200, (byte)31, (byte)0, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)31, 10, 200)]
        public void TestInversColors(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter)
        {
            TestInversColorsGeneric<Pixel16FiveFiveFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestInversColorsGeneric<Pixel16FiveSixFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestInversColorsGeneric<Pixel24>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestInversColorsGeneric<Pixel32>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
        }

        [DataTestMethod]
        [DataRow(0, 0, (byte)0, (byte)0, (byte)0, 0, 0)]
        [DataRow(10, 10, (byte)0, (byte)0, (byte)0, 12, 12)]
        [DataRow(10, 200, (byte)31, (byte)0, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)0, 10, 200)]
        [DataRow(10, 200, (byte)31, (byte)31, (byte)31, 10, 200)]
        public void TestMakingInfoHeader(int width, int height, byte r, byte g, byte b, int xPixelsPerMeter, int yPixelsPerMeter)
        {
            TestMakingInfoHeaderGeneric<Pixel16FiveFiveFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestMakingInfoHeaderGeneric<Pixel16FiveSixFive>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestMakingInfoHeaderGeneric<Pixel24>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
            TestMakingInfoHeaderGeneric<Pixel32>(width, height, r, g, b, xPixelsPerMeter, yPixelsPerMeter);
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(10, 10)]
        [DataRow(10, 200)]
        [DataRow(200, 10)]
        [DataRow(568, 1239)]
        [DataRow(1239, 568)]
        public void TestRotate180(int width, int height)
        {
            TestRotate180Generic<Pixel16FiveFiveFive>(width, height);
            TestRotate180Generic<Pixel16FiveSixFive>(width, height);
            TestRotate180Generic<Pixel24>(width, height);
            TestRotate180Generic<Pixel32>(width, height);
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(10, 10)]
        [DataRow(10, 200)]
        [DataRow(200, 10)]
        [DataRow(568, 1239)]
        [DataRow(1239, 568)]
        public void TestRotateLeft90(int width, int height)
        {
            TestRotateLeft90Generic<Pixel16FiveFiveFive>(width, height);
            TestRotateLeft90Generic<Pixel16FiveSixFive>(width, height);
            TestRotateLeft90Generic<Pixel24>(width, height);
            TestRotateLeft90Generic<Pixel32>(width, height);
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(10, 10)]
        [DataRow(10, 200)]
        [DataRow(200, 10)]
        [DataRow(568, 1239)]
        [DataRow(1239, 568)]
        public void TestRotateRight90(int width, int height)
        {
            TestRotateRight90Generic<Pixel16FiveFiveFive>(width, height);
            TestRotateRight90Generic<Pixel16FiveSixFive>(width, height);
            TestRotateRight90Generic<Pixel24>(width, height);
            TestRotateRight90Generic<Pixel32>(width, height);
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(10, 10)]
        [DataRow(10, 200)]
        [DataRow(200, 10)]
        [DataRow(568, 1239)]
        [DataRow(1239, 568)]
        public void TestVerticalMirror(int width, int height)
        {
            TestVerticalMirrorGeneric<Pixel16FiveFiveFive>(width, height);
            TestVerticalMirrorGeneric<Pixel16FiveSixFive>(width, height);
            TestVerticalMirrorGeneric<Pixel24>(width, height);
            TestVerticalMirrorGeneric<Pixel32>(width, height);
        }
    }

    [TestClass]
    public class FileHeaderTests
    {
        [DataTestMethod]
        [DataRow(uint.MinValue, uint.MinValue)]
        [DataRow(uint.MinValue, uint.MaxValue)]
        [DataRow(uint.MaxValue, uint.MinValue)]
        [DataRow(uint.MaxValue, uint.MaxValue)]
        public unsafe void TestCreation(uint fileSize, uint offsetData)
        {
            FileHeader fileHeader = new(fileSize, offsetData);

            FileHeader benchmarkValue = new();
            var pointer = (void*)&benchmarkValue;
            Unsafe.Write(pointer, (ushort)19778);
            pointer = Unsafe.Add<ushort>(pointer, 1);
            Unsafe.Write(pointer, fileSize);
            pointer = Unsafe.Add<uint>(pointer, 1);
            pointer = Unsafe.Add<ushort>(pointer, 2);
            Unsafe.Write(pointer, offsetData);

            Assert.AreEqual(fileHeader, benchmarkValue);
        }

        [DataTestMethod]
        [DataRow(uint.MinValue, uint.MinValue)]
        [DataRow(uint.MinValue, uint.MaxValue)]
        [DataRow(uint.MaxValue, uint.MinValue)]
        [DataRow(uint.MaxValue, uint.MaxValue)]
        public unsafe void TestGetters(uint fileSize, uint offsetData)
        {
            FileHeader fileHeader = new(fileSize, offsetData);

            Assert.AreEqual(fileHeader.FileSize, fileSize);
            Assert.AreEqual(fileHeader.OffsetData, offsetData);
            Assert.AreEqual(fileHeader.Reserved1, 0);
            Assert.AreEqual(fileHeader.Reserved2, 0);
            Assert.AreEqual(fileHeader.Type, (ushort)19778);
        }

        [DataTestMethod]
        [DataRow(uint.MinValue, uint.MinValue)]
        [DataRow(uint.MinValue, uint.MaxValue)]
        [DataRow(uint.MaxValue, uint.MinValue)]
        [DataRow(uint.MaxValue, uint.MaxValue)]
        public unsafe void TestSetters(uint fileSize, uint offsetData)
        {
            FileHeader fileHeader = new()
            {
                FileSize = fileSize,
                OffsetData = offsetData,
                Reserved1 = 0,
                Reserved2 = 0,
                Type = 19778,
            };

            FileHeader benchmarkValue = new();
            var pointer = (void*)&benchmarkValue;
            Unsafe.Write(pointer, (ushort)19778);
            pointer = Unsafe.Add<ushort>(pointer, 1);
            Unsafe.Write(pointer, fileSize);
            pointer = Unsafe.Add<uint>(pointer, 1);
            pointer = Unsafe.Add<ushort>(pointer, 2);
            Unsafe.Write(pointer, offsetData);

            Assert.AreEqual(fileHeader, benchmarkValue);
        }
    }

    [TestClass]
    public class InfoHeaderTests
    {
        [DataTestMethod]
        [DataRow(uint.MaxValue, int.MaxValue, int.MaxValue, ushort.MaxValue, ushort.MaxValue, uint.MaxValue, uint.MaxValue, int.MaxValue, int.MaxValue, uint.MaxValue, uint.MaxValue)]
        [DataRow(uint.MinValue, int.MinValue, int.MinValue, ushort.MinValue, ushort.MinValue, uint.MinValue, uint.MinValue, int.MinValue, int.MinValue, uint.MinValue, uint.MinValue)]
        public unsafe void TestCreation(uint structSize,
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
            InfoHeader infoHeader = new(structSize,
                                        imageWidth,
                                        imageHeight,
                                        planes,
                                        bitPerPixelCount,
                                        compression,
                                        sizeImage,
                                        xPixelsPerMeter,
                                        yPixelsPerMeter,
                                        colorsUsed,
                                        colorsImportant);

            InfoHeader benchmarkValue = new();
            var pointer = (void*)&benchmarkValue;
            Unsafe.Write(pointer, structSize);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, imageWidth);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, imageHeight);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, planes);
            pointer = Unsafe.Add<ushort>(pointer, 1);
            Unsafe.Write(pointer, bitPerPixelCount);
            pointer = Unsafe.Add<ushort>(pointer, 1);
            Unsafe.Write(pointer, compression);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, sizeImage);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, xPixelsPerMeter);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, yPixelsPerMeter);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, colorsUsed);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, colorsImportant);

            Assert.AreEqual(infoHeader, benchmarkValue);
        }

        [DataTestMethod]
        [DataRow(uint.MaxValue, int.MaxValue, int.MaxValue, ushort.MaxValue, ushort.MaxValue, uint.MaxValue, uint.MaxValue, int.MaxValue, int.MaxValue, uint.MaxValue, uint.MaxValue)]
        [DataRow(uint.MinValue, int.MinValue, int.MinValue, ushort.MinValue, ushort.MinValue, uint.MinValue, uint.MinValue, int.MinValue, int.MinValue, uint.MinValue, uint.MinValue)]
        public unsafe void TestGetters(uint structSize,
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
            InfoHeader infoHeader = new(structSize,
                                        imageWidth,
                                        imageHeight,
                                        planes,
                                        bitPerPixelCount,
                                        compression,
                                        sizeImage,
                                        xPixelsPerMeter,
                                        yPixelsPerMeter,
                                        colorsUsed,
                                        colorsImportant);

            Assert.AreEqual(structSize, infoHeader.StructSize);
            Assert.AreEqual(imageWidth, infoHeader.ImageWidth);
            Assert.AreEqual(infoHeader.ImageHeight, imageHeight);
            Assert.AreEqual(planes, infoHeader.Planes);
            Assert.AreEqual(bitPerPixelCount, infoHeader.BitPerPixelCount);
            Assert.AreEqual(compression, infoHeader.Compression);
            Assert.AreEqual(sizeImage, infoHeader.SizeImage);
            Assert.AreEqual(xPixelsPerMeter, infoHeader.XPixelsPerMeter);
            Assert.AreEqual(yPixelsPerMeter, infoHeader.YPixelsPerMeter);
            Assert.AreEqual(colorsUsed, infoHeader.ColorsUsed);
            Assert.AreEqual(colorsImportant, infoHeader.ColorsImportant);
        }

        [DataTestMethod]
        [DataRow(uint.MaxValue, int.MaxValue, int.MaxValue, ushort.MaxValue, ushort.MaxValue, uint.MaxValue, uint.MaxValue, int.MaxValue, int.MaxValue, uint.MaxValue, uint.MaxValue)]
        [DataRow(uint.MinValue, int.MinValue, int.MinValue, ushort.MinValue, ushort.MinValue, uint.MinValue, uint.MinValue, int.MinValue, int.MinValue, uint.MinValue, uint.MinValue)]
        public unsafe void TestRotation(uint structSize,
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
            InfoHeader infoHeader = new(structSize,
                                        imageWidth,
                                        imageHeight,
                                        planes,
                                        bitPerPixelCount,
                                        compression,
                                        sizeImage,
                                        xPixelsPerMeter,
                                        yPixelsPerMeter,
                                        colorsUsed,
                                        colorsImportant);

            InfoHeader rotatedHeader = new(structSize,
                                            imageHeight,
                                            imageWidth,
                                            planes,
                                            bitPerPixelCount,
                                            compression,
                                            sizeImage,
                                            yPixelsPerMeter,
                                            xPixelsPerMeter,
                                            colorsUsed,
                                            colorsImportant);

            Assert.AreEqual(infoHeader.GetRotatedHeader(), rotatedHeader);
        }

        [DataTestMethod]
        [DataRow(uint.MaxValue, int.MaxValue, int.MaxValue, ushort.MaxValue, ushort.MaxValue, uint.MaxValue, uint.MaxValue, int.MaxValue, int.MaxValue, uint.MaxValue, uint.MaxValue)]
        [DataRow(uint.MinValue, int.MinValue, int.MinValue, ushort.MinValue, ushort.MinValue, uint.MinValue, uint.MinValue, int.MinValue, int.MinValue, uint.MinValue, uint.MinValue)]
        public unsafe void TestSetters(uint structSize,
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
            InfoHeader infoHeader = new()
            {
                StructSize = structSize,
                ImageWidth = imageWidth,
                ImageHeight = imageHeight,
                Planes = planes,
                BitPerPixelCount = bitPerPixelCount,
                Compression = compression,
                SizeImage = sizeImage,
                XPixelsPerMeter = xPixelsPerMeter,
                YPixelsPerMeter = yPixelsPerMeter,
                ColorsUsed = colorsUsed,
                ColorsImportant = colorsImportant,
            };

            InfoHeader benchmarkValue = new();
            var pointer = (void*)&benchmarkValue;
            Unsafe.Write(pointer, structSize);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, imageWidth);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, imageHeight);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, planes);
            pointer = Unsafe.Add<ushort>(pointer, 1);
            Unsafe.Write(pointer, bitPerPixelCount);
            pointer = Unsafe.Add<ushort>(pointer, 1);
            Unsafe.Write(pointer, compression);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, sizeImage);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, xPixelsPerMeter);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, yPixelsPerMeter);
            pointer = Unsafe.Add<int>(pointer, 1);
            Unsafe.Write(pointer, colorsUsed);
            pointer = Unsafe.Add<uint>(pointer, 1);
            Unsafe.Write(pointer, colorsImportant);

            Assert.AreEqual(infoHeader, benchmarkValue);
        }
    }

    [TestClass]
    public class Pixel16FiveFiveFiveTests
    {
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)31)]
        [DataRow((byte)0, (byte)31, (byte)0)]
        [DataRow((byte)0, (byte)31, (byte)31)]
        [DataRow((byte)31, (byte)0, (byte)0)]
        [DataRow((byte)31, (byte)0, (byte)31)]
        [DataRow((byte)31, (byte)31, (byte)0)]
        [DataRow((byte)31, (byte)31, (byte)31)]
        public unsafe void TestGetColor(byte b, byte g, byte r)
        {
            Pixel16FiveFiveFive pixel = new();
            Unsafe.Write(&pixel, b + g * 32 + r * 1024);

            Assert.AreEqual(pixel.Blue, b);
            Assert.AreEqual(pixel.Green, g);
            Assert.AreEqual(pixel.Red, r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)31)]
        [DataRow((byte)0, (byte)31, (byte)0)]
        [DataRow((byte)0, (byte)31, (byte)31)]
        [DataRow((byte)31, (byte)0, (byte)0)]
        [DataRow((byte)31, (byte)0, (byte)31)]
        [DataRow((byte)31, (byte)31, (byte)0)]
        [DataRow((byte)31, (byte)31, (byte)31)]
        public unsafe void TestInversColor(byte b, byte g, byte r)
        {
            Pixel16FiveFiveFive pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
            };

            pixel.Invers();

            Assert.AreEqual(pixel.Blue, 31 - b);
            Assert.AreEqual(pixel.Green, 31 - g);
            Assert.AreEqual(pixel.Red, 31 - r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)31)]
        [DataRow((byte)0, (byte)31, (byte)0)]
        [DataRow((byte)0, (byte)31, (byte)31)]
        [DataRow((byte)31, (byte)0, (byte)0)]
        [DataRow((byte)31, (byte)0, (byte)31)]
        [DataRow((byte)31, (byte)31, (byte)0)]
        [DataRow((byte)31, (byte)31, (byte)31)]
        public unsafe void TestSetColor(byte b, byte g, byte r)
        {
            Pixel16FiveFiveFive pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
            };

            Pixel16FiveFiveFive benchmarkValue = new();
            Unsafe.Write(&benchmarkValue, b + g * 32 + r * 1024);
            Assert.AreEqual(benchmarkValue, pixel);
        }
    }

    [TestClass]
    public class Pixel16FiveSixFiveTests
    {
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)31)]
        [DataRow((byte)0, (byte)63, (byte)0)]
        [DataRow((byte)0, (byte)63, (byte)31)]
        [DataRow((byte)31, (byte)0, (byte)0)]
        [DataRow((byte)31, (byte)0, (byte)31)]
        [DataRow((byte)31, (byte)63, (byte)0)]
        [DataRow((byte)31, (byte)63, (byte)31)]
        public unsafe void TestGetColor(byte b, byte g, byte r)
        {
            Pixel16FiveSixFive pixel = new();
            Unsafe.Write(&pixel, b + g * 32 + r * 2048);

            Assert.AreEqual(pixel.Blue, b);
            Assert.AreEqual(pixel.Green, g);
            Assert.AreEqual(pixel.Red, r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)31)]
        [DataRow((byte)0, (byte)63, (byte)0)]
        [DataRow((byte)0, (byte)63, (byte)31)]
        [DataRow((byte)31, (byte)0, (byte)0)]
        [DataRow((byte)31, (byte)0, (byte)31)]
        [DataRow((byte)31, (byte)63, (byte)0)]
        [DataRow((byte)31, (byte)63, (byte)31)]
        public unsafe void TestInversColor(byte b, byte g, byte r)
        {
            Pixel16FiveSixFive pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
            };

            pixel.Invers();

            Assert.AreEqual(pixel.Blue, 31 - b);
            Assert.AreEqual(pixel.Green, 63 - g);
            Assert.AreEqual(pixel.Red, 31 - r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)31)]
        [DataRow((byte)0, (byte)63, (byte)0)]
        [DataRow((byte)0, (byte)63, (byte)31)]
        [DataRow((byte)31, (byte)0, (byte)0)]
        [DataRow((byte)31, (byte)0, (byte)31)]
        [DataRow((byte)31, (byte)63, (byte)0)]
        [DataRow((byte)31, (byte)63, (byte)31)]
        public unsafe void TestSetColor(byte b, byte g, byte r)
        {
            Pixel16FiveSixFive pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
            };

            Pixel16FiveSixFive benchmarkValue = new();
            Unsafe.Write(&benchmarkValue, b + g * 32 + r * 2048);
            Assert.AreEqual(benchmarkValue, pixel);
        }
    }

    [TestClass]
    public class Pixel16Tests
    {
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)0, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)31, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)31, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)0, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)0, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)31, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)31, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)0, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)0, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)63, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)63, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)0, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)0, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)63, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)63, (byte)31, SchemeType.FiveSixFive)]
        public unsafe void TestGetColor(byte b, byte g, byte r, Pixel16.SchemeType schemeType)
        {
            Pixel16 pixel = new();
            Unsafe.Write(&pixel, b + g * 32 + r * 1024 * (schemeType is SchemeType.FiveSixFive ? 2 : 1));

            Assert.AreEqual(pixel.GetColorComponent(ComponentId.B, schemeType), b);
            Assert.AreEqual(pixel.GetColorComponent(ComponentId.G, schemeType), g);
            Assert.AreEqual(pixel.GetColorComponent(ComponentId.R, schemeType), r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)0, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)31, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)31, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)0, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)0, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)31, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)31, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)0, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)0, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)63, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)63, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)0, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)0, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)63, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)63, (byte)31, SchemeType.FiveSixFive)]
        public unsafe void TestInversColor(byte b, byte g, byte r, SchemeType schemeType)
        {
            Pixel16 pixel = new();
            pixel.SetColorComponent(b, ComponentId.B, schemeType);
            pixel.SetColorComponent(g, ComponentId.G, schemeType);
            pixel.SetColorComponent(r, ComponentId.R, schemeType);

            pixel.Invers(schemeType);

            Pixel16 benchmarkValue = new();
            benchmarkValue.SetColorComponent((byte)(31 - r), ComponentId.R, schemeType);
            benchmarkValue.SetColorComponent((byte)((schemeType is SchemeType.FiveFiveFive ? 31 : 63) - g), ComponentId.G, schemeType);
            benchmarkValue.SetColorComponent((byte)(31 - b), ComponentId.B, schemeType);

            Assert.AreEqual(benchmarkValue, pixel);
        }

        [DataTestMethod]
        [ExpectedException(typeof(ColorValueOutOfRangeException))]
        [DataRow((byte)0, (byte)0, (byte)32, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)32, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)32, (byte)32, SchemeType.FiveFiveFive)]
        [DataRow((byte)32, (byte)0, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)32, (byte)0, (byte)32, SchemeType.FiveFiveFive)]
        [DataRow((byte)32, (byte)32, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)32, (byte)32, (byte)32, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)0, (byte)32, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)64, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)64, (byte)32, SchemeType.FiveSixFive)]
        [DataRow((byte)32, (byte)0, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)32, (byte)0, (byte)32, SchemeType.FiveSixFive)]
        [DataRow((byte)32, (byte)64, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)32, (byte)64, (byte)32, SchemeType.FiveSixFive)]
        public void TestPixel16BadeRange(byte b, byte g, byte r, Pixel16.SchemeType schemeType) => TestSetColor(b, g, r, schemeType);

        [TestMethod]
        public void TestPixel16Range()
        {
            foreach (var type in new[] { SchemeType.FiveFiveFive, SchemeType.FiveSixFive })
            {
                for (byte b = 0; b < 32; ++b)
                {
                    for (byte g = 0; g < (type is Pixel16.SchemeType.FiveFiveFive ? 32 : 64); ++g)
                    {
                        for (byte r = 0; r < 32; ++r)
                        {
                            TestSetColor(b, g, r, type);
                        }
                    }
                }
            }
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)0, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)31, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)31, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)0, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)0, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)31, (byte)0, SchemeType.FiveFiveFive)]
        [DataRow((byte)31, (byte)31, (byte)31, SchemeType.FiveFiveFive)]
        [DataRow((byte)0, (byte)0, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)0, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)63, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)0, (byte)63, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)0, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)0, (byte)31, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)63, (byte)0, SchemeType.FiveSixFive)]
        [DataRow((byte)31, (byte)63, (byte)31, SchemeType.FiveSixFive)]
        public unsafe void TestSetColor(byte b, byte g, byte r, Pixel16.SchemeType schemeType)
        {
            Pixel16 pixel = new();
            pixel.SetColorComponent(b, ComponentId.B, schemeType);
            pixel.SetColorComponent(g, ComponentId.G, schemeType);
            pixel.SetColorComponent(r, ComponentId.R, schemeType);

            Pixel16 benchmarkValue = new();
            Unsafe.Write(&benchmarkValue, b + g * 32 + r * schemeType switch
            {
                SchemeType.FiveFiveFive => 1024,
                SchemeType.FiveSixFive => 2048,
            });
            Assert.AreEqual(benchmarkValue, pixel);
        }
    }

    [TestClass]
    public class Pixel24Tests
    {
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)255)]
        [DataRow((byte)0, (byte)255, (byte)0)]
        [DataRow((byte)0, (byte)255, (byte)255)]
        [DataRow((byte)255, (byte)0, (byte)0)]
        [DataRow((byte)255, (byte)0, (byte)255)]
        [DataRow((byte)255, (byte)255, (byte)0)]
        [DataRow((byte)255, (byte)255, (byte)255)]
        public unsafe void TestGetColor(byte b, byte g, byte r)
        {
            Pixel24 pixel = new();
            Unsafe.Write(&pixel, b + g * 256 + r * 65536);

            Assert.AreEqual(pixel.Blue, b);
            Assert.AreEqual(pixel.Green, g);
            Assert.AreEqual(pixel.Red, r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)255)]
        [DataRow((byte)0, (byte)255, (byte)0)]
        [DataRow((byte)0, (byte)255, (byte)255)]
        [DataRow((byte)255, (byte)0, (byte)0)]
        [DataRow((byte)255, (byte)0, (byte)255)]
        [DataRow((byte)255, (byte)255, (byte)0)]
        [DataRow((byte)255, (byte)255, (byte)255)]
        public unsafe void TestInversColor(byte b, byte g, byte r)
        {
            Pixel24 pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
            };

            pixel.Invers();

            Assert.AreEqual(pixel.Blue, 255 - b);
            Assert.AreEqual(pixel.Green, 255 - g);
            Assert.AreEqual(pixel.Red, 255 - r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)255)]
        [DataRow((byte)0, (byte)255, (byte)0)]
        [DataRow((byte)0, (byte)255, (byte)255)]
        [DataRow((byte)255, (byte)0, (byte)0)]
        [DataRow((byte)255, (byte)0, (byte)255)]
        [DataRow((byte)255, (byte)255, (byte)0)]
        [DataRow((byte)255, (byte)255, (byte)255)]
        public unsafe void TestSetColor(byte b, byte g, byte r)
        {
            Pixel24 pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
            };

            Pixel24 benchmarkValue = new();
            Unsafe.Write(&benchmarkValue, b + g * 256 + r * 65536);
            Assert.AreEqual(benchmarkValue, pixel);
        }
    }

    [TestClass]
    public class Pixel32Tests
    {
        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)255)]
        [DataRow((byte)0, (byte)255, (byte)0)]
        [DataRow((byte)0, (byte)255, (byte)255)]
        [DataRow((byte)255, (byte)0, (byte)0)]
        [DataRow((byte)255, (byte)0, (byte)255)]
        [DataRow((byte)255, (byte)255, (byte)0)]
        [DataRow((byte)255, (byte)255, (byte)255)]
        public unsafe void TestGetColor(byte b, byte g, byte r)
        {
            Pixel32 pixel = new();
            Unsafe.Write(&pixel, b + g * 256 + r * 65536);

            Assert.AreEqual(pixel.Blue, b);
            Assert.AreEqual(pixel.Green, g);
            Assert.AreEqual(pixel.Red, r);
            Assert.AreEqual(pixel.Reserved, 0);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)255)]
        [DataRow((byte)0, (byte)255, (byte)0)]
        [DataRow((byte)0, (byte)255, (byte)255)]
        [DataRow((byte)255, (byte)0, (byte)0)]
        [DataRow((byte)255, (byte)0, (byte)255)]
        [DataRow((byte)255, (byte)255, (byte)0)]
        [DataRow((byte)255, (byte)255, (byte)255)]
        public unsafe void TestInversColor(byte b, byte g, byte r)
        {
            Pixel32 pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
            };

            pixel.Invers();

            Assert.AreEqual(pixel.Blue, 255 - b);
            Assert.AreEqual(pixel.Green, 255 - g);
            Assert.AreEqual(pixel.Red, 255 - r);
        }

        [DataTestMethod]
        [DataRow((byte)0, (byte)0, (byte)0)]
        [DataRow((byte)0, (byte)0, (byte)255)]
        [DataRow((byte)0, (byte)255, (byte)0)]
        [DataRow((byte)0, (byte)255, (byte)255)]
        [DataRow((byte)255, (byte)0, (byte)0)]
        [DataRow((byte)255, (byte)0, (byte)255)]
        [DataRow((byte)255, (byte)255, (byte)0)]
        [DataRow((byte)255, (byte)255, (byte)255)]
        public unsafe void TestSetColor(byte b, byte g, byte r)
        {
            Pixel32 pixel = new()
            {
                Red = r,
                Green = g,
                Blue = b,
                Reserved = 0
            };

            Pixel32 benchmarkValue = new();
            Unsafe.Write(&benchmarkValue, b + g * 256 + r * 65536);
            Assert.AreEqual(benchmarkValue, pixel);
        }
    }

    [TestClass]
    public class ReadWriteTests
    {
        public static void TestReadWriteGeneric<TPixel>(int w, int h) where TPixel : struct, IPixel
        {
            TPixel[] pixeles = new TPixel[w * h];
            Random random = new(698);

            for (var i = 0; i < pixeles.Length; ++i)
                pixeles[i] = new TPixel { Red = (byte)random.Next(32), Green = (byte)random.Next(32), Blue = (byte)random.Next(32) };

            BitmapWithoutPalette<TPixel> bitmap = new(pixeles, w, h, null);

            BitmapFactory.WriteBmp("test.bmp", bitmap);

            var readedBmp = BitmapFactory.ReadBmpFromFile("test.bmp").bitmap;

            if (bitmap.Width != readedBmp.Width
                || bitmap.Height != readedBmp.Height
                || bitmap.XPixelsPerMeter != readedBmp.XPixelsPerMeter
                || bitmap.YPixelsPerMeter != readedBmp.YPixelsPerMeter
                || bitmap.GetType() != bitmap.GetType())
                throw new Exception();

            for (var i = 0; i < bitmap.Width; ++i)
                for (var j = 0; j < bitmap.Height; ++j)
                {
                    if (!bitmap[i, j].Equals((readedBmp as BitmapWithoutPalette<TPixel>)[i, j]))
                        throw new Exception();
                }

            File.Delete("test.bmp");
        }

        [DataTestMethod]
        [DataRow(1, 1)]
        [DataRow(16, 1)]
        [DataRow(1, 16)]
        [DataRow(32, 32)]
        [DataRow(47, 69)]
        [DataRow(1280, 640)]
        [DataRow(640, 1280)]
        public void TestReadWrite(int w, int h)
        {
            TestReadWriteGeneric<Pixel16FiveFiveFive>(w, h);
            TestReadWriteGeneric<Pixel16FiveSixFive>(w, h);
            TestReadWriteGeneric<Pixel24>(w, h);
            TestReadWriteGeneric<Pixel32>(w, h);
        }
    }
}