using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdnXnb
{
    public class Alpha8Pixel : BasePixel
    {
        public override int DataSize
        {
            get { return 1; }
        }

        public Alpha8Pixel(byte[] pixelData)
        {
            this.Data = pixelData;
        }

        public override BasePixel ToRgba(bool bigEndian)
        {
            byte[] newData = new byte[4];

            newData[0] = 0x00;
            newData[1] = 0x00;
            newData[2] = 0x00;
            newData[3] = Data[0];

            return new Alpha8Pixel(newData);
        }
    }
}
