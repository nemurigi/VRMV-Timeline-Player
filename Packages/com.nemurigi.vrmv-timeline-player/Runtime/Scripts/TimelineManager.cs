using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.Playables;
using VRC.SDKBase;
using VRC.Udon;

namespace NmrgLibrary.TimelinePlayer
{
    public enum TimelinePlayState
    {
        Stopped,
        Playing,
        Paused,
    }
    public class TimelineManager : AbstractTimelineManager
    {
        [SerializeField] private PlayableDirector _director;
        private UdonSharpBehaviour _receiver;

        private TimelinePlayState _state;
        private bool _initialized = false;

        void Update()
        {
            if (!_initialized)
                return;
            
            var time = _director.time;
            if (_director.extrapolationMode != DirectorWrapMode.Loop)
            {
                if (Mathf.Approximately((float) time, GetDuration()))
                {
                    OnMediaEnd();
                }
            };
        }

        public override void Initialize(GameObject mediaObj, UdonSharpBehaviour receiver)
        {
            if (_initialized)
                return;

            if (_receiver == null)
            {
                _receiver = receiver;
            }
            
            _director = mediaObj.GetComponent<PlayableDirector>();
            if (_director == null)
                return;
            
            _initialized = true;
        }

        public override void OnMediaStart()
        {
            _state = TimelinePlayState.Playing;
            _receiver.SendCustomEvent(nameof(VRMVTimelinePlayer.OnMediaStart));
        }

        public override void OnMediaEnd()
        {
            _state = TimelinePlayState.Stopped;
            _receiver.SendCustomEvent(nameof(VRMVTimelinePlayer.OnMediaEnd));
        }
        
        public override bool IsPlaying() => (_state == TimelinePlayState.Playing);
        
        public override bool IsStopped() => (_state == TimelinePlayState.Stopped);
        
        public override bool IsPaused() => (_state == TimelinePlayState.Paused);
        
        public override float GetDuration() => (float) _director.duration;
        
        public override float GetTime() => (float) _director.time;
        
        public override void SetTime(float time)
        {
            if (time < 0)
            {
                _director.time = 0;
            }
            else if (_director.duration <= time)
            {
                _director.time = _director.duration;
            }
            else
            {
                _director.time = time;
            }
            
            _director.Evaluate();

            if (_state == TimelinePlayState.Stopped)
            {
                _state = TimelinePlayState.Paused;
            }
        }

        public float GetNormalizeTime() => (float)(_director.time / _director.duration);

        public void SetNormalizeTime(float normalizeTime) => _director.time = _director.duration * normalizeTime; 

        public override void Play()
        {
            if (_state == TimelinePlayState.Playing) return;

            switch (_state)
            {
                case TimelinePlayState.Stopped:
                    break;
                case TimelinePlayState.Playing:
                    break;
                case TimelinePlayState.Paused:
                    _director.Evaluate();
                    break;
            }
            
            _state = TimelinePlayState.Playing;
            _director.Play();
        }
        public override void Pause()
        {
            if (_state != TimelinePlayState.Playing) return;
            _state = TimelinePlayState.Paused;

            var timeCache = _director.time;
            _director.Stop();
            _director.time = timeCache;
        }
    }
}
