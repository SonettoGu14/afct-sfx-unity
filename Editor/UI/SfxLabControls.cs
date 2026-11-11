using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AFCT.SfxLab.EditorTools
{
    /// <summary>
    /// Hairline range slider drawn on paper: a one pixel track, a one pixel fill and a square
    /// ink thumb. No rounded caps, no glow.
    /// </summary>
    internal sealed class RangeSlider : VisualElement
    {
        private readonly VisualElement _fill;
        private readonly VisualElement _thumb;
        private readonly double _min;
        private readonly double _max;
        private double _value;
        private bool _dragging;

        public event Action<double> ValueChanged;

        public double Value
        {
            get { return _value; }
            set { SetValue(value, false); }
        }

        public RangeSlider(double min, double max, double value)
        {
            _min = min;
            _max = max;

            AddToClassList("range");
            pickingMode = PickingMode.Position;

            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("range__track");
            Add(track);

            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList("range__fill");
            Add(_fill);

            _thumb = new VisualElement { pickingMode = PickingMode.Ignore };
            _thumb.AddToClassList("range__thumb");
            Add(_thumb);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);

            SetValue(value, false);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            _dragging = true;
            this.CapturePointer(evt.pointerId);
            ApplyPointer(evt.position);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging || !this.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            ApplyPointer(evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            if (this.HasPointerCapture(evt.pointerId))
            {
                this.ReleasePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt)
        {
            _dragging = false;
        }

        private void ApplyPointer(Vector2 worldPosition)
        {
            float width = layout.width;
            if (width <= 0f)
            {
                return;
            }

            Vector2 local = this.WorldToLocal(worldPosition);
            float t = Mathf.Clamp01(local.x / width);
            SetValue(_min + t * (_max - _min), true);
        }

        private void SetValue(double value, bool notify)
        {
            _value = Math.Max(_min, Math.Min(_max, value));
            float t = _max > _min ? (float)((_value - _min) / (_max - _min)) : 0f;

            _fill.style.width = Length.Percent(t * 100f);
            _thumb.style.left = Length.Percent(t * 100f);

            if (notify && ValueChanged != null)
            {
                ValueChanged(_value);
            }
        }
    }

    /// <summary>
    /// Waveform monitor. Draws the sample envelope as one pixel columns on the dark screen,
    /// with a centre rule and pressed peak guides.
    /// </summary>
    internal sealed class WaveformView : VisualElement
    {
        private static readonly Color ScreenLine = new Color32(232, 228, 217, 255);
        private static readonly Color PeakGuide = new Color32(211, 64, 29, 255);

        private float[] _samples;

        public WaveformView()
        {
            AddToClassList("waveform");
            generateVisualContent += Draw;
        }

        public void SetSamples(float[] samples)
        {
            _samples = samples;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width < 4f || rect.height < 4f)
            {
                return;
            }

            var painter = context.painter2D;
            float mid = Mathf.Round(rect.height * 0.5f) + 0.5f;
            float half = rect.height * 0.42f;

            painter.strokeColor = new Color(ScreenLine.r, ScreenLine.g, ScreenLine.b, 0.24f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, mid));
            painter.LineTo(new Vector2(rect.xMax, mid));
            painter.Stroke();

            painter.strokeColor = new Color(PeakGuide.r, PeakGuide.g, PeakGuide.b, 0.55f);
            painter.BeginPath();
            float topGuide = mid - half;
            float bottomGuide = mid + half;
            for (float x = rect.xMin; x < rect.xMax; x += 10f)
            {
                float end = Mathf.Min(x + 4f, rect.xMax);
                painter.MoveTo(new Vector2(x, topGuide));
                painter.LineTo(new Vector2(end, topGuide));
                painter.MoveTo(new Vector2(x, bottomGuide));
                painter.LineTo(new Vector2(end, bottomGuide));
            }

            painter.Stroke();

            float[] samples = _samples;
            if (samples == null || samples.Length == 0)
            {
                return;
            }

            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float sample = Mathf.Abs(samples[i]);
                if (sample > peak)
                {
                    peak = sample;
                }
            }

            if (peak <= 0f)
            {
                return;
            }

            float gain = half / peak;
            int columns = Mathf.Max(1, Mathf.FloorToInt(rect.width));

            painter.strokeColor = new Color(ScreenLine.r, ScreenLine.g, ScreenLine.b, 0.92f);
            painter.lineWidth = 1f;
            painter.BeginPath();

            for (int x = 0; x < columns; x++)
            {
                int start = (int)((long)x * samples.Length / columns);
                int end = (int)((long)(x + 1) * samples.Length / columns);
                if (end <= start)
                {
                    end = start + 1;
                }

                if (end > samples.Length)
                {
                    end = samples.Length;
                }

                float sampleMin = 0f;
                float sampleMax = 0f;
                for (int i = start; i < end; i++)
                {
                    float sample = samples[i];
                    if (sample < sampleMin)
                    {
                        sampleMin = sample;
                    }

                    if (sample > sampleMax)
                    {
                        sampleMax = sample;
                    }
                }

                float columnTop = mid - sampleMax * gain;
                float columnBottom = mid - sampleMin * gain;
                if (columnBottom - columnTop < 1f)
                {
                    columnBottom = columnTop + 1f;
                }

                painter.MoveTo(new Vector2(rect.xMin + x + 0.5f, columnTop));
                painter.LineTo(new Vector2(rect.xMin + x + 0.5f, columnBottom));
            }

            painter.Stroke();
        }
    }

    /// <summary>
    /// Flat paper select. Opens an editor menu instead of the rounded built-in popup so the
    /// control stays on style.
    /// </summary>
    internal sealed class PaperSelect : VisualElement
    {
        private readonly string[] _options;
        private readonly Label _value;
        private int _index;

        public event Action<int> Changed;

        public int Index
        {
            get { return _index; }
        }

        public PaperSelect(string[] options, int index)
        {
            _options = options;
            _index = Mathf.Clamp(index, 0, options.Length - 1);

            AddToClassList("select");
            pickingMode = PickingMode.Position;

            _value = new Label(_options[_index]) { pickingMode = PickingMode.Ignore };
            _value.AddToClassList("select__value");
            Add(_value);

            var chevron = new Chevron { pickingMode = PickingMode.Ignore };
            Add(chevron);

            RegisterCallback<ClickEvent>(OnClick);
        }

        public void SetIndex(int index, bool notify)
        {
            int next = Mathf.Clamp(index, 0, _options.Length - 1);
            _index = next;
            _value.text = _options[next];

            if (notify && Changed != null)
            {
                Changed(_index);
            }
        }

        private void OnClick(ClickEvent evt)
        {
            var menu = new GenericMenu();
            for (int i = 0; i < _options.Length; i++)
            {
                int captured = i;
                menu.AddItem(new GUIContent(_options[i]), i == _index, () => SetIndex(captured, true));
            }

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private sealed class Chevron : VisualElement
        {
            public Chevron()
            {
                AddToClassList("select__chevron");
                generateVisualContent += DrawChevron;
            }

            private void DrawChevron(MeshGenerationContext context)
            {
                Rect rect = contentRect;
                if (rect.width < 4f || rect.height < 4f)
                {
                    return;
                }

                var painter = context.painter2D;
                painter.strokeColor = new Color32(107, 108, 104, 255);
                painter.lineWidth = 1f;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.MoveTo(new Vector2(rect.xMin + rect.width * 0.2f, rect.yMin + rect.height * 0.38f));
                painter.LineTo(new Vector2(rect.xMin + rect.width * 0.5f, rect.yMin + rect.height * 0.62f));
                painter.LineTo(new Vector2(rect.xMin + rect.width * 0.8f, rect.yMin + rect.height * 0.38f));
                painter.Stroke();
            }
        }
    }

    /// <summary>
    /// Square paper checkbox. The box fills with ink when set.
    /// </summary>
    internal sealed class PaperCheck : VisualElement
    {
        private bool _value;

        public event Action<bool> Changed;

        public bool Value
        {
            get { return _value; }
        }

        public PaperCheck(string label, bool value)
        {
            AddToClassList("check");
            pickingMode = PickingMode.Position;

            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList("check__box");
            Add(box);

            if (!string.IsNullOrEmpty(label))
            {
                var text = new Label(label) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("check__label");
                Add(text);
            }

            RegisterCallback<ClickEvent>(_ => SetValue(!_value, true));
            SetValue(value, false);
        }

        public void SetValue(bool value, bool notify)
        {
            _value = value;
            EnableInClassList("is-on", value);

            if (notify && Changed != null)
            {
                Changed(value);
            }
        }
    }
}
