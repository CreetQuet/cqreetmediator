using System.Threading;
using System.Threading.Tasks;

namespace CQReetMediator.Abstractions;

public interface IPreProcessorBehavior<in TRequest> where TRequest : IRequest
{
    Task ProcessAsync(TRequest request, CancellationToken ct);
}

public interface IPreProcessorBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task ProcessAsync(TRequest request, CancellationToken ct);
}
