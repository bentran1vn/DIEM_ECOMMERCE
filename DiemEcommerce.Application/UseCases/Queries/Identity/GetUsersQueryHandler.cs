using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Constant.SystemRoles;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Queries.Identity;

public class GetUsersQueryHandler: IQueryHandler<Query.GetUsers, PagedResult<Response.GetMe>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetUsersQueryHandler(IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<Response.GetMe>>> Handle(Query.GetUsers request, CancellationToken cancellationToken)
    {
        var query = _userRepository.FindAll(x => !x.IsDeleted && x.Roles.Name != RoleNames.Admin);
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(x => x.Email.Contains(request.SearchTerm) || x.Username.Contains(request.SearchTerm));
        }
        
        var querySelect = query.Select(x => new Response.GetMe()
        {
            Id = x.Id,
            Email = x.Email,
            Username = x.Username,
            Firstname = x.FirstName,
            Lastname = x.LastName,
            PhoneNumber = x.PhoneNumber,
            CreatedOnUtc = x.CreatedOnUtc,
            RoleName = x.Roles.Name,
            FactoryId = x.Factories.Id
        });
        
        var paging = await PagedResult<Response.GetMe>.CreateAsync(querySelect, request.PageIndex, request.PageSize);
        
        return Result.Success(paging);
    }
}