import os
import re

test_dir = r"c:\dev\personal\SPT-Check-Mods\Tests\CheckModsExtended.Tests\Commands"

def fix_file(filename, setup_lines, run_args_str):
    filepath = os.path.join(test_dir, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    if "CommandApp<" in content:
        return # already fixed

    # Ensure using statements
    usings = ["using Microsoft.Extensions.DependencyInjection;", "using CheckModsExtended.Utils;", "using Spectre.Console.Testing;"]
    for using in usings:
        if using not in content:
            content = using + "\n" + content

    # Replace everything from `var settings = new ...` to `var result = ...; var exitCode = await result;`
    # with the CommandApp code.
    
    # We will use regex to find the execute block
    pattern = re.compile(r'var settings = new ([^;]+);[\s\n]+var method = typeof\([^)]+\)\.GetMethod\("ExecuteAsync",[^;]+;[\s\n]+var result = \(Task<int>\)method!\.Invoke\([^;]+;[\s\n]+var exitCode = await result;')
    
    def replacer(match):
        # We need to construct the argument string based on the settings if possible, 
        # or we can just pass the run_args_str if they are the same for all tests in the file.
        # But wait, settings might differ per test!
        return f"{setup_lines}\n        var app = new CommandApp<{filename.replace('Tests.cs', '')}>(new TypeRegistrar(services));\n        app.Configure(config => config.ConfigureConsole(new TestConsole()));\n        var exitCode = await app.RunAsync(new string[0]);"

    content, num_subs = pattern.subn(replacer, content)
    if num_subs == 0:
        print(f"Could not find reflection block in {filename}")
        return
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"Fixed {filename}")


fix_file("IgnoreAddCommandTests.cs", 
         "var services = new ServiceCollection();\n        services.AddSingleton<IIgnoreService>(fakeService);\n        services.AddSingleton<IModCheckReporter>(fakeReporter);",
         "new[] { \"1\", \"1.0\", \"2.0\" }")

fix_file("IgnoreRemoveCommandTests.cs", 
         "var services = new ServiceCollection();\n        services.AddSingleton<IIgnoreService>(fakeService);\n        services.AddSingleton<IModCheckReporter>(fakeReporter);",
         "new[] { \"1\" }")

fix_file("IgnoreListCommandTests.cs", 
         "var services = new ServiceCollection();\n        services.AddSingleton<IIgnoreService>(fakeService);\n        services.AddSingleton<IModCheckReporter>(fakeReporter);",
         "new string[0]")

fix_file("ListModsCommandTests.cs", 
         "var services = new ServiceCollection();\n        services.AddSingleton<IModScannerService>(fakeScanner);\n        services.AddSingleton<IModCheckReporter>(fakeReporter);\n        services.AddSingleton<ISptInstallationService>(fakeSpt);",
         "new string[0]") # Note: there are two tests in ListModsCommandTests.cs, so fix_file with regex might only replace the first one. Let me handle ListModsCommandTests manually.
