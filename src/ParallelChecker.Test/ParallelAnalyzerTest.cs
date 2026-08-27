using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelChecker.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ParallelChecker._Test {
  [TestClass]
  public class ParallelAnalyzerTest {
    [TestMethod]
    public async Task ReportsDiagnosticsFromEverySyntaxTree() {
      var codes = new[] {
        """
        using System.Threading;

        public static class Program {
          public static void Main() {
            var first = new Thread(Worker.WriteOne);
            var second = new Thread(() => Worker.Value = 2);
            first.Start();
            second.Start();
          }
        }
        """,
        """
        public static class Worker {
          public static int Value;

          public static void WriteOne() {
            Value = 1;
          }
        }
        """
      };
      var compilation = CreateCompilation(codes);

      var diagnostics = await Analyze(compilation);

      var warnings = diagnostics.Where(diagnostic => diagnostic.Id == "ParallelChecker").ToArray();
      Assert.IsTrue(warnings.Length > 0);
      Assert.IsTrue(compilation.SyntaxTrees.All(tree => warnings.Any(diagnostic => diagnostic.Location.SourceTree == tree)));
    }

    [TestMethod]
    public async Task DoesNotReportZeroIssues() {
      var codes = new[] {
        """
        public static class Program {
          public static void Main() {
          }
        }
        """
      };
      var compilation = CreateCompilation(codes);

      var diagnostics = await Analyze(compilation);

      Assert.IsFalse(diagnostics.Any(diagnostic => diagnostic.GetMessage().Contains("(0 issues)")));
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(Compilation compilation) {
      var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ParallelAnalyzer());
      return await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();
    }

    private static Compilation CreateCompilation(string[] codes) {
      var compilation = TestUtilities.LoadCompilationModel(OutputKind.ConsoleApplication, codes, Array.Empty<string>()).Compilation;
      var trees = codes.Select((code, index) => CSharpSyntaxTree.ParseText(code, path: $"File{index}.cs"));
      return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(trees);
    }
  }
}
