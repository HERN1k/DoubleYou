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

using DoubleYou.Domain.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Windows.Graphics;
using Windows.UI;

namespace DoubleYou.AppWindows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ErrorWindow : Window, IDisposable
    {
        private readonly Color m_buttonForegroundColor = Color.FromArgb(255, 255, 255, 255);
        private readonly Color m_buttonBackgroundColor = Color.FromArgb(0, 255, 255, 255);
        private bool m_disposedValue;

        public ErrorWindow(string message)
        {
            this.InitializeComponent();

            var windowsHelper = (Application.Current as App)?.ServiceProvider.GetService<IWindowsHelper>()
                ?? throw new ArgumentNullException(nameof(IWindowsHelper));

            CloseButton.Click += OnCloseButtonClick;
            MessageBox.Text = message;

            if (windowsHelper.IsWindowsVersionAtLeast(10, 0, 19041))
            {
#pragma warning disable CA1416
                this.Title = "Critical Error";
                TitleBarTextBlock.Text = "Critical Error";
#pragma warning restore
            }
            else
            {
                this.Title = "Critical Error";
                TitleBarTextBlock.Text = "Critical Error";
            }

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                if (ExtendsContentIntoTitleBar == true)
                {
                    this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
                }

                this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                this.AppWindow.TitleBar.ButtonForegroundColor = m_buttonForegroundColor;
                this.AppWindow.TitleBar.ButtonBackgroundColor = m_buttonBackgroundColor;
                this.AppWindow.TitleBar.ButtonHoverForegroundColor = m_buttonForegroundColor;
                this.AppWindow.TitleBar.ButtonHoverBackgroundColor = m_buttonBackgroundColor;

                this.AppWindow.TitleBar.InactiveForegroundColor = m_buttonForegroundColor;
                this.AppWindow.TitleBar.InactiveBackgroundColor = m_buttonBackgroundColor;
                this.AppWindow.TitleBar.ButtonInactiveForegroundColor = m_buttonForegroundColor;
                this.AppWindow.TitleBar.ButtonInactiveBackgroundColor = m_buttonBackgroundColor;
            }

            this.AppWindow.Resize(new SizeInt32
            {
                Width = 600,
                Height = 720
            });
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs args) => this.Close();

        #region Disposing
        private void Dispose(bool disposing)
        {
            if (!m_disposedValue)
            {
                if (disposing)
                {
                    CloseButton.Click -= OnCloseButtonClick;
                }

                m_disposedValue = true;
            }
        }

        ~ErrorWindow()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
