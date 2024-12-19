using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Domain.DTOs;
using DoubleYou.Domain.Enums;
using DoubleYou.Domain.Interfaces;
using DoubleYou.Infrastructure.Data.Contexts;
using DoubleYou.Utilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DoubleYou.Infrastructure.Repositories
{
    public sealed class UsersRepository : IUsersRepository
    {
        private readonly IDbContextFactory<AppDBContext> m_contextFactory;
        private readonly ILogger<UsersRepository> m_logger;

#pragma warning disable IDE0290
        public UsersRepository(IDbContextFactory<AppDBContext> contextFactory, ILogger<UsersRepository> logger)
#pragma warning restore
        {
            m_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool AnyUser()
        {
            try
            {
                using var context = m_contextFactory.CreateDbContext();

                return context.User.Any();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<Domain.Entities.Entities.User?> GetUserAsync()
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                bool isExist = await context.User.AnyAsync();

                if (!isExist)
                {
                    return null;
                }

                return await context.User.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task SaveUserAsync(DTO.SaveUser dto)
        {
            ArgumentNullException.ThrowIfNull(dto, nameof(dto));
            ArgumentException.ThrowIfNullOrEmpty(dto.TranslationLanguage, nameof(dto.TranslationLanguage));
            ArgumentException.ThrowIfNullOrEmpty(dto.FavoriteTopic, nameof(dto.FavoriteTopic));

            if (!Enum.TryParse(dto.FavoriteTopic, true, out Topic topic))
            {
                throw new ArgumentException($"Invalid topic: {dto.FavoriteTopic}", nameof(dto.FavoriteTopic));
            }

            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                bool isExist = await context.User.AnyAsync();
                if (isExist)
                {
                    return;
                }

                context.User.Add(new()
                {
                    TranslationLanguage = dto.TranslationLanguage,
                    FavoriteTopic = topic
                });

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task RemoveUserAsync()
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User.FirstOrDefaultAsync();

                if (user == null)
                {
                    return;
                }

                context.User.Remove(user);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<bool> GetUserIsDialogShowInstallVoiceAsync()
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User.FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user));
                }

                return user.IsDialogShowInstallVoice;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<string> GetUserCultureAsync()
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User.FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user));
                }

                return user.CultureCode;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<string> GetUserNativeLanguageAsync()
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User.FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user));
                }

                return user.TranslationLanguage;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<Topic> GetUserFavoriteTopicAsync()
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User.FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user));
                }

                return user.FavoriteTopic;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task SetUserIsDialogShowInstallVoiceAsync(bool isDialogShowInstallVoice)
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User.FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new ArgumentNullException(nameof(user));
                }

                user.IsDialogShowInstallVoice = isDialogShowInstallVoice;

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task SaveUserCultureAsync(string culture)
        {
            ArgumentException.ThrowIfNullOrEmpty(culture, nameof(culture));

            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User
                    .FirstOrDefaultAsync() ?? throw new InvalidOperationException(Constants.USER_NOT_FOUND);

                user.CultureCode = culture;

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task SaveUserNativeLanguageAsync(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User
                    .FirstOrDefaultAsync() ?? throw new InvalidOperationException(Constants.USER_NOT_FOUND);

                user.TranslationLanguage = key;

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task SaveUserFavoriteTopicAsync(string key)
        {
            ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

            if (!Enum.TryParse(key, true, out Topic topic))
            {
                throw new ArgumentException($"Invalid topic: {key}", nameof(key));
            }

            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var user = await context.User
                    .FirstOrDefaultAsync() ?? throw new InvalidOperationException(Constants.USER_NOT_FOUND);

                user.FavoriteTopic = topic;

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        private void OnException(Exception ex)
        {
#if DEBUG
            Debug.WriteLine(ex?.InnerException?.Message.ToString());
#endif
            m_logger.LogError(ex?.InnerException, "An error occurred during operation: {Message}", ex?.InnerException?.Message);
        }
    }
}