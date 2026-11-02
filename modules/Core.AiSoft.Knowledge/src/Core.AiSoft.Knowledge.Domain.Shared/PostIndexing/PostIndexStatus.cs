namespace Core.AiSoft.Knowledge.PostIndexing;

public enum PostIndexStatus
{
    NeverIndexed = 0,
    UpToDate = 1,
    Stale = 2,
    Running = 3
}
