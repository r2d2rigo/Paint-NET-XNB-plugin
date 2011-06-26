using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdnXnb
{
    public class RgbaPixel : BasePixel
    {
        public override int DataSize
        {
            get { return 4; }
        }

        public RgbaPixel(byte[] pixelData)
        {
            this.Data = pixelData;
        }

        public override BasePixel ToRgba(bool bigEndian)
        {
            if (bigEndian)
            {
                byte tmp = this.Data[0];
                this.Data[0] = this.Data[3];
                this.Data[3] = tmp;
                tmp = this.Data[1];
                this.Data[1] = this.Data[2];
                this.Data[2] = tmp;
            }

            return this;
        }
    }
}
