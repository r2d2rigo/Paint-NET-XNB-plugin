using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdnXnb
{
    public abstract class BasePixel
    {
        private byte[] data;

        public byte[] Data
        {
            get { return data; }
            set { if (value.Length == DataSize) data = value; }
        }
        public abstract int DataSize { get; }

        public BasePixel()
        {
            data = new byte[DataSize];
        }

        public abstract BasePixel ToRgba(bool bigEndian);
    }
}
