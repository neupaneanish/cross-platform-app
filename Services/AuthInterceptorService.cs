using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using TuinFounder.External.Authentication.V1;
using TuinFounder.Gateway.Authentication.V1;

namespace TuinFounder.Services;

public class AuthInterceptorService(
    ExternalAuthenticationService.ExternalAuthenticationServiceClient client,
    ITokenService tokenService
) : Interceptor
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        if (context.Method.Name == nameof(client.Refresh))
            return continuation(request, context);

        var callTask = StartCallAsync(request, context, continuation);

        return new AsyncUnaryCall<TResponse>(
            GetResponseAsync(callTask),
            GetResponseHeadersAsync(callTask),
            () => GetStatus(callTask),
            () => GetTrailers(callTask),
            () => Dispose(callTask));
    }

    private async Task<AsyncUnaryCall<TResponse>> StartCallAsync<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        var (_, expired) = tokenService.RequiredRefresh();

        if (expired)
        {
            await RefreshLock.WaitAsync(context.Options.CancellationToken);
            try
            {
                var (refresh, expiringSoon) = tokenService.RequiredRefresh();

                if (expiringSoon)
                {
                    if (string.IsNullOrWhiteSpace(refresh))
                    {
                        tokenService.Delete(true);
                        throw ErrorResponse(StatusCode.Unauthenticated, "Session expired");
                    }
                    else
                    {
                        try
                        {
                            var req = new RefreshRequest { Refresh = refresh };
                            var res = await client.RefreshAsync(req);
                            tokenService.Save(res.Token);
                        }
                        catch (RpcException e) when (e.StatusCode is StatusCode.Unauthenticated)
                        {
                            tokenService.Delete(true);
                            throw ErrorResponse(StatusCode.Unauthenticated, "Session expired");
                        }
                    }
                }
            }
            finally
            {
                RefreshLock.Release();
            }
        }

        var options = context.Options;
        var access = tokenService.GetAccess();

        var serviceName = context.Method.ServiceName;

        if (serviceName == GatewayAuthenticationService.Descriptor.FullName)
        {
            if (!tokenService.IsAuthenticated())
                throw ErrorResponse(StatusCode.PermissionDenied, "Permission Denied");

            var headers = new Metadata();
            if (options.Headers is not null)
                foreach (var entry in options.Headers)
                    headers.Add(entry);

            headers.Add("authorization", $"Bearer {access}");
            options = options.WithHeaders(headers);

            var updatedContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                options);

            return continuation(request, updatedContext);
        }

        if (serviceName == ExternalAuthenticationService.Descriptor.FullName)
            return tokenService.IsAuthenticated()
                ? throw ErrorResponse(StatusCode.PermissionDenied, "Permission Denied")
                : continuation(request, context);

        throw ErrorResponse(StatusCode.PermissionDenied, "Permission Denied");
    }

    private async Task<TResponse> GetResponseAsync<TResponse>(Task<AsyncUnaryCall<TResponse>> callTask)
    {
        var call = await callTask;

        try
        {
            return await call.ResponseAsync;
        }
        catch (RpcException e) when (e.StatusCode is StatusCode.Unauthenticated)
        {
            tokenService.Delete(true);
            throw ErrorResponse(StatusCode.Unauthenticated, "Session expired");
        }
    }

    private static async Task<Metadata> GetResponseHeadersAsync<TResponse>(Task<AsyncUnaryCall<TResponse>> callTask)
    {
        var call = await callTask;
        return await call.ResponseHeadersAsync;
    }

    private static Status GetStatus<TResponse>(Task<AsyncUnaryCall<TResponse>> callTask)
    {
        return callTask.IsCompletedSuccessfully ? callTask.Result.GetStatus() : Status.DefaultCancelled;
    }

    private static Metadata GetTrailers<TResponse>(Task<AsyncUnaryCall<TResponse>> callTask)
    {
        return callTask.IsCompletedSuccessfully ? callTask.Result.GetTrailers() : [];
    }

    private static void Dispose<TResponse>(Task<AsyncUnaryCall<TResponse>> callTask)
    {
        if (callTask.IsCompletedSuccessfully) callTask.Result.Dispose();
    }

    private static RpcException ErrorResponse(StatusCode statusCode, string detail)
    {
        return new RpcException(new Status(statusCode, detail));
    }
}