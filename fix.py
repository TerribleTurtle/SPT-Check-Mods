import re

with open("Services/SpectreModCheckReporter.cs", "r", encoding="utf-8") as f:
    text = f.read()

# Fix expression bodied methods
# Matches: public void Banner() => _textRenderer.Banner();
# To: public void Banner()\n{\n    _textRenderer.Banner();\n}
text = re.sub(r'(public (?:async )?(?:Task(?:<T>)?|void|bool|EndOfRunChoice|IReadOnlyList<Mod>)(?:<T>)? \w+\(.*?\))\s*=>\s*(.*?);', r'\1\n    {\n        return \2;\n    }', text)

# For methods returning void, we can't use "return _textRenderer.Banner()".
# But wait, C# doesn't allow returning a void value. Let me just fix them properly.
