using Microsoft.Extensions.Logging.Abstractions;
using OpenXMLSDK.Engine.Word.ReportEngine;
using OpenXMLSDK.Engine.Word.ReportEngine.Models;

namespace MarkdownToDocxGenerator.UnitTests
{
    /// <summary>
    /// Unit tests for the markdown -> OXML document-model mapping performed by
    /// <see cref="MdToOxmlEngine"/>. These assert the produced <see cref="Report"/>
    /// model directly (no files, no template), so they are fast and deterministic.
    /// </summary>
    [TestClass]
    public class MdToOxmlEngineTests
    {
        private static MdToOxmlEngine CreateEngine()
            => new MdToOxmlEngine(NullLogger<MdToOxmlEngine>.Instance);

        private static Page FirstPage(Report report)
            => (Page)report.Document.Pages.Single();

        private static IEnumerable<Label> Labels(Page page)
            => page.ChildElements.OfType<Paragraph>().SelectMany(p => p.ChildElements.OfType<Label>());

        [TestMethod]
        [DataRow(1, "Titre1", DisplayName = "H1 -> Titre1")]
        [DataRow(2, "Titre2", DisplayName = "H2 -> Titre2")]
        [DataRow(3, "Titre3", DisplayName = "H3 -> Titre3")]
        public void Heading_maps_to_Titre_style(int level, string expectedStyle)
        {
            var markdown = new string('#', level) + " Heading text";

            var report = CreateEngine().Transform(markdown, "");

            var paragraph = FirstPage(report).ChildElements.OfType<Paragraph>().First();
            Assert.AreEqual(expectedStyle, paragraph.ParagraphStyleId);
            Assert.AreEqual("Heading text", paragraph.ChildElements.OfType<Label>().First().Text);
        }

        [TestMethod]
        public void Bold_text_produces_a_bold_label()
        {
            var report = CreateEngine().Transform("**bold words**", "");

            var label = Labels(FirstPage(report)).Single(l => l.Text == "bold words");
            Assert.AreEqual(true, label.Bold);
        }

        [TestMethod]
        public void Plain_paragraph_text_is_not_bold()
        {
            var report = CreateEngine().Transform("Just some text", "");

            var label = Labels(FirstPage(report)).First();
            Assert.AreEqual("Just some text", label.Text);
            Assert.AreEqual(false, label.Bold);
        }

        [TestMethod]
        public void Fenced_code_block_lines_get_grey_shading()
        {
            var markdown = "```\nvar x = 1;\n```";

            var report = CreateEngine().Transform(markdown, "");

            var codeParagraphs = FirstPage(report).ChildElements
                .OfType<Paragraph>()
                .Where(p => p.Shading == "EAEAEA")
                .ToList();

            Assert.IsTrue(codeParagraphs.Count >= 1, "expected at least one shaded code paragraph");
            Assert.IsTrue(
                codeParagraphs.SelectMany(p => p.ChildElements.OfType<Label>()).Any(l => l.Text.Contains("var x = 1;")),
                "code text should be preserved in a shaded paragraph");
        }

        [TestMethod]
        public void Pipe_table_produces_a_table_with_header_and_data_rows()
        {
            var markdown = "| Name | Age |\n| --- | --- |\n| Alice | 30 |\n| Bob | 25 |";

            var report = CreateEngine().Transform(markdown, "");

            var table = FirstPage(report).ChildElements.OfType<Table>().Single();
            Assert.AreEqual(2, table.HeaderRow.Cells.Count, "header should have two columns");
            Assert.AreEqual(2, table.Rows.Count, "two data rows expected (header excluded)");
        }

        [TestMethod]
        public void Absolute_url_link_becomes_a_hyperlink()
        {
            var report = CreateEngine().Transform("[Google](https://www.google.com)", "");

            var hyperlink = FirstPage(report).ChildElements
                .OfType<Paragraph>()
                .SelectMany(p => p.ChildElements.OfType<Hyperlink>())
                .Single();

            Assert.AreEqual("https://www.google.com", hyperlink.WebSiteUri);
        }

        [TestMethod]
        public void Relative_link_falls_back_to_a_plain_label_not_a_hyperlink()
        {
            // Regression guard for #25: relative paths are not well-formed absolute URIs
            // and must render as plain text, never as a (broken) hyperlink.
            var report = CreateEngine().Transform("[see file](relative/path.md)", "");

            var paragraphs = FirstPage(report).ChildElements.OfType<Paragraph>().ToList();
            var hyperlinks = paragraphs.SelectMany(p => p.ChildElements.OfType<Hyperlink>()).ToList();
            var labels = paragraphs.SelectMany(p => p.ChildElements.OfType<Label>()).ToList();

            Assert.AreEqual(0, hyperlinks.Count, "relative URLs must not become hyperlinks");
            Assert.IsTrue(labels.Any(l => l.Text == "see file"), "link text should survive as a plain label");
        }

        [TestMethod]
        public void Empty_markdown_produces_a_page_with_no_elements()
        {
            var report = CreateEngine().Transform("", "");

            Assert.AreEqual(0, FirstPage(report).ChildElements.Count);
        }
    }
}
