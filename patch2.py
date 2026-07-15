import sys

content = open('Extensions/ServiceCollectionExtensions.cs', 'r').read()
if 'new SPTarkov.DI.DependencyInjectionHandler' not in content:
    content = content.replace('        return services;\n', '        new SPTarkov.DI.DependencyInjectionHandler(services).AddInjectableTypesFromAssembly(typeof(ServiceCollectionExtensions).Assembly).InjectAll();\n        return services;\n')
    open('Extensions/ServiceCollectionExtensions.cs', 'w').write(content)
