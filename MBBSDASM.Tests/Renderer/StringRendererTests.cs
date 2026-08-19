using System.IO;
using MBBSDASM.Dasm;
using MBBSDASM.Renderer.impl;
using Xunit;

namespace MBBSDASM.Tests.Renderer
{
    public class StringRendererTests
    {
        [Fact]
        public void RenderStrings_PrintsFileOffset()
        {
            //Data segment (at file offset 0x100) containing the string "HI" at segment offset 1
            var data = new byte[] {0x00, (byte) 'H', (byte) 'I', 0x00};

            var inputFile = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(inputFile, MinimalNEFile.Build(data, segmentFlags: 0x0001));
                var file = new Disassembler(inputFile).Disassemble();

                var output = new StringRenderer(file).RenderStrings();

                Assert.Contains("00000101h:0001.0001h 'HI'", output);
            }
            finally
            {
                File.Delete(inputFile);
            }
        }
    }
}
