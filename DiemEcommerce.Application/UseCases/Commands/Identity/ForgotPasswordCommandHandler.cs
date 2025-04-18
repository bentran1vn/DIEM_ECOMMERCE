using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.Extensions.Caching.Distributed;

namespace DiemEcommerce.Application.UseCases.Commands.Identity;

public class ForgotPasswordCommandHandler : ICommandHandler<Command.ForgotPasswordCommand>
{
    private readonly IMailService _mailService;
    private readonly ICacheService _cacheService;
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;

    public ForgotPasswordCommandHandler(IMailService mailService, ICacheService cacheService, IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository)
    {
        _mailService = mailService;
        _cacheService = cacheService;
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(Command.ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user =
            await _userRepository.FindSingleAsync(x =>
                x.Email.Equals(request.Email), cancellationToken);
        
        if (user is null)
        {
            throw new Exception("User Not Existed !");
        }
        
        Random random = new Random();
        var randomNumber = random.Next(0, 100000).ToString("D5");
        
        var slidingExpiration = 120;
        var absoluteExpiration = 120;
        var options = new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(slidingExpiration))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(absoluteExpiration));
        
        // await _mailService.SendMail(EmailExtensions.ForgotPasswordBody(randomNumber, $"{user.Firstname} {user.Lastname}", request.Email));
        
        await _cacheService.SetAsync($"{nameof(Command.ForgotPasswordCommand)}-UserAccount:{user.Email}", randomNumber, options, cancellationToken);
        
        return Result.Success("Send Mail Successfully !");
    }
}