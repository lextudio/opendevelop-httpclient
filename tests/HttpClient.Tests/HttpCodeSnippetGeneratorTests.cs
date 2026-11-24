using System.Net.Http;
using OpenDevelop.HttpClient;
using Xunit;

namespace OpenDevelop.HttpClient.Tests
{
    public class HttpCodeSnippetGeneratorTests
    {
        [Fact]
        public void GeneratePython_ContainsRequestsImport()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var snippet = HttpCodeSnippetGenerator.GeneratePython(request);
            Assert.Contains("import requests", snippet);
            Assert.Contains("requests.get", snippet);
        }

        [Fact]
        public void GenerateJavaScript_ContainsFetch()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var snippet = HttpCodeSnippetGenerator.GenerateJavaScript(request);
            Assert.Contains("fetch", snippet);
        }

        [Fact]
        public void GenerateCSharp_ContainsHttpClient()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var snippet = HttpCodeSnippetGenerator.GenerateCSharp(request);
            Assert.Contains("HttpClient", snippet);
        }

        [Fact]
        public void GenerateCurl_ContainsCurl()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            var snippet = HttpCodeSnippetGenerator.GenerateCurl(request);
            Assert.Contains("curl", snippet);
        }
    }
}
