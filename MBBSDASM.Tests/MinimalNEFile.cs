using System;

namespace MBBSDASM.Tests
{
    /// <summary>
    ///     Builds the smallest NE file NEFile.Load will accept: MZ stub, NE header,
    ///     one fixed code segment (with an empty relocation table), an optional entry
    ///     table, and empty name tables
    /// </summary>
    internal static class MinimalNEFile
    {
        public const ushort CodeWithRelocationInfo = 0x0100;

        public static byte[] Build(byte[] code, byte[] entryTable = null,
            ushort segmentFlags = CodeWithRelocationInfo)
        {
            const int neHeaderOffset = 0x80;
            const int segmentTableOffset = 0x40;  //relative to NE header
            const int entryTableOffset = 0x50;    //relative to NE header
            const int segmentDataOffset = 0x100;

            //An empty entry table is a single end-of-table bundle
            entryTable = entryTable ?? new byte[] {0x00};

            //Resident name table (a lone terminator byte) sits right after the entry table
            var residentNameOffset = entryTableOffset + entryTable.Length;

            var file = new byte[0x200];

            //MZ stub
            WriteUInt16(file, 0x00, 0x5A4D);            //'MZ'
            file[0x18] = 0x40;                          //relocation table at 0x40 -> NE offset at 0x3C is valid
            WriteUInt16(file, 0x3C, neHeaderOffset);

            //NE header
            file[neHeaderOffset] = (byte) 'N';
            file[neHeaderOffset + 1] = (byte) 'E';
            WriteUInt16(file, neHeaderOffset + 0x04, entryTableOffset);
            WriteUInt16(file, neHeaderOffset + 0x1C, 1);                    //segment table entries
            WriteUInt16(file, neHeaderOffset + 0x22, segmentTableOffset);
            WriteUInt16(file, neHeaderOffset + 0x26, (ushort) residentNameOffset);
            WriteUInt16(file, neHeaderOffset + 0x28, (ushort) (residentNameOffset + 2)); //module ref table (empty)
            WriteUInt32(file, neHeaderOffset + 0x2C, (uint) file.Length);   //non-resident names (empty)
            //LogicalSectorAlignmentShift (0x32), table lengths, and counts stay 0

            //Segment table: one fixed segment
            WriteUInt16(file, neHeaderOffset + segmentTableOffset, segmentDataOffset);
            WriteUInt16(file, neHeaderOffset + segmentTableOffset + 2, (ushort) code.Length);
            WriteUInt16(file, neHeaderOffset + segmentTableOffset + 4, segmentFlags);
            WriteUInt16(file, neHeaderOffset + segmentTableOffset + 6, (ushort) code.Length);

            //Entry table; the resident name table terminator after it is already zeroed
            Array.Copy(entryTable, 0, file, neHeaderOffset + entryTableOffset, entryTable.Length);

            //Segment data, followed by a zero-entry relocation table when the flag is set
            Array.Copy(code, 0, file, segmentDataOffset, code.Length);
            if ((segmentFlags & 0x0100) != 0)
                WriteUInt16(file, segmentDataOffset + code.Length, 0);

            return file;
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value) =>
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, 2);

        private static void WriteUInt32(byte[] buffer, int offset, uint value) =>
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, 4);
    }
}
