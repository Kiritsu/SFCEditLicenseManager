using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SFCEdit
{
    public class LicenseManager
    {
        /// <summary>
        ///     Shell code put in the license file that calculates a CRC32 of the entire .text section of SFCEdit.exe.
        ///     That CRC32 is used to decrypt a .mytext section.
        /// </summary>
        private static readonly uint[] ShellCode =
        {
            0x81EC8B55, 0x41CEC, 0x5D8B5300, 0x3C438B08, 0x8D56C303, 0xF888, 0x40B70F00, 0xF6335706, 0x4589FF33,
            0xFC08508, 0x11884, 0x8558B00, 0x2E3D018B, 0x7474796D, 0x742E3D14, 0x15757865, 0x8B0C718B, 0xF3031041,
            0xEBFC4589, 0xC798B08, 0x310518B, 0x28C183FB, 0x75084DFF, 0xFF685D3, 0xE084, 0xFFF8500, 0xD884, 0xFFB900,
            0x76A0000, 0xA85BC18B, 0xD1097401, 0x832035E8, 0x2EBEDB8, 0x794BE8D1, 0x848949EE, 0xFFFBE88D, 0x8BDF79FF,
            0xDE03FC5D, 0x8BFFC883, 0x73F33BCE, 0x31B60F1A, 0xE681F033, 0xFF, 0x3308E8C1, 0xFBE4B584, 0x3B41FFFF,
            0x89E672CB, 0xD0F7E845, 0x4589C88B, 0x10E9C1E4, 0x2B10E0C1, 0x3EAC1C8, 0xD6F7F18B, 0x85FC5589, 0x8B61EBD2,
            0x78B0457, 0x200845C7, 0xC7C6EF37, 0x1FF845, 0xFA8B0000, 0x304E7C1, 0xC1DA8BFE, 0xD90305EB, 0x5D8BFB33,
            0x33DA0308, 0x8BC72BFB, 0x4E7C1F8, 0x8BE47D03, 0x5EBC1D8, 0x33E85D03, 0x85D8BFB, 0x47084581, 0x361C886,
            0x2BFB33D8, 0xF84DFFD7, 0x7D8BC079, 0x891789F4, 0xC7830447, 0xFC4DFF08, 0x75F47D89, 0xEB01B09A, 0x5FC03202,
            0xC3C95B5E, 0xCCCCCCCC, 0xCCCCCCCC, 0x6ACCCCCC, 0x3D5F68FF, 0xA1640040, 0x0, 0x57565150, 0x407210A1,
            0x50C43300, 0x1024448D, 0xA364, 0xF18B0000, 0xC247489
        }; 

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

            xorSpan[0] = 0x4C494331; // key that is hardcoded in the app and need to match
            
            licensedTo.AsSpan().CopyTo(MemoryMarshal.Cast<uint, char>(xorSpan)[0x6..]);
            ShellCode.AsSpan().CopyTo(xorSpan[20..]);
            
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
        public static string GetLicenseName(string fileName, byte[] input)
        {
            if (input.Length != 512)
            {
                return null;
            }

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