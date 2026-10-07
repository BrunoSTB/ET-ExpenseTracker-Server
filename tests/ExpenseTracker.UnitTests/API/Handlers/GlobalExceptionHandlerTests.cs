using ExpenseTracker.API.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ExpenseTracker.UnitTests.API.Handlers;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WithUnhandledException_WritesInternalServerErrorProblem()
    {
        // Arrange
        ProblemDetailsContext? written = null;
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        problemDetailsService.TryWriteAsync(Arg.Do<ProblemDetailsContext>(c => written = c)).Returns(true);
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, problemDetailsService);
        var httpContext = new DefaultHttpContext();
        var exception = new InvalidOperationException("boom");

        // Act
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        written.Should().NotBeNull();
        written!.Exception.Should().BeSameAs(exception);
        written.ProblemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        written.ProblemDetails.Detail.Should().BeNull();
    }
}
