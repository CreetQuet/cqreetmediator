using System.Threading;
using System.Threading.Tasks;

namespace CQReetMediator.Abstractions;

public interface IPostProcessorBehavior<in TRequest> where TRequest : IRequest
{
    Task ProcessAsync(TRequest request, CancellationToken ct);
}

public interface IPostProcessorBehavior<in TRequest, in TResponse> where TRequest : IRequest<TResponse>
{
    Task ProcessAsync(TRequest request, TResponse? response, CancellationToken ct);
}
