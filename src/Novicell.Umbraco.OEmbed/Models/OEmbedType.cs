using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Novicell.Umbraco.OEmbed.Models;

public enum OEmbedType
{
    /// <summary>
    /// This type is used for representing playable videos.
    /// </summary>
    [EnumMember(Value = "video")]
    [JsonStringEnumMemberName("video")]
    Video,
    /// <summary>
    /// This type is used for representing static photos.
    /// </summary>
    [EnumMember(Value = "photo")]
    [JsonStringEnumMemberName("photo")]
    Photo,
    /// <summary>
    /// This type allow a provider to return any generic embed data
    /// </summary>
    [EnumMember(Value = "link")]
    [JsonStringEnumMemberName("link")]
    Link,
    /// <summary>
    /// This type is used for rich HTML content that does not fall under one of the other categories. 
    /// </summary>
    [EnumMember(Value = "rich")]
    [JsonStringEnumMemberName("rich")]
    Rich
}
