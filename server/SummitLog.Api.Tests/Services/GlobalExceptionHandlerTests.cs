using System.Text.Json;
using SummitLog.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace SummitLog.Api.Tests.Services;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_SetsStatus500AndProblemDetailsBody()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        body.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(body);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem!.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
    }
}
