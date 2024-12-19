using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using static DoubleYou.Services.VocabularyService;

namespace DoubleYou.Domain.Interfaces
{
    public interface IVocabularyService : IDisposable
    {
        IReadOnlyList<Entities.Entities.Word> Words { get; }

        event EventHandler<WordsInitializingEventArgs>? WordsInitializing;

        bool IsWordsInitializing { get; }

        Task<bool> InitializeWordsAsync();

        Task<bool> AddRundomWordsAsync();

        Task<bool> UpdateWordsAsync();

        Task<bool> SetWordLearnedAsync(Guid id);

        Task<bool> SetWordsLearnedAsync(List<Guid> wordsIds);

        Task<bool> SetWordRefuseLearnAsync(Guid id);

        Task<List<Entities.Entities.Word>> WordsLearnedAsync(int page);

        Task<List<Entities.Entities.Word>> WordsLearnedForRepetitionAsync();

        Task<int> CountWordsLearnedAsync();

        Task<int> CountWordsLearnedForLastTimeAsync();
    }
}