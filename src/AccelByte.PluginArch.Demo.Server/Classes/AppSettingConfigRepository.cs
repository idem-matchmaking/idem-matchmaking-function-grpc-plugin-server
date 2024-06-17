using System;
using Microsoft.Extensions.Configuration;
using AccelByte.Sdk.Core.Logging;
using AccelByte.Sdk.Core.Repository;

namespace AccelByte.PluginArch.Demo.Server
{
    public class AppSettingConfigRepository : IConfigRepository
    {
        public string BaseUrl { get; set; } = String.Empty;

        public string ClientId { get; set; } = String.Empty;

        public string ClientSecret { get; set; } = String.Empty;

        public string AppName { get; set; } = String.Empty;

        public string TraceIdVersion { get; set; } = String.Empty;

        public string Namespace { get; set; } = String.Empty;

        public bool EnableTraceId { get; set; } = false;

        public bool EnableUserAgentInfo { get; set; } = false;

        public string ResourceName { get; set; } = String.Empty;

        public IHttpLogger? Logger { get; set; } = null;

        public void ReadEnvironmentVariables()
        {
            string? abBaseUrl = Environment.GetEnvironmentVariable("AB_BASE_URL");
            if ((abBaseUrl != null) && (abBaseUrl.Trim() != String.Empty))
                BaseUrl = abBaseUrl.Trim();

            string? abClientId = Environment.GetEnvironmentVariable("AB_CLIENT_ID");
            if ((abClientId != null) && (abClientId.Trim() != String.Empty))
                ClientId = abClientId.Trim();

            string? abClientSecret = Environment.GetEnvironmentVariable("AB_CLIENT_SECRET");
            if ((abClientSecret != null) && (abClientSecret.Trim() != String.Empty))
                ClientSecret = abClientSecret.Trim();

            string? abNamespace = Environment.GetEnvironmentVariable("AB_NAMESPACE");
            if ((abNamespace != null) && (abNamespace.Trim() != String.Empty))
                Namespace = abNamespace.Trim();

            string? appResourceName = Environment.GetEnvironmentVariable("APP_RESOURCE_NAME");
            if (appResourceName == null)
                appResourceName = "MMV2GRPCSERVICE ";
            ResourceName = appResourceName;
        }
    }
}
