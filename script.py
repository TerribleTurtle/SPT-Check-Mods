import sys

def replace_in_file(path, target, replacement):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Normalize line endings to avoid mismatch
    content_norm = content.replace('\r\n', '\n')
    target_norm = target.replace('\r\n', '\n')
    replacement_norm = replacement.replace('\r\n', '\n')
    
    if target_norm in content_norm:
        content_norm = content_norm.replace(target_norm, replacement_norm)
        # Restore CRLF if originally present
        if '\r\n' in content:
            content_norm = content_norm.replace('\n', '\r\n')
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content_norm)
        print(f"Success for {path}")
    else:
        print(f"Target not found in {path}")

# CheckModsCommand.cs
target2 = """                if (endOfRunChoice == EndOfRunChoice.LaunchWebGui)
                {
                    var processPath = System.Environment.ProcessPath;
                    if (processPath != null)
                    {
                        var guiArgs = string.IsNullOrWhiteSpace(settings.SptPath) ? "gui" : $"gui \\"{settings.SptPath}\\"";"""

repl2 = """                if (endOfRunChoice == EndOfRunChoice.LaunchWebGui)
                {
                    // "Process inception": The CLI restarts its own executable but passes the "gui" argument.
                    // This creates a detached child process running the web dashboard, allowing the current
                    // CLI process to exit cleanly without keeping the terminal blocked.
                    var processPath = System.Environment.ProcessPath;
                    if (processPath != null)
                    {
                        var guiArgs = string.IsNullOrWhiteSpace(settings.SptPath) ? "gui" : $"gui \\"{settings.SptPath}\\"";"""

replace_in_file('Commands/CheckModsCommand.cs', target2, repl2)
