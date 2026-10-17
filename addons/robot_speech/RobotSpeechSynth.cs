using Godot;
using System;
using System.Collections.Generic;

public sealed class RobotSpeechClip
{
    public RobotSpeechClip(AudioStreamWav stream, float duration, float[] characterTimes, StringName emotion)
    {
        Stream = stream;
        Duration = duration;
        CharacterTimes = characterTimes;
        Emotion = emotion;
    }

    public AudioStreamWav Stream { get; }
    public float Duration { get; }
    public float[] CharacterTimes { get; }
    public StringName Emotion { get; }
}

// Lightweight deterministic robot-gibberish renderer. Text shapes cadence and
// selects from a deliberately small palette; it is not an encoded protocol.
public static class RobotSpeechSynth
{
    public const int SampleRate = 22_050;

    public static readonly StringName Neutral = "neutral";
    public static readonly StringName Urgent = "urgent";
    public static readonly StringName Sad = "sad";
    public static readonly StringName Curious = "curious";
    public static readonly StringName Happy = "happy";

    private readonly struct Mood
    {
        public Mood(float pitch, float pace, float range, float brightness)
        {
            Pitch = pitch;
            Pace = pace;
            Range = range;
            Brightness = brightness;
        }

        public float Pitch { get; }
        public float Pace { get; }
        public float Range { get; }
        public float Brightness { get; }
    }

    public static RobotSpeechClip RenderLine(
        string text,
        RobotVoiceProfile profile,
        StringName emotion = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        text ??= string.Empty;

        StringName emotionKey = NormalizeEmotion(emotion);
        Mood mood = GetMood(emotionKey);
        var samples = new List<float>(Math.Max(1, text.Length * 1_500));
        var characterTimes = new float[text.Length + 1];
        int cursor = 0;
        int wordIndex = 0;
        int lineSeed = StableHash(text) + profile.VoiceSeed * 7_919;

        while (cursor < text.Length)
        {
            char character = text[cursor];
            if (IsWordCharacter(character))
            {
                int start = cursor;
                while (cursor < text.Length && IsWordCharacter(text[cursor]))
                    cursor++;

                string word = text.Substring(start, cursor - start);
                char following = NextPunctuation(text, cursor);
                float wordStart = (float)samples.Count / SampleRate;
                RenderWord(samples, word, wordIndex, following, lineSeed, profile, mood, emotionKey);
                float wordEnd = (float)samples.Count / SampleRate;

                for (int characterIndex = start; characterIndex < cursor; characterIndex++)
                {
                    float ratio = (float)(characterIndex - start + 1) / Math.Max(1, cursor - start);
                    characterTimes[characterIndex + 1] = Mathf.Lerp(wordStart, wordEnd, ratio);
                }

                AppendSilence(samples, 0.026f / (profile.Pace * mood.Pace));
                wordIndex++;
            }
            else
            {
                float now = (float)samples.Count / SampleRate;
                characterTimes[cursor + 1] = now;
                if (character is ',' or ';' or ':')
                    AppendSilence(samples, 0.105f);
                else if (character is '.' or '!' or '?')
                    AppendSilence(samples, 0.205f);
                cursor++;
            }
        }

        AppendSilence(samples, 0.055f);
        float duration = (float)samples.Count / SampleRate;
        characterTimes[text.Length] = Math.Min(characterTimes[text.Length], duration);
        return new RobotSpeechClip(MakeStream(samples, profile.Gain), duration, characterTimes, emotionKey);
    }

    private static void RenderWord(
        List<float> samples,
        string word,
        int wordIndex,
        char following,
        int lineSeed,
        RobotVoiceProfile profile,
        Mood mood,
        StringName emotion)
    {
        int syllableCount = Math.Clamp((int)Math.Ceiling(word.Length / 3.6f), 1, 4);
        int[] palette = profile.PhonemePalette;
        if (palette.Length == 0)
            palette = new[] { 0 };

        int wordValue = StableHash(word.ToLowerInvariant()) + lineSeed + wordIndex * 104_729;
        for (int syllable = 0; syllable < syllableCount; syllable++)
        {
            int unitSeed = MixSeed(wordValue + syllable * 3_571);
            int pattern = palette[PositiveModulo(unitSeed, palette.Length)];
            float pitchRandom = UnitFloat(unitSeed + 17) * 2.0f - 1.0f;
            float semitones = pitchRandom * profile.PitchRangeSemitones * mood.Range;
            float pitch = profile.BasePitchHz * mood.Pitch * MathF.Pow(2.0f, semitones / 12.0f);
            float duration = Mathf.Lerp(0.082f, 0.142f, UnitFloat(unitSeed + 41)) /
                             (profile.Pace * mood.Pace);
            float endingCurve = 0.0f;

            if (syllable == syllableCount - 1)
            {
                if (following == '?' || emotion == Curious)
                    endingCurve = 0.34f;
                else if (following == '!' || emotion == Urgent)
                    endingCurve = 0.16f;
                else if (following == '.' || emotion == Sad)
                    endingCurve = -0.24f;
            }

            AppendPhoneme(samples, pattern, pitch, duration, endingCurve, unitSeed, profile, mood);
            if (syllable < syllableCount - 1)
                AppendSilence(samples, 0.010f);
        }
    }

