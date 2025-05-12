using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Identity;

public class GetMeHandler : IQueryHandler<Query.GetMe, Response.GetMe>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetMeHandler(IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<Response.GetMe>> Handle(Query.GetMe request, CancellationToken cancellationToken)
    {
        var query = _userRepository.FindAll(
            x => x.Id.Equals(request.UserId));

        var user = await query.SingleOrDefaultAsync(cancellationToken);
        
        if (user is null)
        {
            throw new Exception("User Not Existed !");
        }

        var result = new Response.GetMe()
        {
            Email = user.Email,
            Username = user.Username,
            Firstname = user.FirstName,
            Lastname = user.LastName,
            PhoneNumber = user.PhoneNumber,
            CreatedOnUtc = user.CreatedOnUtc
        };
        
        return Result.Success(result);
    }
}