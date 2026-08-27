using System.Text.Json;
using Microsoft.AspNetCore.Http;
using UltimateSolution.API.Middlewares;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.API.IntegrationTests;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncReturnsStandardBadRequestEnvelopeForDomainValidationException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new DomainValidationException("The request is invalid."));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(document.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("The request is invalid.", document.RootElement.GetProperty("message").GetString());
        Assert.Equal("validation_error", document.RootElement.GetProperty("errors")[0].GetString());
    }
}
