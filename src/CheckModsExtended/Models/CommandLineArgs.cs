namespace CheckModsExtended.Models;

/// <summary>
/// A wrapper for command-line arguments to be injected via dependency injection.
/// </summary>
public sealed record CommandLineArgs(string[] Args);
