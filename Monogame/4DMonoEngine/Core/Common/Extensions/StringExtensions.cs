using System.IO;
using System.IO.Compression;
using System.Text;

namespace _4DMonoEngine.Core.Common.Extensions
{
    public static class StringExtensions
    {
        public static string ZipCompress(this string value)
        {
            //Transform string into byte[]  
            var byteArray = new byte[value.Length];
            var indexBa = 0;
            foreach (var item in value.ToCharArray())
            {
                byteArray[indexBa++] = (byte)item;
            }

            //Prepare for compress
            var ms = new MemoryStream();
            var sw = new GZipStream(ms,
                CompressionMode.Compress);

            //Compress
            sw.Write(byteArray, 0, byteArray.Length);
            //Close, DO NOT FLUSH cause bytes will go missing...
            sw.Close();

            //Transform byte[] zip data to string
            byteArray = ms.ToArray();
            var sB = new StringBuilder(byteArray.Length);
            foreach (var item in byteArray)
            {
                sB.Append((char)item);
            }
            return sB.ToString();
        }

        public static string UnZipCompress(this string value)
        {
            //Transform string into byte[]
            var byteArray = new byte[value.Length];
            var indexBa = 0;
            foreach (var item in value.ToCharArray())
            {
                byteArray[indexBa++] = (byte)item;
            }

            //Prepare for decompress
            var ms = new MemoryStream(byteArray);
            var sr = new GZipStream(ms,
                CompressionMode.Decompress);

            //Reset variable to collect uncompressed result
            byteArray = new byte[byteArray.Length];

            //Decompress
            var rByte = sr.Read(byteArray, 0, byteArray.Length);

            //Transform byte[] unzip data to string
            var sB = new StringBuilder(rByte);
            //Read the number of bytes GZipStream red and do not a for each bytes in
            //resultByteArray;
            for (var i = 0; i < rByte; i++)
            {
                sB.Append((char)byteArray[i]);
            }
            sr.Close();
            return sB.ToString();
        }
    }
}