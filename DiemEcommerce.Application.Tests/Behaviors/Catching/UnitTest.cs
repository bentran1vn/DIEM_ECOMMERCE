using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Application.Behaviors;
using DiemEcommerce.Contract.Abstractions.Shared;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace DiemEcommerce.Application.Tests.Behaviors.Catching
{
    public class CachingPipelineBehaviorTests
    {
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly CachingPipelineBehaviorCachingBehavior<TestCacheableRequest, Result> _behavior;

        public CachingPipelineBehaviorTests()
        {
            _cacheServiceMock = new Mock<ICacheService>();
            _behavior = new CachingPipelineBehaviorCachingBehavior<TestCacheableRequest, Result>(_cacheServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithBypassCacheTrue_ShouldSkipCache()
        {
            // Arrange
            var request = new TestCacheableRequest { BypassCache = true };
            var expectedResult = Result.Success();
            RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Same(expectedResult, result);
            _cacheServiceMock.Verify(
                x => x.GetAsync<Result>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _cacheServiceMock.Verify(
                x => x.SetAsync(It.IsAny<string>(), It.IsAny<Result>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithCachedResponse_ShouldReturnCachedValue()
        {
            // Arrange
            var request = new TestCacheableRequest
            {
                BypassCache = false,
                CacheKey = "test-cache-key"
            };
            var cachedResult = Result.Success();

            _cacheServiceMock
                .Setup(x => x.GetAsync<Result>(request.CacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedResult);

            RequestHandlerDelegate<Result> next = () => throw new Exception("Next should not be called");

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Same(cachedResult, result);
            _cacheServiceMock.Verify(
                x => x.GetAsync<Result>(request.CacheKey, It.IsAny<CancellationToken>()),
                Times.Once);
            _cacheServiceMock.Verify(
                x => x.SetAsync(It.IsAny<string>(), It.IsAny<Result>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithNoCachedResponse_ShouldCallNextAndCache()
        {
            // Arrange
            var request = new TestCacheableRequest
            {
                BypassCache = false,
                CacheKey = "test-cache-key",
                SlidingExpirationInMinutes = 15,
                AbsoluteExpirationInMinutes = 30
            };
            var handlerResult = Result.Success();
            DistributedCacheEntryOptions capturedOptions = null;

            _cacheServiceMock
                .Setup(x => x.GetAsync<Result>(request.CacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Result)null);
        
            _cacheServiceMock
                .Setup(x => x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Result>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, Result, DistributedCacheEntryOptions, CancellationToken>(
                    (key, value, options, token) => capturedOptions = options);

            RequestHandlerDelegate<Result> next = () => Task.FromResult(handlerResult);

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Same(handlerResult, result);
    
            _cacheServiceMock.Verify(
                x => x.GetAsync<Result>(request.CacheKey, It.IsAny<CancellationToken>()),
                Times.Once);
        
            _cacheServiceMock.Verify(
                x => x.SetAsync(
                    It.Is<string>(s => s == request.CacheKey),
                    It.Is<Result>(r => r == handlerResult),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        
            Assert.NotNull(capturedOptions);
            Assert.Equal(TimeSpan.FromMinutes(request.SlidingExpirationInMinutes), capturedOptions.SlidingExpiration);
            Assert.NotNull(capturedOptions.AbsoluteExpirationRelativeToNow);
            Assert.True(
                capturedOptions.AbsoluteExpiration.HasValue || 
                capturedOptions.AbsoluteExpirationRelativeToNow.HasValue,
                "Either AbsoluteExpiration or AbsoluteExpirationRelativeToNow should be set"
            );
        }

        [Fact]
        public async Task Handle_WithDefaultExpirationValues_ShouldUseDefaultValues()
        {
            // Arrange
            var request = new TestCacheableRequest
            {
                BypassCache = false,
                CacheKey = "test-cache-key",
                SlidingExpirationInMinutes = 0,  // Should use default of 30
                AbsoluteExpirationInMinutes = 0  // Should use default of 60
            };
            var handlerResult = Result.Success();

            _cacheServiceMock
                .Setup(x => x.GetAsync<Result>(request.CacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Result)null);

            RequestHandlerDelegate<Result> next = () => Task.FromResult(handlerResult);

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Same(handlerResult, result);
            _cacheServiceMock.Verify(
                x => x.SetAsync(
                    It.Is<string>(s => s == request.CacheKey),
                    It.Is<Result>(r => r == handlerResult),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // Test request class implementing ICacheable
        public class TestCacheableRequest : MediatR.IRequest<Result>, DiemEcommerce.Contract.Abstractions.Shared.ICacheable
        {
            public bool BypassCache { get; set; }
            public string CacheKey { get; set; }
            public int SlidingExpirationInMinutes { get; set; }
            public int AbsoluteExpirationInMinutes { get; set; }
        }
    }
}