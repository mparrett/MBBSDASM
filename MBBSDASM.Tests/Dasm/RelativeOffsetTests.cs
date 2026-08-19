using MBBSDASM.Dasm;
using Xunit;

namespace MBBSDASM.Tests.Dasm
{
    public class RelativeOffsetTests
    {
        [Theory]
        [InlineData(0x03, 1, 2, 6)]        //jmp short forward
        [InlineData(0x7F, 0x100, 2, 0x181)] //maximum forward displacement
        [InlineData(0xFE, 0x50, 2, 0x50)]  //-2: jump to own instruction
        [InlineData(0x80, 0x100, 2, 0x82)] //maximum backward displacement (-128)
        public void ToRelativeOffset8_ComputesTarget(byte operand, ulong currentOffset, int instructionLength,
            ulong expected)
        {
            Assert.Equal(expected, Disassembler.ToRelativeOffset8(operand, currentOffset, instructionLength));
        }

        [Theory]
        [InlineData(0x0081, 0x10, 3, 0x94)]    //jmp near forward
        [InlineData(0x7FFF, 0x10, 3, 0x8012)]  //maximum forward displacement (boundary)
        [InlineData(0xFF25, 0xE9, 3, 0x11)]    //-219: backward jmp near
        [InlineData(0xFFFD, 0x100, 3, 0x100)]  //-3: jump to own instruction
        [InlineData(0xFFF7, 6, 4, 1)]          //-9: backward 0F 8x conditional (4 byte instruction)
        [InlineData(0x8000, 0x9000, 3, 0x1003)] //maximum backward displacement (-32768)
        public void ToRelativeOffset16_ComputesTarget(ushort operand, ulong currentOffset, int instructionLength,
            ulong expected)
        {
            Assert.Equal(expected, Disassembler.ToRelativeOffset16(operand, currentOffset, instructionLength));
        }
    }
}
