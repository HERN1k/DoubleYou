using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

using DoubleYou.Domain.Enums;

namespace DoubleYou.Domain.Interfaces
{
    public interface IWordsRepository
    {
        bool AnyWords();

        Task<List<Entities.Entities.Word>> WordsLearnedAsync(int page);

        Task<List<Entities.Entities.Word>> WordsLearnedForRepetitionAsync();

        Task<int> CountWordsLearnedAsync();

        Task<List<Entities.Entities.Word>> WordsNotLearnedByTopicAsync(Topic topic);

        Task<bool> SetWordLearnedAsync(Guid id, DateTime date);

        Task<bool> SetWordsLearnedAsync(List<Guid> ids, DateTime date);

        Task<int> CountWordsLearnedByTimeSpanAsync(TimeSpan time);

        Task SetWordsIntoDatabaseAsync(ImmutableDictionary<string, Topic> data);
    }
}