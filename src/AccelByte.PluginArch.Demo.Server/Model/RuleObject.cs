// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Text.Json.Serialization;

namespace AccelByte.PluginArch.Demo.Server.Model
{
    public class RuleObject
    {
        [JsonPropertyName("shipCountMin")]
        public int ShipCountMin { get; set; } = 0;

        [JsonPropertyName("shipCountMax")]
        public int ShipCountMax { get; set; } = 0;
    }
}