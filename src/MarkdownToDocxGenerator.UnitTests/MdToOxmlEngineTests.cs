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
            Assert.IsTrue(label.Bold!.Value);
        }

        [TestMethod]
        public void Plain_paragraph_text_is_not_bold()
        {
            var report = CreateEngine().Transform("Just some text", "");

            var label = Labels(FirstPage(report)).First();
            Assert.AreEqual("Just some text", label.Text);
            Assert.IsFalse(label.Bold!.Value);
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

            Assert.IsGreaterThanOrEqualTo(1, codeParagraphs.Count, "expected at least one shaded code paragraph");
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
            Assert.HasCount(2, table.HeaderRow.Cells, "header should have two columns");
            Assert.HasCount(2, table.Rows, "two data rows expected (header excluded)");
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

            Assert.IsEmpty(hyperlinks, "relative URLs must not become hyperlinks");
            Assert.IsTrue(labels.Any(l => l.Text == "see file"), "link text should survive as a plain label");
        }

        [TestMethod]
        public void Inline_code_span_content_is_preserved_as_a_label()
        {
            // Regression guard: inline `code` spans were silently dropped because
            // CodeInline had no handler, so surrounding text appeared mangled.
            var report = CreateEngine().Transform("the `Labware8` db doesn't exist", "");

            var labels = Labels(FirstPage(report)).Select(l => l.Text).ToList();

            Assert.IsTrue(labels.Any(t => t == "Labware8"),
                "inline code content must survive; got: " + string.Join(" | ", labels));
            CollectionAssert.AreEqual(
                new[] { "the ", "Labware8", " db doesn't exist" },
                labels,
                "code span should sit inline between the surrounding text");
        }

        [TestMethod]
        public void Inline_code_with_apostrophes_is_not_dropped()
        {
            // The original report: apostrophes inside an inline code span looked
            // "interpreted wrong" because the whole span vanished.
            var report = CreateEngine().Transform("all 71 `Invalid column name 'PROGRAM'` errors", "");

            var labels = Labels(FirstPage(report)).Select(l => l.Text).ToList();

            // Assert the whole sequence: the original symptom was the surrounding
            // prose looking mangled because the code span (with its apostrophes) vanished.
            CollectionAssert.AreEqual(
                new[] { "all 71 ", "Invalid column name 'PROGRAM'", " errors" },
                labels,
                "code span with apostrophes must be preserved inline; got: " + string.Join(" | ", labels));
        }

        [TestMethod]
        public void Inline_code_nested_in_emphasis_is_preserved()
        {
            // The emphasis sub-loop had the same missing CodeInline case, so
            // `code` inside **bold**/*italic* was dropped.
            var report = CreateEngine().Transform("see **the `FN_ADD_FLAG` routine**", "");

            var labels = Labels(FirstPage(report)).Select(l => l.Text).ToList();

            Assert.IsTrue(labels.Any(t => t == "FN_ADD_FLAG"),
                "code span nested in emphasis must be preserved; got: " + string.Join(" | ", labels));
        }

        [TestMethod]
        public void Inline_code_label_is_monospace_and_shaded()
        {
            var report = CreateEngine().Transform("a `code` span", "");

            var codeLabel = Labels(FirstPage(report)).Single(l => l.Text == "code");
            Assert.AreEqual("Consolas", codeLabel.FontName);
            Assert.AreEqual("EAEAEA", codeLabel.Shading);
        }

        [TestMethod]
        public void Heading_inline_code_inherits_title_style()
        {
            // Inline code in a heading must not carry a shaded monospace box;
            // it should inherit the title style like the rest of the heading.
            var report = CreateEngine().Transform("# Title with `code` here", "");

            var codeLabel = Labels(FirstPage(report)).Single(l => l.Text == "code");
            Assert.IsNull(codeLabel.Shading, "heading code must not keep code shading");
            Assert.IsNull(codeLabel.FontName, "heading code must not keep monospace font");
        }

        [TestMethod]
        public void Link_with_inline_code_label_is_preserved_as_hyperlink()
        {
            // Inline code as a link's text was dropped (hyperlink had null text).
            var report = CreateEngine().Transform("[`code`](https://example.com)", "");

            var hyperlink = FirstPage(report).ChildElements
                .OfType<Paragraph>()
                .SelectMany(p => p.ChildElements.OfType<Hyperlink>())
                .Single();

            Assert.AreEqual("https://example.com", hyperlink.WebSiteUri);
            Assert.AreEqual("code", hyperlink.Text?.Text);
        }

        [TestMethod]
        public void Empty_absolute_link_does_not_throw()
        {
            // Regression guard: an empty-text absolute link `[](url)` used to
            // throw NullReferenceException on FirstChild.
            var report = CreateEngine().Transform("[](https://example.com)", "");

            Assert.IsNotNull(report);
        }

        [TestMethod]
        public void Empty_markdown_produces_a_page_with_no_elements()
        {
            var report = CreateEngine().Transform("", "");

            Assert.IsEmpty(FirstPage(report).ChildElements);
        }
    }
}
