using System;
using NAudio.Wave;

namespace Harmony
{
    public sealed class ToneGenerator : IDisposable
    {
        private WaveOutEvent? _player;
        private ToneProvider? _provider;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public void Start(TonePreset preset, float volume, string? deviceName = null)
        {
            Stop();
            _provider = new ToneProvider(preset, volume);
            _player = new WaveOutEvent { DesiredLatency = 200, NumberOfBuffers = 3 };
            _player.Init(_provider);
            _player.Play();
            _isPlaying = true;
        }

        public void SetVolume(float volume) { if (_provider != null) _provider.Volume = Math.Clamp(volume, 0f, 1f); }
        public void SetPreset(TonePreset preset) { if (_provider != null) _provider.Preset = preset; }

        public void Stop()
        {
            if (!_isPlaying) return;
            _player?.Stop();
            _player?.Dispose();
            _player = null;
            _isPlaying = false;
        }

        public void Dispose() => Stop();
    }

    /// <summary>
    /// Generates continuous multi-layered sine wave tones.
    /// Supports binaural beats (different L/R) and unlimited frequency layers.
    /// </summary>
    public sealed class ToneProvider : ISampleProvider
    {
        private const int SampleRate = 44100;
        private double _phaseLeft, _phaseRight;
        private double[] _layerPhases = Array.Empty<double>();

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);
        public TonePreset Preset { get; set; }
        public float Volume { get; set; }

        public ToneProvider(TonePreset preset, float volume) { Preset = preset; Volume = volume; _layerPhases = new double[preset.Layers.Length]; }

        public int Read(float[] buffer, int offset, int count)
        {
            int frames = count / 2;
            var p = Preset;

            // Ensure layer phases array matches
            if (_layerPhases.Length != p.Layers.Length)
                _layerPhases = new double[p.Layers.Length];

            for (int i = 0; i < frames; i++)
            {
                float left = 0, right = 0;

                // Primary binaural pair
                if (p.FrequencyLeft > 0)
                {
                    left += (float)(Math.Sin(_phaseLeft) * p.PrimaryAmplitude);
                    _phaseLeft += 2 * Math.PI * p.FrequencyLeft / SampleRate;
                    if (_phaseLeft > 2 * Math.PI) _phaseLeft -= 2 * Math.PI;
                }

                if (p.FrequencyRight > 0)
                {
                    right += (float)(Math.Sin(_phaseRight) * p.PrimaryAmplitude);
                    _phaseRight += 2 * Math.PI * p.FrequencyRight / SampleRate;
                    if (_phaseRight > 2 * Math.PI) _phaseRight -= 2 * Math.PI;
                }
                else
                {
                    right = left;
                }

                // All additional layers (mono, added to both channels)
                for (int l = 0; l < p.Layers.Length; l++)
                {
                    var (freq, amp) = p.Layers[l];
                    if (freq <= 0) continue;
                    float sample = (float)(Math.Sin(_layerPhases[l]) * amp);
                    _layerPhases[l] += 2 * Math.PI * freq / SampleRate;
                    if (_layerPhases[l] > 2 * Math.PI) _layerPhases[l] -= 2 * Math.PI;
                    left += sample;
                    right += sample;
                }

                buffer[offset + i * 2] = left * Volume;
                buffer[offset + i * 2 + 1] = right * Volume;
            }
            return count;
        }
    }

    public sealed class TonePreset
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public float FrequencyLeft { get; set; }
        public float FrequencyRight { get; set; }
        public float PrimaryAmplitude { get; set; } = 0.5f;
        public (float Frequency, float Amplitude)[] Layers { get; set; } = Array.Empty<(float, float)>();
        public float BinauralBeatHz => FrequencyRight > 0 ? Math.Abs(FrequencyRight - FrequencyLeft) : 0;

        public static TonePreset AllCombined => new()
        {
            Name = "All Healing Frequencies",
            Description = "Schumann binaural + all 9 solfeggio frequencies layered together.",
            FrequencyLeft = 200f,
            FrequencyRight = 207.83f,
            PrimaryAmplitude = 0.15f,
            Layers = new[]
            {
                (174f, 0.10f), (285f, 0.10f), (396f, 0.10f),
                (432f, 0.10f), (528f, 0.12f), (639f, 0.08f),
                (741f, 0.08f), (852f, 0.08f), (963f, 0.08f),
            }
        };

        public static TonePreset Schumann => new()
        {
            Name = "Schumann Resonance",
            Description = "7.83 Hz — Earth's natural frequency. Grounding, calm, stress relief.",
            FrequencyLeft = 200f, FrequencyRight = 207.83f, PrimaryAmplitude = 0.5f
        };

        public static TonePreset DeepRelax => new()
        {
            Name = "Deep Relaxation",
            Description = "4 Hz theta binaural — deep meditation, near-sleep state.",
            FrequencyLeft = 150f, FrequencyRight = 154f, PrimaryAmplitude = 0.5f
        };

        public static TonePreset Focus => new()
        {
            Name = "Focus & Concentration",
            Description = "14 Hz beta binaural — alertness, productivity.",
            FrequencyLeft = 200f, FrequencyRight = 214f, PrimaryAmplitude = 0.4f
        };

        public static TonePreset Healing528 => new()
        {
            Name = "528 Hz — Miracle Tone",
            Description = "DNA repair, transformation. With 264 Hz sub-harmonic.",
            FrequencyLeft = 528f, PrimaryAmplitude = 0.5f,
            Layers = new[] { (264f, 0.15f) }
        };

        public static TonePreset Healing432 => new()
        {
            Name = "432 Hz — Natural Tuning",
            Description = "Verdi's A — harmonious, calming alternative to 440 Hz.",
            FrequencyLeft = 432f, PrimaryAmplitude = 0.5f,
            Layers = new[] { (216f, 0.15f) }
        };

        public static TonePreset Sleep => new()
        {
            Name = "Sleep Induction",
            Description = "2 Hz delta binaural — deep dreamless sleep.",
            FrequencyLeft = 100f, FrequencyRight = 102f, PrimaryAmplitude = 0.4f
        };

        public static TonePreset AntiAnxiety => new()
        {
            Name = "Anti-Anxiety",
            Description = "10 Hz alpha binaural + 396 Hz solfeggio (liberation from fear).",
            FrequencyLeft = 198f, FrequencyRight = 208f, PrimaryAmplitude = 0.4f,
            Layers = new[] { (396f, 0.2f) }
        };

        public static TonePreset BodyHeal => new()
        {
            Name = "Body Healing",
            Description = "174 Hz (pain) + 285 Hz (tissue) + Schumann binaural.",
            FrequencyLeft = 174f, FrequencyRight = 181.83f, PrimaryAmplitude = 0.35f,
            Layers = new[] { (285f, 0.3f) }
        };

        public static TonePreset[] AllPresets => new[]
        {
            AllCombined, Schumann, DeepRelax, Focus, Healing528, Healing432, Sleep, AntiAnxiety, BodyHeal
        };
    }
}
