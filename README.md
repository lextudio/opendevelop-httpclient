# OpenDevelop HTTP Client Add-in

An OpenDevelop add-in that provides HTTP client support for `.http` and `.rest` files, similar to VS Code's REST Client extension.

## Features

- Open and edit `.http` / `.rest` files in OpenDevelop
- Syntax highlighting for HTTP requests
- Send HTTP requests and view responses
- Variable support (file variables, environment variables, system variables)
- Named requests for request chaining
- Request separator (`###`) support

## Architecture

This add-in leverages [Microsoft.DotNet.Interactive.Http.Parsing](https://github.com/dotnet/interactive/tree/main/src/Microsoft.DotNet.Interactive.Http.Parsing) (MIT licensed) for parsing `.http` files and converting parsed requests to .NET `HttpRequestMessage` objects. The parser source is embedded in the `Parsing/` directory.

## Building

```bash
# Set OpenDevelop root
export OPENDEVELOP_ROOT=/path/to/OpenDevelop

# Build
cd src/OpenDevelop.HttpClient
dotnet build OpenDevelop.HttpClient.csproj -p:OpenDevelopRoot=/path/to/OpenDevelop/
```

## Deploying

The build output is deployed to `$(OpenDevelopRoot)AddIns/Misc/HttpClient/`.

## License

MIT (same as Microsoft.DotNet.Interactive.Http.Parsing)
