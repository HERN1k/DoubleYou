using System.Threading.Tasks;

namespace DoubleYou.Domain.Interfaces
{
    public interface IWindowsHelper
    {
        bool IsWindows11OrHigher();

        bool IsWindowsVersionAtLeast(int major, int minor, int build);

        bool IsInternetAvailable();

        bool IsCorrectVoiceInstalled();

        Task SpeakAsync(string text);
    }
}