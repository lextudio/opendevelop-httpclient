using Microsoft.DotNet.Interactive.Http.Parsing;
using Xunit;

namespace OpenDevelop.HttpClient.Tests
{
    public class HttpParserTests
    {
        [Fact]
        public void Parse_SimpleGetRequest_ReturnsOneRequest()
        {
            var text = "GET https://example.com/api HTTP/1.1";
            var result = HttpRequestParser.Parse(text);
            var requests = new System.Collections.Generic.List<HttpRequestNode>();
            foreach (var child in result.SyntaxTree.RootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }
            Assert.Single(requests);
            Assert.Equal("GET", requests[0].MethodNode?.Text);
        }

        [Fact]
        public void Parse_MultipleRequests_ReturnsMultiple()
        {
            var text = "GET https://example.com/1\n###\nPOST https://example.com/2";
            var result = HttpRequestParser.Parse(text);
            var requests = new System.Collections.Generic.List<HttpRequestNode>();
            foreach (var child in result.SyntaxTree.RootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }
            Assert.Equal(2, requests.Count);
        }

        [Fact]
        public void TryGetHttpRequestMessage_SimpleRequest_ReturnsRequest()
        {
            var text = "GET https://example.com/api HTTP/1.1";
            var result = HttpRequestParser.Parse(text);
            var requests = new System.Collections.Generic.List<HttpRequestNode>();
            foreach (var child in result.SyntaxTree.RootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }
            Assert.Single(requests);

            HttpBindingDelegate bind = node => HttpBindingResult<object?>.Failure(Array.Empty<Microsoft.CodeAnalysis.Diagnostic>());
            var msgResult = requests[0].TryGetHttpRequestMessage(bind);
            Assert.True(msgResult.IsSuccessful);
            Assert.NotNull(msgResult.Value);
            Assert.Equal(HttpMethod.Get, msgResult.Value!.Method);
        }

        [Fact]
        public void Parse_WithHeaders_ParsesHeaders()
        {
            var text = "GET https://example.com/api HTTP/1.1\nContent-Type: application/json\nAccept: */*";
            var result = HttpRequestParser.Parse(text);
            var requests = new System.Collections.Generic.List<HttpRequestNode>();
            foreach (var child in result.SyntaxTree.RootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }
            Assert.Single(requests);
            Assert.NotNull(requests[0].HeadersNode);
        }

        [Fact]
        public void Parse_WithBody_ParsesBody()
        {
            var text = "POST https://example.com/api HTTP/1.1\nContent-Type: application/json\n\n{\"key\": \"value\"}";
            var result = HttpRequestParser.Parse(text);
            var requests = new System.Collections.Generic.List<HttpRequestNode>();
            foreach (var child in result.SyntaxTree.RootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }
            Assert.Single(requests);
            Assert.NotNull(requests[0].BodyNode);
        }

        [Fact]
        public void Parse_WithFileVariable_ParsesVariable()
        {
            var text = "@host = example.com\nGET https://{{host}}/api HTTP/1.1";
            var result = HttpRequestParser.Parse(text);
            var root = result.SyntaxTree.RootNode;
            var vars = root.TryGetDeclaredVariables(node => HttpBindingResult<object?>.Failure(Array.Empty<Microsoft.CodeAnalysis.Diagnostic>()));
            Assert.True(vars.declaredVariables.Count > 0);
        }

        [Fact]
        public void Parse_NamedRequest_DetectsName()
        {
            var text = "# @name login\nPOST https://example.com/login HTTP/1.1";
            var result = HttpRequestParser.Parse(text);
            var requests = new System.Collections.Generic.List<HttpRequestNode>();
            foreach (var child in result.SyntaxTree.RootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }
            Assert.Single(requests);
            Assert.True(requests[0].IsNamedRequest);
        }
    }
}
