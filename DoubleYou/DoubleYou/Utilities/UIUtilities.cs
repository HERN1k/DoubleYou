﻿/*  
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

using DoubleYou.Domain.Enums;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace DoubleYou.Utilities
{
    public static class UIUtilities
    {
        public static void ChangeThemeTitleBar(Grid customTitleBar, AppWindowTitleBar titleBar)
        {
            ArgumentNullException.ThrowIfNull(customTitleBar, nameof(customTitleBar));
            ArgumentNullException.ThrowIfNull(titleBar, nameof(titleBar));

            SolidColorBrush transparentBrush = new(Color.FromArgb(0, 255, 255, 255));

            if (customTitleBar != null)
            {
                customTitleBar.Background = transparentBrush;
            }

            if (titleBar != null)
            {
                titleBar.ButtonBackgroundColor = transparentBrush.Color;

                titleBar.InactiveBackgroundColor = transparentBrush.Color;
                titleBar.ButtonInactiveBackgroundColor = transparentBrush.Color;
            }
        }

        public static T GetValueFormThemeDictionary<T>(ElementTheme theme, string key, T defaultValue) where T : class
        {
            var themeString = theme == ElementTheme.Light
                ? "Light"
                : "Default";

            var themeDictionary = Application.Current.Resources.ThemeDictionaries
                .Where(r => (string)r.Key == themeString)
                .FirstOrDefault()
                .Value as IDictionary<object, object>;

            return themeDictionary?
                .Where(r => (string)r.Key == key)
                .FirstOrDefault()
                .Value as T ?? defaultValue;
        }

        public static T? GetValueFormThemeDictionary<T>(ElementTheme theme, string key) where T : class
        {
            var themeString = theme == ElementTheme.Light
                ? "Light"
                : "Default";

            var themeDictionary = Application.Current.Resources.ThemeDictionaries
                .Where(r => (string)r.Key == themeString)
                .FirstOrDefault()
                .Value as IDictionary<object, object>;

            return themeDictionary?
                .Where(r => (string)r.Key == key)
                .FirstOrDefault()
                .Value as T;
        }

        public static T? FindElementByTag<T>(this DependencyObject parent, object tag) where T : FrameworkElement
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Tag?.Equals(tag) == true)
                {
                    return element;
                }

                var result = FindElementByTag<T>(child, tag);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T target)
                {
                    return target;
                }
                else
                {
                    var result = FindChild<T>(child);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        public static string CreateElementTag(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return string.Concat(Constants.PAGE_ELEMENT_TAG, index.ToString());
        }

        public static HorizontalAlignment ResolveAlignmentByLanguage(this string languageCode)
        {
            Language language = languageCode.ParseLanguageCode();

            return language switch
            {
                Language.Hebrew => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left,
            };
        }
    }
}