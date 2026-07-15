import sys
content = open('Extensions/ServiceCollectionExtensions.cs', 'r').read()
content = content.replace('new SPTarkov.DI.DependencyInjectionHandler(services).AddInjectableTypesFromAssembly(typeof(ServiceCollectionExtensions).Assembly).InjectAll();\n', '')
content = content.replace('        return services;\n', '        var handler = new SPTarkov.DI.DependencyInjectionHandler(services);\n        handler.AddInjectableTypesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);\n        handler.InjectAll();\n        return services;\n')
open('Extensions/ServiceCollectionExtensions.cs', 'w').write(content)
