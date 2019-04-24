using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Indieteur.ChecksumZIP
{
    public class FileAlreadyExistsException : Exception
    {
        public FileAlreadyExistsException (string message) : base(message)
        {

        }
    }

    public class FailedChecksumException : Exception
    {
        public FailedChecksumException(string message) : base(message)
        {

        }
    }

    public class NoChecksumStoredException : Exception
    {
        public NoChecksumStoredException(string message) : base(message)
        {

        }
    }
    public static class ChecksumZIP
    {

        public static void CreateArchiveFromDirectory(string sourceDirectory, string destinationFile, bool storeChecksumInArchive = true, bool overwrite = true, CompressionLevel compressionLevel = CompressionLevel.Optimal, bool includeBaseDirectory = false, int checksumBufferSize = ChecksumZIPHelper.KB_4_IN_BYTES)
        {
            CreateArchiveFromDirectory(sourceDirectory, destinationFile, storeChecksumInArchive, overwrite, compressionLevel, includeBaseDirectory, Encoding.ASCII, checksumBufferSize);
        }

        public static void CreateArchiveFromDirectory(string sourceDirectory, string destinationFile, bool storeChecksumInArchive, bool overwrite, CompressionLevel compressionLevel, bool includeBaseDirectory, Encoding encoding, int checksumBufferSize = ChecksumZIPHelper.KB_4_IN_BYTES)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException(sourceDirectory + " was not found.");
            if (File.Exists(destinationFile))
            {
                if (overwrite)
                    File.Delete(destinationFile);
                else
                    throw new FileAlreadyExistsException(destinationFile + " already exists.");
            }
            ZipFile.CreateFromDirectory(sourceDirectory, destinationFile, compressionLevel, includeBaseDirectory, encoding);
            if (storeChecksumInArchive)
                AppendChecksumToArchive(destinationFile, checksumBufferSize);
        }

        public static void AppendChecksumToArchive (string archivePath, int checksumBufferSize = ChecksumZIPHelper.KB_4_IN_BYTES)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException(archivePath + " was not found.");
            string stringToAppend = ChecksumZIPHelper.CalculateMD5(archivePath, checksumBufferSize) + ChecksumZIPHelper.MAGIC_CONSTANT;
            byte[] byteToAppend = Encoding.ASCII.GetBytes(stringToAppend);
            ChecksumZIPHelper.AppendAllBytes(archivePath, byteToAppend);
        }

        public static string RemoveChecksumValueFromArchive (string archivePath)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException(archivePath + " was not found.");
            if (!ArchiveHasChecksumValue(archivePath))
                return null;
            byte[] byteStore = new byte[ChecksumZIPHelper.MD5_HASH_BYTE_LENGTH + ChecksumZIPHelper.MAGIC_CONSTANT.Length];
            using (var stream = new FileStream(archivePath, FileMode.Open))
            {
                stream.Seek(stream.Length - byteStore.Length, SeekOrigin.Begin);
                stream.Read(byteStore, 0, byteStore.Length);
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(stream.Length - (ChecksumZIPHelper.MD5_HASH_BYTE_LENGTH + ChecksumZIPHelper.MAGIC_CONSTANT.Length));
            }
            string byteToString = Encoding.ASCII.GetString(byteStore);
            return byteToString.Substring(0, byteToString.Length - ChecksumZIPHelper.MAGIC_CONSTANT.Length);
        }

        public static bool ArchiveHasChecksumValue(string archivePath)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException(archivePath + " was not found.");
            byte[] byteStore = new byte[ChecksumZIPHelper.MAGIC_CONSTANT.Length];
            ChecksumZIPHelper.ReadEndBytes(archivePath, byteStore);
            return Encoding.ASCII.GetString(byteStore) == ChecksumZIPHelper.MAGIC_CONSTANT;
        }

        public static string ReadArchiveStoredChecksumValue(string archivePath)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException(archivePath + " was not found.");
            if (!ArchiveHasChecksumValue(archivePath))
                throw new NoChecksumStoredException(archivePath + " doesn't have a stored checksum value.");

            byte[] byteStore = new byte[ChecksumZIPHelper.MD5_HASH_BYTE_LENGTH + ChecksumZIPHelper.MAGIC_CONSTANT.Length];
            ChecksumZIPHelper.ReadEndBytes(archivePath, byteStore);
            string byteToString = Encoding.ASCII.GetString(byteStore);
            return byteToString.Substring(0, byteToString.Length - ChecksumZIPHelper.MAGIC_CONSTANT.Length);
        }

        public static string CalculateChecksumOfArchive(string archivePath, int checksumBufferSize = ChecksumZIPHelper.KB_4_IN_BYTES)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException(archivePath + " was not found.");
            return ChecksumZIPHelper.CalculateMD5(archivePath, checksumBufferSize);
        }

        public static bool ArchiveHasMatchingStoredChecksum(string archivePath, int checksumBufferSize = ChecksumZIPHelper.KB_4_IN_BYTES)
        {
            return (ReadArchiveStoredChecksumValue(archivePath) == CalculateChecksumOfArchive(archivePath, checksumBufferSize));
        }

        public static void ExtractZip(string archivePath, string destination, bool performSpecialChecksumCheck = true, int checksumBufferSize = ChecksumZIPHelper.KB_4_IN_BYTES)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException(archivePath + " was not found.");
            if (!Directory.Exists(destination))
                throw new DirectoryNotFoundException(destination + " destination directory could not be found.");
            if (performSpecialChecksumCheck)
            {
                if (ArchiveHasMatchingStoredChecksum(archivePath, checksumBufferSize) == false)
                    throw new FailedChecksumException("Calculated checksum for file " + archivePath + " does not match with the one stored in it.");
            }
            ZipFile.ExtractToDirectory(archivePath, destination);
            
        }
    }
}
