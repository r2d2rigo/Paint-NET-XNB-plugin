using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdnXnb
{
    public class RgbaPixel : BasePixel
    {
        private byte[] rgbaData;

        public override int DataSize
        {
            get { return 4; }
        }

        public RgbaPixel(byte[] pixelData)
        {
            this.Data = pixelData;
        }

        public override BasePixel ToRgba()
        {
            return this;
        }
    }
}
