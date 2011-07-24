using System;
using PaintDotNet;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using PaintDotNet.Data.Dds;

namespace PdnXnb
{
    public class XnbFileType : PropertyBasedFileType
    {
        private static Bitmap defaultBitmap;

        static XnbFileType()
        {
            defaultBitmap = new Bitmap(256, 256);
        }

        public XnbFileType()
            : base("XNB", FileTypeFlags.SupportsSaving | FileTypeFlags.SupportsLoading, new string[] { ".xnb" })
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

            byte xnbVersion = reader.ReadByte();

            if (xnbVersion != 0x05)
            {
                MessageBox.Show("XNB version not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            byte flags = reader.ReadByte();

            if (flags == 0x80)
            {
                MessageBox.Show("Compressed XNB not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            int totalLength = reader.ReadInt32();

            byte numTypeReaders = reader.ReadByte();

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

            int typeVersion = reader.ReadInt32();

            byte numSharedResources = reader.ReadByte();
            byte nullSharedREsource = reader.ReadByte();

            PixelFormat imageFormat = (PixelFormat)reader.ReadInt32();

            if (!IsSupportedPixelFormat(imageFormat))
            {
                MessageBox.Show("Pixel format " + imageFormat.ToString() + " not supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return Document.FromImage(defaultBitmap);
            }

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int numLevels = reader.ReadInt32();

            Bitmap finalBitmap;

            Point[] mipmapSizes = GetMipmapSizes(width, height);

            int canvasWidth = 0;
            for (int i = 0; i < mipmapSizes.Length; i++)
            {
                canvasWidth += mipmapSizes[i].X;
            }

            if (numLevels > 1)
            {
                finalBitmap = new Bitmap(canvasWidth, height);
            }
            else
            {
                finalBitmap = new Bitmap(width, height);
            }

            int rectX = 0;

            for (int i = 0; i < numLevels; i++)
            {
                int mipmapWidth = mipmapSizes[i].X;
                int mipmapHeight = mipmapSizes[i].Y;

                int dataSize = reader.ReadInt32();

                byte[] sourceData = new byte[dataSize];
                sourceData = reader.ReadBytes(dataSize);

                byte[] finalData = new byte[mipmapWidth * mipmapHeight * 4];

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
                            finalData = ManagedSquish.SquishWrapper.DecompressImage(sourceData, mipmapWidth, mipmapHeight, SquishFlags.Dxt1);
                            needsSwapping = true;
                        }
                        break;
                    case PixelFormat.Dxt3:
                        {
                            finalData = ManagedSquish.SquishWrapper.DecompressImage(sourceData, mipmapWidth, mipmapHeight, SquishFlags.Dxt3);
                            needsSwapping = true;
                        }
                        break;
                    case PixelFormat.Dxt5:
                        {
                            finalData = ManagedSquish.SquishWrapper.DecompressImage(sourceData, mipmapWidth, mipmapHeight, SquishFlags.Dxt5);
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

                BitmapData bmpData = finalBitmap.LockBits(new Rectangle(rectX, 0, mipmapWidth, mipmapHeight), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for (int j = 0; j < mipmapHeight; j++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(finalData, mipmapWidth * j * 4, new IntPtr(bmpData.Scan0.ToInt32() + (bmpData.Stride * j)), mipmapWidth * 4);
                }
                finalBitmap.UnlockBits(bmpData);

                rectX += mipmapWidth;
            }
            reader.Close();

            Document newDocument = Document.FromImage(finalBitmap);

            if (numLevels > 1)
            {
                newDocument.Metadata.SetUserValue("Mipmaps", "true");
            }

            return newDocument;
        }

        public override ControlInfo OnCreateSaveConfigUI(PropertyCollection props)
        {
            ControlInfo saveWindow = PropertyBasedFileType.CreateDefaultSaveConfigUI(props);

            foreach (PixelFormat format in Enum.GetValues(typeof(PixelFormat)))
            {
                if (IsSupportedPixelFormat(format))
                {
                    saveWindow.FindControlForPropertyName(PropertyNames.FileFormat).SetValueDisplayName(format, format.ToString());
                }
            }
            //saveWindow.SetPropertyControlType(PropertyNames.CompressorType, PropertyControlType.RadioButton);
            //saveWindow.FindControlForPropertyName(PropertyNames.CompressorType).SetValueDisplayName(DdsCompressorType.RangeFit, PdnResources.GetString("DdsFileType.SaveConfigWidget.RangeFit.Text"));
            //saveWindow.FindControlForPropertyName(PropertyNames.CompressorType).SetValueDisplayName(DdsCompressorType.ClusterFit, PdnResources.GetString("DdsFileType.SaveConfigWidget.ClusterFit.Text"));
            //saveWindow.FindControlForPropertyName(PropertyNames.CompressorType).SetValueDisplayName(DdsCompressorType.IterativeFit, PdnResources.GetString("DdsFileType.SaveConfigWidget.IterativeFit.Text"));
            //saveWindow.SetPropertyControlType(PropertyNames.ErrorMetric, PropertyControlType.RadioButton);
            //saveWindow.FindControlForPropertyName(PropertyNames.ErrorMetric).SetValueDisplayName(DdsErrorMetric.Perceptual, PdnResources.GetString("DdsFileType.SaveConfigWidget.Perceptual.Text"));
            //saveWindow.FindControlForPropertyName(PropertyNames.ErrorMetric).SetValueDisplayName(DdsErrorMetric.Uniform, PdnResources.GetString("DdsFileType.SaveConfigWidget.Uniform.Text"));
            //saveWindow.SetPropertyControlValue(PropertyNames.GenerateMipMaps, ControlInfoPropertyNames.DisplayName, string.Empty);
            //saveWindow.SetPropertyControlValue(PropertyNames.GenerateMipMaps, ControlInfoPropertyNames.Description, PdnResources.GetString("DdsFileType.SaveConfigWidget.GenerateMipMaps.Text"));
            //saveWindow.SetPropertyControlValue(PropertyNames.WeightColorByAlpha, ControlInfoPropertyNames.DisplayName, PdnResources.GetString("DdsFileType.SaveConfigWidget.AdditionalOptions.Text"));
            //saveWindow.SetPropertyControlValue(PropertyNames.WeightColorByAlpha, ControlInfoPropertyNames.Description, PdnResources.GetString("DdsFileType.SaveConfigWidget.WeightColourByAlpha"));
            //saveWindow.SetPropertyControlValue(PropertyNames.MipMapResamplingAlgorithm, ControlInfoPropertyNames.DisplayName, string.Empty);
            //saveWindow.FindControlForPropertyName(PropertyNames.MipMapResamplingAlgorithm).SetValueDisplayName(ResamplingAlgorithm.SuperSampling, PdnResources.GetString("DdsFileType.SaveConfigWidget.MipMapResamplingAlgorithm.SuperSampling"));
            //saveWindow.FindControlForPropertyName(PropertyNames.MipMapResamplingAlgorithm).SetValueDisplayName(ResamplingAlgorithm.Bicubic, PdnResources.GetString("DdsFileType.SaveConfigWidget.MipMapResamplingAlgorithm.Bicubic"));
            //saveWindow.FindControlForPropertyName(PropertyNames.MipMapResamplingAlgorithm).SetValueDisplayName(ResamplingAlgorithm.Bilinear, PdnResources.GetString("DdsFileType.SaveConfigWidget.MipMapResamplingAlgorithm.Bilinear"));
            //saveWindow.FindControlForPropertyName(PropertyNames.MipMapResamplingAlgorithm).SetValueDisplayName(ResamplingAlgorithm.NearestNeighbor, PdnResources.GetString("DdsFileType.SaveConfigWidget.MipMapResamplingAlgorithm.NearestNeighbor"));

            return saveWindow;
        }

        public override PropertyCollection OnCreateSavePropertyCollection()
        {
            List<Property> props = new List<Property>
            {
                StaticListChoiceProperty.CreateForEnum<PixelFormat>(PropertyNames.FileFormat, PixelFormat.Rgba, false),
                //new StaticListChoiceProperty((PropertyNames) 1, new object[] { (DdsCompressorType) 1, (DdsCompressorType) 0, (DdsCompressorType) 2 }, 1),
                //new StaticListChoiceProperty((PropertyNames) 2, new object[] { (DdsErrorMetric) 1, (DdsErrorMetric) 0 }, 1),
                //new BooleanProperty(((PropertyNames) 3, 0, 1),
                //new BooleanProperty((PropertyNames) 4, 0),
                //new StaticListChoiceProperty((PropertyNames) 5, new object[] { (ResamplingAlgorithm) 3, (ResamplingAlgorithm) 1, (ResamplingAlgorithm) 2, (ResamplingAlgorithm) 0 }, 0)
            };

            List<PropertyCollectionRule> rules = new List<PropertyCollectionRule> {
                //new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>((PropertyNames) 2, (PropertyNames) 0, new object[] { (DdsFileFormat) 5, (DdsFileFormat) 3, (DdsFileFormat) 8, (DdsFileFormat) 7, (DdsFileFormat) 10, (DdsFileFormat) 9, (DdsFileFormat) 6, (DdsFileFormat) 4 }, 0),
                //new ReadOnlyBoundToValueRule<object, StaticListChoiceProperty>((PropertyNames) 1, (PropertyNames) 0, new object[] { (DdsFileFormat) 5, (DdsFileFormat) 3, (DdsFileFormat) 8, (DdsFileFormat) 7, (DdsFileFormat) 10, (DdsFileFormat) 9, (DdsFileFormat) 6, (DdsFileFormat) 4 }, 0)
            };

            //Pair<object, object>[] CS$0$0005 = new Pair<object, object>[] { Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_A8B8G8R8), Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_A8R8G8B8), Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_A4R4G4B4), Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_A1R5G5B5), Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_R5G6B5), Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_R8G8B8), Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_X8B8G8R8), Pair.Create<object, object>(PropertyNames.FileFormat, DdsFileFormat.DDS_FORMAT_X8R8G8B8), Pair.Create<object, object>(PropertyNames.CompressorType, DdsCompressorType.RangeFit) };
            //rules.Add(new ReadOnlyBoundToNameValuesRule(PropertyNames.WeightColorByAlpha, false, CS$0$0005));
            //rules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.MipMapResamplingAlgorithm, PropertyNames.GenerateMipMaps, true));
            
            return new PropertyCollection(props, rules);
        }

        protected override void OnSaveT(Document input, Stream output, PropertyBasedSaveConfigToken token, Surface scratchSurface, ProgressEventHandler callback)
        {
            scratchSurface.Clear(ColorBgra.Transparent);
            using (RenderArgs ra = new RenderArgs(scratchSurface))
            {
                input.Render(ra, true);
            }

            token.GetProperty<StaticListChoiceProperty>(PropertyNames.FileFormat).Value = PixelFormat.Dxt5;

            //DdsFileFormat fileFormat = (DdsFileFormat)token.GetProperty<StaticListChoiceProperty>(PropertyNames.FileFormat).Value;
            //DdsCompressorType compressorType = (DdsCompressorType)token.GetProperty<StaticListChoiceProperty>(PropertyNames.CompressorType).Value;
            //DdsErrorMetric errorMetric = (DdsErrorMetric)token.GetProperty<StaticListChoiceProperty>(PropertyNames.ErrorMetric).Value;
            //bool weightColorByAlpha = token.GetProperty<BooleanProperty>(PropertyNames.WeightColorByAlpha).Value;
            //bool generateMipMaps = token.GetProperty<BooleanProperty>(PropertyNames.GenerateMipMaps).Value;
            //ResamplingAlgorithm mipMapResamplingAlgorithm = (ResamplingAlgorithm)token.GetProperty<StaticListChoiceProperty>(PropertyNames.MipMapResamplingAlgorithm).Value;
            new DdsFile().Save(output, scratchSurface, DdsFileFormat.DDS_FORMAT_A8B8G8R8, DdsCompressorType.ClusterFit, DdsErrorMetric.Perceptual, false, ResamplingAlgorithm.Bicubic, false, null);
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

        private Point[] GetMipmapSizes(int width, int height)
        {
            List<Point> sizes = new List<Point>();

            sizes.Add(new Point(width, height));

            while (width != 1 && height != 1)
            {
                width /= 2;
                if (width < 1)
                    width = 1;

                height /= 2;
                if (height < 1)
                    height = 1;

                sizes.Add(new Point(width, height));
            }

            return sizes.ToArray();
        }

        public enum PropertyNames
        {
            FileFormat
        }
    }
}