using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Match;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Match;

public class GetMatchByIdQueryHandler : IQueryHandler<Contract.Services.Match.Queries.GetMatchByIdQuery, Responses.MatchDetailResponse>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;

    public GetMatchByIdQueryHandler(IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository)
    {
        _matchRepository = matchRepository;
    }

    public async Task<Result<Responses.MatchDetailResponse>> Handle(Contract.Services.Match.Queries.GetMatchByIdQuery request, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.FindAll(
                m => m.Id == request.Id && !m.IsDeleted,
                m => m.Categories,
                m => m.Factories)
            .FirstOrDefaultAsync(cancellationToken);

        if (match == null)
        {
            return Result.Failure<Responses.MatchDetailResponse>(new Error("404", "Match not found"));
        }

        // Map to response
        var response = new Responses.MatchDetailResponse
        {
            Name = match.Name,
            Description = match.Description,
            Price = match.Price,
            Quantity = match.Quantity,
            CoverImages = match.CoverImages.Where(x => !x.IsDeleted).Select(x => new Responses.MatchMedia
            {
                Id = x.Id,
                Url = x.Url
            }),
            CategoryId = match.CategoriesId,
            CategoryName = match.Categories.Name,
            FactoryId = match.FactoriesId,
            FactoryName = match.Factories.Name,
            FactoryAddress = match.Factories.Address,
            FactoryPhoneNumber = match.Factories.PhoneNumber,
            FactoryEmail = match.Factories.Email,
            FactoryWebsite = match.Factories.Website,
            FactoryDescription = match.Factories.Description,
            FactoryTaxCode = match.Factories.TaxCode,
            FactoryBankAccount = match.Factories.BankAccount,
            FactoryBankName = match.Factories.BankName,
            FactoryLogo = match.Factories.Logo,
            Feedbacks = new List<Contract.Services.Feedback.Responses.FeedbackResponse>() // Initialize empty feedbacks list
        };

        return Result.Success(response);
    }
}