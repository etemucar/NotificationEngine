namespace NotificationEngine.Service.Dtos.Responses;

/// <summary>
/// Offset/limit list endpoint'lerinin dönüş zarfı. TotalCount, controller'ın
/// X-Total-Count header'ını set edebilmesi için taşınıyor — response body'ye
/// eklenmiyor (TMF list endpoint'leri düz dizi döner, zarf değil).
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items      { get; set; } = [];
    public int              TotalCount { get; set; }
}
