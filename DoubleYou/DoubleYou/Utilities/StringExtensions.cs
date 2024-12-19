using System;

using DoubleYou.Domain.Enums;

namespace DoubleYou.Utilities
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim().ToLower();

            if (text.Length == 0)
            {
                return string.Empty;
            }

            char firstChar = char.ToUpperInvariant(text[0]);

            if (text.Length == 1)
            {
                return firstChar.ToString();
            }

            return string.Create(text.Length, (firstChar, text), (span, state) =>
            {
                span[0] = state.firstChar;
                state.text.AsSpan(1).CopyTo(span[1..]);
            });
        }

        public static string TrimToLength(this string? text, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim();

            if (text.Length <= length)
            {
                return text;
            }

            return string.Create(length + 3, (text), (span, state) =>
            {
                state.AsSpan(0, length).CopyTo(span);
                span[length] = '.';
                span[length + 1] = '.';
                span[length + 2] = '.';
            });
        }

        public static Language ParseLanguageCode(this string? code)
        {
            return code switch
            {
                Constants.ENGLISH_LANGUAGE_KEY => Language.English,
                Constants.UKRAINIAN_LANGUAGE_KEY => Language.Ukrainian,
                Constants.HEBREW_LANGUAGE_KEY => Language.Hebrew,
                Constants.POLISH_LANGUAGE_KEY => Language.Polish,
                Constants.GERMAN_LANGUAGE_KEY => Language.German,
                Constants.RUSSIAN_LANGUAGE_KEY => Language.Russian,
                _ => Language.English,
            };
        }

        public static string GetISOCode(this Language language)
        {
            return language switch
            {
                Language.English => Constants.ENGLISH_LANGUAGE_KEY.Split('-')[0],
                Language.Ukrainian => Constants.UKRAINIAN_LANGUAGE_KEY.Split('-')[0],
                Language.Hebrew => Constants.HEBREW_LANGUAGE_KEY.Split('-')[0],
                Language.Polish => Constants.POLISH_LANGUAGE_KEY.Split('-')[0],
                Language.German => Constants.GERMAN_LANGUAGE_KEY.Split('-')[0],
                Language.Russian => Constants.RUSSIAN_LANGUAGE_KEY.Split('-')[0],
                _ => Constants.ENGLISH_LANGUAGE_KEY.Split('-')[0]
            };
        }
    }
}