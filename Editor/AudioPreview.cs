using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AFCT.SfxLab.EditorTools
{
    /// <summary>
    /// Preview playback for a single generated clip. The host GameObject exists only while a
    /// preview is playing, is hidden from the hierarchy and excluded from saving, and is moved
    /// into whichever scene is active so the AudioSource is audible.
    /// </summary>
    internal sealed class AudioPreview : IDisposable
    {
        private const string HostName = "AFCT SFX Lab Preview";

        private GameObject _host;
        private AudioSource _source;

        public bool IsPlaying
        {
            get { return _source != null && _source.isPlaying; }
        }

        public void Play(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            Stop();

            _host = new GameObject(HostName);
            _host.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(_host, SceneManager.GetActiveScene());

            _source = _host.AddComponent<AudioSource>();
            _source.hideFlags = HideFlags.HideAndDontSave;
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = 1f;
            _source.clip = clip;
            _source.Play();
        }

        public void Stop()
        {
            if (_source != null)
            {
                if (_source.isPlaying)
                {
                    _source.Stop();
                }

                _source.clip = null;
            }

            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            _host = null;
            _source = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
