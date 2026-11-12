using UnityEditor;
using UnityEngine;

namespace AFCT.SfxLab.EditorTools
{
    /// <summary>
    /// Cached styles for the SFX Lab window. Controls that only need to look like a
    /// built-in inspector use EditorStyles directly; these are the few extra roles the
    /// window needs in both the Pro and the Light skin.
    /// </summary>
    internal static class SfxLabSkin
    {
        private static GUIStyle _presetTitle;
        private static GUIStyle _presetLine;
        private static GUIStyle _railRow;
        private static GUIStyle _railRowSelected;
        private static GUIStyle _readoutLabel;
        private static GUIStyle _readoutValue;
        private static GUIStyle _waveEmpty;
        private static GUIStyle _folderField;

        public static GUIStyle PresetTitle
        {
            get
            {
                if (_presetTitle == null)
                {
                    _presetTitle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 15,
                        wordWrap = true
                    };
                }

                return _presetTitle;
            }
        }

        public static GUIStyle PresetLine
        {
            get
            {
                if (_presetLine == null)
                {
                    _presetLine = new GUIStyle(EditorStyles.miniLabel)
                    {
                        wordWrap = true
                    };
                }

                return _presetLine;
            }
        }

        public static GUIStyle RailRow
        {
            get
            {
                if (_railRow == null)
                {
                    _railRow = new GUIStyle(EditorStyles.label)
                    {
                        fixedHeight = 18f,
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        padding = new RectOffset(16, 4, 0, 0)
                    };
                }

                return _railRow;
            }
        }

        public static GUIStyle RailRowSelected
        {
            get
            {
                if (_railRowSelected == null)
                {
                    _railRowSelected = new GUIStyle(EditorStyles.whiteLabel)
                    {
                        fixedHeight = 18f,
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        padding = new RectOffset(16, 4, 0, 0)
                    };
                }

                return _railRowSelected;
            }
        }

        public static GUIStyle ReadoutLabel
        {
            get
            {
                if (_readoutLabel == null)
                {
                    _readoutLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return _readoutLabel;
            }
        }

        public static GUIStyle ReadoutValue
        {
            get
            {
                if (_readoutValue == null)
                {
                    _readoutValue = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return _readoutValue;
            }
        }

        public static GUIStyle WaveEmpty
        {
            get
            {
                if (_waveEmpty == null)
                {
                    _waveEmpty = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                }

                _waveEmpty.normal.textColor = EditorGUIUtility.isProSkin
                    ? new Color(0.62f, 0.62f, 0.62f)
                    : new Color(0.38f, 0.38f, 0.38f);
                return _waveEmpty;
            }
        }

        public static GUIStyle FolderField
        {
            get
            {
                if (_folderField == null)
                {
                    _folderField = new GUIStyle(EditorStyles.textField)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        clipping = TextClipping.Clip,
                        padding = new RectOffset(6, 4, 0, 0)
                    };
                }

                return _folderField;
            }
        }

        /// <summary>Row highlight, close to the Project window selection in both skins.</summary>
        public static Color RailSelection
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.172f, 0.365f, 0.529f)
                    : new Color(0.243f, 0.490f, 0.902f);
            }
        }

        public static Color RailHover
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.045f)
                    : new Color(0f, 0f, 0f, 0.06f);
            }
        }

        public static Color Splitter
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.13f, 0.13f, 0.13f)
                    : new Color(0.74f, 0.74f, 0.74f);
            }
        }

        public static Color SplitterLine
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.06f, 0.06f, 0.06f)
                    : new Color(0.55f, 0.55f, 0.55f);
            }
        }

        public static Color WaveBackground
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.105f, 0.105f, 0.112f)
                    : new Color(0.82f, 0.82f, 0.82f);
            }
        }

        public static Color WaveBorder
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.09f)
                    : new Color(0f, 0f, 0f, 0.16f);
            }
        }

        public static Color WaveCentre
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.16f)
                    : new Color(0f, 0f, 0f, 0.22f);
            }
        }

        public static Color WaveColumn
        {
            get
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.88f, 0.88f, 0.86f, 0.92f)
                    : new Color(0.22f, 0.24f, 0.26f, 0.92f);
            }
        }
    }
}
