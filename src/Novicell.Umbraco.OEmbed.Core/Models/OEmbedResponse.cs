using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Novicell.Umbraco.OEmbed.Core.Models
{
    [DataContract]
    public class OEmbedResponse
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }
        
        [DataMember(Name = "author_name")]
        public string AuthorName { get; set; }
        
        [DataMember(Name = "author_url")]
        public string AuthorUrl { get; set; }
        
        [DataMember(Name = "provider_name")]
        public string ProviderName { get; set; }
        
        [DataMember(Name = "provider_url")]
        public string ProviderUrl { get; set; }
        
        [DataMember(Name = "thumbnail_url")]
        public string ThumbnailUrl { get; set; }
        
        [DataMember(Name = "thumbnail_height")]
        public int? ThumbnailHeight { get; set; }
        
        [DataMember(Name = "thumbnail_width")]
        public int? ThumbnailWidth { get; set; }
        
        [DataMember(Name = "html")]
        public string Html { get; set; }
        
        [DataMember(Name = "url")]
        public string Url { get; set; }
        
        [DataMember(Name = "height")]
        public int? Height { get; set; }
        
        [DataMember(Name = "width")]
        public int? Width { get; set; }

        [DataMember(Name = "cache_age")]
        public int? CacheAge { get; set; }

        [JsonExtensionData]
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly IDictionary<string, JToken> _additionalData = new Dictionary<string, JToken>();

        public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
        {
            value = default;

            if (_additionalData.TryGetValue(key, out var token))
            {
                value = token.ToObject<T>();

                return true;
            }

            return false;
        }

    }
}