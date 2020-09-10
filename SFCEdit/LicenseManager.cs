#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SFCEdit
{
    public class LicenseManager
    {
        /*
         * First 4 bytes must be '0x4C494331'
         * 0xC  bytes in is a unicode string (who the license belongs to)
         * 0x4C bytes in is a byte (site license)
         * 0x4D bytes in is a byte (individual license)
         * 0x80 bytes in is assembly instructions
         */

        /// <summary>
        ///     Creates a license from a given license filename. It must follow the following format:
        ///     SFCEDIT_XXXXXX.lic where X can be any valid letter/number.
        /// </summary>
        /// <param name="fileName">Name of the license file.</param>
        /// <param name="licensedTo">Name of the owner of the license.</param>
        /// <returns>The license as a fixed 512 bytes array.</returns>
        public static byte[] CreateLicense(string fileName, string licensedTo)
        {
            Span<uint> xorSpan = new uint[128];
            uint xorKey = GetXorKey(fileName);

            xorSpan[0] = 0x4C494331;
            xorSpan[0x20] = 0x000001B8; // assembly instructions 
            xorSpan[0x21] = 0x9090C300; // that returns true
            
            licensedTo.AsSpan().CopyTo(MemoryMarshal.Cast<uint, char>(xorSpan)[0x6..]);
            
            for (int i = 0; i < 128; i++)
                xorSpan[i] ^= xorKey;

            Span<byte> bytes = MemoryMarshal.Cast<uint, byte>(xorSpan);
            return bytes.ToArray();
        }

        /// <summary>
        ///     Gets the name of the owner of the given license.
        /// </summary>
        /// <param name="fileName">Name of the license. Used to XOR the content of the license.</param>
        /// <param name="input">License content as a fixed 512 bytes array.</param>
        public static string? GetLicenseName(string fileName, byte[] input)
        {
            Span<uint> xorSpan = MemoryMarshal.Cast<byte, uint>(input);

            uint xorKey = GetXorKey(fileName);
            for (int i = 0; i < 128; i++)
                xorSpan[i] ^= xorKey;

            if (xorSpan[0] == 0x4C494331)
            {
                Span<char> licensedToSpan = MemoryMarshal.Cast<byte, char>(input.AsSpan()[0xC..]);
                return licensedToSpan[..licensedToSpan.IndexOf('\0')].ToString();
            }

            return null;
        }

        /// <summary>
        ///     Gets the XOR Key used to decrypt the license content.
        /// </summary>
        /// <param name="fileName">File name used to decode the XOR key of the license.</param>
        public static uint GetXorKey(string fileName)
        {
            uint xorKey = 0;
            for (int i = 8; i < 14; i++)
            {
                xorKey *= 36;

                ushort currentChar = fileName[i];
                if (currentChar >= 'A' && currentChar <= 'Z')
                    xorKey = xorKey + currentChar - 'A';
                else if (currentChar >= 'a' && currentChar <= 'z')
                    xorKey = xorKey + currentChar - 'a';
                else
                    xorKey = xorKey + currentChar - 0x16;
            }

            xorKey ^= 0xAAAAAAAA;
            return xorKey;
        }

        private static void Main()
        {
            var license = CreateLicense("SFCEDIT_CLOWN1.LIC", "Allan");
            File.WriteAllBytes("SFCEDIT_CLOWN1.LIC", license);
        }
    }
}