[![NuGet](https://img.shields.io/nuget/v/Lost.Environment.Observable.svg)](https://www.nuget.org/packages/Lost.Environment.Observable/)

C# package for observing changes to **user** environment variables. Windows-only.

```csharp
Lost.EnvironmentVariables.Vars.PropertyChanged += (_, e) =>
{
    string? value = Lost.EnvironmentVariables.Vars[e.PropertyName];
    Console.WriteLine($"Variable {e.PropertyName} changed to {value}");
};
```
