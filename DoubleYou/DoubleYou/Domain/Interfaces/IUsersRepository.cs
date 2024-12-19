using System.Threading.Tasks;

using DoubleYou.Domain.DTOs;
using DoubleYou.Domain.Enums;

namespace DoubleYou.Domain.Interfaces
{
    public interface IUsersRepository
    {
        bool AnyUser();

        Task<Entities.Entities.User?> GetUserAsync();

        Task SaveUserAsync(DTO.SaveUser dto);

        Task RemoveUserAsync();

        Task<bool> GetUserIsDialogShowInstallVoiceAsync();

        Task<string> GetUserCultureAsync();

        Task<string> GetUserNativeLanguageAsync();

        Task<Topic> GetUserFavoriteTopicAsync();

        Task SetUserIsDialogShowInstallVoiceAsync(bool isDialogShowInstallVoice);

        Task SaveUserCultureAsync(string culture);

        Task SaveUserNativeLanguageAsync(string key);

        Task SaveUserFavoriteTopicAsync(string key);
    }
}