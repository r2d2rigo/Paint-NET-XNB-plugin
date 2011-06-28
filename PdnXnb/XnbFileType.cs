using System;
using PaintDotNet;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PdnXnb
{
    public class XnbFileType : FileType
    {
        private static Bitmap defaultBitmap;

        static XnbFileType()
        {
            defaultBitmap = new Bitmap(256, 256);
        }

        public XnbFileType()
            : base("XNB", FileTypeFlags.SupportsLoading, new string[] { ".xnb" })
        {
        }

        protected override Document OnLoad(Stream input)
        {
            BinaryReader reader = new BinaryReader(input);

            byte[] magicNumber = reader.ReadBytes(3);

            if ((magicNumber[0] != 0x58) && (magicNumber[1] != 0x4E) && (magicNumber[2] != 0x42))
            {
                MessageBox.Show("Invalid XNB file header.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            char platform = Convert.ToChar(reader.ReadByte());

            if (platform != 'w' && platform != 'm' && platform != 'x')
            {
                MessageBox.Show("XNB target platform not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            ushort version = reader.ReadUInt16();
            int ver = version & 0x80ff;
            int profile = version & 0x7f00;

            if (ver == 0x8005)
            {
                MessageBox.Show("Compressed XNB not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            int totalLength = reader.ReadInt32();

            reader.ReadByte();

            string type = reader.ReadString();

            if (!type.Contains("Texture2DReader"))
            {
                MessageBox.Show("XNB is not a valid Texture2D resource.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            if (!type.Contains("Version=4.0.0.0"))
            {
                MessageBox.Show("XNB invalid version, only 4.0.0.0 supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }       

            reader.ReadBytes(6);

            PixelFormat imageFormat = (PixelFormat)reader.ReadInt32();

            if (!IsSupportedPixelFormat(imageFormat))
            {
                MessageBox.Show("Pixel format " + imageFormat.ToString() + " not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int numLevels = reader.ReadInt32();

            if (numLevels > 1)
            {
                MessageBox.Show("Image contains mipmaps but won't be loaded.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Bitmap finalBitmap = new Bitmap(width, height);

            int dataSize = reader.ReadInt32();

            byte[] sourceData = new byte[dataSize];
            sourceData = reader.ReadBytes(dataSize);

            byte[] finalData = new byte[width * height * 4];

            bool needsSwapping = false;

            switch (imageFormat)
            {
                case PixelFormat.Alpha8:
                    {
                        for (int j = 0; j < dataSize; j++)
                        {
                            Array.Copy(new Alpha8Pixel(sourceData.Subarray(j, 1)).ToRgba((platform == 'x')).Data, 0, finalData, j * 4, 4);
                        }
                    }
                    break;
                case PixelFormat.Dxt1:
                    {
                        finalData = ManagedSquish.SquishWrapper.DecompressImage(sourceData, width, height, SquishFlags.Dxt1);
                        needsSwapping = true;
                    }
                    break;
                case PixelFormat.Dxt3:
                    {
                        finalData = ManagedSquish.SquishWrapper.DecompressImage(sourceData, width, height, SquishFlags.Dxt3);
                        needsSwapping = true;
                    }
                    break;
                case PixelFormat.Dxt5:
                    {
                        finalData = ManagedSquish.SquishWrapper.DecompressImage(sourceData, width, height, SquishFlags.Dxt5);
                        needsSwapping = true;
                    }
                    break;
                case PixelFormat.Bgr565:
                    {
                        for (int j = 0; j < dataSize / 2; j++)
                        {
                            Array.Copy(new Bgr565Pixel(sourceData.Subarray(j * 2, 2)).ToRgba((platform == 'x')).Data, 0, finalData, j * 4, 4);
                        }
                    }
                    break;
                case PixelFormat.Bgra4444:
                    {
                        for (int j = 0; j < dataSize / 2; j++)
                        {
                            Array.Copy(new Bgra4444Pixel(sourceData.Subarray(j * 2, 2)).ToRgba((platform == 'x')).Data, 0, finalData, j * 4, 4);
                        }
                    }
                    break;
                case PixelFormat.Bgra5551:
                    {
                        for (int j = 0; j < dataSize / 2; j++)
                        {
                            Array.Copy(new Bgra5551Pixel(sourceData.Subarray(j * 2, 2)).ToRgba((platform == 'x')).Data, 0, finalData, j * 4, 4);
                        }
                    }
                    break;
                case PixelFormat.Rgba:
                    {
                        for (int j = 0; j < dataSize / 4; j++)
                        {
                            Array.Copy(new RgbaPixel(sourceData.Subarray(j * 4, 4)).ToRgba((platform == 'x')).Data, 0, finalData, j * 4, 4);
                        }

                        needsSwapping = true;
                    }
                    break;
            }

            if (needsSwapping)
            {
                for (int j = 0; j < finalData.Length; j += 4)
                {
                    byte tmp = finalData[j];
                    finalData[j] = finalData[j + 2];
                    finalData[j + 2] = tmp;
                }
            }

            BitmapData bmpData = finalBitmap.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(finalData, 0, bmpData.Scan0, finalData.Length);
            finalBitmap.UnlockBits(bmpData);

            reader.Close();
            
            return Document.FromImage(finalBitmap);
        }

        private bool IsSupportedPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Alpha8:
                case PixelFormat.Bgr565:
                case PixelFormat.Bgra4444:
                case PixelFormat.Bgra5551:
                case PixelFormat.Dxt1:
                case PixelFormat.Dxt3:
                case PixelFormat.Dxt5:
                case PixelFormat.Rgba:
                    return true;
                default:
                    return false;
            }
        }
    }
}