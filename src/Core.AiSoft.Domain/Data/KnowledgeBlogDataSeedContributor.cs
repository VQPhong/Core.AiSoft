using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.GlobalFeatures;
using Volo.Abp.MultiTenancy;
using Volo.CmsKit.Blogs;
using Volo.CmsKit.GlobalFeatures;

namespace Core.AiSoft.Data;

public class KnowledgeBlogDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    public const string BlogName = "Knowledge";
    public const string BlogSlug = "knowledge";

    private readonly IBlogRepository _blogRepository;
    private readonly BlogManager _blogManager;
    private readonly ICurrentTenant _currentTenant;

    public KnowledgeBlogDataSeedContributor(
        IBlogRepository blogRepository,
        BlogManager blogManager,
        ICurrentTenant currentTenant)
    {
        _blogRepository = blogRepository;
        _blogManager = blogManager;
        _currentTenant = currentTenant;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (!GlobalFeatureManager.Instance.IsEnabled<BlogsFeature>())
        {
            return;
        }

        using (_currentTenant.Change(context.TenantId))
        {
            if (await _blogRepository.SlugExistsAsync(BlogSlug))
            {
                return;
            }

            var blog = await _blogManager.CreateAsync(BlogName, BlogSlug);
            await _blogRepository.InsertAsync(blog, autoSave: true);
        }
    }
}
