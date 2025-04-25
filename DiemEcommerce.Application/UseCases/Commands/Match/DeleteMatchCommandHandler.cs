using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Match;

public class DeleteMatchCommandHandler: ICommandHandler<Contract.Services.Match.Commands.DeleteMatchCommand>
{
    
    private readonly IRepositoryBase<ApplicationDbContext, Matches, Guid> _matchRepository;

    public DeleteMatchCommandHandler(
        IRepositoryBase<ApplicationDbContext, Matches, Guid> matchRepository)
    {
        _matchRepository = matchRepository;
    }

    public async Task<Result> Handle(Contract.Services.Match.Commands.DeleteMatchCommand request, CancellationToken cancellationToken)
    {
        // Validate match exists
        var match = await _matchRepository.FindByIdAsync(request.Id, cancellationToken);
        if (match == null || match.IsDeleted)
        {
            return Result.Failure(new Error("404", "Match not found"));
        }

        _matchRepository.Remove(match);

        return Result.Success("Match deleted successfully");
    }
}