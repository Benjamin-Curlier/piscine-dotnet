using Asteria.Commands.V1;
using Grpc.Core;

public interface ICommandHandler
{
    Task<Guid> SubmitAsync(string key, string kind, string payload, CancellationToken cancellationToken);
}

public sealed class CommandGrpcService(ICommandHandler handler) : CommandService.CommandServiceBase
{
    public override async Task<SubmitCommandReply> Submit(
        SubmitCommandRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "idempotency_key required"));
        }

        _ = context.Deadline;
        var id = await handler.SubmitAsync(
            request.IdempotencyKey,
            request.Kind,
            request.Payload,
            context.CancellationToken);

        return new SubmitCommandReply { CommandId = id.ToString() };
    }
}
