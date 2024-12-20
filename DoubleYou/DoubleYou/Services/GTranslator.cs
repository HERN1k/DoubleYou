/*  
    MIT License

    Copyright (c) 2024 Vlad Hirnyk (HERN1k)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE. 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DoubleYou.Domain.Enums;
using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using GTranslatorAPI;

using Microsoft.Extensions.Caching.Memory;

using static DoubleYou.Domain.Entities.Entities;

namespace DoubleYou.Services
{
    public sealed partial class GTranslator : ITranslator
    {
        private readonly IMemoryCache m_cache;

        public GTranslator(IMemoryCache cache)
        {
            m_cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<Dictionary<Word, string>> Translate(Language language, IEnumerable<Word> words)
        {
            if (!words.Any())
            {
                return new Dictionary<Word, string>();
            }

            if (language == Language.English)
            {
                return words.ToDictionary(word => word, word => word.Data);
            }

            var wordsDto = new TranslateWordsDto(language);

            SortWords(wordsDto, words);

            if (wordsDto.NotTranslatedWords.Count == 0)
            {
                return wordsDto.TranslatedWords.ToDictionary();
            }

            var translatedWords = await TranslateAsync(language, wordsDto.NotTranslatedWords);

            SetTranslatedWordsInCache(wordsDto, translatedWords);

            if (wordsDto.NotTranslatedWordsEntities.Count != translatedWords.Count)
            {
                return wordsDto.TranslatedWords.ToDictionary();
            }

            ConcatWords(wordsDto, translatedWords);

            return wordsDto.TranslatedWords.ToDictionary();
        }

        private void SortWords(TranslateWordsDto data, IEnumerable<Word> words)
        {
            foreach (var word in words)
            {
                var key = CreateCacheKey(data.Language, word.Data);

                if (m_cache.TryGetValue<string>(key, out var result))
                {
                    data.TranslatedWords.Add(word, result ?? "Null");
                }
                else
                {
                    data.NotTranslatedWordsEntities.Add(word);
                    data.NotTranslatedWords.Add(word.Data);
                }
            }
        }

        private void SetTranslatedWordsInCache(TranslateWordsDto data, List<string> translatedWords)
        {
            for (var i = 0; i < data.NotTranslatedWordsEntities.Count; i++)
            {
                var key = CreateCacheKey(data.Language, data.NotTranslatedWordsEntities[i].Data);

                m_cache.Set(key, translatedWords[i], TimeSpan.FromHours(2));
            }
        }

        private static void ConcatWords(TranslateWordsDto data, List<string> translatedWords)
        {
            for (var i = 0; i < data.NotTranslatedWordsEntities.Count; i++)
            {
                data.TranslatedWords.Add(data.NotTranslatedWordsEntities[i], translatedWords[i]);
            }
        }

        private static async Task<List<string>> TranslateAsync(Language language, List<string> words)
        {
            if (words == null || words.Count == 0)
            {
                throw new ArgumentNullException(nameof(words));
            }

            var translator = new GTranslatorAPIClient();

            string queryText = string.Join(
                separator: Constants.API_WORD_SEPARATOR_REQUEST,
                values: words.Select(word => word.Trim()));

            var targetLanguage = language switch
            {
                Language.Ukrainian => Languages.uk,
                Language.Hebrew => Languages.iw,
                Language.Polish => Languages.pl,
                Language.German => Languages.de,
                Language.Russian => Languages.ru,
                _ => Languages.uk
            };

            var response = await translator.TranslateAsync(Languages.en, targetLanguage, queryText);

            return ParseResponse(response);
        }

        private static string CreateCacheKey(Language language, string word) =>
            string.Concat(language.ToString(), Constants.PART_OF_TRANSLATE_KEY, word).ToLower();

        private static List<string> ParseResponse(Translation content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));

            var result = new List<string>();

            var words = string.IsNullOrWhiteSpace(content.TranslatedText)
                ? Array.Empty<string>()
                : content.TranslatedText.Split(Constants.API_WORD_SEPARATOR_RESPONSE, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                result.Add(word);
            }

            return result;
        }

        public class TranslateWordsDto(Language language)
        {
            public Language Language { get; init; } = language;

            public Dictionary<Word, string> TranslatedWords { get; init; } = new();

            public List<Word> NotTranslatedWordsEntities { get; init; } = new();

            public List<string> NotTranslatedWords { get; init; } = new();
        }
    }
}