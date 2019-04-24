using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Indieteur.ChecksumZIP
{
    public static class ChecksumZIPHelper
    {
        public const int MD5_HASH_BYTE_LENGTH = 32;
        public const int MB_10_IN_BYTES = 10485760;
        public const int KB_4_IN_BYTES = 4096;
        public const float GB_10_TO_MB_10_RATIO = 0.0005f;
        public const string MAGIC_CONSTANT = "ChecksumZIP";




        public static int GetSuggestedBufferSize(string pathToFile, int min = KB_4_IN_BYTES, int max = MB_10_IN_BYTES, float ratio = GB_10_TO_MB_10_RATIO)
        {
            if (!File.Exists(pathToFile))
                throw new FileNotFoundException(pathToFile + " was not found.");
            if (min < 1)
                throw new ArgumentException("Minimum buffer size cannot be less than 1 byte.");
            if (max < 1)
                throw new ArgumentException("Maximum buffer size cannot be less than 1 byte.");

            FileInfo fInfo = new FileInfo(pathToFile);
            return IntClamp((int)(fInfo.Length * ratio), min, max);


        }

        public static int IntClamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            else if (value > max)
                return max;
            else
                return value;
        }
        public static string CalculateMD5(string filename, int bufferSize = 4096) // From - https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
        {
            if (bufferSize < 1)
                throw new ArgumentException("Buffer size cannot be less than 1 byte.");
            string hashString = null;
           
            using (var md5 = MD5.Create())
            {
                using (var stream = new BufferedStream(File.Open(filename, FileMode.Open), bufferSize))
                {
                    bool md5HashAtEnd = false;
                    int totalLengthOfChecksumZIPStoredHash = MAGIC_CONSTANT.Length + MD5_HASH_BYTE_LENGTH;
                    if (stream.Length > totalLengthOfChecksumZIPStoredHash)
                    {
                        stream.Seek(stream.Length - MAGIC_CONSTANT.Length, SeekOrigin.Begin);
                        byte[] byteToRead = new byte[MAGIC_CONSTANT.Length];
                        stream.Read(byteToRead, 0, byteToRead.Length);
                        if (Encoding.ASCII.GetString(byteToRead) == MAGIC_CONSTANT)
                            md5HashAtEnd = true;
                        else
                            goto noMAGICCONSTANT;
                        stream.SetLength(stream.Length - totalLengthOfChecksumZIPStoredHash);
                    }
                    noMAGICCONSTANT:
                    stream.Seek(0, SeekOrigin.Begin);
                    var hash = md5.ComputeHash(stream);
                    hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    if (md5HashAtEnd)
                    {
                        byte[] byteArray = Encoding.ASCII.GetBytes(hashString + MAGIC_CONSTANT);
                        stream.Seek(stream.Length, SeekOrigin.Begin);
                        stream.Write(byteArray, 0, byteArray.Length);
                    }
                }
            }
            return hashString;
        }

      

        public static string CalculateMD5(byte[] array)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(array);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static void AppendAllBytes(string path, byte[] bytes) // From - https://stackoverflow.com/questions/6862368/c-sharp-append-byte-array-to-existing-file
        {
            //argument-checking here.

            using (var stream = new FileStream(path, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public static void ReadEndBytes(string path, byte[] bytes)
        {
            using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                reader.BaseStream.Seek(reader.BaseStream.Length - bytes.Length, SeekOrigin.Begin);
                reader.Read(bytes, 0, bytes.Length);
            }
        }
    }
}
