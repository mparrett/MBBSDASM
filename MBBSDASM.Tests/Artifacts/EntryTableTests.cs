using System.IO;
using MBBSDASM.Artifacts;
using Xunit;

namespace MBBSDASM.Tests.Artifacts
{
    public class EntryTableTests
    {
        private static NEFile Load(byte[] entryTable)
        {
            var inputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(inputFile, MinimalNEFile.Build(new byte[] {0xC3}, entryTable));
                return new NEFile(inputFile);
            }
            finally
            {
                File.Delete(inputFile);
            }
        }

        [Fact]
        public void NullBundle_ReservesCountOrdinals()
        {
            var file = Load(new byte[]
            {
                0x03, 0x00,                         //null bundle: ordinals 1-3 unused
                0x01, 0x01, 0x00, 0x10, 0x00,       //fixed bundle: 1 entry in segment 1, offset 0x0010
                0x00                                //end of table
            });

            var entry = Assert.Single(file.EntryTable);
            Assert.Equal(4, entry.Ordinal);
            Assert.Equal(1, entry.SegmentNumber);
            Assert.Equal(0x0010, entry.Offset);
        }

        [Fact]
        public void MovableEntry_GetsOrdinalSegmentAndOffset()
        {
            var file = Load(new byte[]
            {
                0x02, 0x01, 0x00, 0x10, 0x00, 0x00, 0x20, 0x00, //fixed bundle: 2 entries in segment 1
                0x01, 0xFF, 0x01, 0xCD, 0x3F, 0x01, 0x30, 0x00, //movable bundle: flag, int 3Fh, segment 1, offset 0x0030
                0x00                                            //end of table
            });

            Assert.Equal(3, file.EntryTable.Count);
            Assert.Equal(1, file.EntryTable[0].Ordinal);
            Assert.Equal(0x0010, file.EntryTable[0].Offset);
            Assert.Equal(2, file.EntryTable[1].Ordinal);
            Assert.Equal(0x0020, file.EntryTable[1].Offset);

            var movable = file.EntryTable[2];
            Assert.Equal(3, movable.Ordinal);
            Assert.Equal(1, movable.SegmentNumber);
            Assert.Equal(0x0030, movable.Offset);
        }

        [Fact]
        public void EndOfTableBundle_StopsParsing()
        {
            //Bytes after the terminator must not be parsed as further bundles
            var file = Load(new byte[]
            {
                0x01, 0x01, 0x00, 0x10, 0x00,       //fixed bundle: 1 entry in segment 1
                0x00,                               //end of table
                0x01, 0x01, 0x00, 0x99, 0x00        //garbage past the terminator
            });

            Assert.Single(file.EntryTable);
        }
    }
}
