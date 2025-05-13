using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiemEcommerce.Application.Behaviors;
using DiemEcommerce.Contract.Abstractions.Shared;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace DiemEcommerce.Application.Tests.Behaviors.ValidationPipeline;

public class ValidationPipelineBehaviorTests
    {
        private readonly Mock<IValidator<TestRequest>> _validatorMock;
        private readonly ValidationPipelineBehavior<TestRequest, Result> _behavior;

        public ValidationPipelineBehaviorTests()
        {
            _validatorMock = new Mock<IValidator<TestRequest>>();
            _behavior = new ValidationPipelineBehavior<TestRequest, Result>(new[] { _validatorMock.Object });
        }

        [Fact]
        public async Task Handle_WithValidRequest_ShouldProceed()
        {
            // Arrange
            var request = new TestRequest();
            var validationResult = new ValidationResult();
            
            _validatorMock
                .Setup(x => x.Validate(request))
                .Returns(validationResult);
            
            var expectedResult = Result.Success();
            RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Same(expectedResult, result);
        }

        [Fact]
        public async Task Handle_WithValidationErrors_ShouldReturnValidationResult()
        {
            // Arrange
            var request = new TestRequest();
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("PropertyName1", "Error message 1"),
                new ValidationFailure("PropertyName2", "Error message 2")
            };
    
            var validationResult = new ValidationResult(validationFailures);
    
            _validatorMock
                .Setup(x => x.Validate(request))
                .Returns(validationResult);
    
            RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
    
            // Check if result implements IValidationResult
            var validationResultTyped = result as IValidationResult;
            Assert.NotNull(validationResultTyped);
    
            // Verify error count
            Assert.Equal(2, validationResultTyped.Errors.Length);
    
            // Check that errors have correct property names and messages
            // The implementation might be using property names as error codes
            Assert.Contains(validationResultTyped.Errors, 
                e => e.Code == "PropertyName1" && e.Message == "Error message 1");
            Assert.Contains(validationResultTyped.Errors, 
                e => e.Code == "PropertyName2" && e.Message == "Error message 2");
        }
        
        [Fact]
        public async Task Handle_WithNoValidators_ShouldProceed()
        {
            // Arrange
            var request = new TestRequest();
            var emptyValidators = new List<IValidator<TestRequest>>();
            var behavior = new ValidationPipelineBehavior<TestRequest, Result>(emptyValidators);
            
            var expectedResult = Result.Success();
            RequestHandlerDelegate<Result> next = () => Task.FromResult(expectedResult);

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Same(expectedResult, result);
        }

        [Fact]
        public async Task Handle_WithGenericResultType_ShouldReturnCorrectValidationResultType()
        {
            // Arrange
            var request = new TestGenericRequest();
            var validator = new Mock<IValidator<TestGenericRequest>>();
            
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("PropertyName", "Error message")
            };
            
            var validationResult = new ValidationResult(validationFailures);
            
            validator
                .Setup(x => x.Validate(request))
                .Returns(validationResult);
            
            var behavior = new ValidationPipelineBehavior<TestGenericRequest, Result<string>>(
                new[] { validator.Object });
            
            RequestHandlerDelegate<Result<string>> next = () => 
                Task.FromResult(Result.Success("Test"));

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.IsType<ValidationResult<string>>(result);
            
            var validationResultTyped = result as IValidationResult;
            Assert.NotNull(validationResultTyped);
            Assert.Single(validationResultTyped.Errors);
        }

        // Test classes
        public class TestRequest : IRequest<Result> { }
        public class TestGenericRequest : IRequest<Result<string>> { }
    }