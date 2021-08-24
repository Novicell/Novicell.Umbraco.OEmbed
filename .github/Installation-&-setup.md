### Install with dotnet
> dotnet add package Novicell.Umbraco.OEmbed

### Setup
Add AutoDiscover in Startup.cs to automatically generate embed configuration from media url
```ruby
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddUmbraco(_env, _config).AddNovicellOEmbed(o =>
            {
                o.Autodiscover = true;
            })
                .AddBackOffice()             
                .AddWebsite()
                .AddComposers()
                .Build();
        }
```