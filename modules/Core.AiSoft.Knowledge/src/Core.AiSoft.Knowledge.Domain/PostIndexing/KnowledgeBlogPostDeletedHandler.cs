using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;
using Volo.CmsKit.Blogs;

namespace Core.AiSoft.Knowledge.PostIndexing;

/// <summary>
/// A Knowledge post can be (soft) deleted from the index page, the CMS Kit admin or its API:
/// whichever path, its Knowledge data goes with it. Runs in the deleting unit of work, so a failure rolls the deletion back.
/// </summary>
public class KnowledgeBlogPostDeletedHandler : ILocalEventHandler<EntityDeletedEventData<BlogPost>>, ITransientDependency
{
    private readonly PostIngestionManager _postIngestionManager;
    private readonly KnowledgePostDeletionManager _deletionManager;

    public KnowledgeBlogPostDeletedHandler(PostIngestionManager postIngestionManager, KnowledgePostDeletionManager deletionManager)
    {
        _postIngestionManager = postIngestionManager;
        _deletionManager = deletionManager;
    }

    public virtual async Task HandleEventAsync(EntityDeletedEventData<BlogPost> eventData)
    {
        Guid knowledgeBlogId;
        try
        {
            knowledgeBlogId = await _postIngestionManager.GetKnowledgeBlogIdAsync();
        }
        catch (BusinessException)
        {
            // No Knowledge blog (yet): nothing of ours to clean, and other blogs must stay deletable.
            return;
        }

        if (eventData.Entity.BlogId == knowledgeBlogId)
        {
            await _deletionManager.CleanupAsync(eventData.Entity.Id);
        }
    }
}
