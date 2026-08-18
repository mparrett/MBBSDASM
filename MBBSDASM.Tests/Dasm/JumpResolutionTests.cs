using System;
using System.IO;
using System.Linq;
using MBBSDASM.Dasm;
using MBBSDASM.Enums;
using Xunit;

namespace MBBSDASM.Tests.Dasm
{
    /// <summary>
    ///     End-to-end tests for jump target resolution: builds a minimal NE file around a
    ///     hand-crafted code segment and verifies the branch records after disassembly
    /// </summary>
    public class JumpResolutionTests
    {
        /*
        *   0000: 90            nop
        *   0001: EB 03         jmp short 0x6   (forward)
        *   0003: E9 FA FF      jmp 0x0         (backward near)
        *   0006: 0F 85 F7 FF   jnz 0x1         (backward near conditional, 2-byte opcode)
        *   000A: FF E0         jmp ax          (indirect, no static target)
        *   000C: C3            ret
        */
        private static readonly byte[] CodeSegment =
        {
            0x90,
            0xEB, 0x03,
            0xE9, 0xFA, 0xFF,
            0x0F, 0x85, 0xF7, 0xFF,
            0xFF, 0xE0,
            0xC3
        };

        [Fact]
        public void ResolveJumpTargets_LabelsRelativeJumps()
        {
            var inputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(inputFile, BuildNEFile(CodeSegment));
                var segment = new Disassembler(inputFile).Disassemble().SegmentTable[0];

                //jmp short 0x6 from 0001
                var fromShortJump = Assert.Single(LineAt(segment, 0x6).BranchFromRecords);
                Assert.Equal(0x1UL, fromShortJump.Offset);
                Assert.Equal(EnumBranchType.Unconditional, fromShortJump.BranchType);

                //jmp 0x0 from 0003 -- backward near jump, must land on 0x0, not 0x1
                var fromNearJump = Assert.Single(LineAt(segment, 0x0).BranchFromRecords);
                Assert.Equal(0x3UL, fromNearJump.Offset);
                Assert.Equal(EnumBranchType.Unconditional, fromNearJump.BranchType);

                //jnz 0x1 from 0006 -- 0F 8x near conditional
                var fromNearConditional = Assert.Single(LineAt(segment, 0x1).BranchFromRecords);
                Assert.Equal(0x6UL, fromNearConditional.Offset);
                Assert.Equal(EnumBranchType.Conditional, fromNearConditional.BranchType);

                //jmp ax has no static target and must produce no branch records
                Assert.Empty(LineAt(segment, 0xA).BranchToRecords);
            }
            finally
            {
                File.Delete(inputFile);
            }
        }

        private static DisassemblyLine LineAt(MBBSDASM.Artifacts.Segment segment, ulong offset) =>
            segment.DisassemblyLines.First(x => x.Disassembly.Offset == offset);

        /// <summary>
        ///     Builds the smallest NE file NEFile.Load will accept: MZ stub, NE header,
        ///     one fixed code segment (with an empty relocation table), and empty name/entry tables
        /// </summary>
        private static byte[] BuildNEFile(byte[] code)
        {
            const int neHeaderOffset = 0x80;
            const int segmentTableOffset = 0x40;  //relative to NE header
            const int entryTableOffset = 0x60;    //relative to NE header
            const int residentNameOffset = 0x62;  //relative to NE header
            const int segmentDataOffset = 0x100;

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
            WriteUInt16(file, neHeaderOffset + 0x26, residentNameOffset);
            WriteUInt16(file, neHeaderOffset + 0x28, residentNameOffset + 2); //module ref table (empty)
            WriteUInt32(file, neHeaderOffset + 0x2C, (uint) file.Length);     //non-resident names (empty)
            //LogicalSectorAlignmentShift (0x32), table lengths, and counts stay 0

            //Segment table: one fixed code segment with relocation info present but empty
            WriteUInt16(file, neHeaderOffset + segmentTableOffset, segmentDataOffset);
            WriteUInt16(file, neHeaderOffset + segmentTableOffset + 2, (ushort) code.Length);
            WriteUInt16(file, neHeaderOffset + segmentTableOffset + 4, 0x0100); //code + HasRelocationInfo
            WriteUInt16(file, neHeaderOffset + segmentTableOffset + 6, (ushort) code.Length);

            //Entry table (count byte 0) and resident name table (terminator) are already zeroed

            //Segment data followed by a zero-entry relocation table
            Array.Copy(code, 0, file, segmentDataOffset, code.Length);
            WriteUInt16(file, segmentDataOffset + code.Length, 0);

            return file;
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value) =>
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, 2);

        private static void WriteUInt32(byte[] buffer, int offset, uint value) =>
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, 4);
    }
}
