// 版权归 MaxyMCP 所有，遵循 MIT 许可证。

namespace MaxyMCP.Editor.MCP.Server
{
    internal static class MCPBrokerProtocol
    {
        // v2: pull responses carry AcceptSseHeader (client's Accept: text/event-stream),
        //     push requests may carry ContentTypeHeader to override the client-facing
        //     response content type (used for SSE-piggybacked notifications).
        // v3: no client-facing wire change. The broker now sweeps stale attached sessions
        //     (crashed editors that never detached). Bumped so a pre-v3 broker still running
        //     after a package upgrade fails the health probe and is replaced by the new one
        //     (see MCPBrokerProcessManager.EnsureRunning's upgrade-cleanup path).
        public const int Version = 3;
        public const string Name = "maxy-unity-mcp-broker";
        public const string HealthPath = "/_maxymcp/broker/health";
        public const string AttachPath = "/_maxymcp/broker/attach";
        public const string PullPath = "/_maxymcp/broker/pull";
        public const string PushPath = "/_maxymcp/broker/push";
        public const string DetachPath = "/_maxymcp/broker/detach";
        public const string ShutdownPath = "/_maxymcp/broker/shutdown";
        public const string TokenHeader = "X-MaxyMCP-Broker-Token";
        public const string SessionHeader = "X-MaxyMCP-Broker-Session";
        public const string ReqIdHeader = "X-MaxyMCP-Broker-ReqId";
        public const string RedeliveryHeader = "X-MaxyMCP-Broker-Redelivery";
        public const string BrokerHeader = "X-MaxyMCP-Broker";
        public const string AcceptSseHeader = "X-MaxyMCP-Broker-Accept-SSE";
        public const string ContentTypeHeader = "X-MaxyMCP-Broker-Content-Type";
    }
}
