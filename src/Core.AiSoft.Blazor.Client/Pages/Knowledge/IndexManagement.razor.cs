using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge;
using Core.AiSoft.Knowledge.Documents;
using Core.AiSoft.Knowledge.IngestionJobs;
using Core.AiSoft.Knowledge.Localization;
using Core.AiSoft.Knowledge.Permissions;
using Core.AiSoft.Knowledge.PostIndexing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;
using Volo.Abp.Content;
using Volo.CmsKit.Admin.Blogs;

namespace Core.AiSoft.Blazor.Client.Pages.Knowledge;

/// <summary>
/// Blogs (CMS Kit) → posts with their index state (Radzen grids); dev-operated buttons publish, index and delete posts.
/// Replaces the CMS Kit blog / blog post list pages (those redirect here).
/// </summary>
public partial class IndexManagement : IAsyncDisposable
{
    private static readonly TimeSpan RunningRefreshInterval = TimeSpan.FromSeconds(3);

    // CMS Kit permission (Volo.CmsKit.Permissions.CmsKitAdminPermissions.BlogPosts.Publish).
    private const string BlogPostPublishPermission = "CmsKit.BlogPosts.Publish";

    [Inject] protected IBlogAdminAppService BlogAppService { get; set; } = default!;
    [Inject] protected IBlogPostAdminAppService BlogPostAppService { get; set; } = default!;
    [Inject] protected IKnowledgePostAppService PostAppService { get; set; } = default!;
    [Inject] protected IIngestionJobAppService JobAppService { get; set; } = default!;
    [Inject] protected IKnowledgeDocumentAppService DocumentAppService { get; set; } = default!;
    [Inject] protected IStringLocalizer<KnowledgeResource> KL { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;

    /// <summary>
    /// Optional ?blogId= (used by the redirect from the CMS Kit blog post list).
    /// </summary>
    [SupplyParameterFromQuery(Name = "blogId")]
    public Guid? InitialBlogId { get; set; }

    protected const int PageSize = 10;

    protected RadzenDataGrid<BlogDto> BlogGrid { get; set; } = default!;
    protected RadzenDataGrid<KnowledgePostDto>? PostGrid { get; set; }

    protected IReadOnlyList<BlogDto> Blogs { get; set; } = [];
    protected IList<BlogDto> SelectedBlogs { get; set; } = [];
    protected BlogDto? SelectedBlog => SelectedBlogs.FirstOrDefault();
    protected bool IsKnowledgeBlog => SelectedBlog?.Slug == KnowledgeBlogConsts.BlogSlug;

    protected IReadOnlyList<KnowledgePostDto> Posts { get; set; } = [];
    protected int PostTotalCount { get; set; }
    protected bool IsLoadingPosts { get; set; }
    protected string? Filter { get; set; }
    private int _postSkipCount;

    protected bool CanCreateJob { get; set; }
    protected bool CanForce { get; set; }
    protected bool CanUpload { get; set; }
    protected bool CanPublish { get; set; }
    protected bool CanDelete { get; set; }
    protected bool IsUploading { get; set; }

    private Timer? _refreshTimer;

    protected override async Task OnInitializedAsync()
    {
        CanCreateJob = await AuthorizationService.IsGrantedAsync(KnowledgePermissions.IngestionJobs.Create);
        CanForce = await AuthorizationService.IsGrantedAsync(KnowledgePermissions.IngestionJobs.Force);
        CanUpload = await AuthorizationService.IsGrantedAsync(KnowledgePermissions.Documents.Upload);
        CanDelete = await AuthorizationService.IsGrantedAsync(KnowledgePermissions.Documents.Delete);
        CanPublish = await AuthorizationService.IsGrantedAsync(BlogPostPublishPermission);

        await LoadBlogsAsync();

        var initial = Blogs.FirstOrDefault(b => b.Id == InitialBlogId)
                      ?? Blogs.FirstOrDefault(b => b.Slug == KnowledgeBlogConsts.BlogSlug)
                      ?? Blogs.FirstOrDefault();

        SelectedBlogs = initial == null ? [] : [initial];
    }

    protected virtual async Task LoadBlogsAsync()
    {
        try
        {
            Blogs = (await BlogAppService.GetAllListAsync()).Items;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    /// <summary>
    /// Server-side paging for the post grid (Radzen LoadData). The API always orders by creation time, newest first.
    /// </summary>
    protected virtual async Task LoadPostsAsync(LoadDataArgs args)
    {
        _postSkipCount = args.Skip ?? 0;
        await FetchPostsAsync();
    }

    protected virtual async Task FetchPostsAsync()
    {
        if (SelectedBlog == null)
        {
            return;
        }

        IsLoadingPosts = true;
        try
        {
            var result = await PostAppService.GetListAsync(new GetKnowledgePostsDto
            {
                BlogId = SelectedBlog.Id,
                Filter = Filter,
                SkipCount = _postSkipCount,
                MaxResultCount = PageSize
            });

            Posts = result.Items;
            PostTotalCount = (int)result.TotalCount;
            ScheduleRefreshIfRunning();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
        finally
        {
            IsLoadingPosts = false;
        }
    }

    protected virtual async Task ReloadPostsAsync(bool resetPage = false)
    {
        if (PostGrid == null)
        {
            return;
        }

        if (resetPage)
        {
            await PostGrid.FirstPage(forceReload: true);
            return;
        }

        await PostGrid.Reload();
    }

    protected virtual async Task OnBlogSelectedAsync(BlogDto blog)
    {
        SelectedBlogs = [blog];
        Filter = null;
        _postSkipCount = 0;

        // The post grid is re-created when the knowledge-blog hint toggles; give it a render before reloading.
        await InvokeAsync(StateHasChanged);
        await ReloadPostsAsync(resetPage: true);
    }

    protected virtual async Task RefreshAsync()
    {
        await LoadBlogsAsync();
        await ReloadPostsAsync();
    }

    protected virtual async Task PublishAsync(KnowledgePostDto post)
    {
        try
        {
            await BlogPostAppService.PublishAsync(post.Id);
            NotificationService.Notify(NotificationSeverity.Success, KL["SuccessfullyPublished"]);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }

        await ReloadPostsAsync();
    }

    protected virtual async Task CreateJobAsync(KnowledgePostDto post, bool force)
    {
        if (force && post.IndexStatus == PostIndexStatus.UpToDate
                  && await DialogService.Confirm(KL["ForceIndexConfirmation"], KL["ForceIndex"]) != true)
        {
            return;
        }

        try
        {
            var job = await JobAppService.CreateAsync(new CreateIngestionJobDto { BlogPostId = post.Id, Force = force });
            NotificationService.Notify(NotificationSeverity.Success, KL["IngestionJobQueued", job.Version]);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }

        await ReloadPostsAsync();
    }

    protected virtual async Task DeleteAsync(KnowledgePostDto post)
    {
        if (await DialogService.Confirm(KL["DeleteConfirmation", post.Title], KL["Delete"]) != true)
        {
            return;
        }

        try
        {
            await PostAppService.DeleteAsync(post.Id);
            NotificationService.Notify(NotificationSeverity.Success, KL["SuccessfullyDeleted"]);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }

        await LoadBlogsAsync();
        await ReloadPostsAsync();
    }

    /// <summary>
    /// Uploads the file (converted to Markdown server-side into a Draft post) and opens that post in the CMS Kit editor.
    /// </summary>
    protected virtual async Task UploadAsync(InputFileChangeEventArgs e)
    {
        IsUploading = true;
        try
        {
            var file = e.File;
            await using var stream = file.OpenReadStream(KnowledgeDocumentConsts.DefaultMaxFileSize);

            var document = await DocumentAppService.UploadAsync(new RemoteStreamContent(stream, file.Name, file.ContentType));

            if (document.BlogPostId.HasValue)
            {
                Navigation.NavigateTo($"/Cms/BlogPosts/Update/{document.BlogPostId}", forceLoad: true);
                return;
            }

            await ReloadPostsAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
        finally
        {
            IsUploading = false;
        }
    }

    protected virtual async Task OpenHistoryAsync(KnowledgePostDto post)
    {
        try
        {
            var jobs = (await JobAppService.GetListAsync(new GetIngestionJobsDto { BlogPostId = post.Id, MaxResultCount = 50 })).Items;

            await DialogService.OpenAsync<IngestionJobHistory>(
                $"{KL["JobHistory"]} – {post.Title}",
                new Dictionary<string, object?> { [nameof(IngestionJobHistory.Jobs)] = jobs },
                new DialogOptions { Width = "960px", Resizable = true, CloseDialogOnOverlayClick = true });
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex);
        }
    }

    // CMS Kit pages are MVC: force a full page load instead of Blazor navigation.
    protected virtual void OpenEditor(KnowledgePostDto post)
        => Navigation.NavigateTo($"/Cms/BlogPosts/Update/{post.Id}", forceLoad: true);

    protected virtual void OpenViewer(KnowledgePostDto post)
        => Navigation.NavigateTo($"/blogs/{SelectedBlog!.Slug}/{post.Slug}", forceLoad: true);

    protected static BadgeStyle StatusStyle(PostIndexStatus status) => status switch
    {
        PostIndexStatus.UpToDate => BadgeStyle.Success,
        PostIndexStatus.Stale => BadgeStyle.Warning,
        PostIndexStatus.Running => BadgeStyle.Info,
        _ => BadgeStyle.Light
    };

    /// <summary>
    /// While a job of the visible page is queued/running, reload the page every few seconds until it settles.
    /// </summary>
    private void ScheduleRefreshIfRunning()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;

        if (Posts.Any(p => p.IndexStatus == PostIndexStatus.Running))
        {
            _refreshTimer = new Timer(
                _ => InvokeAsync(async () =>
                {
                    await FetchPostsAsync();
                    StateHasChanged();
                }),
                null,
                RunningRefreshInterval,
                Timeout.InfiniteTimeSpan);
        }
    }

    public ValueTask DisposeAsync()
    {
        _refreshTimer?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
