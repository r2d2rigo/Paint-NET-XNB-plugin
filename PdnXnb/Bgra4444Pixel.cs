using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdnXnb
{
    public class Bgra4444Pixel : BasePixel
    {
        public override int DataSize
        {
            get { return 2; }
        }

        public Bgra4444Pixel(byte[] pixelData)
        {
            this.Data = pixelData;
        }

        public override BasePixel ToRgba(bool bigEndian)
        {
            byte upperByte = bigEndian == true ? Data[0] : Data[1];
            byte lowerByte = bigEndian == true ? Data[1] : Data[0];

            byte[] newData = new byte[4];

            newData[0] = (byte)((lowerByte & 0x0f) << 4);
            newData[1] = (byte)((lowerByte & 0xf0));
            newData[2] = (byte)((upperByte & 0x0f) << 4);
            newData[3] = (byte)(upperByte & 0xf0);

            return new RgbaPixel(newData);
        }
    }
}
