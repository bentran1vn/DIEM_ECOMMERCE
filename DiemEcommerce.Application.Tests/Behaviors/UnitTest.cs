// using System.Data.Common;
// using DiemEcommerce.Application.Behaviors;
// using DiemEcommerce.Contract.Abstractions.Shared;
// using DiemEcommerce.Persistence;
// using FluentValidation;
// using FluentValidation.Results;
// using MediatR;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Storage;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Xunit;
//
// namespace DiemEcommerce.Application.Tests.Behaviors;
//
// public class ValidationPipelineBehaviorTests
// {
//     private readonly Mock<IValidator<TestRequest>> _validatorMock;
//     private readonly ValidationPipelineBehavior<TestRequest, Result> _behavior;
//
//     public ValidationPipelineBehaviorTests()
//     {
//         _validatorMock = new Mock<IValidator<TestRequest>>();
//         _behavior = new ValidationPipelineBehavior<TestRequest, Result>(new[] { _validatorMock.Object });
//     }
//
//     [Fact]
//     public async Task Handle_WithValidRequest_ShouldProceed()
//     {
//         // Arrange
//         var request = new TestRequest();
//         var validationResult = new FluentValidation.Results.ValidationResult();
//         
//         _validatorMock
//             .Setup(x => x.Validate(request))
//             .Returns(validationResult);
//         
//         var nextResult = Result.Success();
//         RequestHandlerDelegate<Result> next = () => Task.FromResult(nextResult);
//
//         // Act
//         var result = await _behavior.Handle(request, next, CancellationToken.None);
//
//         // Assert
//         Assert.Same(nextResult, result);
//     }
//
//     [Fact]
//     public async Task Handle_WithValidationErrors_ShouldReturnValidationResult()
//     {
//         // Arrange
//         var request = new TestRequest();
//         var validationFailures = new List<ValidationFailure>
//         {
//             new ValidationFailure("Property1", "Error message 1"),
//             new ValidationFailure("Property2", "Error message 2")
//         };
//         
//         var validationResult = new FluentValidation.Results.ValidationResult(validationFailures);
//         
//         _validatorMock
//             .Setup(x => x.Validate(request))
//             .Returns(validationResult);
//         
//         RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Success());
//
//         // Act
//         var result = await _behavior.Handle(request, next, CancellationToken.None);
//
//         // Assert
//         var validationErrors = ((IValidationResult)result).Errors;
//         Assert.Equal(2, validationErrors.Length);
//         Assert.Equal("Property1", validationErrors[0].Code);
//         Assert.Equal("Error message 1", validationErrors[0].Message);
//         Assert.Equal("Property2", validationErrors[1].Code);
//         Assert.Equal("Error message 2", validationErrors[1].Message);
//     }
//
//     // Test request class
//     public class TestRequest : IRequest<Result> { }
// }
//
// public class PerformancePipelineBehaviorTests
// {
//     private readonly Mock<ILogger<TestRequest>> _loggerMock;
//     private readonly PerformancePipelineBehavior<TestRequest, Result> _behavior;
//
//     public PerformancePipelineBehaviorTests()
//     {
//         _loggerMock = new Mock<ILogger<TestRequest>>();
//         _behavior = new PerformancePipelineBehavior<TestRequest, Result>(_loggerMock.Object);
//     }
//
//     [Fact]
//     public async Task Handle_WhenExecutionIsFast_ShouldNotLogWarning()
//     {
//         // Arrange
//         var request = new TestRequest();
//         var result = Result.Success();
//         
//         RequestHandlerDelegate<Result> next = () => Task.FromResult(result);
//
//         // Act
//         var actualResult = await _behavior.Handle(request, next, CancellationToken.None);
//
//         // Assert
//         Assert.Same(result, actualResult);
//         _loggerMock.Verify(
//             x => x.Log(
//                 LogLevel.Warning,
//                 It.IsAny<EventId>(),
//                 It.IsAny<It.IsAnyType>(),
//                 It.IsAny<Exception>(),
//                 It.IsAny<Func<It.IsAnyType, Exception, string>>()),
//             Times.Never);
//     }
//
//     // Test request class
//     public class TestRequest : IRequest<Result> { }
// }
//
// public class TransactionPipelineBehaviorTests
// {
//     private readonly Mock<ApplicationDbContext> _dbContextMock;
//     private readonly Mock<DbTransaction> _transactionMock;
//     private readonly Mock<Database> _databaseMock;
//     private readonly TransactionPipelineBehavior<TestCommand, Result> _behavior;
//
//     public TransactionPipelineBehaviorTests()
//     {
//         _transactionMock = new Mock<DbTransaction>();
//         _databaseMock = new Mock<Database>();
//         _dbContextMock = new Mock<ApplicationDbContext>();
//         
//         var dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
//         _dbContextMock.Object.Database.Returns(_databaseMock.Object);
//         
//         _databaseMock
//             .Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
//             .ReturnsAsync(_transactionMock.Object);
//         
//         _databaseMock
//             .Setup(db => db.CreateExecutionStrategy())
//             .Returns(new Mock<IExecutionStrategy>().Object);
//         
//         _behavior = new TransactionPipelineBehavior<TestCommand, Result>(_dbContextMock.Object);
//     }
//
//     [Fact]
//     public async Task Handle_WhenCommandRequest_ShouldUseTransaction()
//     {
//         // Arrange
//         var command = new TestCommand();
//         var result = Result.Success();
//         
//         RequestHandlerDelegate<Result> next = () => Task.FromResult(result);
//
//         // Act
//         var actualResult = await _behavior.Handle(command, next, CancellationToken.None);
//
//         // Assert
//         Assert.Same(result, actualResult);
//         _databaseMock.Verify(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
//         _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
//     }
//
//     // Test command class
//     public class TestCommand : IRequest<Result> { }
// }