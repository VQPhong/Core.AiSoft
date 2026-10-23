using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Core.AiSoft.Blazor;

public class AiSoftStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add(new BundleFile("main.css", true));
    }
}
