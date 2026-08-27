using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace ParallelChecker.Core {
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class ParallelAnalyzer : DiagnosticAnalyzer {
    private const string _DiagnosticId = "ParallelChecker";
    private const string _DiagnosticInfoId = "ParallelCheckerInfo"; // NEW: separate ID for info
    private const string _DiagnosticTitle = "Concurrency Issue Detection";
    private const string _WarningFormat = "Issue: #{0} {1}";
    private const string _InfoFormat = "Detection in {0} ms ({1} issues) {2}";
    private const string _Category = "Parallelization";
    private const string _FaultSign = "*";
    private const string _NoneSign = "-";

    private static readonly Dictionary<IssueCategory, string> _helpLinks = new() {
      { IssueCategory.DataRace, "https://github.com/blaeser/parallelchecker/blob/main/doc/DataRace.md" },
      { IssueCategory.Deadlock, "https://github.com/blaeser/parallelchecker/blob/main/doc/Deadlock.md" },
      { IssueCategory.UnsafeCalls, "https://github.com/blaeser/parallelchecker/blob/main/doc/ThreadUnsafeUsage.md" }
    };

    private const string _GeneralHelpLink = "https://github.com/blaeser/parallelchecker";

    private static readonly DiagnosticDescriptor _diagnosticWarning =
      new(_DiagnosticId, _DiagnosticTitle, _WarningFormat,
        _Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: _GeneralHelpLink);
    private static readonly DiagnosticDescriptor _diagnosticInfo =
      new(_DiagnosticInfoId, _DiagnosticTitle, _InfoFormat,
        _Category, DiagnosticSeverity.Info, isEnabledByDefault: true, helpLinkUri: _GeneralHelpLink);

    private static readonly AnalysisOptions _options = new() {
      DetectedIssues = { IssueCategory.DataRace, IssueCategory.Deadlock, IssueCategory.UnsafeCalls }
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
      get {
        return ImmutableArray.Create(_diagnosticWarning, _diagnosticInfo);
      }
    }

    public override void Initialize(AnalysisContext context) {
      context.EnableConcurrentExecution();
      context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
      context.RegisterCompilationAction(CompilationAction);
    }

    private void CompilationAction(CompilationAnalysisContext context) {
      var location = context.Compilation.SyntaxTrees.FirstOrDefault()?.GetRoot().GetLocation() ?? Location.None;
      var watch = Stopwatch.StartNew();
      try {
        var result = ParallelAnalysis.FindIssues(context.Compilation, context.CancellationToken, _options, out bool faulted);
        ReportIssues(context.ReportDiagnostic, result);
        var issueCount = result.Count();
#if DEBUG
        if (issueCount > 0 || faulted) {
          ReportInfo(context.ReportDiagnostic, location, watch, issueCount.ToString(), faulted ? _FaultSign : string.Empty);
        }
#else
        if (faulted) {
          ReportInfo(context.ReportDiagnostic, location, watch, issueCount.ToString(), _FaultSign);
        }
#endif
      } catch (OperationCanceledException) {
#if DEBUG
        ReportInfo(context.ReportDiagnostic, location, watch, _NoneSign, "Cancelled");
#endif
      } catch (Exception exception) {
        ReportInfo(context.ReportDiagnostic, location, watch, _NoneSign, exception.Message);
      }
    }
    
    private void ReportInfo(Action<Diagnostic> report, Location location, Stopwatch watch, string issues, string text) {
      var diagnostic = Diagnostic.Create(_diagnosticInfo, location, watch.ElapsedMilliseconds, issues, text);
      report(diagnostic);
    }

    private void ReportIssues(Action<Diagnostic> report, IEnumerable<Issue> issueList) {
      int number = 0;
      foreach (var issue in issueList) {
        ReportIssue(report, number, issue);
        number++;
      }
    }

    private static void ReportIssue(Action<Diagnostic> report, int number, Issue issue) {
      foreach (var cause in new HashSet<Cause>(issue.Causes)) {
        if (cause.Location.SourceTree != null) {
          var title = string.Format(_WarningFormat, number, issue.Message);
          var helpLink = _GeneralHelpLink;
          _helpLinks.TryGetValue(issue.Category, out helpLink);
          var diagnostic = Diagnostic.Create(_diagnosticWarning.Id, _diagnosticWarning.Category, title, _diagnosticWarning.DefaultSeverity, _diagnosticWarning.DefaultSeverity, _diagnosticWarning.IsEnabledByDefault, 3, title, issue.Description, helpLink, cause.Location, null, null, null);
          report(diagnostic);
        }
      }
    }
  }
}
