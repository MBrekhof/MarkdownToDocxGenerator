using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using OpenXMLSDK.Engine.Word.ReportEngine;
using OpenXMLSDK.Engine.Word.ReportEngine.Models;
using OpenXMLSDK.Engine.Word.ReportEngine.Models.ExtendedModels;

namespace MarkdownToDocxGenerator.UnitTests
{
    /// <summary>
    /// The engine is intended to be registered as a singleton
    /// (RegisterMarkdownToDocxGenerator(asSingleton: true)), so a single instance
    /// must tolerate concurrent Transform calls. This guards against per-call state
    /// (e.g. rootFolder) being stored on the instance and racing between threads.
    /// </summary>
    [TestClass]
    public class ThreadSafetyTests
    {
        private static Image SingleImage(Report report)
            => ((Page)report.Document.Pages.Single())
                .ChildElements.OfType<Paragraph>()
                .SelectMany(p => p.ChildElements.OfType<Image>())
                .SingleOrDefault();

        [TestMethod]
        public void Concurrent_Transform_calls_resolve_images_against_their_own_rootFolder()
        {
            // Two distinct root folders, each containing the same relative image name.
            var folderA = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), "md2docx_" + Guid.NewGuid().ToString("N"))).FullName;
            var folderB = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), "md2docx_" + Guid.NewGuid().ToString("N"))).FullName;
            try
            {
                File.WriteAllBytes(Path.Combine(folderA, "img.png"), Array.Empty<byte>());
                File.WriteAllBytes(Path.Combine(folderB, "img.png"), Array.Empty<byte>());

                // Large document so the AST walk (which reads rootFolder near the end,
                // at the image) stays in flight long enough for a concurrent call to
                // clobber a shared rootFolder field — widening the race window.
                var filler = string.Join("\n\n", Enumerable.Range(0, 1500).Select(i => $"Paragraph number {i} with some text."));
                var markdown = filler + "\n\n![](img.png)";

                var engine = new MdToOxmlEngine(NullLogger<MdToOxmlEngine>.Instance); // ONE shared instance
                var folders = new[] { folderA, folderB };
                var failures = new ConcurrentBag<string>();

                Parallel.For(0, 300, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
                {
                    var folder = folders[i % 2];
                    var report = engine.Transform(markdown, folder);
                    var image = SingleImage(report);
                    if (image is null)
                        failures.Add($"iter {i}: no image produced (folder {folder})");
                    else if (!image.Path.StartsWith(folder, StringComparison.Ordinal))
                        failures.Add($"iter {i}: expected image under {folder}, got {image.Path}");
                });

                Assert.IsEmpty(failures,
                    $"{failures.Count} concurrent call(s) resolved the image against the wrong rootFolder. " +
                    "First few: " + string.Join(" ; ", failures.Take(3)));
            }
            finally
            {
                Directory.Delete(folderA, true);
                Directory.Delete(folderB, true);
            }
        }
    }
}
