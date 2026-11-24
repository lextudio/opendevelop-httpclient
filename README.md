# OpenDevelop HTTP Client Add-in

An OpenDevelop add-in that provides HTTP client support for `.http` and `.rest` files, similar to VS Code's REST Client extension.

## Features

- Open and edit `.http` / `.rest` files in OpenDevelop
- Syntax highlighting for HTTP requests
- Send HTTP requests and view responses
- Variable support (file variables, environment variables, system variables)
- Named requests for request chaining
- Request separator (`###`) support
- Authentication (Basic, Bearer, Digest, AWS Signature v4, Cognito, OAuth2, Azure AD v2, OIDC, SSL client cert)
- Cookie jar for subsequent requests
- Proxy support
- cURL import/export
- Code snippet generation (Python, JavaScript, C#, cURL)
- Request history
- Hover tooltips and Go to Definition
- Code completion (Ctrl+Space)

## Installation

### From OpenDevelop AddIn Manager (Recommended)

1. Open OpenDevelop
2. Go to **Tools > Options > AddIns > Trusted repositories**
3. Add `lextudio/opendevelop-httpclient` as a trusted repository
4. Open **Tools > AddIn Manager**
5. Search for "HTTP Client" and install
6. Restart OpenDevelop

### From GitHub Release

1. Download the latest `.odaddin` file from [Releases](https://github.com/lextudio/opendevelop-httpclient/releases)
2. In OpenDevelop, go to **Tools > AddIn Manager > Install from file**
3. Select the downloaded `.odaddin` file
4. Restart OpenDevelop

### From Source

```bash
# Clone the repository
git clone https://github.com/lextudio/opendevelop-httpclient.git
cd opendevelop-httpclient

# Set OpenDevelop root
export OPENDEVELOP_ROOT=/path/to/OpenDevelop

# Build
cd src/OpenDevelop.HttpClient
dotnet build OpenDevelop.HttpClient.csproj -p:OpenDevelopRoot=/path/to/OpenDevelop/
```

The build output is deployed to `$(OpenDevelopRoot)AddIns/Misc/HttpClient/`.

## Usage

1. Create a `.http` or `.rest` file in your project
2. Write HTTP requests:

```http
GET https://httpbin.org/get HTTP/1.1
Accept: application/json

###

POST https://httpbin.org/post HTTP/1.1
Content-Type: application/json

{
  "name": "test",
  "value": 42
}
```

3. Place cursor on a request and press **Tools > Send HTTP Request** (or right-click > Send Request)
4. View the response in a new document tab

### Variables

```http
@host = api.example.com
@token = {{$guid}}

GET https://{{host}}/api/users HTTP/1.1
Authorization: Bearer {{token}}
```

### Environments

Create `http-client.env.json` in your workspace:

```json
{
  "dev": {
    "host": "localhost:8080",
    "token": "dev-token"
  },
  "prod": {
    "host": "api.example.com",
    "token": "prod-token"
  }
}
```

Switch environments via **Tools > Switch HTTP Environment**.

### Authentication

```http
# Basic Auth
GET https://httpbin.org/basic-auth/user/pass HTTP/1.1
Authorization: Basic user pass

# Bearer Token
GET https://api.example.com/data HTTP/1.1
Authorization: Bearer {{token}}

# AWS Signature v4
GET https://s3.amazonaws.com/bucket/key HTTP/1.1
Authorization: AWS accessKey secretKey region:us-east-1 service:s3
```

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

## Testing

```bash
cd tests/HttpClient.Tests
dotnet test HttpClient.Tests.csproj
```

## License

MIT (same as Microsoft.DotNet.Interactive.Http.Parsing)