    private static void AppendPhoneme(
        List<float> samples,
        int pattern,
        float pitchHz,
        float duration,
        float endingCurve,
        int seed,
        RobotVoiceProfile profile,
        Mood mood)
    {
        int sampleCount = Math.Max(1, (int)(duration * SampleRate));
        double phase = 0.0;
        long noiseState = seed & 0x7fff_ffff;

        for (int index = 0; index < sampleCount; index++)
        {
            float t = (float)index / Math.Max(1, sampleCount - 1);
            float contour = 1.0f + endingCurve * t * t;
            float amplitudeMod = 1.0f;

            switch (PositiveModulo(pattern, 6))
            {
                case 0:
                    contour *= 1.0f + 0.06f * MathF.Sin(t * MathF.Tau);
                    break;
                case 1:
                    contour *= 0.78f + 0.42f * t;
                    break;
                case 2:
                    contour *= 1.24f - 0.38f * t;
                    break;
                case 3:
                    contour *= 1.0f + 0.10f * MathF.Sin(t * MathF.Tau * 3.0f);
                    break;
                case 4:
                    amplitudeMod = 0.72f + 0.28f * MathF.Sin(t * MathF.Tau * 7.0f);
                    break;
                case 5:
                    contour *= 0.91f + 0.12f * MathF.Sin(t * MathF.Tau * 2.0f) + 0.10f * t;
                    break;
            }

            phase += Math.Tau * pitchHz * contour / SampleRate;
            float attack = Mathf.SmoothStep(0.0f, 0.10f, t);
            float release = 1.0f - Mathf.SmoothStep(0.74f, 1.0f, t);
            float envelope = attack * release * amplitudeMod;
            float fundamental = MathF.Sin((float)phase);
            float bright = MathF.Sin((float)(phase * 2.01 + 0.35)) * 0.34f *
                           profile.Brightness * mood.Brightness;
            float buzzy = MathF.Sign(MathF.Sin((float)(phase * 0.505))) * 0.16f * profile.Buzz;
            noiseState = (noiseState * 1_103_515_245 + 12_345) & 0x7fff_ffff;
            float noiseSample = ((float)noiseState / 1_073_741_824.0f - 1.0f) * profile.Noise;
            float click = 0.0f;
            if (index < SampleRate * 0.005f)
            {
                click = (1.0f - index / (SampleRate * 0.005f)) * 0.12f *
                        MathF.Sin((float)(phase * 4.0));
            }

            samples.Add((fundamental + bright + buzzy + noiseSample + click) * envelope);
        }
    }

    private static void AppendSilence(List<float> samples, float seconds)
    {
        int count = Math.Max(0, (int)(seconds * SampleRate));
        for (int index = 0; index < count; index++)
            samples.Add(0.0f);
    }

    private static AudioStreamWav MakeStream(List<float> samples, float gain)
    {
        var bytes = new byte[samples.Count * 2];
        for (int index = 0; index < samples.Count; index++)
        {
            float shaped = MathF.Tanh(samples[index] * 1.20f) * gain;
            short pcm = (short)Math.Clamp((int)MathF.Round(shaped * 32_767.0f), -32_768, 32_767);
            bytes[index * 2] = (byte)(pcm & 0xff);
            bytes[index * 2 + 1] = (byte)((pcm >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false,
            Data = bytes,
        };
    }

    private static StringName NormalizeEmotion(StringName emotion)
    {
        if (emotion == Urgent || emotion == Sad || emotion == Curious || emotion == Happy)
            return emotion;
        return Neutral;
    }

    private static Mood GetMood(StringName emotion)
    {
        if (emotion == Urgent) return new Mood(1.12f, 1.24f, 1.28f, 1.18f);
        if (emotion == Sad) return new Mood(0.84f, 0.80f, 0.62f, 0.70f);
        if (emotion == Curious) return new Mood(1.06f, 0.94f, 1.20f, 1.08f);
        if (emotion == Happy) return new Mood(1.10f, 1.10f, 1.16f, 1.15f);
        return new Mood(1.0f, 1.0f, 1.0f, 1.0f);
    }

    private static bool IsWordCharacter(char character) =>
        !char.IsWhiteSpace(character) && ",.!?;:—-…".IndexOf(character) < 0;

    private static char NextPunctuation(string text, int fromIndex)
    {
        int index = fromIndex;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;
        if (index < text.Length && ",.!?;:…".IndexOf(text[index]) >= 0)
            return text[index];
        return '\0';
    }

    private static int StableHash(string value)
    {
        long result = 5_381;
        foreach (char character in value)
            result = (result * 33 + character) % 2_147_483_647;
        return (int)result;
    }

    private static int MixSeed(int value)
    {
        long result = value & 0x7fff_ffff;
        result = ((result ^ (result >> 16)) * 73_244_475) & 0x7fff_ffff;
        result = ((result ^ (result >> 15)) * 73_244_475) & 0x7fff_ffff;
        return (int)(result ^ (result >> 16));
    }

    private static float UnitFloat(int seed) => (MixSeed(seed) & 0xffff) / 65_535.0f;

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
