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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;

using DoubleYou.Domain.Interfaces;
using DoubleYou.Utilities;

using Microsoft.UI.Dispatching;

using Windows.Media.SpeechSynthesis;

using Windows.Networking.Connectivity;
using Windows.System.Profile;

namespace DoubleYou.Services
{
    public sealed class WindowsHelper : IWindowsHelper
    {
        private readonly ulong m_versionNumber;
        private readonly ulong m_major;
        private readonly ulong m_minor;
        private readonly ulong m_build;
        private readonly ulong m_revision;
        private readonly double m_audioVolume;
        private readonly double m_speakingRate;
        private readonly VoiceInformation m_voiceMale;
        private readonly VoiceInformation m_voiceFemale;
        private readonly ThreadLocal<Random> m_random;
        private int m_playInParallel = 0;

        public WindowsHelper()
        {
            string version = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;

            if (ulong.TryParse(version, out ulong versionNumber))
            {
                m_versionNumber = versionNumber;
                m_major = (versionNumber & 0xFFFF000000000000L) >> 48;
                m_minor = (versionNumber & 0x0000FFFF00000000L) >> 32;
                m_build = (versionNumber & 0x00000000FFFF0000L) >> 16;
                m_revision = (versionNumber & 0x000000000000FFFFL);
            }
            else
            {
                throw new InvalidOperationException(Constants.FAILED_TO_RETRIEVE_THE_WINDOWS_VERSION);
            }

            m_audioVolume = 0.75D;
            m_speakingRate = 1D;
            m_voiceMale = GetVoice(VoiceGender.Male);
            m_voiceFemale = GetVoice(VoiceGender.Female);

            m_random = new ThreadLocal<Random>(() => new Random());
        }

        public bool IsWindows11OrHigher() => m_major >= 10 && m_build >= 22000;

        public bool IsWindowsVersionAtLeast(int major, int minor, int build)
        {
            if (m_major > (ulong)major)
            {
                return true;
            }
            if (m_major == (ulong)major && m_minor > (ulong)minor)
            {
                return true;
            }
            if (m_major == (ulong)major && m_minor == (ulong)minor && m_build >= (ulong)build)
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
            if (string.IsNullOrEmpty(text) || m_playInParallel >= 3)
            {
                return;
            }

            try
            {
                m_playInParallel++;

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

                        dispatcherQueue.TryEnqueue(async () => await SpeakAsyncImpl(text));
                    }
                }
            }
            catch (Exception) { } // We skip it because it is not a critical method
#if DEBUG
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
#endif
            finally
            {
                m_playInParallel--;
            }
        }

        private async Task<bool> SpeakAsyncImpl(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            try
            {
                using var synthesizer = new SpeechSynthesizer();
                synthesizer.Voice = GetRandomVoice();
                synthesizer.Options.AudioVolume = m_audioVolume;
                synthesizer.Options.SpeakingRate = m_speakingRate;

                using var source = await synthesizer.SynthesizeTextToStreamAsync(text);

                if (source == null || source.Size <= 0)
                {
                    return false;
                }

                string tempFilePath = Path.GetTempFileName();

                try
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await source
                            .AsStreamForRead()
                            .CopyToAsync(fileStream);
                    }

                    using (var soundOut = new WasapiOut())
                    using (var audioSource = CodecFactory.Instance
                        .GetCodec(tempFilePath)
                        .ToSampleSource()
                        .ToStereo()
                        .ToWaveSource(16))
                    {
                        soundOut.Initialize(audioSource);

                        var mediaEndedCompletionSource = new TaskCompletionSource<bool>();

                        void MediaEndedHandler(object? sender, PlaybackStoppedEventArgs args)
                        {
                            if (sender is WasapiOut device)
                            {
                                device.Stopped -= MediaEndedHandler;
                            }

                            mediaEndedCompletionSource.SetResult(true);
                        }

                        soundOut.Stopped += MediaEndedHandler;

                        soundOut.Play();

                        return await mediaEndedCompletionSource.Task;
                    }
                }
                catch (Exception) 
                { 
                    return false; 
                }
                finally 
                { 
                    if (File.Exists(tempFilePath)) 
                    { 
                        File.Delete(tempFilePath); 
                    } 
                }

                //using var player = new MediaPlayer();

                //player.SetStreamSource(source);

                //var mediaEndedCompletionSource = new TaskCompletionSource<bool>();

                //void mediaEndedHandler(MediaPlayer sender, object args)
                //{
                //    sender.MediaEnded -= mediaEndedHandler;
                //    mediaEndedCompletionSource.SetResult(true);
                //}

                //player.MediaEnded += mediaEndedHandler;

                //player.Play();

                //return await mediaEndedCompletionSource.Task;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static VoiceInformation GetVoice(VoiceGender gender)
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

            voice = SpeechSynthesizer.AllVoices.FirstOrDefault(v => v.Gender == gender);

            if (voice != null)
            {
                return voice;
            }

            return SpeechSynthesizer.DefaultVoice;
        }

        private VoiceInformation GetRandomVoice()
        {
            if (m_voiceMale == null && m_voiceFemale == null)
            {
                return SpeechSynthesizer.DefaultVoice;
            }

            if (m_voiceMale == null)
            {
                return m_voiceFemale!;
            }

            if (m_voiceFemale == null)
            {
                return m_voiceMale!;
            }

            bool useMaleVoice = m_random.Value?.Next(0, 2) == 0;

            return useMaleVoice ? m_voiceMale : m_voiceFemale;
        }
    }
}