using System;
using Microsoft.AspNetCore.Mvc;

namespace Core.AiSoft.Blazor.Pages.CmsKit;

/*
 * Blogs/Index.cshtml and BlogPosts/Index.cshtml next to this file override the CMS Kit admin list pages
 * (/Cms/Blogs, /Cms/BlogPosts), copied from abpframework/abp 10.7.0,
 * modules/cms-kit/src/Volo.CmsKit.Admin.Web/Pages/CmsKit/{Blogs,BlogPosts}/Index.cshtml (only @model changed).
 * Both lists are replaced by the Blazor index management page, so their models just redirect there.
 * The CMS Kit create / update (editor) pages are untouched.
 */

public static class KnowledgeIndexManagementPage
{
    public const string Url = "/knowledge/index-management";
}

public class KnowledgeBlogsIndexRedirectModel : Volo.CmsKit.Admin.Web.Pages.CmsKit.Blogs.IndexModel
{
    public IActionResult OnGet() => Redirect(KnowledgeIndexManagementPage.Url);
}

public class KnowledgeBlogPostsIndexRedirectModel : Volo.CmsKit.Admin.Web.Pages.CmsKit.BlogPosts.IndexModel
{
    /// <summary>
    /// Optional blog to preselect (kept when a link carries it).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public Guid? BlogId { get; set; }

    public IActionResult OnGet() => Redirect(BlogId.HasValue
        ? $"{KnowledgeIndexManagementPage.Url}?blogId={BlogId}"
        : KnowledgeIndexManagementPage.Url);
}
