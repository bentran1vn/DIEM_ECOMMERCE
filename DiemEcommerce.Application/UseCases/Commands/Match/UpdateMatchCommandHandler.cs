using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Commands.Match;

public class UpdateMatchCommandHandler: ICommandHandler<Contract.Services.Match.Commands.UpdateMatchCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationDbContext, MatchMedias, Guid> _matchMediaRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Categories, Guid> _categoryRepository;
    private readonly IMediaService _mediaService;
    
    public UpdateMatchCommandHandler(
        IRepositoryBase<ApplicationDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationDbContext, Categories, Guid> categoryRepository,
        IMediaService mediaService, IRepositoryBase<ApplicationDbContext, MatchMedias, Guid> matchMediaRepository)
    {
        _matchRepository = matchRepository;
        _categoryRepository = categoryRepository;
        _mediaService = mediaService;
        _matchMediaRepository = matchMediaRepository;
    }
    
    public async Task<Result> Handle(Contract.Services.Match.Commands.UpdateMatchCommand request, CancellationToken cancellationToken)
    {
        // Validate match exists
        var match = await _matchRepository.FindByIdAsync(request.Id, cancellationToken);
        if (match == null || match.IsDeleted)
        {
            return Result.Failure(new Error("404", "Match not found"));
        }

        // Validate factory authorization
        if (match.FactoriesId != request.FactoryId)
        {
            return Result.Failure(new Error("403", "You are not authorized to update this match"));
        }

        // Validate category exists
        var category = await _categoryRepository.FindByIdAsync(request.CategoryId, cancellationToken);
        if (category == null || category.IsDeleted)
        {
            return Result.Failure(new Error("404", "Category not found"));
        }
        
        // Check if match with same name already exists for this factory (but different id)
        var existingMatch = await _matchRepository.FindAll(
                m => m.Name.ToLower() == request.Name.Trim().ToLower() && 
                     m.FactoriesId == request.FactoryId && 
                     m.Id != request.Id &&
                     !m.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingMatch != null)
        {
            return Result.Failure(new Error("409", "A match with this name already exists for this factory"));
        }
        
        // Remove images specified for deletion
        if (request.DeleteImages != null && request.DeleteImages.Any())
        {
            var mediaToDeletes = await _matchMediaRepository
                .FindAll(x => request.DeleteImages.Contains(x.Id)).ToListAsync(cancellationToken);

            if (mediaToDeletes.Count != request.DeleteImages.Count)
                return Result.Failure(new Error("500", "Some images not found"));
            
            _matchMediaRepository.RemoveMultiple(mediaToDeletes);
        }

        if (request.NewImages != null && request.NewImages.Any())
        {
            List<Task<string>> uploadTasks = new List<Task<string>>();
            foreach (var image in request.NewImages)
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
            
            var medias = coverImageUrls.Select(x => new MatchMedias
            {
                Id = Guid.NewGuid(),
                MatchesId = match.Id,
                Url = x,
            });
        
            _matchMediaRepository.AddRange(medias);
        }
        
        match.Name = request.Name.Trim();
        match.Description = request.Description.Trim();
        match.CategoriesId = request.CategoryId;
        match.Quantity = int.Parse(request.Quantity);
        match.Price = int.Parse(request.Price);
        match.CategoriesId = request.CategoryId;
        
        return Result.Success("Match updated successfully");
    }
}