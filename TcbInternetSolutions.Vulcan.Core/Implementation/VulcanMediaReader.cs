namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.Core;
    using EPiServer.ServiceLocation;
    using System;

    /// <summary>
    /// Converts media data to byte array
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanMediaReader), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanMediaReader : IVulcanMediaReader
    {
        /// <summary>
        /// Reads complete media data to byte array
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public virtual byte[] ReadToEnd(MediaData media)
        {
            if (media?.BinaryData != null)
            {
                byte[] bytes = null;

                using (var s = media.BinaryData.OpenRead())
                {
                    bytes = ReadToEnd(s);
                }

                return bytes;
            }

            return null;
        }

        /// <summary>
        /// Reads full stream to byte array
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];
                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;

                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }

                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
    }
}
