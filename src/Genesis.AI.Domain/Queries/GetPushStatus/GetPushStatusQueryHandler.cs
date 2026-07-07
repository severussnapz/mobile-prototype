using Genesis.AI.Domain.Interfaces;
using MediatR;

namespace Genesis.AI.Domain.Queries.GetPushStatus;

public sealed class GetPushStatusQueryHandler : IRequestHandler<GetPushStatusQuery, GetPushStatusResult>
{
    private readonly IPushFailureLogRepository _repository;

    public GetPushStatusQueryHandler(IPushFailureLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetPushStatusResult> Handle(GetPushStatusQuery request, CancellationToken cancellationToken)
    {
        var count = await _repository.GetUnresolvedCountAsync(request.ProjectId, cancellationToken);
        return new GetPushStatusResult(count);
    }
}