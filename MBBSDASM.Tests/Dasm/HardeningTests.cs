using System.IO;
using MBBSDASM.Artifacts;
using MBBSDASM.Dasm;
using Xunit;

namespace MBBSDASM.Tests.Dasm
{
    /// <summary>
    ///     Regression tests for inputs that previously crashed the disassembler
    /// </summary>
    public class HardeningTests
    {
        private static NEFile Disassemble(byte[] neFile)
        {
            var inputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(inputFile, neFile);
                return new Disassembler(inputFile).Disassemble();
            }
            finally
            {
                File.Delete(inputFile);
            }
        }

        [Fact]
        public void CodeSegmentWithoutRelocationInfo_DoesNotThrow()
        {
            var file = Disassemble(MinimalNEFile.Build(new byte[] {0x90, 0xC3}, segmentFlags: 0x0000));

            Assert.Equal(2, file.SegmentTable[0].DisassemblyLines.Count);
        }

        [Fact]
        public void EntryPointInMissingSegment_DoesNotThrow()
        {
            var entryTable = new byte[]
            {
                0x01, 0x02, 0x00, 0x10, 0x00, //fixed bundle: 1 entry in segment 2, which doesn't exist
                0x00                          //end of table
            };

            var file = Disassemble(MinimalNEFile.Build(new byte[] {0xC3}, entryTable));

            Assert.Single(file.EntryTable);
        }
    }
}
