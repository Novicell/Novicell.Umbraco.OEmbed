using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Novicell.Umbraco.OEmbed.Models
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
        public double? ThumbnailHeight { get; set; }
        
        [DataMember(Name = "thumbnail_width")]
        public double? ThumbnailWidth { get; set; }
        
        [DataMember(Name = "html")]
        public string Html { get; set; }
        
        [DataMember(Name = "url")]
        public string Url { get; set; }
        
        [DataMember(Name = "height")]
        public double? Height { get; set; }
        
        [DataMember(Name = "width")]
        public double? Width { get; set; }

        [JsonExtensionData]
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly IDictionary<string, JToken> _additionalData = new Dictionary<string, JToken>();

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out JToken value) =>
            _additionalData.TryGetValue(key, out value);
    }
}