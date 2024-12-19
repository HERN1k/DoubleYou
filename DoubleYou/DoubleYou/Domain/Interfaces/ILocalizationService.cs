using System;
using System.Globalization;
using System.Threading.Tasks;

using static DoubleYou.Services.Localization;

namespace DoubleYou.Domain.Interfaces
{
    public interface ILocalizationService : IDisposable
    {
        CultureInfo CurrentCulture { get; }

        event EventHandler<CultureChangedEventArgs>? CultureChanged;

        string GetString(string key, CultureInfo? culture = null);

        Task<bool> SetCultureOnStartup();

        Task<bool> ChangeCulture(string cultureCode);
    }
}