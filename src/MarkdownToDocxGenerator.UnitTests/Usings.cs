global using Microsoft.VisualStudio.TestTools.UnitTesting;

// Tests share on-disk fixtures (template + MdFiles) and produce documents, so run
// them serially. Explicit choice required by the MSTEST0001 analyzer.
[assembly: DoNotParallelize]