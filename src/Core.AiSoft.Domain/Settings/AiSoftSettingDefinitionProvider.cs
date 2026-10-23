using Volo.Abp.Settings;

namespace Core.AiSoft.Settings;

public class AiSoftSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AiSoftSettings.MySetting1));
    }
}
