using System;
using PaintDotNet;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

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

            byte platform = reader.ReadByte();

            if (platform != 0x77 && platform != 0x6D)
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

            if (imageFormat != PixelFormat.Rgba && imageFormat != PixelFormat.Bgr565 && imageFormat != PixelFormat.Bgra4444 && imageFormat != PixelFormat.Bgra5551)
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

            int sourceX = 0;
            int rectWidth = width;
            int rectHeight = height;

            int dataSize = reader.ReadInt32();

            byte[] data = new byte[dataSize];
            data = reader.ReadBytes(dataSize);

            byte[] finalData = new byte[width * height * 4];

            switch (imageFormat)
            {
                case PixelFormat.Bgr565:
                    {
                        for (int j = 0; j < dataSize / 2; j++)
                        {
                            Array.Copy(new Bgr565Pixel(data.Subarray(j * 2, 2)).ToRgba().Data, 0, finalData, j * 4, 4);
                        }
                    }
                    break;
                case PixelFormat.Bgra4444:
                    {
                        for (int j = 0; j < dataSize / 2; j++)
                        {
                            Array.Copy(new Bgra4444Pixel(data.Subarray(j * 2, 2)).ToRgba().Data, 0, finalData, j * 4, 4);
                        }
                    }
                    break;
                case PixelFormat.Bgra5551:
                    {
                        for (int j = 0; j < dataSize / 2; j++)
                        {
                            Array.Copy(new Bgra5551Pixel(data.Subarray(j * 2, 2)).ToRgba().Data, 0, finalData, j * 4, 4);
                        }
                    }
                    break;
                case PixelFormat.Rgba:
                    {
                        finalData = data;

                        // GDI bitmap is Bgra
                        for (int j = 0; j < dataSize; j += 4)
                        {
                            byte tmp = finalData[j];
                            finalData[j] = finalData[j + 2];
                            finalData[j + 2] = tmp;
                        }
                    }
                    break;
            }

            BitmapData bmpData = finalBitmap.LockBits(new Rectangle(sourceX, 0, rectWidth, rectHeight), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(finalData, 0, bmpData.Scan0, finalData.Length);
            finalBitmap.UnlockBits(bmpData);

            reader.Close();

            return Document.FromImage(finalBitmap);
        }
    }
}