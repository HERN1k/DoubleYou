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
using System.Text;

using DoubleYou.AppWindows;
using DoubleYou.Domain.Interfaces;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace DoubleYou.Utilities
{
    public static class Alerts
    {
        public static void ShowAlert(this Page page, string title, string message, InfoBarSeverity severity)
        {
            var infoBar = new InfoBar()
            {
                Title = title ?? string.Empty,
                Message = message ?? string.Empty,
                Severity = severity,
                IsOpen = true
            };

            bool isEnqueued = page.DispatcherQueue.TryEnqueue(() =>
            {
                if (page.Content is Grid grid)
                {
                    grid.Children.Add(infoBar);
                }
                else if (page.Content is StackPanel stack)
                {
                    stack.Children.Add(infoBar);
                }
            });
#if DEBUG
            if (!isEnqueued)
            {
                Debug.WriteLine(Constants.FAILED_TO_ADD_TASK_TO_UI_THREAD);
            }
#endif
        }

        public static void ShowAlertWithButton(this Page page, string title, string message, Button button, InfoBarSeverity severity)
        {
            var infoBar = new InfoBar()
            {
                Title = title ?? string.Empty,
                Message = message ?? string.Empty,
                Severity = severity,
                IsOpen = true,
                ActionButton = button
            };

            bool isEnqueued = page.DispatcherQueue.TryEnqueue(() =>
            {
                if (page.Content is Grid grid)
                {
                    grid.Children.Add(infoBar);
                }
                else if (page.Content is StackPanel stack)
                {
                    stack.Children.Add(infoBar);
                }
            });
#if DEBUG
            if (!isEnqueued)
            {
                Debug.WriteLine(Constants.FAILED_TO_ADD_TASK_TO_UI_THREAD);
            }
#endif
        }

        public static void ShowAlertException(this Page page, Exception ex, ILocalizationService? localization = null)
        {
            string title;
            if (localization == null)
            {
                title = "Error";
            }
            else
            {
                title = localization.GetString("Error");
            }

            var infoBar = new InfoBar()
            {
                Title = title,
                Message = ex.Message ?? string.Empty,
                Severity = InfoBarSeverity.Error,
                IsOpen = true
            };

            bool isEnqueued = page.DispatcherQueue.TryEnqueue(() =>
            {
                if (page.Content is Grid grid)
                {
                    grid.Children.Add(infoBar);
                }
                else if (page.Content is StackPanel stack)
                {
                    stack.Children.Add(infoBar);
                }
            });
#if DEBUG
            if (!isEnqueued)
            {
                Debug.WriteLine(Constants.FAILED_TO_ADD_TASK_TO_UI_THREAD);
            }
#endif
        }

        public static void ShowAlertExceptionWithTrace(this Page page, Exception ex, ILocalizationService? localization = null)
        {
            bool isEnqueued = page.DispatcherQueue.TryEnqueue(async () =>
            {
                StringBuilder sb = new();
                string title;
                string cancel;
                if (localization == null)
                {
                    title = "Error";
                    cancel = "OK";
                    sb.Append("Message:    ");
                    sb.Append(ex.Message);
                    sb.Append("\n\nStackTrace:\n");
                    sb.Append(ex.StackTrace);
                }
                else
                {
                    title = localization.GetString("Error") ?? string.Empty;
                    cancel = localization.GetString("OK") ?? string.Empty;
                    sb.Append(localization.GetString("Message") ?? "Message");
                    sb.Append(":    ");
                    sb.Append(ex.Message);
                    sb.Append("\n\nStackTrace:\n");
                    sb.Append(ex.StackTrace);
                }

                var dialog = new ContentDialog
                {
                    Title = title ?? string.Empty,
                    Content = sb.ToString() ?? string.Empty,
                    CloseButtonText = cancel ?? "OK",
                    XamlRoot = page.Content.XamlRoot
                };

                await dialog.ShowAsync();
            });
#if DEBUG
            if (!isEnqueued)
            {
                Debug.WriteLine(Constants.FAILED_TO_ADD_TASK_TO_UI_THREAD);
            }
#endif
        }

        public static void ShowCriticalErrorWindow(string message)
        {
            var appWindowDispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException(Constants.UNABLE_TO_ACCESS_DISPATCHERQUEUE);

            appWindowDispatcherQueue.TryEnqueue(() =>
            {
                StringBuilder sb = new();
                string messageText = string.Concat("Message:\t", message);
                sb.AppendLine(messageText);

                var errorWindow = new ErrorWindow(sb.ToString());

                errorWindow.Activate();
            });
        }

        public static void ShowCriticalErrorWindow(Exception ex)
        {
            var appWindowDispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException(Constants.UNABLE_TO_ACCESS_DISPATCHERQUEUE);

            appWindowDispatcherQueue.TryEnqueue(() =>
            {
                StringBuilder sb = new();
                string messageText = string.Concat("Message:\t", ex.Message);
                sb.AppendLine(messageText);
                sb.AppendLine();
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);

                var errorWindow = new ErrorWindow(sb.ToString());

                errorWindow.Activate();
            });
        }
    }
}