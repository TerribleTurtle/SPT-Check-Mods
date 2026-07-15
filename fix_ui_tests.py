import os
import re

test_dir = r"c:\dev\personal\SPT-Check-Mods\Tests\CheckModsExtended.Tests\Services\UI"

for filename in os.listdir(test_dir):
    if filename.endswith("Tests.cs"):
        filepath = os.path.join(test_dir, filename)
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            
        if "IDisposable" in content:
            continue
            
        # Add System if not present
        if "using System;" not in content:
            content = "using System;\n" + content
            
        # Replace class definition
        class_match = re.search(r'public sealed class (\w+Tests)', content)
        if not class_match:
            continue
            
        class_name = class_match.group(1)
        content = re.sub(
            r'public sealed class ' + class_name,
            f'public sealed class {class_name} : IDisposable',
            content
        )
        
        # Add constructor and Dispose
        constructor_and_dispose = f"""
    private readonly Spectre.Console.IAnsiConsole _originalConsole;

    public {class_name}()
    {{
        _originalConsole = AnsiConsole.Console;
    }}

    public void Dispose()
    {{
        AnsiConsole.Console = _originalConsole;
    }}
"""
        # insert after the class definition
        class_def_end = content.find('{', content.find(f'class {class_name}')) + 1
        content = content[:class_def_end] + constructor_and_dispose + content[class_def_end:]
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"Fixed {filename}")

