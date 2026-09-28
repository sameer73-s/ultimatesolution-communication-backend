using UltimateSolution.Domain.Entities.Chat;

namespace UltimateSolution.Application.Interfaces;

public interface IActionItemAuthorizationService
{
    Task<bool> CanConvertMessageToActionItemAsync(Guid userId, ChatMessage message, CancellationToken cancellationToken = default);
}
