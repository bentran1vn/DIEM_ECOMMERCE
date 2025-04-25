using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Commands.Match;

public class CreateMatchCommandHandler : ICommandHandler<Contract.Services.Match.Commands.CreateMatchCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Categories, Guid> _categoryRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Factories, Guid> _factoryRepository;
    private readonly IRepositoryBase<ApplicationDbContext, MatchMedias, Guid> _matchMediasRepository;
    private readonly IMediaService _mediaService;

    public CreateMatchCommandHandler(IRepositoryBase<ApplicationDbContext, Matches, Guid> matchRepository, IRepositoryBase<ApplicationDbContext, Categories, Guid> categoryRepository, IRepositoryBase<ApplicationDbContext, Factories, Guid> factoryRepository, IMediaService mediaService, IRepositoryBase<ApplicationDbContext, MatchMedias, Guid> matchMediasRepository)
    {
        _matchRepository = matchRepository;
        _categoryRepository = categoryRepository;
        _factoryRepository = factoryRepository;
        _mediaService = mediaService;
        _matchMediasRepository = matchMediasRepository;
    }

    public async Task<Result> Handle(Contract.Services.Match.Commands.CreateMatchCommand request, CancellationToken cancellationToken)
    {
        // Validate Factory exists and belongs to the user
        var factory = await _factoryRepository.FindByIdAsync(request.FactoryId, cancellationToken);
        if (factory == null || factory.IsDeleted)
        {
            return Result.Failure(new Error("404", "Factory not found"));
        }

        // Validate Category exists
        var category = await _categoryRepository.FindByIdAsync(request.CategoryId, cancellationToken);
        if (category == null || category.IsDeleted)
        {
            return Result.Failure(new Error("404", "Category not found"));
        }

        // Check if match with same name already exists for this factory
        var existingMatch = await _matchRepository.FindAll(
                m => m.Name.ToLower() == request.Name.Trim().ToLower() && 
                     m.FactoryId == request.FactoryId && 
                     !m.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingMatch != null)
        {
            return Result.Failure(new Error("409", "A match with this name already exists for this factory"));
        }

        // Upload images
        List<Task<string>> uploadTasks = new List<Task<string>>();
        foreach (var image in request.CoverImages)
        {
            if (image.Length > 0)
            {
                var imageTask = _mediaService.UploadImageAsync(image);
                uploadTasks.Add(imageTask);
            }
        }

        if (uploadTasks.Count == 0)
        {
            return Result.Failure(new Error("400", "At least one valid image must be provided"));
        }

        List<string> coverImageUrls = (await Task.WhenAll(uploadTasks)).ToList();

        // Create new match
        var match = new Matches
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            FactoryId = request.FactoryId,
            CategoryId = request.CategoryId
        };

        _matchRepository.Add(match);

        var medias = coverImageUrls.Select(x => new MatchMedias
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            Url = x,
        });
        
        _matchMediasRepository.AddRange(medias);

        return Result.Success("Match created successfully");
    }
}