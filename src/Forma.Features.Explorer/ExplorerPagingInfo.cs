namespace Forma.Features.Explorer;

/// <summary>How the currently shown resources are paged.</summary>
/// <param name="PageSize">How many resources a page holds.</param>
/// <param name="PageCount">How many pages the current match count spans.</param>
/// <param name="EffectivePage">The page currently shown.</param>
/// <param name="HasMorePages">Whether a further page is available beyond what is shown.</param>
public sealed record ExplorerPagingInfo(int PageSize, int PageCount, int EffectivePage, bool HasMorePages);
