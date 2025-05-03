using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Match;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Match;

public class GetAllMatchesQueryHandler : IQueryHandler<Contract.Services.Match.Queries.GetAllMatchQuery, PagedResult<Responses.MatchResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;

    public GetAllMatchesQueryHandler(IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository)
    {
        _matchRepository = matchRepository;
    }

    public async Task<Result<PagedResult<Responses.MatchResponse>>> Handle(Contract.Services.Match.Queries.GetAllMatchQuery request, CancellationToken cancellationToken)
    {
        var query = _matchRepository.FindAll(m => !m.IsDeleted);

        // Apply category filter if provided
        if (request.CategoryId != null && request.CategoryId.Any())
        {
            query = query.Where(m => request.CategoryId.Contains(m.CategoriesId));
        }

        // Apply search term filter if provided
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLower();
            query = query.Where(m => 
                m.Name.ToLower().Contains(searchTerm) || 
                m.Description.ToLower().Contains(searchTerm));
        }

        // Include related entities
        query = query
            .Include(m => m.Categories)
            .Include(m => m.Factories);

        // Project to response type
        var pagedQuery = query.Select(m => new Responses.MatchResponse
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            CoverImages = m.CoverImages.Select(x => new Responses.MatchMedia
            {
                Id = x.Id,
                Url = x.Url
            }),
            CategoryId = m.CategoriesId,
            CategoryName = m.Categories.Name,
            FactoryId = m.FactoriesId,
            FactoryName = m.Factories.Name,
            FactoryAddress = m.Factories.Address,
            FactoryPhoneNumber = m.Factories.PhoneNumber
        });

        // Create paged result
        var result = await PagedResult<Responses.MatchResponse>.CreateAsync(
            pagedQuery, 
            request.PageIndex, 
            request.PageSize);

        return Result.Success(result);
    }
}