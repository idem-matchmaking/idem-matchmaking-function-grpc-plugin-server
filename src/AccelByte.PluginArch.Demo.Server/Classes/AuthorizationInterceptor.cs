// Copyright (c) 2022-2023 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Grpc.Core;
using Grpc.Core.Interceptors;

using AccelByte.Sdk.Core;
using AccelByte.Sdk.Feature.LocalTokenValidation;

namespace AccelByte.PluginArch.Demo.Server
{
    public class AuthorizationInterceptor : Interceptor
    {
        private readonly ILogger<AuthorizationInterceptor> _Logger;

        private readonly IAccelByteServiceProvider _ABProvider;

        private readonly string _Namespace;

        private readonly List<string> _Whitelist = new List<string>()
        {
            "/grpc.reflection.v1alpha.ServerReflection/ServerReflectionInfo",
            "/grpc.health.v1.Health/Check",
            "/grpc.health.v1.Health/Watch"
        };

        protected void Authenticate(ServerCallContext context)
        {
            if (_Whitelist.IndexOf(context.Method.Trim()) > -1)
                return;

            string? authToken = context.RequestHeaders.GetValue("authorization");
            if (authToken == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "No authorization token provided."));

            string[] authParts = authToken.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (authParts.Length != 2)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid authorization token format"));

            bool b = _ABProvider.Sdk.ValidateToken(authParts[1]);
            if (!b)
                throw new Exception("Invalid access token.");

            AccessTokenPayload? payload = _ABProvider.Sdk.ParseAccessToken(authParts[1], false);
            if (payload == null)
                throw new Exception("Could not read access token payload");

            if (payload.ExtendNamespace != _Namespace)
                throw new Exception($"Invalid access token for this namespace. Access token is intended for '{payload.ExtendNamespace}' namespace");
        }

        public AuthorizationInterceptor(ILogger<AuthorizationInterceptor> logger, IAccelByteServiceProvider abSdkProvider)
        {
            _Logger = logger;
            _ABProvider = abSdkProvider;
            _Namespace = abSdkProvider.Config.Namespace;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                Authenticate(context);
                return await continuation(request, context);
            }
            catch (Exception x)
            {
                _Logger.LogError(x, $"Authorization error: {x.Message}");
                throw new RpcException(new Status(StatusCode.Unauthenticated, x.Message));
            }
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                Authenticate(context);
                await continuation(request, responseStream, context);
            }
            catch (Exception x)
            {
                _Logger.LogError(x, $"Authorization error: {x.Message}");
                throw new RpcException(new Status(StatusCode.Unauthenticated, x.Message));
            }
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                Authenticate(context);
                return await continuation(requestStream, context);
            }
            catch (Exception x)
            {
                _Logger.LogError(x, $"Authorization error: {x.Message}");
                throw new RpcException(new Status(StatusCode.Unauthenticated, x.Message));
            }
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                Authenticate(context);
                await continuation(requestStream, responseStream, context);
            }
            catch (Exception x)
            {
                _Logger.LogError(x, $"Authorization error: {x.Message}");
                throw new RpcException(new Status(StatusCode.Unauthenticated, x.Message));
            }
        }
    }
}