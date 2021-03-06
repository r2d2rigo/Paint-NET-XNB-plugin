﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdnXnb
{
    public class Bgr565Pixel : BasePixel
    {
        public override int DataSize
        {
            get { return 2; }
        }

        public Bgr565Pixel(byte[] pixelData)
        {
            this.Data = pixelData;
        }

        public override BasePixel ToRgba(bool bigEndian)
        {
            byte upperByte = bigEndian == true ? Data[0] : Data[1];
            byte lowerByte = bigEndian == true ? Data[1] : Data[0];

            byte[] newData = new byte[4];

            newData[0] = (byte)((lowerByte & 0x1f) << 3);
            newData[1] = (byte)((upperByte & 0x07) << 5 | (lowerByte & 0xe0) >> 3);
            newData[2] = (byte)(upperByte & 0xf8);
            newData[3] = 0xff;

            return new RgbaPixel(newData);
        }
    }
}
