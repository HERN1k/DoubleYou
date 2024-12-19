using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using Microsoft.UI.Dispatching;

using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;

using Windows.Networking.Connectivity;

using Windows.System.Profile;

namespace DoubleYou.Services
{
    public sealed class WindowsHelper : IWindowsHelper
    {
        private readonly ulong s_versionNumber;
        private readonly ulong s_major;
        private readonly ulong s_minor;
        private readonly ulong s_build;
        private readonly ulong s_revision;
        private readonly double s_audioVolume;
        private readonly double s_speakingRate;
        private readonly VoiceInformation s_voiceMale;
        private readonly VoiceInformation s_voiceFemale;
        private readonly ThreadLocal<Random> s_random;

        public WindowsHelper()
        {
            string version = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;

            if (ulong.TryParse(version, out ulong versionNumber))
            {
                s_versionNumber = versionNumber;
                s_major = (versionNumber & 0xFFFF000000000000L) >> 48;
                s_minor = (versionNumber & 0x0000FFFF00000000L) >> 32;
                s_build = (versionNumber & 0x00000000FFFF0000L) >> 16;
                s_revision = (versionNumber & 0x000000000000FFFFL);
            }
            else
            {
                throw new InvalidOperationException(Constants.FAILED_TO_RETRIEVE_THE_WINDOWS_VERSION);
            }

            s_audioVolume = 0.75D;
            s_speakingRate = 1D;
            s_voiceMale = GetVoice(VoiceGender.Male);
            s_voiceFemale = GetVoice(VoiceGender.Female);

            s_random = new ThreadLocal<Random>(() => new Random());
        }

        public bool IsWindows11OrHigher() => s_major >= 10 && s_build >= 22000;

        public bool IsWindowsVersionAtLeast(int major, int minor, int build)
        {
            if (s_major > (ulong)major)
            {
                return true;
            }
            if (s_major == (ulong)major && s_minor > (ulong)minor)
            {
                return true;
            }
            if (s_major == (ulong)major && s_minor == (ulong)minor && s_build >= (ulong)build)
            {
                return true;
            }

            return false;
        }

        public bool IsInternetAvailable()
        {
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();

            return connectionProfile != null &&
                   connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }

        public bool IsCorrectVoiceInstalled()
        {
            return SpeechSynthesizer.AllVoices
                .Any(v =>
                    v.Language.StartsWith("en") &&
                    v.DisplayName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));
        }

        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            if (dispatcherQueue != null)
            {
                if (dispatcherQueue.HasThreadAccess)
                {
                    await SpeakAsyncImpl(text);
                }
                else
                {
                    dispatcherQueue.EnsureSystemDispatcherQueue();

                    await EnqueueAsync(dispatcherQueue, async () =>
                    {
                        await SpeakAsyncImpl(text);
                    });
                }

            }
        }

        private Task EnqueueAsync(DispatcherQueue dispatcherQueue, Func<Task> func)
        {
            var tcs = new TaskCompletionSource();

            dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        private async Task SpeakAsyncImpl(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                var synthesizer = new SpeechSynthesizer();
                var player = new MediaPlayer();

                synthesizer.Voice = GetRandomVoice();
                synthesizer.Options.AudioVolume = s_audioVolume;
                synthesizer.Options.SpeakingRate = s_speakingRate;

                SpeechSynthesisStream? source = null;
                try
                {
                    source = await synthesizer.SynthesizeTextToStreamAsync(text);
                }
                catch (FileNotFoundException)
                {
                    synthesizer.Voice = SpeechSynthesizer.DefaultVoice;

                    source = await synthesizer.SynthesizeTextToStreamAsync(text);
                }
                catch (Exception)
                {
                    throw;
                }

                if (source == null)
                {
                    return;
                }

                player.SetStreamSource(source);

                void mediaEndedHandler(MediaPlayer sender, object args)
                {
                    sender.MediaEnded -= mediaEndedHandler;

                    source?.Dispose();
                    player?.Dispose();

                    synthesizer = null;
                    source = null;
                    player = null;
                };

                synthesizer?.Dispose();
                player.MediaEnded += mediaEndedHandler;

                player.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private VoiceInformation GetVoice(VoiceGender gender)
        {
            VoiceInformation? voice = SpeechSynthesizer.AllVoices
                .FirstOrDefault(v =>
                    v.Language.StartsWith("en") &&
                    v.Gender == gender &&
                    v.DisplayName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));

            if (voice != null)
            {
                return voice;
            }

            voice = SpeechSynthesizer.AllVoices
                .FirstOrDefault(v => v.Gender == gender);

            if (voice != null)
            {
                return voice;
            }

            return SpeechSynthesizer.DefaultVoice;
        }

        private VoiceInformation GetRandomVoice()
        {
            if (s_voiceMale == null && s_voiceFemale == null)
            {
                return SpeechSynthesizer.DefaultVoice;
            }

            if (s_voiceMale == null)
            {
                return s_voiceFemale!;
            }

            if (s_voiceFemale == null)
            {
                return s_voiceMale!;
            }

            bool useMaleVoice = s_random.Value?.Next(0, 2) == 0;

            return useMaleVoice ? s_voiceMale : s_voiceFemale;
        }
    }
}