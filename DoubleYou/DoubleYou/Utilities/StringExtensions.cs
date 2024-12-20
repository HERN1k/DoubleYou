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