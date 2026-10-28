using MediatR;

namespace NotificationEngine.Service.Features.Commands;

/// <summary>
/// Giriş yapmış kullanıcının şifresini değiştirir. Başarılı olunca kullanıcının TÜM refresh
/// token'ları (bu cihaz dahil) iptal edilir — her yerde yeniden giriş gerekir.
/// UserId istemciden değil, controller'da oturumdan (ICurrentUserService) gelir.
/// </summary>
public class ChangePasswordCommand : IRequest
{
    public int UserId { get; set; }
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
