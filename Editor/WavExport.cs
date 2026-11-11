using System;
using System.IO;
using System.Text;
using AFCT.SfxLab;
using UnityEditor;
using UnityEngine;

namespace AFCT.SfxLab.EditorTools
{
    /// <summary>
    /// Mastered WAV export into Assets/Audio/SFX/&lt;category&gt;/ with importer settings that
    /// match the sound being written: mono, preloaded, uncompressed for short one-shots and
    /// ADPCM for anything that runs longer.
    /// </summary>
    internal static class WavExport
    {
        public const string RootFolder = "Assets/Audio/SFX";
        private const string FallbackStem = "afct-sfx";
        private const int BitDepth = 16;

        public static string ExportFolder(SfxPresetId preset)
        {
            string category = SfxPresets.Category(preset);
            string folder = string.IsNullOrEmpty(category) ? "utility" : category.ToLowerInvariant();
            return RootFolder + "/" + folder;
        }

        public static string Export(
            SfxPresetId preset,
            string fileName,
            float[] samples,
            int sampleRate,
            bool trimSilence,
            double targetPeakDb)
        {
            if (samples == null || samples.Length == 0)
            {
                throw new InvalidOperationException("there is no rendered audio to export");
            }

            float[] mastered = Mastering.MasterForExport(samples, sampleRate, trimSilence, targetPeakDb);
            if (mastered == null || mastered.Length == 0)
            {
                throw new InvalidOperationException("mastering returned an empty clip");
            }

            byte[] wav = WavEncoder.Encode(mastered, sampleRate, BitDepth);
            string folder = ExportFolder(preset);
            EnsureFolder(folder);

            string stem = SanitizeFileStem(string.IsNullOrWhiteSpace(fileName)
                ? SfxPresets.DisplayName(preset)
                : fileName);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + stem + ".wav");

            File.WriteAllBytes(assetPath, wav);
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer != null)
            {
                ConfigureImporter(importer, mastered.Length, sampleRate);
            }
            else
            {
                Debug.LogWarning("AFCT SFX Lab: no AudioImporter found for " + assetPath);
            }

            return assetPath;
        }

        private static void ConfigureImporter(AudioImporter importer, int sampleCount, int sampleRate)
        {
            double duration = sampleCount / (double)sampleRate;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.compressionFormat = duration < 1.0
                ? AudioCompressionFormat.PCM
                : AudioCompressionFormat.ADPCM;

            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        public static string SanitizeFileStem(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return FallbackStem;
            }

            string trimmed = value.Trim();
            if (trimmed.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 4);
            }

            var builder = new StringBuilder(trimmed.Length);
            bool lastWasDash = false;

            foreach (char c in trimmed)
            {
                bool invalid = c == '\\' || c == '/' || c == ':' || c == '*' || c == '?' ||
                               c == '"' || c == '<' || c == '>' || c == '|' || char.IsWhiteSpace(c);

                if (invalid)
                {
                    if (!lastWasDash)
                    {
                        builder.Append('-');
                        lastWasDash = true;
                    }
                }
                else
                {
                    builder.Append(c);
                    lastWasDash = false;
                }
            }

            string stem = builder.ToString().Trim('-');
            return stem.Length == 0 ? FallbackStem : stem;
        }
    }
}
