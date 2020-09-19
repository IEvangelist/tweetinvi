﻿using System;
using Newtonsoft.Json;
using Tweetinvi.Core.JsonConverters;
using Tweetinvi.Models.DTO;

namespace Tweetinvi.Core.DTO
{
    public class SavedSearchDTO : ISavedSearchDTO
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("IdStr")]
        public string IdStr { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("created_at")]
        [JsonConverter(typeof(JsonPropertyConverterRepository))]
        public DateTimeOffset CreatedAt { get; set; }
    }
}