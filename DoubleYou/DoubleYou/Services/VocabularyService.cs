using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DoubleYou.Domain.Enums;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using Microsoft.Extensions.Logging;

using Windows.ApplicationModel;

namespace DoubleYou.Services
{
    public partial class VocabularyService : IVocabularyService
    {
        public IReadOnlyList<Domain.Entities.Entities.Word> Words { get => m_words; }
        public bool IsWordsInitializing
        {
            get => m_isWordsInitializing;
            private set
            {
                if (m_isWordsInitializing != value)
                {
                    m_isWordsInitializing = value;
                    OnWordsInitializing(value);
                }
            }
        }
        public event EventHandler<WordsInitializingEventArgs>? WordsInitializing;

        private readonly IWordsRepository m_wordsRepository;
        private readonly IUsersRepository m_usersRepository;
        private readonly ILogger<VocabularyService> m_logger;
        private readonly string m_wordsPath = Path.Combine(Package.Current.InstalledLocation.Path, "Words");
        private readonly ThreadLocal<Random> m_random = new(() => new Random());
        private readonly List<string> m_topics = new()
        {
            "common",
            "education",
            "finance",
            "health",
            "law",
            "medicine",
            "programming",
            "technology",
            "travel"
        };
        private readonly object m_lockObject = new();
        private readonly List<Domain.Entities.Entities.Word> m_words = new();
        private readonly TimeSpan m_lastDays = TimeSpan.FromDays(3);
        private readonly int m_wordsCount = 10;
        private bool m_isWordsInitializing;
        private bool m_disposedValue;

#pragma warning disable IDE0290
        public VocabularyService(IWordsRepository wordsRepository, IUsersRepository usersRepository, ILogger<VocabularyService> logger)
#pragma warning restore
        {
            m_wordsRepository = wordsRepository ?? throw new ArgumentNullException(nameof(wordsRepository));
            m_usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> InitializeWordsAsync()
        {
            if (IsWordsInitializing)
            {
                return false;
            }

            if (Words.Count > 0)
            {
                return true;
            }

            if (m_wordsRepository.AnyWords())
            {
                try
                {
                    IsWordsInitializing = true;

                    await AddRundomWordsNotLearnedByTopicAsync();

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    IsWordsInitializing = false;
                }
            }
            else
            {
                try
                {
                    IsWordsInitializing = true;

                    await SetWordsIntoDatabaseAsync();

                    await AddRundomWordsNotLearnedByTopicAsync();

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    IsWordsInitializing = false;
                }
            }
        }

        public async Task<bool> AddRundomWordsAsync()
        {
            if (IsWordsInitializing)
            {
                return false;
            }

            if (m_wordsRepository.AnyWords())
            {
                try
                {
                    IsWordsInitializing = true;

                    await AddRundomWordsNotLearnedByTopicAsync();

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    IsWordsInitializing = false;
                }
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UpdateWordsAsync()
        {
            if (IsWordsInitializing)
            {
                return false;
            }

            if (m_wordsRepository.AnyWords())
            {
                try
                {
                    IsWordsInitializing = true;

                    m_words.Clear();

                    await AddRundomWordsNotLearnedByTopicAsync();
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    IsWordsInitializing = false;
                }
            }

            return true;
        }

        public async Task<bool> SetWordLearnedAsync(Guid id)
        {
            if (!m_words.Any(w => w.Id == id))
            {
                return false;
            }

            bool isSuccess = await m_wordsRepository.SetWordLearnedAsync(id, DateTime.UtcNow);

            if (isSuccess)
            {
                var word = m_words.FirstOrDefault(w => w.Id == id);

                if (word != null)
                {
                    m_words.Remove(word);
                }

                return true;
            }

            return isSuccess;
        }

        public async Task<bool> SetWordsLearnedAsync(List<Guid> wordsIds)
        {
            var words = m_words
                .Where(w => wordsIds.Contains(w.Id))
                .ToList();

            if (words.Count != wordsIds.Count)
            {
                return false;
            }

            bool isSuccess = await m_wordsRepository.SetWordsLearnedAsync(wordsIds, DateTime.UtcNow);

            if (isSuccess)
            {
                foreach (var word in words)
                {
                    m_words.Remove(word);
                }

                return true;
            }

            return isSuccess;
        }

        public async Task<bool> SetWordRefuseLearnAsync(Guid id)
        {
            if (!m_words.Any(w => w.Id == id))
            {
                return false;
            }

            bool isSuccess = await m_wordsRepository.SetWordLearnedAsync(id, DateTime.MinValue);

            if (isSuccess)
            {
                var word = m_words.FirstOrDefault(w => w.Id == id);

                if (word != null)
                {
                    m_words.Remove(word);
                }

                return true;
            }

            return isSuccess;
        }

        public async Task<List<Domain.Entities.Entities.Word>> WordsLearnedAsync(int page)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(page);

            return await m_wordsRepository.WordsLearnedAsync(page);
        }

        public async Task<List<Domain.Entities.Entities.Word>> WordsLearnedForRepetitionAsync() => await m_wordsRepository.WordsLearnedForRepetitionAsync();

        public async Task<int> CountWordsLearnedAsync() => await m_wordsRepository.CountWordsLearnedAsync();

        public async Task<int> CountWordsLearnedForLastTimeAsync() => await m_wordsRepository.CountWordsLearnedByTimeSpanAsync(m_lastDays);

        private void AddNewWordImpl(List<Domain.Entities.Entities.Word> words, List<Domain.Entities.Entities.Word> sourceWords, Random random)
        {
            if (words == null || sourceWords == null || random == null || sourceWords.Count == 0)
            {
                return;
            }

            int attempts = 0;
            bool isSuccess = false;

            do
            {
                attempts++;

                var word = sourceWords.ElementAtOrDefault(random.Next(sourceWords.Count));

                if (word != null)
                {
                    if (!words.Contains(word) && !m_words.Contains(word))
                    {
                        words.Add(word);

                        if (sourceWords.Remove(word))
                        {
                            isSuccess = true;
                        }
                        else
                        {
                            words.Remove(word);
                        }
                    }
                }

                if (attempts > 5)
                {
                    isSuccess = true;
                }
            } while (!isSuccess);
        }

        private async Task AddRundomWordsNotLearnedByTopicAsync()
        {
            var random = m_random.Value;

            if (random == null || m_words == null)
            {
                return;
            }

            var topic = await m_usersRepository.GetUserFavoriteTopicAsync();
            var wordsCommon = (await m_wordsRepository.WordsNotLearnedByTopicAsync(Topic.Common)).ToList();
            var wordsByTopic = (await m_wordsRepository.WordsNotLearnedByTopicAsync(topic)).ToList();

            if (wordsCommon.Count == 0)
            {
                return;
            }

            var newWords = new List<Domain.Entities.Entities.Word>();

            if (wordsByTopic.Count > 0)
            {
                for (var i = 0; i < m_wordsCount / 2; i++)
                {
                    AddNewWordImpl(newWords, wordsCommon, random);
                }

                for (var i = 0; i < m_wordsCount / 2; i++)
                {
                    AddNewWordImpl(newWords, wordsByTopic, random);
                }

                if (newWords.Count < m_wordsCount)
                {
                    int needWords = m_wordsCount - m_words.Count;

                    for (int i = 0; i < needWords; i++)
                    {
                        AddNewWordImpl(newWords, wordsCommon, random);
                    }
                }
            }
            else
            {
                for (var i = 0; i < m_wordsCount; i++)
                {
                    AddNewWordImpl(newWords, wordsCommon, random);
                }
            }

            m_words.AddRange(newWords);
        }

        private async Task SetWordsIntoDatabaseAsync()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, Topic>();

            foreach (var topicName in m_topics)
            {
                if (Enum.TryParse(topicName, true, out Topic topic))
                {
                    var words = GetWordsFromFile(topicName);

                    foreach (var word in words)
                    {
                        if (!builder.ContainsKey(word))
                        {
                            builder.Add(word, topic);
                        }
                    }
                }
            }

            await m_wordsRepository.SetWordsIntoDatabaseAsync(builder.ToImmutable());
        }

        private ImmutableList<string> GetWordsFromFile(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName, nameof(fileName));

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException($"{Constants.INVALID_FILE_NAME} '{fileName}'", nameof(fileName));
            }

