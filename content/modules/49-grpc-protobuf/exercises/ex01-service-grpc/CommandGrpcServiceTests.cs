global using System;
global using System.Threading;
global using System.Threading.Tasks;

using Asteria.Commands.V1;
using Grpc.Core;
using Xunit;

public sealed class CommandGrpcServiceTests
{
    [Fact]
    public async Task Empty_idempotency_key_is_rejected_before_calling_the_handler()
    {
        var handler = new RecordingHandler();
        var service = new CommandGrpcService(handler);

        var exception = await Assert.ThrowsAsync<RpcException>(
            () => service.Submit(new SubmitCommandRequest(), null!));

        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Valid_request_observes_deadline_and_propagates_cancellation()
    {
        var handler = new RecordingHandler();
        var service = new CommandGrpcService(handler);
        using var cancellation = new CancellationTokenSource();
        var context = new RecordingServerCallContext(cancellation.Token);

        var reply = await service.Submit(
            new SubmitCommandRequest
            {
                IdempotencyKey = "request-42",
                Kind = "render",
                Payload = "scene-7"
            },
            context);

        Assert.True(context.DeadlineObserved);
        Assert.Equal(cancellation.Token, handler.ReceivedToken);
        Assert.Equal("request-42", handler.ReceivedKey);
        Assert.Equal(handler.Result.ToString(), reply.CommandId);
    }

    private sealed class RecordingHandler : ICommandHandler
    {
        public Guid Result { get; } = Guid.NewGuid();
        public int CallCount { get; private set; }
        public string? ReceivedKey { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<Guid> SubmitAsync(
            string key,
            string kind,
            string payload,
            CancellationToken cancellationToken)
        {
            CallCount++;
            ReceivedKey = key;
            ReceivedToken = cancellationToken;
            return Task.FromResult(Result);
        }
    }

    private sealed class RecordingServerCallContext(CancellationToken cancellationToken)
        : ServerCallContext
    {
        public bool DeadlineObserved { get; private set; }

        protected override string MethodCore => "asteria.commands.v1.CommandService/Submit";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "in-memory";
        protected override DateTime DeadlineCore
        {
            get
            {
                DeadlineObserved = true;
                return DateTime.UtcNow.AddSeconds(5);
            }
        }

        protected override Metadata RequestHeadersCore { get; } = [];
        protected override CancellationToken CancellationTokenCore => cancellationToken;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => null!;

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
            Task.CompletedTask;

        protected override ContextPropagationToken CreatePropagationTokenCore(
            ContextPropagationOptions? options) => null!;
    }
}

namespace Asteria.Commands.V1
{
    public sealed class SubmitCommandRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }

    public sealed class SubmitCommandReply
    {
        public string CommandId { get; set; } = string.Empty;
    }

    public static class CommandService
    {
        public abstract class CommandServiceBase
        {
            public virtual Task<SubmitCommandReply> Submit(
                SubmitCommandRequest request,
                ServerCallContext context) =>
                throw new NotImplementedException();
        }
    }
}
