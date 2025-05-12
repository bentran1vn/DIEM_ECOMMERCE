using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Match;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Match;

public class GetMatchByFactoryIdQueryHandler: IQueryHandler<Contract.Services.Match.Queries.GetMatchByFactoryIdQuery, PagedResult<Responses.MatchResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> _factoryRepository;

    public GetMatchByFactoryIdQueryHandler(IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository, IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> factoryRepository)
    {
        _matchRepository = matchRepository;
        _factoryRepository = factoryRepository;
    }

    public async Task<Result<PagedResult<Responses.MatchResponse>>> Handle(Contract.Services.Match.Queries.GetMatchByFactoryIdQuery request, CancellationToken cancellationToken)
    {
        // First check if factory exists
        var factory = await _factoryRepository.FindByIdAsync(request.Id, cancellationToken);
        if (factory == null || factory.IsDeleted)
        {
            return Result.Failure<PagedResult<Responses.MatchResponse>>(new Error("404", "Factory not found"));
        }

        // Get matches by factory id
        var query = _matchRepository.FindAll(
            m => m.FactoriesId == request.Id && !m.IsDeleted,
            m => m.Categories);
        
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
        
        query = query.Include(m => m.Factories);

        // Project to response type
        var pagedQuery = query.Select(m => new Responses.MatchResponse
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Price = m.Price,
            Quantity = m.Quantity,
            CoverImages = m.CoverImages.Where(x => !x.IsDeleted).Select(x => new Responses.MatchMedia
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

        // Create paged result - using default paging
        var result = await PagedResult<Responses.MatchResponse>.CreateAsync(
            pagedQuery, 
            request.PageIndex, 
            request.PageSize); // Default page size

        return Result.Success(result);
    }
}