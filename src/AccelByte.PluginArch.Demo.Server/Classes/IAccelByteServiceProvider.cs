using System;
using System.IdentityModel.Tokens.Jwt;

using AccelByte.Sdk.Core;

namespace AccelByte.PluginArch.Demo.Server
{
    public interface IAccelByteServiceProvider
    {
        AccelByteSDK Sdk { get; }

        AppSettingConfigRepository Config { get; }        
    }
}
