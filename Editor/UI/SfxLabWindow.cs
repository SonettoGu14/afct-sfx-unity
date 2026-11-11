using System;
using System.Collections.Generic;
using System.Globalization;
using AFCT.SfxLab;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace AFCT.SfxLab.EditorTools
{
    /// <summary>
    /// The AFCT SFX Lab production sheet: preset rail, parameter column and a dark monitor band.
    /// Everything is built in code on top of a static UXML skeleton so the window survives
    /// domain reloads with plain elements only.
    /// </summary>
    internal sealed class SfxLabWindow : EditorWindow
    {
        private const string UxmlPath = "Editor/UI/SfxLabWindow.uxml";
        private const string UssPath = "Editor/UI/SfxLabWindow.uss";
        private const int RenderDelayMs = 60;

        private static readonly int[] SampleRates = { 44100, 48000, 22050, 11025, 8000 };

        private static readonly string[] SampleRateLabels =
        {
            "44.1 kHz", "48 kHz", "22.05 kHz", "11.025 kHz", "8 kHz"
        };

        private static readonly string[] PeakLabels = { "Off", "-3 dBFS", "-6 dBFS", "-12 dBFS" };

        private static readonly double[] PeakValues =
        {
            double.NegativeInfinity, -3d, -6d, -12d
        };

        private static readonly string[] RailCategories =
        {
            "UI", "Action", "Cinematic", "Footsteps", "Utility"
        };

        private readonly AudioPreview _preview = new AudioPreview();
        private readonly Dictionary<SfxPresetId, VisualElement> _cueRows =
            new Dictionary<SfxPresetId, VisualElement>();

        private SfxPresetId _activePreset = SfxPresetId.Click;
        private SfxParams _params;
        private float[] _samples;
        private SfxRenderStats _stats;
        private int _sampleRate = SampleRates[0];
        private Font _monoFont;

        private bool _built;
        private bool _dirty;
        private bool _rendering;
        private bool _playbackActive;

        private IVisualElementScheduledItem _renderItem;
        private IVisualElementScheduledItem _watchItem;
        private AudioClip _previewClip;

        private Label _exportFolder;
        private ScrollView _railScroll;
        private VisualElement _rail;
        private ScrollView _paramScroll;
        private Label _presetName;
        private Label _presetLine;
        private VisualElement _paramGroups;
        private Label _monitorMeta;
        private VisualElement _waveformSlot;
        private WaveformView _waveform;
        private Label _readoutDuration;
        private Label _readoutSamples;
        private Label _readoutPeak;
        private Label _readoutClipped;
        private Button _playButton;
        private Button _stopButton;
        private Button _exportButton;
        private Label _status;
        private TextField _fileNameField;
        private PaperSelect _sampleRateSelect;
        private PaperSelect _peakSelect;
        private PaperCheck _trimCheck;

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("sfxlab-root");
            minSize = new Vector2(900f, 600f);
            titleContent = new GUIContent("AFCT SFX Lab");

            _renderItem = null;
            _watchItem = null;
            _built = false;
            StopPreview();

            VisualTreeAsset uxml = LoadAsset<VisualTreeAsset>(UxmlPath);
            if (uxml == null)
            {
                rootVisualElement.Add(new Label("AFCT SFX Lab could not find " + UxmlPath + "."));
                return;
            }

            StyleSheet uss = LoadAsset<StyleSheet>(UssPath);
            if (uss != null && !rootVisualElement.styleSheets.Contains(uss))
            {
                rootVisualElement.styleSheets.Add(uss);
            }

            var host = new VisualElement { name = "sfxlab-host" };
            host.style.flexGrow = 1f;
            rootVisualElement.Add(host);
            uxml.CloneTree(host);

            _monoFont = LoadMonoFont();
            CacheElements();
            ApplyMonoFonts();
            BindControls();
            BuildRail();

            _built = true;
            SelectPreset(SfxPresetId.Click);
        }

        private void OnDisable()
        {
            if (_renderItem != null)
            {
                _renderItem.Pause();
                _renderItem = null;
            }

            if (_watchItem != null)
            {
                _watchItem.Pause();
                _watchItem = null;
            }

            StopPreview();
        }

        private void WatchPreview()
        {
            if (!_playbackActive)
            {
                if (_watchItem != null)
                {
                    _watchItem.Pause();
                }

                return;
            }

            if (_preview.IsPlaying)
            {
                return;
            }

            StopPreview();
            SetStatus("Ready.", false);
        }

        private static T LoadAsset<T>(string relativePath) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>("Packages/com.afct.sfxlab/" + relativePath);
            if (asset != null)
            {
                return asset;
            }

            string fileName = relativePath.Substring(relativePath.LastIndexOf('/') + 1);
            string kind = typeof(T) == typeof(StyleSheet) ? "StyleSheet" : "VisualTreeAsset";
            string[] guids = AssetDatabase.FindAssets(fileName + " t:" + kind);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
            }

            return null;
        }

        private static Font LoadMonoFont()
        {
            Font bundled = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
            if (bundled != null)
            {
                return bundled;
            }

            return Font.CreateDynamicFontFromOSFont(
                new[] { "SF Mono", "Menlo", "Consolas", "monospace" },
                11);
        }

        private void CacheElements()
        {
            _exportFolder = rootVisualElement.Q<Label>("export-folder");
            _railScroll = rootVisualElement.Q<ScrollView>("rail-scroll");
            _rail = rootVisualElement.Q<VisualElement>("rail");
            _paramScroll = rootVisualElement.Q<ScrollView>("param-scroll");
            _presetName = rootVisualElement.Q<Label>("preset-name");
            _presetLine = rootVisualElement.Q<Label>("preset-line");
            _paramGroups = rootVisualElement.Q<VisualElement>("param-groups");
            _monitorMeta = rootVisualElement.Q<Label>("monitor-meta");
            _waveformSlot = rootVisualElement.Q<VisualElement>("waveform-slot");
            _readoutDuration = rootVisualElement.Q<Label>("readout-duration");
            _readoutSamples = rootVisualElement.Q<Label>("readout-samples");
            _readoutPeak = rootVisualElement.Q<Label>("readout-peak");
            _readoutClipped = rootVisualElement.Q<Label>("readout-clipped");
            _playButton = rootVisualElement.Q<Button>("play-button");
            _stopButton = rootVisualElement.Q<Button>("stop-button");
            _exportButton = rootVisualElement.Q<Button>("export-button");
            _status = rootVisualElement.Q<Label>("status");

            if (_railScroll != null)
            {
                _railScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            if (_paramScroll != null)
            {
                _paramScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            }
        }

        private void ApplyMonoFonts()
        {
            rootVisualElement.Query<Label>(className: "mono").ForEach(StyleMono);
        }

        private void StyleMono(Label label)
        {
            if (label == null || _monoFont == null)
            {
                return;
            }

            label.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(_monoFont));
        }

        private void BindControls()
        {
            var sampleRateSlot = rootVisualElement.Q<VisualElement>("sample-rate-slot");
            _sampleRateSelect = new PaperSelect(SampleRateLabels, 0) { name = "sample-rate" };
            _sampleRateSelect.Changed += OnSampleRateChanged;
            if (sampleRateSlot != null)
            {
                sampleRateSlot.Add(_sampleRateSelect);
            }

            var trimSlot = rootVisualElement.Q<VisualElement>("trim-slot");
            _trimCheck = new PaperCheck(string.Empty, false) { name = "trim-silence" };
            if (trimSlot != null)
            {
                trimSlot.Add(_trimCheck);
            }

            var peakSlot = rootVisualElement.Q<VisualElement>("peak-slot");
            _peakSelect = new PaperSelect(PeakLabels, 0) { name = "peak-target" };
            if (peakSlot != null)
            {
                peakSlot.Add(_peakSelect);
            }

            _fileNameField = rootVisualElement.Q<TextField>("file-name");
            if (_fileNameField != null)
            {
                _fileNameField.value = SfxPresets.DisplayName(_activePreset);
            }

            _waveform = new WaveformView();
            if (_waveformSlot != null)
            {
                _waveformSlot.Add(_waveform);
            }

            if (_playButton != null)
            {
                _playButton.clicked += OnPlayClicked;
            }

            if (_stopButton != null)
            {
                _stopButton.clicked += OnStopClicked;
            }

            if (_exportButton != null)
            {
                _exportButton.clicked += OnExportClicked;
            }

            UpdateTransportState();
        }

        private void BuildRail()
        {
            if (_rail == null)
            {
                return;
            }

            _rail.Clear();
            _cueRows.Clear();

            for (int c = 0; c < RailCategories.Length; c++)
            {
                string category = RailCategories[c];
                var section = new VisualElement();
                section.AddToClassList("rail-section");

                var head = new VisualElement();
                head.AddToClassList("rail-head");

                var title = new Label(category);
                title.AddToClassList("rail-head__title");

                var count = new Label("0");
                count.AddToClassList("rail-head__count");
                StyleMono(count);

                head.Add(title);
                head.Add(count);
                section.Add(head);

                var rows = new VisualElement();
                section.Add(rows);

                int index = 0;
                SfxPresetId[] order = SfxPresets.Order;
                for (int i = 0; i < order.Length; i++)
                {
                    SfxPresetId preset = order[i];
                    if (!string.Equals(SfxPresets.Category(preset), category, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    index++;
                    SfxPresetId captured = preset;

                    var row = new VisualElement();
                    row.AddToClassList("cue-row");
                    row.RegisterCallback<ClickEvent>(_ => SelectPreset(captured));

                    var label = new Label(SfxPresets.DisplayName(preset));
                    label.AddToClassList("cue-row__label");

                    var number = new Label(index.ToString("00", CultureInfo.InvariantCulture));
                    number.AddToClassList("cue-row__index");
                    StyleMono(number);

                    row.Add(label);
                    row.Add(number);
                    rows.Add(row);
                    _cueRows[preset] = row;
                }

                count.text = index.ToString(CultureInfo.InvariantCulture);
                _rail.Add(section);
            }
        }

        private void SelectPreset(SfxPresetId preset)
        {
            if (!_built)
            {
                return;
            }

            if (_cueRows.TryGetValue(_activePreset, out VisualElement previous) && previous != null)
            {
                previous.RemoveFromClassList("is-active");
            }

            _activePreset = preset;
            if (_cueRows.TryGetValue(preset, out VisualElement current) && current != null)
            {
                current.AddToClassList("is-active");
            }

            _params = SfxPresets.Create(preset);

            if (_presetName != null)
            {
                _presetName.text = SfxPresets.DisplayName(preset);
            }

            if (_presetLine != null)
            {
                _presetLine.text = BuildPresetLine();
            }

            if (_exportFolder != null)
            {
                _exportFolder.text = WavExport.ExportFolder(preset) + "/";
            }

            if (_fileNameField != null)
            {
                _fileNameField.SetValueWithoutNotify(SfxPresets.DisplayName(preset));
            }

            BuildParameterGroups();

            _dirty = true;
            RenderCurrent();
        }

        private string BuildPresetLine()
        {
            string category = SfxPresets.Category(_activePreset);

            if (_params != null && _params.engine == SfxEngine.Footsteppr)
            {
                return category + " preset, footsteppr engine, " + TerrainLabel(_params.footstepTerrain) + " terrain.";
            }

            string waveform = _params != null ? WaveformLabel(_params.waveform) : "waveform";
            return category + " preset, sfxr engine, " + waveform + ".";
        }

        private void BuildParameterGroups()
        {
            if (_paramGroups == null)
            {
                return;
            }

            _paramGroups.Clear();

            VisualElement envelope = CreateSection(
                "Envelope",
                "Attack, sustain and decay over the life of the sound.",
                true);
            AddParamRow(envelope, "Attack", p => p.attack, (p, v) => p.attack = v, 0d, 1d, false);
            AddParamRow(envelope, "Sustain", p => p.sustain, (p, v) => p.sustain = v, 0d, 1d, false);
            AddParamRow(envelope, "Sustain punch", p => p.sustainPunch, (p, v) => p.sustainPunch = v, 0d, 1d, false);
            AddParamRow(envelope, "Decay", p => p.decay, (p, v) => p.decay = v, 0d, 1d, false);

            VisualElement frequency = CreateSection(
                "Frequency",
                "Pitch floor and the glide from the start tone.",
                true);
            AddParamRow(frequency, "Start frequency", p => p.startFrequency, (p, v) => p.startFrequency = v, 0d, 1d, false);
            AddParamRow(frequency, "Min frequency", p => p.minFrequency, (p, v) => p.minFrequency = v, 0d, 1d, false);
            AddParamRow(frequency, "Slide", p => p.slide, (p, v) => p.slide = v, -1d, 1d, true);
            AddParamRow(frequency, "Delta slide", p => p.deltaSlide, (p, v) => p.deltaSlide = v, -1d, 1d, true);

            VisualElement modulation = CreateSection(
                "Modulation",
                "Vibrato and repeating pitch changes.",
                false);
            AddParamRow(modulation, "Vibrato depth", p => p.vibratoDepth, (p, v) => p.vibratoDepth = v, 0d, 1d, false);
            AddParamRow(modulation, "Vibrato speed", p => p.vibratoSpeed, (p, v) => p.vibratoSpeed = v, 0d, 1d, false);
            AddParamRow(modulation, "Change amount", p => p.changeAmount, (p, v) => p.changeAmount = v, -1d, 1d, true);
            AddParamRow(modulation, "Change speed", p => p.changeSpeed, (p, v) => p.changeSpeed = v, 0d, 1d, false);

            VisualElement texture = CreateSection(
                "Texture",
                "Wave shape, duty cycle and phaser movement.",
                false);
            AddParamRow(texture, "Square duty", p => p.squareDuty, (p, v) => p.squareDuty = v, 0d, 1d, false);
            AddParamRow(texture, "Duty sweep", p => p.dutySweep, (p, v) => p.dutySweep = v, -1d, 1d, true);
            AddParamRow(texture, "Repeat speed", p => p.repeatSpeed, (p, v) => p.repeatSpeed = v, 0d, 1d, false);
            AddParamRow(texture, "Phaser offset", p => p.phaserOffset, (p, v) => p.phaserOffset = v, -1d, 1d, true);
            AddParamRow(texture, "Phaser sweep", p => p.phaserSweep, (p, v) => p.phaserSweep = v, -1d, 1d, true);

            VisualElement filter = CreateSection(
                "Filter",
                "Low and high pass shaping plus master level.",
                false);
            AddParamRow(filter, "Low pass cutoff", p => p.lowPassCutoff, (p, v) => p.lowPassCutoff = v, 0d, 1d, false);
            AddParamRow(filter, "Low pass sweep", p => p.lowPassSweep, (p, v) => p.lowPassSweep = v, -1d, 1d, true);
            AddParamRow(filter, "Low pass resonance", p => p.lowPassResonance, (p, v) => p.lowPassResonance = v, 0d, 1d, false);
            AddParamRow(filter, "High pass cutoff", p => p.highPassCutoff, (p, v) => p.highPassCutoff = v, 0d, 1d, false);
            AddParamRow(filter, "High pass sweep", p => p.highPassSweep, (p, v) => p.highPassSweep = v, -1d, 1d, true);
            AddParamRow(filter, "Master volume", p => p.masterVolume, (p, v) => p.masterVolume = v, 0d, 1d, false);

            VisualElement fx = CreateSection(
                "FX rack",
                "Drive, crush, tone, delay and space.",
                false);
            AddParamRow(fx, "Drive", p => p.fxDrive, (p, v) => p.fxDrive = v, 0d, 1d, false);
            AddParamRow(fx, "Crush rate", p => p.fxCrushRate, (p, v) => p.fxCrushRate = v, 0d, 1d, false);
            AddParamRow(fx, "Crush bits", p => p.fxCrushBits, (p, v) => p.fxCrushBits = v, 0d, 1d, false);
            AddParamRow(fx, "Tone", p => p.fxTone, (p, v) => p.fxTone = v, -1d, 1d, true);
            AddParamRow(fx, "Delay time", p => p.fxDelayTime, (p, v) => p.fxDelayTime = v, 0d, 1d, false);
            AddParamRow(fx, "Delay feedback", p => p.fxDelayFeedback, (p, v) => p.fxDelayFeedback = v, 0d, 1d, false);
            AddParamRow(fx, "Delay mix", p => p.fxDelayMix, (p, v) => p.fxDelayMix = v, 0d, 1d, false);
            AddParamRow(fx, "Reverb size", p => p.fxReverbSize, (p, v) => p.fxReverbSize = v, 0d, 1d, false);
            AddParamRow(fx, "Reverb mix", p => p.fxReverbMix, (p, v) => p.fxReverbMix = v, 0d, 1d, false);
        }

        private VisualElement CreateSection(string title, string detail, bool expanded)
        {
            var section = new VisualElement();
            section.AddToClassList("section");

            var head = new VisualElement();
            head.AddToClassList("section-head");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("section-head__title");

            var detailLabel = new Label(detail);
            detailLabel.AddToClassList("section-head__detail");

            bool open = expanded;
            var state = new Label(open ? "-" : "+");
            state.AddToClassList("section-head__state");
            StyleMono(state);

            var body = new VisualElement();
            body.AddToClassList("section-body");
            body.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;

            head.RegisterCallback<ClickEvent>(_ =>
            {
                open = !open;
                body.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
                state.text = open ? "-" : "+";
            });

            head.Add(titleLabel);
            head.Add(detailLabel);
            head.Add(state);
            section.Add(head);
            section.Add(body);
            _paramGroups.Add(section);

            return body;
        }

        private void AddParamRow(
            VisualElement parent,
            string label,
            Func<SfxParams, double> getter,
            Action<SfxParams, double> setter,
            double min,
            double max,
            bool signed)
        {
            if (_params == null)
            {
                return;
            }

            var row = new VisualElement();
            row.AddToClassList("param-row");

            var name = new Label(label);
            name.AddToClassList("param-row__label");

            var value = new Label(FormatValue(getter(_params), signed));
            value.AddToClassList("param-row__value");
            StyleMono(value);

            var slider = new RangeSlider(min, max, getter(_params));
            slider.ValueChanged += v =>
            {
                setter(_params, v);
                value.text = FormatValue(v, signed);
                _dirty = true;
                ScheduleRender();
            };

            row.Add(name);
            row.Add(slider);
            row.Add(value);
            parent.Add(row);
        }

        private static string FormatValue(double value, bool signed)
        {
            double percent = Math.Round(value * 100d);
            if (percent == 0d)
            {
                percent = 0d;
            }

            string text = percent.ToString("0", CultureInfo.InvariantCulture);
            if (signed && percent > 0d)
            {
                text = "+" + text;
            }

            return text + "%";
        }

        private void ScheduleRender()
        {
            if (!_built)
            {
                return;
            }

            if (_renderItem == null)
            {
                _renderItem = rootVisualElement.schedule.Execute(RenderCurrent);
            }

            if (!_renderItem.isActive)
            {
                _renderItem.Resume();
            }

            _renderItem.ExecuteLater(RenderDelayMs);
        }

        private void RenderCurrent()
        {
            if (!_built || !_dirty)
            {
                return;
            }

            _dirty = false;
            _rendering = true;

            if (_playbackActive)
            {
                StopPreview();
            }

            if (_playButton != null)
            {
                _playButton.SetEnabled(false);
            }

            if (_exportButton != null)
            {
                _exportButton.SetEnabled(false);
            }

            SetStatus("Rendering...", false);

            try
            {
                var options = new SfxRenderOptions { SampleRate = _sampleRate, BitDepth = 16 };
                SfxRenderResult result = SfxLabEngine.Render(_params, options);

                _samples = result.Samples;
                _stats = result.Stats;

                if (_waveform != null)
                {
                    _waveform.SetSamples(_samples);
                }

                UpdateReadouts();
                UpdateMonitorMeta();
                SetStatus("Ready.", false);
            }
            catch (Exception exception)
            {
                _samples = null;
                _stats = null;

                if (_waveform != null)
                {
                    _waveform.SetSamples(null);
                }

                UpdateReadouts();
                SetStatus("Render failed: " + exception.Message, false);
                Debug.LogException(exception);
            }
            finally
            {
                _rendering = false;

                if (_playButton != null)
                {
                    _playButton.SetEnabled(true);
                }

                if (_exportButton != null)
                {
                    _exportButton.SetEnabled(true);
                }

                UpdateTransportState();
            }
        }

        private void UpdateReadouts()
        {
            if (_readoutDuration != null)
            {
                _readoutDuration.text = _stats != null
                    ? _stats.DurationSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " s"
                    : "0.00 s";
            }

            if (_readoutSamples != null)
            {
                _readoutSamples.text = _stats != null
                    ? _stats.Samples.ToString("N0", CultureInfo.InvariantCulture)
                    : "0";
            }

            if (_readoutPeak != null)
            {
                _readoutPeak.text = _stats != null
                    ? _stats.Peak.ToString("0.00", CultureInfo.InvariantCulture)
                    : "0.00";

                if (_samples != null && _samples.Length > 0)
                {
                    _readoutPeak.tooltip = Mastering.MeasurePeakDb(_samples)
                        .ToString("0.0", CultureInfo.InvariantCulture) + " dBFS";
                }
            }

            if (_readoutClipped != null)
            {
                _readoutClipped.text = _stats != null
                    ? _stats.ClippedSamples.ToString("N0", CultureInfo.InvariantCulture)
                    : "0";
            }
        }

        private void UpdateMonitorMeta()
        {
            if (_monitorMeta == null)
            {
                return;
            }

            string source;
            if (_params != null && _params.engine == SfxEngine.Footsteppr)
            {
                source = Capitalize(TerrainLabel(_params.footstepTerrain));
            }
            else
            {
                source = Capitalize(_params != null ? WaveformLabel(_params.waveform) : "waveform");
            }

            _monitorMeta.text = source + ", " + RateLabel(_sampleRate) + ", 16 bit";
        }

        private void UpdateTransportState()
        {
            if (_playButton != null)
            {
                _playButton.EnableInClassList("is-live", _playbackActive);
            }

            if (_stopButton != null)
            {
                _stopButton.SetEnabled(_playbackActive);
            }
        }

        private void OnSampleRateChanged(int index)
        {
            if (index < 0 || index >= SampleRates.Length)
            {
                return;
            }

            _sampleRate = SampleRates[index];
            _dirty = true;
            ScheduleRender();
        }

        private void OnPlayClicked()
        {
            if (_rendering)
            {
                return;
            }

            RenderCurrent();
            if (_samples == null || _samples.Length == 0)
            {
                return;
            }

            StopPreview();

            AudioClip clip = AudioClip.Create(
                "AFCT SFX Lab Preview",
                _samples.Length,
                1,
                _sampleRate,
                false);
            clip.hideFlags = HideFlags.HideAndDontSave;

            if (!clip.SetData(_samples, 0))
            {
                Object.DestroyImmediate(clip);
                SetStatus("Preview failed: the clip could not be filled.", false);
                return;
            }

            _previewClip = clip;
            _preview.Play(clip);
            _playbackActive = true;
            UpdateTransportState();
            SetStatus("Playing.", true);

            if (_watchItem == null)
            {
                _watchItem = rootVisualElement.schedule.Execute(WatchPreview).Every(100);
            }

            if (!_watchItem.isActive)
            {
                _watchItem.Resume();
            }
        }

        private void OnStopClicked()
        {
            StopPreview();
            SetStatus("Ready.", false);
        }

        private void OnExportClicked()
        {
            if (_rendering)
            {
                return;
            }

            RenderCurrent();
            if (_samples == null || _samples.Length == 0)
            {
                SetStatus("Export failed: there is no rendered audio.", false);
                return;
            }

            int peakIndex = _peakSelect != null
                ? Mathf.Clamp(_peakSelect.Index, 0, PeakValues.Length - 1)
                : 0;

            try
            {
                string assetPath = WavExport.Export(
                    _activePreset,
                    _fileNameField != null ? _fileNameField.value : null,
                    _samples,
                    _sampleRate,
                    _trimCheck != null && _trimCheck.Value,
                    PeakValues[peakIndex]);

                SetStatus("Exported to " + WavExport.ExportFolder(_activePreset) + "/.", false);
                Debug.Log("AFCT SFX Lab: exported " + assetPath);
            }
            catch (Exception exception)
            {
                SetStatus("Export failed: " + exception.Message, false);
                Debug.LogException(exception);
            }
        }

        private void StopPreview()
        {
            _preview.Stop();

            if (_previewClip != null)
            {
                Object.DestroyImmediate(_previewClip);
                _previewClip = null;
            }

            _playbackActive = false;

            if (_watchItem != null)
            {
                _watchItem.Pause();
            }

            UpdateTransportState();
        }

        private void SetStatus(string message, bool live)
        {
            if (_status == null)
            {
                return;
            }

            _status.text = message;
            _status.tooltip = message;
            _status.EnableInClassList("is-live", live);
        }

        private static string WaveformLabel(SfxWaveform waveform)
        {
            switch (waveform)
            {
                case SfxWaveform.Square:
                    return "square wave";
                case SfxWaveform.Sawtooth:
                    return "sawtooth wave";
                case SfxWaveform.Sine:
                    return "sine wave";
                default:
                    return "noise";
            }
        }

        private static string TerrainLabel(SfxFootstepTerrain terrain)
        {
            return terrain.ToString().ToLowerInvariant();
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Length == 1)
            {
                return value.ToUpperInvariant();
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static string RateLabel(int sampleRate)
        {
            for (int i = 0; i < SampleRates.Length; i++)
            {
                if (SampleRates[i] == sampleRate)
                {
                    return SampleRateLabels[i];
                }
            }

            return sampleRate.ToString(CultureInfo.InvariantCulture) + " Hz";
        }
    }
}
