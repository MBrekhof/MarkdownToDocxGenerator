using DocumentFormat.OpenXml.Packaging;
using MarkdownToDocxGenerator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MarkdownToDocxGenerator.UnitTests
{
    /// <summary>
    /// Tests the DI registration contract and the full end-to-end pipeline
    /// (markdown -> in-memory DOCX) via <see cref="MdReportGenenerator"/>.
    /// </summary>
    [TestClass]
    public class MarkdownToDocxIntegrationTests
    {
        private static ServiceProvider BuildProvider(bool asSingleton)
        {
            var services = new ServiceCollection();
            services.AddLogging(c => c.AddConsole());
            services.RegisterMarkdownToDocxGenerator(asSingleton);
            return services.BuildServiceProvider();
        }

        [TestMethod]
        public void Register_as_singleton_resolves_the_same_instance()
        {
            using var provider = BuildProvider(asSingleton: true);

            var first = provider.GetRequiredService<MdToOxmlEngine>();
            var second = provider.GetRequiredService<MdToOxmlEngine>();
            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Register_as_transient_resolves_new_instances()
        {
            using var provider = BuildProvider(asSingleton: false);

            var first = provider.GetRequiredService<MdToOxmlEngine>();
            var second = provider.GetRequiredService<MdToOxmlEngine>();
            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void Generator_resolves_with_its_full_dependency_graph()
        {
            using var provider = BuildProvider(asSingleton: true);

            // Throws if MdReportGenenerator / MdToOxmlEngine / ILogger<> are not all wired up.
            var generator = provider.GetRequiredService<MdReportGenenerator>();
            Assert.IsNotNull(generator);
        }

        [TestMethod]
        public void TransformWithStream_produces_a_valid_docx_containing_the_text()
        {
            using var provider = BuildProvider(asSingleton: true);
            var generator = provider.GetRequiredService<MdReportGenenerator>();

            using var stream = generator.TransformWithStream(new List<string> { "# Hello World" });

            Assert.IsTrue(stream.Length > 0, "generated stream should not be empty");

            stream.Position = 0;
            using var document = WordprocessingDocument.Open(stream, false);
            var text = document.MainDocumentPart?.Document?.InnerText ?? string.Empty;
            StringAssert.Contains(text, "Hello World");
        }
    }
}
