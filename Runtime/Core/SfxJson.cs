using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AFCT.SfxLab
{
    /// <summary>
    /// Minimal JSON reader/writer for <see cref="SfxParams"/>, mirroring
    /// <c>JSON.stringify(params, null, 2)</c> and <c>JSON.parse</c> in the web engine.
    /// Kept in-house so the runtime stays dependency free.
    /// </summary>
    internal static class SfxJson
    {
        internal static string Write(SfxParams p)
        {
            if (p == null)
            {
                throw new ArgumentNullException(nameof(p));
            }

            var builder = new StringBuilder(1024);
            builder.Append("{\n");
            AppendString(builder, "engine", EngineName(p.engine), true);
            AppendString(builder, "waveform", WaveformName(p.waveform), true);
            AppendString(builder, "footstepTerrain", TerrainName(p.footstepTerrain), true);
            AppendNumber(builder, "footstepHeel", p.footstepHeel, true);
            AppendNumber(builder, "footstepRoll", p.footstepRoll, true);
            AppendNumber(builder, "footstepBall", p.footstepBall, true);
            AppendNumber(builder, "footstepSwiftness", p.footstepSwiftness, true);
            AppendNumber(builder, "attack", p.attack, true);
            AppendNumber(builder, "sustain", p.sustain, true);
            AppendNumber(builder, "sustainPunch", p.sustainPunch, true);
            AppendNumber(builder, "decay", p.decay, true);
            AppendNumber(builder, "startFrequency", p.startFrequency, true);
            AppendNumber(builder, "minFrequency", p.minFrequency, true);
            AppendNumber(builder, "slide", p.slide, true);
            AppendNumber(builder, "deltaSlide", p.deltaSlide, true);
            AppendNumber(builder, "vibratoDepth", p.vibratoDepth, true);
            AppendNumber(builder, "vibratoSpeed", p.vibratoSpeed, true);
            AppendNumber(builder, "changeAmount", p.changeAmount, true);
            AppendNumber(builder, "changeSpeed", p.changeSpeed, true);
            AppendNumber(builder, "squareDuty", p.squareDuty, true);
            AppendNumber(builder, "dutySweep", p.dutySweep, true);
            AppendNumber(builder, "repeatSpeed", p.repeatSpeed, true);
            AppendNumber(builder, "phaserOffset", p.phaserOffset, true);
            AppendNumber(builder, "phaserSweep", p.phaserSweep, true);
            AppendNumber(builder, "lowPassCutoff", p.lowPassCutoff, true);
            AppendNumber(builder, "lowPassSweep", p.lowPassSweep, true);
            AppendNumber(builder, "lowPassResonance", p.lowPassResonance, true);
            AppendNumber(builder, "highPassCutoff", p.highPassCutoff, true);
            AppendNumber(builder, "highPassSweep", p.highPassSweep, true);
            AppendNumber(builder, "masterVolume", p.masterVolume, true);
            AppendNumber(builder, "fxDrive", p.fxDrive, true);
            AppendNumber(builder, "fxCrushRate", p.fxCrushRate, true);
            AppendNumber(builder, "fxCrushBits", p.fxCrushBits, true);
            AppendNumber(builder, "fxTone", p.fxTone, true);
            AppendNumber(builder, "fxDelayTime", p.fxDelayTime, true);
            AppendNumber(builder, "fxDelayFeedback", p.fxDelayFeedback, true);
            AppendNumber(builder, "fxDelayMix", p.fxDelayMix, true);
            AppendNumber(builder, "fxReverbSize", p.fxReverbSize, true);
            AppendNumber(builder, "fxReverbMix", p.fxReverbMix, true);
            AppendNumber(builder, "fxNormalize", p.fxNormalize, false);
            builder.Append("\n}");
            return builder.ToString();
        }

        internal static SfxParams Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var reader = new JsonReader(text);
            object root = reader.ReadValue();
            var map = root as Dictionary<string, object>;
            var result = new SfxParams();
            if (map == null)
            {
                return result;
            }

            object value;
            if (map.TryGetValue("engine", out value))
            {
                result.engine = AsString(value) == "footsteppr" ? SfxEngine.Footsteppr : SfxEngine.Sfxr;
            }

            if (map.TryGetValue("waveform", out value))
            {
                string name = AsString(value);
                if (name == "square") result.waveform = SfxWaveform.Square;
                else if (name == "sawtooth") result.waveform = SfxWaveform.Sawtooth;
                else if (name == "sine") result.waveform = SfxWaveform.Sine;
                else if (name == "noise") result.waveform = SfxWaveform.Noise;
            }

            if (map.TryGetValue("footstepTerrain", out value))
            {
                string name = AsString(value);
                if (name == "snow") result.footstepTerrain = SfxFootstepTerrain.Snow;
                else if (name == "grass") result.footstepTerrain = SfxFootstepTerrain.Grass;
                else if (name == "dirt") result.footstepTerrain = SfxFootstepTerrain.Dirt;
                else if (name == "gravel") result.footstepTerrain = SfxFootstepTerrain.Gravel;
                else if (name == "mud") result.footstepTerrain = SfxFootstepTerrain.Mud;
                else if (name == "wood") result.footstepTerrain = SfxFootstepTerrain.Wood;
                else if (name == "metal") result.footstepTerrain = SfxFootstepTerrain.Metal;
                else if (name == "water") result.footstepTerrain = SfxFootstepTerrain.Water;
            }

            result.footstepHeel = Number(map, "footstepHeel", result.footstepHeel);
            result.footstepRoll = Number(map, "footstepRoll", result.footstepRoll);
            result.footstepBall = Number(map, "footstepBall", result.footstepBall);
            result.footstepSwiftness = Number(map, "footstepSwiftness", result.footstepSwiftness);
            result.attack = Number(map, "attack", result.attack);
            result.sustain = Number(map, "sustain", result.sustain);
            result.sustainPunch = Number(map, "sustainPunch", result.sustainPunch);
            result.decay = Number(map, "decay", result.decay);
            result.startFrequency = Number(map, "startFrequency", result.startFrequency);
            result.minFrequency = Number(map, "minFrequency", result.minFrequency);
            result.slide = Number(map, "slide", result.slide);
            result.deltaSlide = Number(map, "deltaSlide", result.deltaSlide);
            result.vibratoDepth = Number(map, "vibratoDepth", result.vibratoDepth);
            result.vibratoSpeed = Number(map, "vibratoSpeed", result.vibratoSpeed);
            result.changeAmount = Number(map, "changeAmount", result.changeAmount);
            result.changeSpeed = Number(map, "changeSpeed", result.changeSpeed);
            result.squareDuty = Number(map, "squareDuty", result.squareDuty);
            result.dutySweep = Number(map, "dutySweep", result.dutySweep);
            result.repeatSpeed = Number(map, "repeatSpeed", result.repeatSpeed);
            result.phaserOffset = Number(map, "phaserOffset", result.phaserOffset);
            result.phaserSweep = Number(map, "phaserSweep", result.phaserSweep);
            result.lowPassCutoff = Number(map, "lowPassCutoff", result.lowPassCutoff);
            result.lowPassSweep = Number(map, "lowPassSweep", result.lowPassSweep);
            result.lowPassResonance = Number(map, "lowPassResonance", result.lowPassResonance);
            result.highPassCutoff = Number(map, "highPassCutoff", result.highPassCutoff);
            result.highPassSweep = Number(map, "highPassSweep", result.highPassSweep);
            result.masterVolume = Number(map, "masterVolume", result.masterVolume);
            result.fxDrive = Number(map, "fxDrive", result.fxDrive);
            result.fxCrushRate = Number(map, "fxCrushRate", result.fxCrushRate);
            result.fxCrushBits = Number(map, "fxCrushBits", result.fxCrushBits);
            result.fxTone = Number(map, "fxTone", result.fxTone);
            result.fxDelayTime = Number(map, "fxDelayTime", result.fxDelayTime);
            result.fxDelayFeedback = Number(map, "fxDelayFeedback", result.fxDelayFeedback);
            result.fxDelayMix = Number(map, "fxDelayMix", result.fxDelayMix);
            result.fxReverbSize = Number(map, "fxReverbSize", result.fxReverbSize);
            result.fxReverbMix = Number(map, "fxReverbMix", result.fxReverbMix);
            result.fxNormalize = Number(map, "fxNormalize", result.fxNormalize);
            return result;
        }

        private static double Number(Dictionary<string, object> map, string key, double fallback)
        {
            object value;
            if (!map.TryGetValue(key, out value))
            {
                return fallback;
            }

            if (value is double)
            {
                return (double)value;
            }

            // A non-numeric token behaves like NaN, which the clamp maps to the minimum
            // (or to the default for fxNormalize), matching clampSfxParams on parsed JSON.
            return double.NaN;
        }

        private static string AsString(object value)
        {
            return value as string;
        }

        private static void AppendString(StringBuilder builder, string key, string value, bool comma)
        {
            builder.Append("  \"");
            builder.Append(key);
            builder.Append("\": \"");
            builder.Append(value);
            builder.Append(comma ? "\",\n" : "\"\n");
        }

        private static void AppendNumber(StringBuilder builder, string key, double value, bool comma)
        {
            builder.Append("  \"");
            builder.Append(key);
            builder.Append("\": ");
            builder.Append(FormatNumber(value));
            builder.Append(comma ? ",\n" : "\n");
        }

        internal static string FormatNumber(double value)
        {
            if (value == 0.0)
            {
                return "0";
            }

            if (!SfxMath.IsFinite(value))
            {
                return "null";
            }

            if (value == Math.Truncate(value) && Math.Abs(value) < 1e21)
            {
                return value.ToString("0", CultureInfo.InvariantCulture);
            }

            string text = value.ToString("R", CultureInfo.InvariantCulture);
            int exponentIndex = text.IndexOf('E');
            if (exponentIndex < 0)
            {
                return text;
            }

            string mantissa = text.Substring(0, exponentIndex);
            string exponent = text.Substring(exponentIndex + 1);
            bool negative = exponent.Length > 0 && exponent[0] == '-';
            if (exponent.Length > 0 && (exponent[0] == '-' || exponent[0] == '+'))
            {
                exponent = exponent.Substring(1);
            }

            exponent = exponent.TrimStart('0');
            if (exponent.Length == 0)
            {
                exponent = "0";
            }

            return mantissa + "e" + (negative ? "-" : "+") + exponent;
        }

        private static string EngineName(SfxEngine engine)
        {
            return engine == SfxEngine.Footsteppr ? "footsteppr" : "sfxr";
        }

        private static string WaveformName(SfxWaveform waveform)
        {
            switch (waveform)
            {
                case SfxWaveform.Sawtooth: return "sawtooth";
                case SfxWaveform.Sine: return "sine";
                case SfxWaveform.Noise: return "noise";
                default: return "square";
            }
        }

        private static string TerrainName(SfxFootstepTerrain terrain)
        {
            switch (terrain)
            {
                case SfxFootstepTerrain.Snow: return "snow";
                case SfxFootstepTerrain.Grass: return "grass";
                case SfxFootstepTerrain.Dirt: return "dirt";
                case SfxFootstepTerrain.Gravel: return "gravel";
                case SfxFootstepTerrain.Mud: return "mud";
                case SfxFootstepTerrain.Wood: return "wood";
                case SfxFootstepTerrain.Metal: return "metal";
                case SfxFootstepTerrain.Water: return "water";
                default: return "dirt";
            }
        }

        /// <summary>Small recursive-descent JSON reader; only plain JSON is supported.</summary>
        private sealed class JsonReader
        {
            private readonly string _text;
            private int _index;

            internal JsonReader(string text)
            {
                _text = text;
            }

            internal object ReadValue()
            {
                SkipWhitespace();
                if (_index >= _text.Length)
                {
                    throw Error("Unexpected end of JSON");
                }

                char current = _text[_index];
                switch (current)
                {
                    case '{':
                        return ReadObject();
                    case '[':
                        return ReadArray();
                    case '"':
                        return ReadString();
                    case 't':
                        Expect("true");
                        return true;
                    case 'f':
                        Expect("false");
                        return false;
                    case 'n':
                        Expect("null");
                        return null;
                    default:
                        return ReadNumber();
                }
            }

            private Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                _index += 1;
                SkipWhitespace();
                if (Peek() == '}')
                {
                    _index += 1;
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    string key = ReadString();
                    SkipWhitespace();
                    if (Peek() != ':')
                    {
                        throw Error("Expected ':'");
                    }

                    _index += 1;
                    object value = ReadValue();
                    result[key] = value;
                    SkipWhitespace();
                    char next = Peek();
                    if (next == ',')
                    {
                        _index += 1;
                        continue;
                    }

                    if (next == '}')
                    {
                        _index += 1;
                        return result;
                    }

                    throw Error("Expected ',' or '}'");
                }
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                _index += 1;
                SkipWhitespace();
                if (Peek() == ']')
                {
                    _index += 1;
                    return result;
                }

                while (true)
                {
                    result.Add(ReadValue());
                    SkipWhitespace();
                    char next = Peek();
                    if (next == ',')
                    {
                        _index += 1;
                        continue;
                    }

                    if (next == ']')
                    {
                        _index += 1;
                        return result;
                    }

                    throw Error("Expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                if (Peek() != '"')
                {
                    throw Error("Expected a string");
                }

                _index += 1;
                var builder = new StringBuilder();
                while (_index < _text.Length)
                {
                    char current = _text[_index];
                    _index += 1;
                    if (current == '"')
                    {
                        return builder.ToString();
                    }

                    if (current != '\\')
                    {
                        builder.Append(current);
                        continue;
                    }

                    if (_index >= _text.Length)
                    {
                        break;
                    }

                    char escaped = _text[_index];
                    _index += 1;
                    switch (escaped)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (_index + 4 > _text.Length)
                            {
                                throw Error("Truncated unicode escape");
                            }

                            string hex = _text.Substring(_index, 4);
                            _index += 4;
                            builder.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            break;
                        default:
                            throw Error("Invalid escape sequence");
                    }
                }

                throw Error("Unterminated string");
            }

            private double ReadNumber()
            {
                int start = _index;
                while (_index < _text.Length)
                {
                    char current = _text[_index];
                    if ((current >= '0' && current <= '9')
                        || current == '-'
                        || current == '+'
                        || current == '.'
                        || current == 'e'
                        || current == 'E')
                    {
                        _index += 1;
                        continue;
                    }

                    break;
                }

                string token = _text.Substring(start, _index - start);
                double parsed;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    throw Error("Invalid number '" + token + "'");
                }

                return parsed;
            }

            private void Expect(string literal)
            {
                if (_index + literal.Length > _text.Length
                    || string.CompareOrdinal(_text, _index, literal, 0, literal.Length) != 0)
                {
                    throw Error("Expected '" + literal + "'");
                }

                _index += literal.Length;
            }

            private char Peek()
            {
                if (_index >= _text.Length)
                {
                    throw Error("Unexpected end of JSON");
                }

                return _text[_index];
            }

            private void SkipWhitespace()
            {
                while (_index < _text.Length)
                {
                    char current = _text[_index];
                    if (current == ' ' || current == '\t' || current == '\n' || current == '\r')
                    {
                        _index += 1;
                        continue;
                    }

                    break;
                }
            }

            private FormatException Error(string message)
            {
                return new FormatException(message + " at offset " + _index + ".");
            }
        }
    }
}
