using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace AFCT.SfxLab.EditorTools
{
    /// <summary>
    /// Native editor window for the SFX Lab. The layout follows the Project window plus
    /// Inspector idiom so the tool feels like part of Unity rather than a web page.
    /// </summary>
    internal sealed class SfxLabWindow : EditorWindow
    {
        private const string PrefsKey = "afct-sfx-lab.window";
        private const float RenderDebounceSeconds = 0.06f;
        private const float MinLeftWidth = 180f;
        private const float MaxLeftWidth = 360f;
        private const float MinRightWidth = 360f;
        private const float SplitterWidth = 6f;
        private const float MinWindowWidth = 760f;
        private const float MinWindowHeight = 560f;

        // Menu order is high to low; 44.1 kHz stays the default selection.
        private static readonly int[] SampleRates = { 48000, 44100, 22050, 11025, 8000 };
        private const int DefaultSampleRate = 44100;

        private static readonly string[] SampleRateLabels =
        {
            "48 kHz", "44.1 kHz", "22.05 kHz", "11.025 kHz", "8 kHz"
        };

        private static readonly string[] PeakLabels = { "Off", "-3 dBFS", "-6 dBFS", "-12 dBFS" };

        private static readonly double[] PeakValues =
        {
            double.NegativeInfinity, -3d, -6d, -12d
        };

        private static readonly string[] RailCategories = { "UI", "Action", "Cinematic", "Footsteps", "Utility" };

        private static readonly SfxWaveform[] Waveforms =
        {
            SfxWaveform.Square, SfxWaveform.Sawtooth, SfxWaveform.Sine, SfxWaveform.Noise
        };

        private static readonly string[] WaveformLabels = { "Square", "Sawtooth", "Sine", "Noise" };

        private static readonly SfxFootstepTerrain[] Terrains =
        {
            SfxFootstepTerrain.Snow, SfxFootstepTerrain.Grass, SfxFootstepTerrain.Dirt,
            SfxFootstepTerrain.Gravel, SfxFootstepTerrain.Mud, SfxFootstepTerrain.Wood,
            SfxFootstepTerrain.Metal, SfxFootstepTerrain.Water
        };

        private static readonly string[] TerrainLabels =
        {
            "Snow", "Grass", "Dirt", "Gravel", "Mud", "Wood", "Metal", "Water"
        };

        private static readonly string[] PresetLabels;
        private static readonly string[] PresetCategories;
        private static readonly GroupSpec[] Groups = BuildGroups();

        static SfxLabWindow()
        {
            SfxPresetId[] order = SfxPresets.Order;
            PresetLabels = new string[order.Length];
            PresetCategories = new string[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                PresetLabels[i] = SfxPresets.DisplayName(order[i]);
                PresetCategories[i] = SfxPresets.Category(order[i]);
            }
        }

        [SerializeField] private SfxPresetId _activePreset = SfxPresetId.Click;
        [SerializeField] private int _sampleRate = DefaultSampleRate;
        [SerializeField] private string _exportName = string.Empty;

        private SfxParams _params;
        private float[] _samples;
        private SfxRenderStats _stats;
        private AudioPreview _preview;
        private AudioClip _previewClip;
        private bool _playbackActive;
        private bool _rendering;
        private bool _dirty = true;
        private double _renderDue;
        private string _status = "Ready.";
        private MessageType _statusType = MessageType.Info;
        private string _search = string.Empty;
        private float _leftWidth = 228f;
        private bool _splitterDrag;
        private Vector2 _railScroll;
        private Vector2 _inspectorScroll;
        private readonly bool[] _categoryOpen = { true, true, true, true, true };
        private bool[] _groupOpen;
        private float[] _waveMin;
        private float[] _waveMax;
        private int _waveWidth = -1;
        private bool _waveDirty = true;
        private bool _trimSilence;
        private PeakTarget _peakTarget = PeakTarget.Off;

        private enum PeakTarget
        {
            Off = 0,
            Minus3 = 1,
            Minus6 = 2,
            Minus12 = 3
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("AFCT SFX Lab");
            minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            wantsMouseMove = true;
            _preview = new AudioPreview();

            if (Array.IndexOf(SampleRates, _sampleRate) < 0)
            {
                _sampleRate = DefaultSampleRate;
            }

            if (_groupOpen == null || _groupOpen.Length != Groups.Length)
            {
                _groupOpen = new bool[Groups.Length];
                for (int i = 0; i < Groups.Length; i++)
                {
                    _groupOpen[i] = Groups[i].ExpandedByDefault;
                }
            }

            if (_params == null)
            {
                _params = LoadSavedParams();
                if (_params == null)
                {
                    _params = SfxPresets.Create(_activePreset);
                }
            }

            if (string.IsNullOrEmpty(_exportName))
            {
                _exportName = SfxPresets.DisplayName(_activePreset);
            }

            _dirty = true;
            _renderDue = 0d;
            _waveDirty = true;
            Repaint();
        }

        private void OnDisable()
        {
            StopPreview();

            if (_preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }

            if (_params != null)
            {
                try
                {
                    EditorPrefs.SetString(PrefsKey, SfxLabEngine.Serialize(_params));
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void OnInspectorUpdate()
        {
            if (_playbackActive)
            {
                if (_preview == null || !_preview.IsPlaying)
                {
                    StopPreview();
                    SetStatus("Ready.", MessageType.Info);
                }

                Repaint();
            }
            else if (_dirty && EditorApplication.timeSinceStartup >= _renderDue)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (_params == null)
            {
                _params = SfxPresets.Create(_activePreset);
                _dirty = true;
                _renderDue = 0d;
            }

            HandleKeyboard();

            if (_dirty && !_rendering && EditorApplication.timeSinceStartup >= _renderDue)
            {
                RenderCurrent();
            }

            DrawToolbar();
            GUILayout.Space(2f);
            DrawBody();

            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            int presetIndex = Array.IndexOf(SfxPresets.Order, _activePreset);
            if (presetIndex < 0)
            {
                presetIndex = 0;
            }

            int nextPreset = EditorGUILayout.Popup(presetIndex, PresetLabels, EditorStyles.toolbarPopup, GUILayout.Width(170f));
            if (nextPreset != presetIndex && nextPreset >= 0 && nextPreset < SfxPresets.Order.Length)
            {
                SelectPreset(SfxPresets.Order[nextPreset]);
            }

            _search = GUILayout.TextField(_search ?? string.Empty, EditorStyles.toolbarSearchField, GUILayout.Width(160f));

            using (new EditorGUI.DisabledScope(_rendering || _samples == null || _samples.Length == 0))
            {
                GUIContent playIcon = EditorGUIUtility.IconContent("PlayButton");
                playIcon.tooltip = "Play the rendered sound (Space)";
                if (GUILayout.Button(playIcon, EditorStyles.toolbarButton, GUILayout.Width(30f)))
                {
                    PlayPreview();
                }
            }

            using (new EditorGUI.DisabledScope(!_playbackActive))
            {
                GUIContent stopIcon = EditorGUIUtility.IconContent("StopButton");
                stopIcon.tooltip = "Stop playback";
                if (GUILayout.Button(stopIcon, EditorStyles.toolbarButton, GUILayout.Width(30f)))
                {
                    StopPreview();
                    SetStatus("Ready.", MessageType.Info);
                }
            }

            using (new EditorGUI.DisabledScope(_rendering || _samples == null || _samples.Length == 0))
            {
                if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    ExportCurrent();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Sample rate", EditorStyles.miniLabel);

            int rateIndex = Array.IndexOf(SampleRates, _sampleRate);
            if (rateIndex < 0)
            {
                rateIndex = 0;
            }

            int nextRate = EditorGUILayout.Popup(rateIndex, SampleRateLabels, EditorStyles.toolbarPopup, GUILayout.Width(92f));
            if (nextRate != rateIndex)
            {
                _sampleRate = SampleRates[nextRate];
                MarkDirty();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBody()
        {
            float available = position.width - MinRightWidth - SplitterWidth - 24f;
            float maxLeft = Mathf.Max(MinLeftWidth, Mathf.Min(MaxLeftWidth, available));
            _leftWidth = Mathf.Clamp(_leftWidth, MinLeftWidth, maxLeft);

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DrawRail();
            DrawSplitter(maxLeft);
            DrawInspector();
            GUILayout.EndHorizontal();
        }

        private void DrawRail()
        {
            GUILayout.BeginVertical(GUILayout.Width(_leftWidth), GUILayout.ExpandHeight(true));
            _railScroll = GUILayout.BeginScrollView(_railScroll);

            string search = (_search ?? string.Empty).Trim();

            for (int categoryIndex = 0; categoryIndex < RailCategories.Length; categoryIndex++)
            {
                string category = RailCategories[categoryIndex];
                int count = CountPresets(category, search);
                if (count == 0)
                {
                    continue;
                }

                GUIContent header = new GUIContent(category + "  (" + count.ToString(CultureInfo.InvariantCulture) + ")");
                Rect headerRect = GUILayoutUtility.GetRect(header, EditorStyles.foldoutHeader, GUILayout.ExpandWidth(true));
                _categoryOpen[categoryIndex] = EditorGUI.Foldout(headerRect, _categoryOpen[categoryIndex], header, true, EditorStyles.foldoutHeader);

                if (_categoryOpen[categoryIndex])
                {
                    for (int i = 0; i < SfxPresets.Order.Length; i++)
                    {
                        SfxPresetId preset = SfxPresets.Order[i];
                        if (!string.Equals(PresetCategories[i], category, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (search.Length > 0 && PresetLabels[i].IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        DrawRailRow(preset, PresetLabels[i]);
                    }
                }

                GUILayout.Space(4f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawRailRow(SfxPresetId preset, string label)
        {
            bool selected = preset == _activePreset;
            Rect row = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            Event current = Event.current;

            if (current.type == EventType.Repaint)
            {
                if (selected)
                {
                    EditorGUI.DrawRect(row, SfxLabSkin.RailSelection);
                }
                else if (row.Contains(current.mousePosition))
                {
                    EditorGUI.DrawRect(row, SfxLabSkin.RailHover);
                }

                GUIStyle style = selected ? SfxLabSkin.RailRowSelected : SfxLabSkin.RailRow;
                style.Draw(row, new GUIContent(label), false, false, false, false);
            }

            if (current.type == EventType.MouseDown && current.button == 0 && row.Contains(current.mousePosition))
            {
                SelectPreset(preset);
                current.Use();
            }
        }

        private void DrawSplitter(float maxLeft)
        {
            Rect splitter = GUILayoutUtility.GetRect(SplitterWidth, SplitterWidth, GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeHorizontal);

            Event current = Event.current;

            if (current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(splitter, SfxLabSkin.Splitter);
                EditorGUI.DrawRect(new Rect(splitter.x + 2f, splitter.y, 1f, splitter.height), SfxLabSkin.SplitterLine);
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && splitter.Contains(current.mousePosition))
            {
                _splitterDrag = true;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && _splitterDrag)
            {
                _leftWidth = Mathf.Clamp(current.mousePosition.x, MinLeftWidth, maxLeft);
                current.Use();
                Repaint();
            }
            else if (current.type == EventType.MouseUp && _splitterDrag)
            {
                _splitterDrag = false;
                current.Use();
            }
        }

        private void DrawInspector()
        {
            GUILayout.BeginVertical(GUILayout.MinWidth(MinRightWidth), GUILayout.ExpandWidth(true));
            _inspectorScroll = EditorGUILayout.BeginScrollView(
                _inspectorScroll,
                false,
                false,
                GUIStyle.none,
                GUI.skin.verticalScrollbar,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            GUILayout.Label(SfxPresets.DisplayName(_activePreset), SfxLabSkin.PresetTitle);
            GUILayout.Label(BuildPresetLine(), SfxLabSkin.PresetLine);
            GUILayout.Space(6f);

            DrawEnginePopup();
            DrawParameterGroups();
            GUILayout.Space(10f);
            DrawWaveform();
            GUILayout.Space(6f);
            DrawReadouts();
            GUILayout.Space(4f);
            EditorGUILayout.HelpBox(_status, _statusType);
            GUILayout.Space(10f);
            DrawExportBlock();

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawEnginePopup()
        {
            if (_params.engine == SfxEngine.Sfxr)
            {
                int waveformIndex = Array.IndexOf(Waveforms, _params.waveform);
                if (waveformIndex < 0)
                {
                    waveformIndex = 0;
                }

                int nextWaveform = EditorGUILayout.Popup("Waveform", waveformIndex, WaveformLabels);
                if (nextWaveform != waveformIndex && nextWaveform >= 0)
                {
                    _params.waveform = Waveforms[nextWaveform];
                    MarkDirty();
                }
            }
            else
            {
                int terrainIndex = Array.IndexOf(Terrains, _params.footstepTerrain);
                if (terrainIndex < 0)
                {
                    terrainIndex = 0;
                }

                int nextTerrain = EditorGUILayout.Popup("Terrain", terrainIndex, TerrainLabels);
                if (nextTerrain != terrainIndex && nextTerrain >= 0)
                {
                    _params.footstepTerrain = Terrains[nextTerrain];
                    MarkDirty();
                }
            }
        }

        private void DrawParameterGroups()
        {
            for (int groupIndex = 0; groupIndex < Groups.Length; groupIndex++)
            {
                GroupSpec group = Groups[groupIndex];
                _groupOpen[groupIndex] = EditorGUILayout.Foldout(_groupOpen[groupIndex], group.Title, true);
                if (!_groupOpen[groupIndex])
                {
                    continue;
                }

                EditorGUI.indentLevel++;
                for (int i = 0; i < group.Parameters.Length; i++)
                {
                    DrawParameter(group.Parameters[i]);
                }

                EditorGUI.indentLevel--;
                GUILayout.Space(2f);
            }
        }

        private void DrawParameter(ParameterSpec parameter)
        {
            double value = parameter.Get(_params);
            EditorGUI.BeginChangeCheck();
            float next = EditorGUILayout.Slider(parameter.Label, (float)value, parameter.Min, parameter.Max);
            if (EditorGUI.EndChangeCheck())
            {
                parameter.Set(_params, next);
                MarkDirty();
            }
        }

        private void DrawWaveform()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 88f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(rect, SfxLabSkin.WaveBackground);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), SfxLabSkin.WaveBorder);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), SfxLabSkin.WaveBorder);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), SfxLabSkin.WaveBorder);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), SfxLabSkin.WaveBorder);

            float middle = rect.y + rect.height * 0.5f;
            EditorGUI.DrawRect(new Rect(rect.x + 1f, middle, Mathf.Max(0f, rect.width - 2f), 1f), SfxLabSkin.WaveCentre);

            if (_samples == null || _samples.Length == 0)
            {
                GUI.Label(rect, "No audio", SfxLabSkin.WaveEmpty);
                return;
            }

            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width) - 2);
            EnsureWaveCache(width);

            float half = Mathf.Max(1f, (rect.height - 8f) * 0.5f);
            Color column = SfxLabSkin.WaveColumn;

            for (int x = 0; x < _waveWidth; x++)
            {
                float min = _waveMin[x];
                float max = _waveMax[x];
                float top = middle - max * half;
                float bottom = middle - min * half;
                float height = Mathf.Max(1f, bottom - top);
                EditorGUI.DrawRect(new Rect(rect.x + 1f + x, top, 1f, height), column);
            }
        }

        private void EnsureWaveCache(int width)
        {
            if (!_waveDirty && _waveWidth == width && _waveMin != null && _waveMax != null)
            {
                return;
            }

            _waveWidth = width;
            _waveDirty = false;

            if (_waveMin == null || _waveMin.Length != width)
            {
                _waveMin = new float[width];
                _waveMax = new float[width];
            }

            for (int x = 0; x < width; x++)
            {
                _waveMin[x] = 0f;
                _waveMax[x] = 0f;
            }

            if (_samples == null || _samples.Length == 0)
            {
                return;
            }

            float peak = _stats != null ? (float)_stats.Peak : 0f;
            if (peak <= 0.0001f)
            {
                peak = 0.0001f;
            }

            int sampleCount = _samples.Length;
            for (int x = 0; x < width; x++)
            {
                int start = (int)((long)x * sampleCount / width);
                int end = (int)((long)(x + 1) * sampleCount / width);
                if (end <= start)
                {
                    end = Mathf.Min(sampleCount, start + 1);
                }

                float min = _samples[start];
                float max = _samples[start];
                for (int i = start; i < end; i++)
                {
                    float sample = _samples[i];
                    if (sample < min)
                    {
                        min = sample;
                    }

                    if (sample > max)
                    {
                        max = sample;
                    }
                }

                _waveMin[x] = Mathf.Clamp(min / peak, -1f, 1f);
                _waveMax[x] = Mathf.Clamp(max / peak, -1f, 1f);
            }
        }

        private void DrawReadouts()
        {
            EditorGUILayout.BeginHorizontal();
            DrawReadout("Duration", _stats != null ? _stats.DurationSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " s" : "0.00 s");
            DrawReadout("Samples", _stats != null ? _stats.Samples.ToString("N0", CultureInfo.InvariantCulture) : "0");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawReadout("Peak", _stats != null ? _stats.Peak.ToString("0.00", CultureInfo.InvariantCulture) : "0.00");
            DrawReadout("Clipped", _stats != null ? _stats.ClippedSamples.ToString("N0", CultureInfo.InvariantCulture) : "0");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawReadout(string label, string value)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(120f));
            GUILayout.Label(label, SfxLabSkin.ReadoutLabel);
            GUILayout.Label(value, SfxLabSkin.ReadoutValue);
            EditorGUILayout.EndVertical();
        }

        private void DrawExportBlock()
        {
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
            GUILayout.Label("Writes a 16-bit WAV into the project.", EditorStyles.miniLabel);
            GUILayout.Space(2f);

            _exportName = EditorGUILayout.TextField("File name", _exportName ?? string.Empty);
            _trimSilence = EditorGUILayout.Toggle("Trim silence", _trimSilence);
            _peakTarget = (PeakTarget)EditorGUILayout.EnumPopup("Peak target", _peakTarget);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Folder");
            if (GUILayout.Button(WavExport.ExportFolder(_activePreset) + "/", SfxLabSkin.FolderField, GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                RevealExportFolder();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(_rendering || _samples == null || _samples.Length == 0 || string.IsNullOrEmpty(_exportName)))
            {
                if (GUILayout.Button("Export", GUILayout.Height(24f)))
                {
                    ExportCurrent();
                }
            }
        }

        private void HandleKeyboard()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown || GUIUtility.keyboardControl != 0)
            {
                return;
            }

            if (current.keyCode == KeyCode.Space)
            {
                PlayPreview();
                current.Use();
            }
            else if (current.keyCode == KeyCode.R)
            {
                SelectPreset(SfxPresetId.Random);
                current.Use();
            }
        }

        private void SelectPreset(SfxPresetId preset)
        {
            _activePreset = preset;
            _params = SfxPresets.Create(preset);
            _exportName = SfxPresets.DisplayName(preset);
            _waveDirty = true;
            _dirty = true;
            _renderDue = 0d;
            StopPreview();
            RenderCurrent();
            Repaint();
        }

        private void RenderCurrent()
        {
            if (_params == null)
            {
                return;
            }

            _dirty = false;

            if (_rendering)
            {
                return;
            }

            _rendering = true;
            StopPreview();
            SetStatus("Rendering...", MessageType.Info);

            try
            {
                SfxRenderOptions options = new SfxRenderOptions
                {
                    SampleRate = _sampleRate,
                    BitDepth = 16
                };

                SfxRenderResult result = SfxLabEngine.Render(_params, options);
                _samples = result.Samples;
                _stats = result.Stats;
                _waveDirty = true;
                SetStatus("Ready.", MessageType.Info);
            }
            catch (Exception exception)
            {
                _samples = null;
                _stats = null;
                _waveDirty = true;
                SetStatus("Render failed: " + exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
            finally
            {
                _rendering = false;
                Repaint();
            }
        }

        private void PlayPreview()
        {
            if (_rendering)
            {
                return;
            }

            RenderCurrent();

            if (_samples == null || _samples.Length == 0)
            {
                SetStatus("Preview failed: there is no rendered audio.", MessageType.Warning);
                return;
            }

            StopPreview();

            if (_preview == null)
            {
                _preview = new AudioPreview();
            }

            AudioClip clip = AudioClip.Create("AFCT SFX Lab Preview", _samples.Length, 1, _sampleRate, false);
            if (!clip.SetData(_samples, 0))
            {
                DestroyImmediate(clip);
                SetStatus("Preview failed: the clip could not be filled.", MessageType.Error);
                return;
            }

            clip.hideFlags = HideFlags.HideAndDontSave;
            _previewClip = clip;
            _preview.Play(clip);
            _playbackActive = true;
            SetStatus("Playing.", MessageType.Info);
            Repaint();
        }

        private void StopPreview()
        {
            if (_preview != null)
            {
                _preview.Stop();
            }

            if (_previewClip != null)
            {
                DestroyImmediate(_previewClip);
                _previewClip = null;
            }

            if (_playbackActive)
            {
                _playbackActive = false;
                Repaint();
            }
        }

        private void ExportCurrent()
        {
            if (_rendering)
            {
                return;
            }

            RenderCurrent();

            if (_samples == null || _samples.Length == 0)
            {
                SetStatus("Export failed: there is no rendered audio.", MessageType.Error);
                return;
            }

            try
            {
                double targetPeak = PeakValues[(int)_peakTarget];
                string assetPath = WavExport.Export(_activePreset, _exportName, _samples, _sampleRate, _trimSilence, targetPeak);
                SetStatus("Exported to " + WavExport.ExportFolder(_activePreset) + "/.", MessageType.Info);
                Debug.Log("AFCT SFX Lab: exported " + assetPath);
            }
            catch (Exception exception)
            {
                SetStatus("Export failed: " + exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        private void RevealExportFolder()
        {
            string relative = WavExport.ExportFolder(_activePreset);
            string absolute = System.IO.Path.GetFullPath(relative);
            if (!System.IO.Directory.Exists(absolute))
            {
                absolute = System.IO.Path.GetFullPath(".");
            }

            EditorUtility.RevealInFinder(absolute);
        }

        private void MarkDirty()
        {
            _dirty = true;
            _renderDue = EditorApplication.timeSinceStartup + RenderDebounceSeconds;
            Repaint();
        }

        private void SetStatus(string message, MessageType type)
        {
            _status = message;
            _statusType = type;
        }

        private string BuildPresetLine()
        {
            string category = SfxPresets.Category(_activePreset);
            if (_params.engine == SfxEngine.Footsteppr)
            {
                return category + " preset, footsteppr engine, " + TerrainLabels[Array.IndexOf(Terrains, _params.footstepTerrain)].ToLowerInvariant() + " terrain.";
            }

            return category + " preset, sfxr engine, " + WaveformLabels[Array.IndexOf(Waveforms, _params.waveform)].ToLowerInvariant() + ".";
        }

        private static int CountPresets(string category, string search)
        {
            int count = 0;
            for (int i = 0; i < SfxPresets.Order.Length; i++)
            {
                if (!string.Equals(PresetCategories[i], category, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (search.Length > 0 && PresetLabels[i].IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private static SfxParams LoadSavedParams()
        {
            string json = EditorPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return SfxLabEngine.Clamp(SfxLabEngine.Deserialize(json));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static GroupSpec[] BuildGroups()
        {
            return new[]
            {
                new GroupSpec("Envelope", true,
                    Unit("Attack", p => p.attack, (p, v) => p.attack = v),
                    Unit("Sustain", p => p.sustain, (p, v) => p.sustain = v),
                    Unit("Sustain punch", p => p.sustainPunch, (p, v) => p.sustainPunch = v),
                    Unit("Decay", p => p.decay, (p, v) => p.decay = v)),
                new GroupSpec("Frequency", true,
                    Unit("Start", p => p.startFrequency, (p, v) => p.startFrequency = v),
                    Unit("Min", p => p.minFrequency, (p, v) => p.minFrequency = v),
                    Signed("Slide", p => p.slide, (p, v) => p.slide = v),
                    Signed("Delta slide", p => p.deltaSlide, (p, v) => p.deltaSlide = v)),
                new GroupSpec("Modulation", false,
                    Unit("Vibrato depth", p => p.vibratoDepth, (p, v) => p.vibratoDepth = v),
                    Unit("Vibrato speed", p => p.vibratoSpeed, (p, v) => p.vibratoSpeed = v),
                    Signed("Change amount", p => p.changeAmount, (p, v) => p.changeAmount = v),
                    Unit("Change speed", p => p.changeSpeed, (p, v) => p.changeSpeed = v)),
                new GroupSpec("Texture", false,
                    Unit("Square duty", p => p.squareDuty, (p, v) => p.squareDuty = v),
                    Signed("Duty sweep", p => p.dutySweep, (p, v) => p.dutySweep = v),
                    Unit("Repeat speed", p => p.repeatSpeed, (p, v) => p.repeatSpeed = v),
                    Signed("Phaser offset", p => p.phaserOffset, (p, v) => p.phaserOffset = v),
                    Signed("Phaser sweep", p => p.phaserSweep, (p, v) => p.phaserSweep = v)),
                new GroupSpec("Filter", false,
                    Unit("Low pass", p => p.lowPassCutoff, (p, v) => p.lowPassCutoff = v),
                    Signed("Low pass sweep", p => p.lowPassSweep, (p, v) => p.lowPassSweep = v),
                    Unit("Low pass resonance", p => p.lowPassResonance, (p, v) => p.lowPassResonance = v),
                    Unit("High pass", p => p.highPassCutoff, (p, v) => p.highPassCutoff = v),
                    Signed("High pass sweep", p => p.highPassSweep, (p, v) => p.highPassSweep = v),
                    Unit("Master volume", p => p.masterVolume, (p, v) => p.masterVolume = v)),
                new GroupSpec("FX rack", false,
                    Unit("Drive", p => p.fxDrive, (p, v) => p.fxDrive = v),
                    Unit("Crush rate", p => p.fxCrushRate, (p, v) => p.fxCrushRate = v),
                    Unit("Crush bits", p => p.fxCrushBits, (p, v) => p.fxCrushBits = v),
                    Signed("Tone", p => p.fxTone, (p, v) => p.fxTone = v),
                    Unit("Delay time", p => p.fxDelayTime, (p, v) => p.fxDelayTime = v),
                    Unit("Delay feedback", p => p.fxDelayFeedback, (p, v) => p.fxDelayFeedback = v),
                    Unit("Delay mix", p => p.fxDelayMix, (p, v) => p.fxDelayMix = v),
                    Unit("Reverb size", p => p.fxReverbSize, (p, v) => p.fxReverbSize = v),
                    Unit("Reverb mix", p => p.fxReverbMix, (p, v) => p.fxReverbMix = v))
            };
        }

        private static ParameterSpec Unit(string label, Func<SfxParams, double> get, Action<SfxParams, double> set)
        {
            return new ParameterSpec(label, get, set, 0f, 1f);
        }

        private static ParameterSpec Signed(string label, Func<SfxParams, double> get, Action<SfxParams, double> set)
        {
            return new ParameterSpec(label, get, set, -1f, 1f);
        }

        private sealed class ParameterSpec
        {
            public readonly string Label;
            public readonly Func<SfxParams, double> Get;
            public readonly Action<SfxParams, double> Set;
            public readonly float Min;
            public readonly float Max;

            public ParameterSpec(string label, Func<SfxParams, double> get, Action<SfxParams, double> set, float min, float max)
            {
                Label = label;
                Get = get;
                Set = set;
                Min = min;
                Max = max;
            }
        }

        private sealed class GroupSpec
        {
            public readonly string Title;
            public readonly bool ExpandedByDefault;
            public readonly ParameterSpec[] Parameters;

            public GroupSpec(string title, bool expandedByDefault, params ParameterSpec[] parameters)
            {
                Title = title;
                ExpandedByDefault = expandedByDefault;
                Parameters = parameters;
            }
        }
    }
}
