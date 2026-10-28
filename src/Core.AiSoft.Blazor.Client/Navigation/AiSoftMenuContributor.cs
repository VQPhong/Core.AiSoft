using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Core.AiSoft.Localization;
using Core.AiSoft.Permissions;
using Core.AiSoft.MultiTenancy;
using Volo.Abp.Account.Localization;
using Volo.Abp.UI.Navigation;
using Localization.Resources.AbpUi;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.Users;
using Volo.Abp.TenantManagement.Blazor.Navigation;
using Volo.Abp.Identity.Blazor;
namespace Core.AiSoft.Blazor.Client.Navigation;
public class AiSoftMenuContributor : IMenuContributor
{
    private readonly IConfiguration _configuration;
    public AiSoftMenuContributor(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
        else if (context.Menu.Name == StandardMenus.User)
        {
            await ConfigureUserMenuAsync(context);
        }
    }
    private static async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<AiSoftResource>();

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 6;
        context.Menu.AddItem(new ApplicationMenuItem(
            AiSoftMenus.Home,
            l["Menu:Home"],
            "/",
            icon: "fas fa-home",
            order: 1
        ));

        //context.Menu.AddItem(new ApplicationMenuItem(
        //        AiSoftMenus.Knowledge,
        //        l["Menu:Knowledge"],
        //        icon: "fas fa-book",
        //        order: 2)
        //    .AddItem(new ApplicationMenuItem(
        //        AiSoftMenus.KnowledgeEditor,
        //        l["Menu:KnowledgeEditor"],
        //        "/Cms/BlogPosts",
        //        icon: "fas fa-pen-to-square",
        //        target: "_self",
        //        requiredPermissionName: "CmsKit.BlogPosts")));

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 3);
    }

    private async Task ConfigureUserMenuAsync(MenuConfigurationContext context)
    {
        if (OperatingSystem.IsBrowser())
        {
            //Blazor wasm menu items
            var authServerUrl = _configuration["AuthServer:Authority"] ?? "";
            var accountResource = context.GetLocalizer<AccountResource>();
            context.Menu.AddItem(new ApplicationMenuItem("Account.Manage", accountResource["MyAccount"], $"{authServerUrl.EnsureEndsWith('/')}Account/Manage", icon: "fa fa-cog", order: 900,  target: "_blank").RequireAuthenticated());

        }
        else
        {
            //Blazor server menu items
        }
        await Task.CompletedTask;
    }
}
