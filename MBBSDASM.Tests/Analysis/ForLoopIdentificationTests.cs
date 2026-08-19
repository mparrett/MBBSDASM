using System.IO;
using MBBSDASM.Dasm;
using Xunit;

namespace MBBSDASM.Tests.Analysis
{
    public class ForLoopIdentificationTests
    {
        /*
        *   0000: 81 3E 00 00 05 00   cmp word [0x0], 0x5   (target of the jump below)
        *   0006: EB F8               jmp 0x0
        *   0008: C3                  ret
        *
        *   A cmp that is the target of an unconditional jump enters the FOR loop
        *   pattern check; as the first line of the segment it has no predecessor
        */
        private static readonly byte[] CodeSegment =
        {
            0x81, 0x3E, 0x00, 0x00, 0x05, 0x00,
            0xEB, 0xF8,
            0xC3
        };

        [Fact]
        public void CmpAtStartOfSegment_DoesNotThrow()
        {
            var inputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(inputFile, MinimalNEFile.Build(CodeSegment));
                var file = new Disassembler(inputFile).Disassemble();

                MBBSDASM.Analysis.MBBS.Analyze(file);

                //The lone cmp is not part of a FOR pattern and must not be labeled as one
                Assert.DoesNotContain(file.SegmentTable[0].DisassemblyLines,
                    x => x.Comments.Exists(c => c.Contains("[FOR]")));
            }
            finally
            {
                File.Delete(inputFile);
            }
        }
    }
}
