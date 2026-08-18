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
                File.WriteAllBytes(inputFile, MinimalNEFile.Build(CodeSegment));
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
    }
}
