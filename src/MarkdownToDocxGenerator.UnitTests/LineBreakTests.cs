using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenXMLSDK.Engine.Word.ReportEngine;
using OpenXMLSDK.Engine.Word.ReportEngine.Models;

namespace MarkdownToDocxGenerator.UnitTests
{
    /// <summary>
    /// Guards against losing line breaks / empty lines during Markdown -> OXML mapping.
    /// </summary>
    [TestClass]
    public class LineBreakTests
    {
        private static MdToOxmlEngine CreateEngine()
            => new MdToOxmlEngine(NullLogger<MdToOxmlEngine>.Instance);

        private static List<Paragraph> Paragraphs(Report report)
            => ((Page)report.Document.Pages.Single()).ChildElements.OfType<Paragraph>().ToList();

        private static string TextOf(Paragraph p)
            => string.Concat(p.ChildElements.OfType<Label>().Select(l => l.Text));

        private static bool IsEmpty(Paragraph p)
            => p.ChildElements.OfType<Label>().All(l => string.IsNullOrEmpty(l.Text));

        [TestMethod]
        public void Hard_line_breaks_in_a_paragraph_become_separate_paragraphs()
        {
            // Issue 1: soft breaks are promoted to hard breaks by the pipeline; they
            // were rendered as empty runs (no break), so lines ran together.
            var report = CreateEngine().Transform("Line 1\nLine 2\nLine 3", "");

            var paragraphs = Paragraphs(report);
            CollectionAssert.AreEqual(
                new[] { "Line 1", "Line 2", "Line 3" },
                paragraphs.Select(TextOf).ToArray(),
                "each hard-broken line should be its own paragraph");
        }

        [TestMethod]
        public void Single_blank_line_between_paragraphs_adds_no_empty_paragraph()
        {
            // Regression guard: the normal one-blank-line separator must NOT introduce
            // a visible empty line (that would change every existing document).
            var report = CreateEngine().Transform("A\n\nB", "");

            var paragraphs = Paragraphs(report);
            Assert.HasCount(2, paragraphs, "exactly two content paragraphs expected");
            Assert.AreEqual("A", TextOf(paragraphs[0]));
            Assert.AreEqual("B", TextOf(paragraphs[1]));
        }

        [TestMethod]
        public void Extra_blank_lines_between_paragraphs_are_preserved_as_empty_paragraphs()
        {
            // Issue 2: four newlines = three blank lines between A and B. One is the
            // normal separator; the other two should survive as empty paragraphs.
            var report = CreateEngine().Transform("A\n\n\n\nB", "");

            var paragraphs = Paragraphs(report);
            Assert.HasCount(4, paragraphs, "A + 2 empty lines + B");
            Assert.AreEqual("A", TextOf(paragraphs[0]));
            Assert.IsTrue(IsEmpty(paragraphs[1]), "first preserved blank line");
            Assert.IsTrue(IsEmpty(paragraphs[2]), "second preserved blank line");
            Assert.AreEqual("B", TextOf(paragraphs[3]));
        }
    }
}
