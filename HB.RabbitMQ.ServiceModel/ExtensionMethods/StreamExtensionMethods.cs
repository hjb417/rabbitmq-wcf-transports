using System.IO;

namespace HB
{
    public static class StreamExtensionMethods
    {
        public static byte[] CopyToByteArray(this Stream stream)
        {
            var memStream = stream as MemoryStream;
            if (memStream != null)
            {
                return memStream.ToArray();
            }
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                buffer.Flush();
                return buffer.ToArray();
            }
        }
    }
}