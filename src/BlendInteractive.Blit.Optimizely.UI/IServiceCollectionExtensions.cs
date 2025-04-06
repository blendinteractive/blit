using BlendInteractive.Blit.Optimizely.Data;
using BlendInteractive.Blit.Xml;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlendInteractive.Blit.Optimizely.UI;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBlendImportAdminUI(this IServiceCollection builder, IConfiguration configuration, Action<BlitConfiguration>? configure = null)
    {

        var blitConfiguration = new BlitConfiguration();
        configure?.Invoke(blitConfiguration);

        builder.AddSingleton(new DatastoreFactory(configuration.GetConnectionString("EPiServerDB")!));
        builder.AddTransient<IBatchService, BatchService>();
        builder.AddSingleton<IContentSerializer, XmlContentSerializer>();
        builder.AddTransient<ContentImportService>();
        builder.AddTransient<IContentQueryResolver, ContentLoaderQueryResolver>();
        builder.AddSingleton(blitConfiguration);

        builder.Configure<ProtectedModuleOptions>(
            pm =>
            {
                if (!pm.Items.Any(i => i.Name.Equals("BlendInteractive.Blit.Optimizely.UI", StringComparison.OrdinalIgnoreCase)))
                {
                    pm.Items.Add(new ModuleDetails { Name = "BlendInteractive.Blit.Optimizely.UI" });
                }
            });

        return builder;
    }
}
