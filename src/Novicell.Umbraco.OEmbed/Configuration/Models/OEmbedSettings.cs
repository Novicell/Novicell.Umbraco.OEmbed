namespace Novicell.Umbraco.OEmbed.Configuration.Models
{
    public class OEmbedSettings
    {
        internal const string SectionKey = nameof(Novicell) + ":" + nameof(OEmbed);

        public bool Autodiscover { get; init; }
    }
}