using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Domain.Enums;
using DoubleYou.Domain.Interfaces;
using DoubleYou.Infrastructure.Data.Contexts;
using DoubleYou.Utilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DoubleYou.Infrastructure.Repositories
{
    public sealed class WordsRepository : IWordsRepository
    {
        private readonly IDbContextFactory<AppDBContext> m_contextFactory;
        private readonly IMemoryCache m_cache;
        private readonly ILogger<WordsRepository> m_logger;
        private readonly TimeSpan m_absoluteExpiration = TimeSpan.FromHours(2);
        private readonly ImmutableList<string> m_cacheKeys;
        private readonly int m_pageSize = 25;
        private readonly int m_pageSizeForRepetition = 100;

#pragma warning disable IDE0290
        public WordsRepository(IDbContextFactory<AppDBContext> contextFactory, IMemoryCache cache, ILogger<WordsRepository> logger)
#pragma warning restore
        {
            m_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            m_cache = cache ?? throw new ArgumentNullException(nameof(cache));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_cacheKeys = CacheKeys();
        }

        public bool AnyWords()
        {
            try
            {
                if (!m_cache.TryGetValue<bool>(Constants.ANY_WORDS_KEY, out var result))
                {
                    using var context = m_contextFactory.CreateDbContext();

                    result = context.Words.Any();

                    m_cache.Set(Constants.ANY_WORDS_KEY, result, m_absoluteExpiration);
                }

                return result;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<List<Domain.Entities.Entities.Word>> WordsLearnedAsync(int page)
        {
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(page);

                var key = string.Concat(Constants.WORDS_LEARNED_KEY, page.ToString());

                if (!CacheGet<List<Domain.Entities.Entities.Word>>(key, out var result))
                {
                    await using var context = await m_contextFactory.CreateDbContextAsync();

                    result = await context.Words
                        .Where(e => e.LearnedDate != null && e.LearnedDate != DateTime.MinValue)
                        .OrderBy(e => e.LearnedDate)
                        .Skip((page - 1) * m_pageSize)
                        .Take(m_pageSize)
                        .ToListAsync();

                    CacheSet(key, result);
                }

                ArgumentNullException.ThrowIfNull(result, nameof(ImmutableList<Domain.Entities.Entities.Word>));

                return result;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<List<Domain.Entities.Entities.Word>> WordsLearnedForRepetitionAsync()
        {
            try
            {
                if (!CacheGet<List<Domain.Entities.Entities.Word>>(Constants.WORDS_LEARNED_FOR_REPETITION_KEY, out var result))
                {
                    await using var context = await m_contextFactory.CreateDbContextAsync();

                    result = await context.Words
                        .Where(e => e.LearnedDate != null && e.LearnedDate != DateTime.MinValue)
                        .OrderByDescending(e => e.LearnedDate)
                        .Take(m_pageSizeForRepetition)
                        .ToListAsync();

                    CacheSet(Constants.WORDS_LEARNED_FOR_REPETITION_KEY, result);
                }

                ArgumentNullException.ThrowIfNull(result, nameof(ImmutableList<Domain.Entities.Entities.Word>));

                return result;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<int> CountWordsLearnedAsync()
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                return await context.Words
                    .Where(e => e.LearnedDate != null && e.LearnedDate != DateTime.MinValue)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<List<Domain.Entities.Entities.Word>> WordsNotLearnedByTopicAsync(Topic topic)
        {
            try
            {
                var key = string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, topic.ToString());

                if (!CacheGet<List<Domain.Entities.Entities.Word>>(key, out var result))
                {
                    await using var context = await m_contextFactory.CreateDbContextAsync();

                    result = await context.Words
                        .Where(e => e.LearnedDate == null && e.Topic == topic)
                        .Take(250)
                        .ToListAsync();

                    CacheSet(key, result);
                }

                ArgumentNullException.ThrowIfNull(result, nameof(ImmutableList<Domain.Entities.Entities.Word>));

                return result;
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task SetWordsIntoDatabaseAsync(ImmutableDictionary<string, Topic> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));

            if (dictionary.IsEmpty)
            {
                return;
            }

            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var words = new List<Domain.Entities.Entities.Word>();

                foreach (var item in dictionary)
                {
                    words.Add(new Domain.Entities.Entities.Word()
                    {
                        Data = item.Key,
                        Topic = item.Value
                    });
                }

                await context.Words.AddRangeAsync(words);

                await context.SaveChangesAsync();

                await ClearCache();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        public async Task<bool> SetWordLearnedAsync(Guid id, DateTime date)
        {
            if (id == Guid.Empty)
            {
                return false;
            }

            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var word = await context.Words
                    .Where(e => e.Id == id)
                    .SingleOrDefaultAsync();

                if (word == null)
                {
                    return false;
                }

                word.LearnedDate = date;

                await context.SaveChangesAsync();

                await ClearCache();

                return true;
            }
            catch (Exception ex)
            {
                OnException(ex);
                return false;
            }
        }

        public async Task<bool> SetWordsLearnedAsync(List<Guid> ids, DateTime date)
        {
            if (ids == null || ids.Count == 0)
            {
                return false;
            }

            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                var words = await context.Words
                    .Where(e => ids.Contains(e.Id))
                    .ToListAsync();

                foreach (var word in words)
                {
                    word.LearnedDate = date;
                }

                await context.SaveChangesAsync();

                await ClearCache();

                return true;
            }
            catch (Exception ex)
            {
                OnException(ex);
                return false;
            }
        }

        public async Task<int> CountWordsLearnedByTimeSpanAsync(TimeSpan time)
        {
            try
            {
                await using var context = await m_contextFactory.CreateDbContextAsync();

                DateTime lowerBound = DateTime.UtcNow - time;

                return await context.Words
                    .Where(e => e.LearnedDate != null && e.LearnedDate >= lowerBound)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                OnException(ex);
                throw;
            }
        }

        private void CacheSet<T>(string key, T value) where T : class
        {
            var weakReference = new WeakReference<T>(value);

            m_cache.Set(key, weakReference, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = m_absoluteExpiration
            });
        }

        private bool CacheGet<T>(string key, out T? result) where T : class
        {
            if (m_cache.TryGetValue(key, out WeakReference<T>? weakReference))
            {
                if (weakReference != null && weakReference.TryGetTarget(out var target))
                {
                    result = target;
                    return true;
                }

                m_cache.Remove(key);
            }

            result = default;
            return false;
        }

        private async Task ClearCache()
        {
            foreach (var key in m_cacheKeys)
            {
                m_cache.Remove(key);
            }

            int wordsLearnedCount = await CountWordsLearnedAsync();
            int page = (wordsLearnedCount / m_pageSize) + 1;

            for (int i = page; i > 0; i--)
            {
                m_cache.Remove(string.Concat(Constants.WORDS_LEARNED_KEY, i.ToString()));
            }
        }

        private static ImmutableList<string> CacheKeys()
        {
            var builder = ImmutableList.CreateBuilder<string>();

            builder.Add(Constants.ANY_WORDS_KEY);
            builder.Add(Constants.WORDS_NOT_LEARNED_KEY);
            builder.Add(Constants.COUNT_WORDS_NOT_LEARNED_KEY);
            builder.Add(Constants.WORDS_LEARNED_KEY);
            builder.Add(Constants.WORDS_LEARNED_FOR_REPETITION_KEY);
            builder.Add(Constants.COUNT_WORDS_LEARNED_KEY);
            builder.Add(Constants.COUNT_WORDS_NOT_LEARNED_BY_TOPIC_KEY);
            builder.Add(Constants.WORDS_LEARNED_BY_TOPIC_KEY);
            builder.Add(Constants.COUNT_WORDS_LEARNED_BY_TOPIC_KEY);
            builder.Add(Constants.COUNT_WORDS_LEARNED_BY_TIME_SPAN_KEY);

            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Common.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Education.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Finance.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Health.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Law.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Medicine.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Programming.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Technology.ToString()));
            builder.Add(string.Concat(Constants.WORDS_NOT_LEARNED_BY_TOPIC_KEY, Topic.Travel.ToString()));

            return builder.ToImmutableList();
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