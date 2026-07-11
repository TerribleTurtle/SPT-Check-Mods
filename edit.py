import sys
import re

with open('Services/UI/ITextRenderer.cs', 'r', encoding='utf-8') as f:
    text = f.read()

methods_to_remove = [
    r'    /// <summary>Asks whether to fetch the author-maintained remote ignore list\. Defaults to no\.</summary>\n    bool PromptFetchRemoteIgnores\(\);\n\n',
    r'    /// <summary>Shows the end-of-run menu and returns the chosen action\.</summary>\n    EndOfRunChoice PromptEndOfRun\(int openableUpdateCount, bool canManageIgnoredUpdates\);\n\n',
    r'    /// <summary>Shows a checklist of update candidates \(those in <paramref name="preIgnoredApiModIds"/> pre-checked\) and returns the mods the user chose to ignore\.</summary>\n    IReadOnlyList<Mod> SelectUpdatesToIgnore\(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds\);\n\n',
    r'    /// <summary>Prompts whether to submit new ignore entries as a GitHub issue, defaulting to no\.</summary>\n    bool PromptReportIgnores\(\);\n\n',
    r'    /// <summary>Prompts the user to confirm a mod match\.</summary>\n    Task<bool> PromptForConfirmationAsync\(PendingConfirmation confirmation\);\n'
]

for m in methods_to_remove:
    text = re.sub(m, '', text)

with open('Services/UI/ITextRenderer.cs', 'w', encoding='utf-8') as f:
    f.write(text)

