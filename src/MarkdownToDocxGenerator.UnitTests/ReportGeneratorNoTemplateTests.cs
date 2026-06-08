using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarkdownToDocxGenerator.UnitTests
{
    /// <summary>
    /// The file-based Transform must write a .docx even when no template is supplied.
    /// WordManager.New()/SaveDoc() take no path, so without this the call was a silent no-op.
    /// </summary>
    [TestClass]
    public class ReportGeneratorNoTemplateTests
    {
        private static MdReportGenenerator CreateGenerator()
            => new MdReportGenenerator(
                NullLogger<MdReportGenenerator>.Instance,
                new MdToOxmlEngine(NullLogger<MdToOxmlEngine>.Instance));

        [TestMethod]
        public void Transform_without_template_writes_a_valid_docx_to_outputPath()
        {
            var rootFolder = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), "md2docx_notmpl_" + Guid.NewGuid().ToString("N"))).FullName;
            var outputPath = Path.Combine(rootFolder, "out.docx");
            try
            {
                File.WriteAllText(Path.Combine(rootFolder, "a.md"), "# Hello World");

                CreateGenerator().Transform(outputPath, rootFolder); // no template

                Assert.IsTrue(File.Exists(outputPath), "a .docx must be written even without a template");

                using var doc = WordprocessingDocument.Open(outputPath, false);
                var text = doc.MainDocumentPart?.Document?.InnerText ?? string.Empty;
                StringAssert.Contains(text, "Hello World");
            }
            finally
            {
                Directory.Delete(rootFolder, true);
            }
        }
    }
}
