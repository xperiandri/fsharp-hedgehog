# Copilot Instructions

## Project Details

* Hedgehog is a property-based testing library for .NET. It generates inputs, runs a property against them and shrinks a failure to a minimal example. It is written in F# and can be used from C# as well.
* The .NET SDK is pinned in #file:'global.json'. Every project targets net8.0, so the .NET 8 runtime has to be installed next to the SDK to run them.
* Build and test the way the workflows do, from the repository root:
  * `dotnet restore` and `dotnet tool restore`
  * `dotnet build --no-restore --configuration Release`
  * `dotnet test --no-build --configuration Release`
* The tests of the core also run under Fable: `npm install`, then `npm test` in `tests/Hedgehog.Tests`. The Node.js version is in #file:'.nvmrc', and `engine-strict` in #file:'.npmrc' makes `npm install` fail on any other version.
* The API documentation is generated with `dotnet fsi gen-docfx.fsx Release` and built with `dotnet docfx build docs/docfx.json`.
* Package versions are written in the project files; there is no central package management.
* Files are committed with LF line endings. `./validate.sh`, which the workflows run, rejects Windows line endings.
* #file:'.editorconfig' sets the indentation: four spaces, two in project files.

## Solution Structure

F# compiles the files of a project in the order of its `Compile` items. A new file goes into `src/Hedgehog/Hedgehog.fsproj` at the place its dependencies allow.

```text
/
├── src/
│   ├── Hedgehog/                  – the library: generators, ranges, shrinking and properties
│   │   ├── Autogen/               – Gen.auto, which uses reflection (not compiled by Fable)
│   │   └── Linq/                  – the API for C# (not compiled by Fable)
│   ├── Hedgehog.Xunit/            – xUnit v3 integration
│   ├── Hedgehog.NUnit/            – NUnit integration
│   └── Hedgehog.MSTest/           – MSTest integration
├── tests/
│   ├── Hedgehog.Tests/            – tests of the core, also compiled by Fable and run with Mocha
│   ├── Hedgehog.Autogen.Tests/    – tests of Gen.auto; Hedgehog.Autogen.Tests.CSharp does the same from C#
│   ├── Hedgehog.Linq.Tests/       – tests of the API for C#
│   ├── Hedgehog.Xunit.Tests.FSharp/ and Hedgehog.Xunit.Tests.CSharp/
│   ├── Hedgehog.NUnit.Tests.FSharp/
│   ├── Hedgehog.MSTest.Tests.FSharp/, Hedgehog.MSTest.Tests.CSharp/ and Hedgehog.MSTest.Tests.FSharp.MinimumMSTest/
│   ├── Hedgehog.Benchmarks/       – BenchmarkDotNet benchmarks
│   └── Hedgehog.DieHarder/        – writes random numbers to stdout for the dieharder test suite
├── docs/                          – the DocFX site: articles, templates and the API section
├── gen-docfx.fsx                  – generates the DocFX YAML of the API
└── img/                           – logos
```

`Hedgehog.MSTest.Tests.FSharp.MinimumMSTest` compiles the test files of `Hedgehog.MSTest.Tests.FSharp` against the oldest MSTest that `Hedgehog.MSTest` supports.

## Libraries in Use

* [`TypeShape`](https://github.com/eiriktsarpalis/TypeShape) – the reflection behind `Gen.auto`; not referenced under Fable
* [`Fable`](https://fable.io/) – compiles the core to JavaScript; the sources of the core ship in the NuGet package for it
* [`Expecto`](https://github.com/haf/expecto) and [`Fable.Mocha`](https://github.com/Zaid-Ajaj/Fable.Mocha) – tests of the core on .NET and under Fable
* [`xUnit v3`](https://xunit.net/), [`NUnit`](https://nunit.org/) and [`MSTest`](https://github.com/microsoft/testfx) – the test frameworks that Hedgehog integrates with and is tested against
* [`Unquote`](https://github.com/SwensenSoftware/unquote) – assertions in `Hedgehog.Autogen.Tests`
* [`AwesomeAssertions`](https://awesomeassertions.org/) – assertions in `Hedgehog.Xunit.Tests.CSharp`, `Hedgehog.MSTest.Tests.CSharp` and `Hedgehog.Autogen.Tests.CSharp`
* [`BenchmarkDotNet`](https://benchmarkdotnet.org/) – benchmarks

## MCP Servers

Agents discover servers from #file:'.mcp.json'; these are only hints on when to prefer one.

* For F# work, prefer the `F#` server ([FsLangMCP](https://github.com/Neftedollar/FsLangMCP)) over `rg`/plain text search whenever the task depends on symbol meaning, compile context, cross-project usage, diagnostics, or safe refactoring preview. Its tool package is pinned in #file:'.config/dotnet-tools.json' – run `dotnet tool restore` before first use. It targets net10.0, which is why #file:'global.json' asks for the .NET 10 SDK.
* Use the `GitHub` server for code search in dependency repositories. In Claude Code it authenticates through `.claude/scripts/github-mcp-headers.ps1`, which reuses the GitHub token stored by Git Credential Manager, because the server does not support the OAuth dynamic client registration Claude Code needs.
* Use the `Microsoft Docs` server for official Microsoft and Azure documentation.
* When adding or changing a server, keep `servers` (the format of Visual Studio and VS Code) and `mcpServers` (Claude Code) in sync and sorted alphabetically by key.

## Testing

* Start with `dotnet test tests/Hedgehog.Tests`, see #file:'CONTRIBUTING.md'. `src/Hedgehog/Script.fsx` holds scripts that include properties which fail on purpose.
* Code of the core that Fable cannot compile belongs under `Autogen/` or `Linq/`, or behind `#if !FABLE_COMPILER`. The same directive keeps such tests out of the Fable run.
* The test `PropertyAttribute is discovered and run` in `Hedgehog.Xunit.Tests.FSharp` depends on the order in which xUnit runs the tests of its module, and that order depends on the directory the assembly runs from. It can fail without any change in the code.
* If a change can affect performance, run the benchmarks in `tests/Hedgehog.Benchmarks`.

## General

* Match the style of the surrounding code.
* Discuss a significant change in an issue before opening a pull request, see #file:'CONTRIBUTING.md'.
* All comments, documentation, README files, and markdown files must be written in **English only**.
* Make only high-confidence suggestions when reviewing code changes.
