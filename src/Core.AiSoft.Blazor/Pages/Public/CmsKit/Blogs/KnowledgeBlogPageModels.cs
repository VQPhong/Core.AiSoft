using Microsoft.AspNetCore.Authorization;
using Volo.CmsKit.Blogs;
using Volo.CmsKit.Public.Blogs;
using Volo.CmsKit.Public.Web.Pages.Public.CmsKit.Blogs;
using Volo.CmsKit.Web.Contents;

namespace Core.AiSoft.Blazor.Pages.Public.CmsKit.Blogs;

[Authorize]
public class KnowledgeBlogIndexModel : IndexModel
{
    public KnowledgeBlogIndexModel(
        IBlogPostPublicAppService blogPostPublicAppService,
        IBlogFeatureAppService blogFeatureAppService)
        : base(blogPostPublicAppService, blogFeatureAppService)
    {
    }
}

[Authorize]
public class KnowledgeBlogPostModel : BlogPostModel
{
    public KnowledgeBlogPostModel(
        IBlogPostPublicAppService blogPostPublicAppService,
        IBlogFeatureAppService blogFeaturePublicAppService,
        ContentParser contentParser)
        : base(blogPostPublicAppService, blogFeaturePublicAppService, contentParser)
    {
    }
}