            string file = string.Concat(fileName, ".txt");
            string path = Path.Combine(m_wordsPath, file);

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"{Constants.FILE_NOT_EXISTS} '{file}'");
            }

            var builder = ImmutableList.CreateBuilder<string>();

            try
            {
                lock (m_lockObject)
                {
                    using var stream = File.OpenRead(path);
                    using var reader = new StreamReader(stream);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        builder.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                m_logger.LogError(ex, "{Message}", ex.Message);
                throw new InvalidOperationException($"{Constants.ERROR_READING_FILE} '{fileName}': {ex.Message}", ex);
            }

            return builder.ToImmutableList();
        }

        protected virtual void OnWordsInitializing(bool isWordsInitializing)
        {
            var temp = Volatile.Read(ref WordsInitializing);

            temp?.Invoke(this, new WordsInitializingEventArgs(isWordsInitializing));
        }

        public sealed class WordsInitializingEventArgs(bool isInitializing)
        {
            public readonly bool IsInitializing = isInitializing;
        }

        #region Disposing
        private void Dispose(bool disposing)
        {
            if (!m_disposedValue)
            {
                if (disposing)
                {
                    WordsInitializing = null;
                }

                m_disposedValue = true;
            }
        }

        ~VocabularyService()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
        #endregion
    }
}