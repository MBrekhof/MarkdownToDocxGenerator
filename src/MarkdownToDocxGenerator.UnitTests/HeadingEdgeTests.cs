using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenXMLSDK.Engine.Word.ReportEngine.Models;

namespace MarkdownToDocxGenerator.UnitTests
{
    /// <summary>
    /// A heading with no inline content used to crash: the handler dereferenced
    /// FirstOrDefault() (null) to set the title style.
    /// </summary>
    [TestClass]
    public class HeadingEdgeTests
    {
        private static MdToOxmlEngine CreateEngine()
            => new MdToOxmlEngine(NullLogger<MdToOxmlEngine>.Instance);

        [TestMethod]
        public void Empty_heading_does_not_throw()
        {
            var report = CreateEngine().Transform("# ", "");

            Assert.IsNotNull(report);
        }

        [TestMethod]
        public void Empty_heading_followed_by_text_keeps_the_text()
        {
            var report = CreateEngine().Transform("# \nfollowing text", "");

            var page = (Page)report.Document.Pages.Single();
            var labels = page.ChildElements.OfType<Paragraph>()
                .SelectMany(p => p.ChildElements.OfType<Label>())
                .Select(l => l.Text);
            Assert.IsTrue(labels.Any(t => t == "following text"),
                "text after an empty heading must still render");
        }
    }
}
