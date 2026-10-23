using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Localization;
using Core.AiSoft.Localization;

namespace Core.AiSoft.Blazor.Client;

[Dependency(ReplaceServices = true)]
public class AiSoftBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AiSoftResource> _localizer;

    public AiSoftBrandingProvider(IStringLocalizer<AiSoftResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];

    public override string? LogoUrl => "~/images/logo/leptonxlite/logo-light-thumbnail.png";

    public override string? LogoIconUrl => "~/images/logo/leptonxlite/logo-dark-thumbnail.png";
}
