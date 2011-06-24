using PaintDotNet;

namespace PdnXnb
{
    public class XnbFileTypeFactory : IFileTypeFactory
    {
        public FileType[] GetFileTypeInstances()
        {
            return new FileType[] { new XnbFileType() };
        }
    }
}
