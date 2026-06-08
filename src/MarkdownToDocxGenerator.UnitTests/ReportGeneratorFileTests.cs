using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarkdownToDocxGenerator.UnitTests
{
    /// <summary>
    /// Covers the file-based MdReportGenenerator.Transform: it must collect *.md
    /// deterministically (sorted) and case-insensitively.
    /// </summary>
    [TestClass]
    public class ReportGeneratorFileTests
    {
        private static MdReportGenenerator CreateGenerator()
            => new MdReportGenenerator(
                NullLogger<MdReportGenenerator>.Instance,
                new MdToOxmlEngine(NullLogger<MdToOxmlEngine>.Instance));

        [TestMethod]
        public void Md_files_are_collected_sorted_and_case_insensitively()
        {
            var templatePath = Path.Combine(Environment.CurrentDirectory, "Dotx", "sample.dotx");
            var rootFolder = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), "md2docx_order_" + Guid.NewGuid().ToString("N"))).FullName;
            var outputPath = Path.Combine(rootFolder, "out.docx");
            try
            {
                // Written out of order; 'c' uses an uppercase .MD extension.
                File.WriteAllText(Path.Combine(rootFolder, "b.md"), "# Bravo");
                File.WriteAllText(Path.Combine(rootFolder, "a.md"), "# Alpha");
                File.WriteAllText(Path.Combine(rootFolder, "c.MD"), "# Charlie");

                CreateGenerator().Transform(outputPath, rootFolder, templatePath);

                string text;
                using (var doc = WordprocessingDocument.Open(outputPath, false))
                    text = doc.MainDocumentPart?.Document?.InnerText ?? string.Empty;

                var alpha = text.IndexOf("Alpha", StringComparison.Ordinal);
                var bravo = text.IndexOf("Bravo", StringComparison.Ordinal);
                var charlie = text.IndexOf("Charlie", StringComparison.Ordinal);

                StringAssert.Contains(text, "Charlie", "the .MD (uppercase) file must be included");
                StringAssert.Contains(text, "Alpha", "all markdown files must be included");
                StringAssert.Contains(text, "Bravo", "all markdown files must be included");
                Assert.IsTrue(alpha < bravo && bravo < charlie,
                    $"files must render in sorted order; got Alpha@{alpha} Bravo@{bravo} Charlie@{charlie}");
            }
            finally
            {
                Directory.Delete(rootFolder, true);
            }
        }
    }
}
