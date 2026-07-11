using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModCheckReporter"/>.
/// </summary>
public sealed class FakeModCheckReporter : IModCheckReporter
{
    private readonly List<string> _headings = [];
    private readonly List<string> _statuses = [];
    private readonly List<string> _successes = [];
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private readonly List<string> _validatedSptVersions = [];
    private List<Mod> _selectUpdatesToIgnoreResult = [];

    /// <summary> Gets headings. </summary>
    public List<string> Headings { get { return _headings.ToList(); } }
    /// <summary> Gets statuses. </summary>
    public List<string> Statuses { get { return _statuses.ToList(); } }
    /// <summary> Gets successes. </summary>
    public List<string> Successes { get { return _successes.ToList(); } }
    /// <summary> Gets warnings. </summary>
    public List<string> Warnings { get { return _warnings.ToList(); } }
    /// <summary> Gets errors. </summary>
    public List<string> Errors { get { return _errors.ToList(); } }
    /// <summary> Gets validated versions. </summary>
    public List<string> ValidatedSptVersions { get { return _validatedSptVersions.ToList(); } }

    /// <summary> Gets the last exception. </summary>
    public Exception? LastException { get; private set; }
    /// <summary> Gets or sets if prompted. </summary>
    public bool EndOfRunPrompted { get; set; }
    /// <summary> Gets or sets choice. </summary>
    public EndOfRunChoice ChoiceToReturn { get; set; } = EndOfRunChoice.Exit;
    /// <summary> Gets or sets fetch prompt result. </summary>
    public bool PromptFetchRemoteIgnoresResult { get; set; }
    /// <summary> Gets or sets report prompt result. </summary>
    public bool PromptReportIgnoresResult { get; set; }
    
    /// <summary> Gets or sets updates to ignore. </summary>
    public List<Mod> SelectUpdatesToIgnoreResult
    {
        get { return _selectUpdatesToIgnoreResult.ToList(); }
        set { _selectUpdatesToIgnoreResult = value.ToList(); }
    }

    /// <inheritdoc />
    public void Banner() { }
    /// <inheritdoc />
    public void Rule() { }
    /// <inheritdoc />
    public void Blank() { }

    /// <inheritdoc />
    public void Heading(string text) { _headings.Add(text); }
    /// <inheritdoc />
    public void Status(string text) { _statuses.Add(text); }
    /// <inheritdoc />
    public void Success(string text) { _successes.Add(text); }
    /// <inheritdoc />
    public void Warning(string text) { _warnings.Add(text); }
    /// <inheritdoc />
    public void Error(string text) { _errors.Add(text); }

    /// <inheritdoc />
    public void CouldNotReadModDll(string fileName, string reason) { _warnings.Add($"CouldNotReadModDll: {fileName} - {reason}"); }
    /// <inheritdoc />
    public void CouldNotReadSptVersion(string reason) { _warnings.Add($"CouldNotReadSptVersion: {reason}"); }
    /// <inheritdoc />
    public void PluginsDirectoryNotFound(string path) { _warnings.Add($"PluginsDirectoryNotFound: {path}"); }

    /// <inheritdoc />
    public async Task RunForgeQueryProgressAsync(int total, Func<Action<int>, Task> work)
    {
        await work(_ => { });
    }

    /// <inheritdoc />
    public async Task<T> RunForgeQueryProgressAsync<T>(int total, Func<Action<int>, Task<T>> work)
    {
        return await work(_ => { });
    }

    /// <inheritdoc />
    public void UsingPath(string path) { }
    /// <inheritdoc />
    public void DirectoryDoesNotExist(string path) { _errors.Add($"DirectoryDoesNotExist: {path}"); }

    /// <inheritdoc />
    public void ValidatingSptVersion(string version) { }
    /// <inheritdoc />
    public void SptVersionValidated(string version) { _validatedSptVersions.Add(version); }
    /// <inheritdoc />
    public void SptUpdateAvailable(SptVersionResult latest) { }
    /// <inheritdoc />
    public void CheckModsUpdate(CheckModsUpdateResult result, Version sptVersion) { }
    /// <inheritdoc />
    public void NoModsFound() { }
    /// <inheritdoc />
    public void VersionCompatibilityResults(List<Mod> mods, Version sptVersion) { }
    /// <inheritdoc />
    public void Exception(Exception ex) { LastException = ex; }
    /// <inheritdoc />
    public void LoadingWarnings(List<Mod> modsWithWarnings) { }
    /// <inheritdoc />
    public void ReconciliationResults(ModReconciliationResult result) { }
    /// <inheritdoc />
    public void MisplacedMods(MisplacedModReport report) { }
    /// <inheritdoc />
    public void UnverifiedMods(List<Mod> mods) { }
    /// <inheritdoc />
    public void DependencyResults(DependencyAnalysisResult result) { }
    /// <inheritdoc />
    public void VersionTable(List<Mod> mods) { }

    /// <inheritdoc />
    public bool PromptFetchRemoteIgnores() { return PromptFetchRemoteIgnoresResult; }
    /// <inheritdoc />
    public void RemoteIgnoresMerged(int added) { }
    /// <inheritdoc />
    public void RemoteIgnoresUnavailable() { }
    
    /// <inheritdoc />
    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates)
    {
        EndOfRunPrompted = true;
        return ChoiceToReturn;
    }

    /// <inheritdoc />
    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds)
    {
        return _selectUpdatesToIgnoreResult.ToList();
    }

    /// <inheritdoc />
    public void UpdatePagesOpened(int opened, int total) { }
    /// <inheritdoc />
    public bool PromptReportIgnores() { return PromptReportIgnoresResult; }
    /// <inheritdoc />
    public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled) { }
}
