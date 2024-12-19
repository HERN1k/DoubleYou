using System.Collections.Generic;
using System.Threading.Tasks;

using DoubleYou.Domain.Enums;

using static DoubleYou.Domain.Entities.Entities;

namespace DoubleYou.Domain.Interfaces
{
    public interface ITranslator
    {
        Task<Dictionary<Word, string>> Translate(Language language, IEnumerable<Word> words);
    }
}