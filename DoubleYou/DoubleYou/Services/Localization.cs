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
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using Microsoft.Extensions.Logging;
using Microsoft.Windows.Globalization;

namespace DoubleYou.Services
{
    public partial class Localization : ILocalizationService
    {
        private CultureInfo m_currentCulture;
        public CultureInfo CurrentCulture
        {
            get => m_currentCulture;
            private set
            {
                if (m_currentCulture != value)
                {
                    m_currentCulture = value;
                    OnCultureChanged(new CultureChangedEventArgs(value));
                }
            }
        }
        public event EventHandler<CultureChangedEventArgs>? CultureChanged;

        private readonly IUsersRepository m_usersRepository;
        private readonly ResourceManager m_resourceManager;
        private readonly ILogger<Localization> m_logger;
        private readonly object m_lockObj = new();
        private bool m_disposedValue;

        public Localization(IUsersRepository usersRepository, ILogger<Localization> logger)
        {
            m_usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_currentCulture = new CultureInfo("en-US");
            m_resourceManager = new ResourceManager("DoubleYou.Resources.Languages.Language", typeof(Localization).Assembly);
        }

        public string GetString(string key, CultureInfo? culture = null)
        {
            return m_resourceManager.GetString(key, culture ?? CurrentCulture) ?? string.Empty;
        }

        public async Task<bool> SetCultureOnStartup() => await ChangeCulture(await m_usersRepository.GetUserCultureAsync());

        public async Task<bool> ChangeCulture(string cultureCode)
        {
            if (!string.IsNullOrEmpty(cultureCode))
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureCode);
                }
                catch (CultureNotFoundException)
                {
#if DEBUG
                    Debug.WriteLine(Constants.CULTURE_CODE_NOT_FOUND_OR_INVALID);
#endif
                    m_logger.LogWarning(Constants.CULTURE_CODE_NOT_FOUND_OR_INVALID);
                    return false;
                }
                catch (Exception)
                {
                    throw;
                }

                await m_usersRepository.SaveUserCultureAsync(culture.Name);

                lock (m_lockObj)
                {
                    ApplicationLanguages.PrimaryLanguageOverride = culture.Name;

                    Windows.ApplicationModel.Resources.Core.ResourceContext.GetForViewIndependentUse().Reset();

                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                }

                var retries = 0;
                const int maxRetries = 10;
                do
                {
                    await Task.Delay(50);
                    retries++;
                    if (retries >= maxRetries)
                    {
                        throw new TimeoutException(Constants.FAILED_TO_LOAD_RESOURCES_FOR_NEW_CULTURE);
                    }
                }
                while (string.IsNullOrEmpty(GetString("ApplicationLanguage", culture)));

                CurrentCulture = culture;

                return true;
            }

            return false;
        }

        protected virtual void OnCultureChanged(CultureChangedEventArgs e)
        {
            var temp = Volatile.Read(ref CultureChanged);

            temp?.Invoke(this, e);
        }

        public sealed class CultureChangedEventArgs
        {
            public CultureInfo CurrentCulture { get; }

#pragma warning disable IDE0290
            public CultureChangedEventArgs(CultureInfo currentCulture)
#pragma warning restore
            {
                CurrentCulture = currentCulture ?? throw new ArgumentNullException(nameof(currentCulture));
            }
        }

        #region Disposing
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposedValue)
            {
                if (disposing)
                {
                    CultureChanged = null;
                }

                m_disposedValue = true;
            }
        }

        ~Localization()
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