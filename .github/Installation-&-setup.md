### Install with dotnet
Gone are the days of installation via backoffice. Your only option is now to install via [Nuget](https://www.nuget.org/packages/Novicell.Umbraco.OEmbed/) - or dotnet as below:

```
> dotnet add package Novicell.Umbraco.OEmbed
```

### Setup
For everything to kick in and be registered properly, you need to enable AutoDiscover when registering in your Startup class, like so:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddUmbraco(_env, _config)
        .AddNovicellOEmbed(o => {
            o.Autodiscover = true;
        })
        .AddBackOffice()             
        .AddWebsite()
        .AddComposers()
        .Build();
}
```