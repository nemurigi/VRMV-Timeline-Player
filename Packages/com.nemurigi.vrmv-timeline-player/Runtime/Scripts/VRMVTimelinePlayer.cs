
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Playables;
using VRC.SDKBase;
using VRC.Udon;

namespace NmrgLibrary.TimelinePlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VRMVTimelinePlayer : UdonSharpBehaviour
    {
        public GameObject timelineObj;
        public TimelineManager timelineManager;
        public float syncThreshold = 0.5f;
        
        [UdonSynced]
        private int _networkTimeVideoStart;
        private int _localNetworkTimeStart;
        
        // [UdonSynced]
        // private bool _ownerPlaying;

        [UdonSynced] private bool _ownerPaused;
        private bool _locallyPaused = true;

        [UdonSynced]
        private bool _ownerPlaying;
        private bool _locallyPlaying;
        
        private float _lastCurrentTime;
        private float _lastTimelineTime;
        private float _videoTargetStartTime;

        private TimelineControlHandler[] _registeredControlHandlers;
        private UdonSharpBehaviour[] _registeredCallbackReceivers;
        void Start()
        {
            if (timelineManager == null)
            {
                timelineManager = GetMediaManager();
            }
            
            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new TimelineControlHandler[0];

            if (_registeredCallbackReceivers == null)
                _registeredCallbackReceivers = new UdonSharpBehaviour[0];
            
            timelineManager.Initialize(timelineObj, (UdonSharpBehaviour)this);
        }
        
        public void OnMediaStart()
        {
            if (Networking.IsOwner(gameObject))
            {
                SetPausedInternal(false, false);
                _networkTimeVideoStart = Networking.GetServerTimeInMilliseconds() - (int)(_videoTargetStartTime * 1000f);
                _ownerPlaying = true;
                QueueSerialize();
            }
            else if (!_ownerPlaying)
            {
                timelineManager.Pause();
            }
            else
            {
                SetPausedInternal(_ownerPaused, false);
                SyncTimeline();
            }

            SetUIPaused(_locallyPaused);
        }

        public void OnMediaEnd()
        {
            if (Networking.IsOwner(gameObject))
            {
                _ownerPlaying = false;
                _ownerPaused = _locallyPaused = false;
                SetUIPaused(false);
                QueueSerialize();
            }
        }

        [PublicAPI]
        public void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject))
                return;
            
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject))
                return;
            
            SetPausedInternal(_ownerPaused, false);
            // SetLoopingInternal(_loopVideo);
            
            if (!_ownerPlaying && timelineManager.IsPlaying())
                timelineManager.Pause();

            if (_networkTimeVideoStart != _localNetworkTimeStart)
            {
                _localNetworkTimeStart = _networkTimeVideoStart;
                SyncTimeline();
            }

            if (!_locallyPaused)
            {
                float duration = GetMediaManager().GetDuration();
                
                if((Networking.GetServerTimeInMilliseconds() - _networkTimeVideoStart) / 1000f < duration -3f)
                    timelineManager.Play();
            }
        }
        
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            SendUIOwnerUpdate();
            // SendCallback("OnUSharpVideoOwnershipChange");
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!player.isLocal)
                QueueSerialize();
        }

        public void SyncTimeline()
        {
            float offsetTime = Mathf.Clamp((Networking.GetServerTimeInMilliseconds() - _networkTimeVideoStart) / 1000f,
                0f, timelineManager.GetDuration());

            if (Mathf.Abs(timelineManager.GetTime()) > syncThreshold)
            {
                timelineManager.SetTime(offsetTime);
            }
        }
        
        private void SetPausedInternal(bool paused, bool updatePauseTime)
        {
            if (Networking.IsOwner(gameObject))
                _ownerPaused = paused;

            if (_locallyPaused != paused)
            {
                _locallyPaused = paused;
                
                if (_ownerPaused)
                    timelineManager.Pause();
                else
                {
                    timelineManager.Play();
                    if (updatePauseTime)
                        _videoTargetStartTime = _lastCurrentTime;
                }

                SetUIPaused(paused);

                // if (_locallyPaused)
                //     SendCallback("OnUSharpVideoPause");
                // else
                //     SendCallback("OnUSharpVideoUnpause");
            }

            QueueSerialize();
        }
        
        [PublicAPI]
        public bool IsPaused()
        {
            return _ownerPaused;
        }
        
        [PublicAPI]
        public void SeekTo(float progress)
        {
            if (!Networking.IsOwner(gameObject))
                return;

            float newTargetTime = timelineManager.GetDuration() * progress;
            _lastTimelineTime = newTargetTime;
            _lastCurrentTime = newTargetTime;
            
            _localNetworkTimeStart = Networking.GetServerTimeInMilliseconds() - (int)(newTargetTime * 1000f);
            _networkTimeVideoStart = _localNetworkTimeStart;

            SyncTimeline();
            QueueSerialize();
        }
        
        [PublicAPI]
        public void SetPaused(bool paused)
        {
            if (Networking.IsOwner(gameObject))
                SetPausedInternal(paused, true);
        }
        
        [PublicAPI]
        public void QueueSerialize()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            RequestSerialization();
        }
        
        public TimelineManager GetMediaManager()
        {
            if (timelineManager)
                return timelineManager;

            timelineManager = GetComponentInChildren<TimelineManager>(true);
            if (timelineManager == null)
                LogError("Video Player Manager not found, make sure you have a manager setup properly");

            return timelineManager;
        }

#region UI Control handling
        public void RegisterControlHandler(TimelineControlHandler newControlHandler)
        {
            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new TimelineControlHandler[0];

            foreach (TimelineControlHandler controlHandler in _registeredControlHandlers)
            {
                if (newControlHandler == controlHandler)
                    return;
            }

            TimelineControlHandler[] newControlHandlers = new TimelineControlHandler[_registeredControlHandlers.Length + 1];
            _registeredControlHandlers.CopyTo(newControlHandlers, 0);
            _registeredControlHandlers = newControlHandlers;

            _registeredControlHandlers[_registeredControlHandlers.Length - 1] = newControlHandler;
            newControlHandler.SetPaused(_locallyPaused);
            // newControlHandler.SetLooping(_localLoopVideo);
            // newControlHandler.SetStatusText(_lastStatusText);
        }
        
        public void UnregisterControlHandler(TimelineControlHandler controlHandler)
        {
            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new TimelineControlHandler[0];

            int controlHandlerCount = _registeredControlHandlers.Length;
            for (int i = 0; i < controlHandlerCount; ++i)
            {
                TimelineControlHandler handler = _registeredControlHandlers[i];

                if (controlHandler == handler)
                {
                    TimelineControlHandler[] newControlHandlers = new TimelineControlHandler[controlHandlerCount - 1];

                    for (int j = 0; j < i; ++ j)
                        newControlHandlers[j] = _registeredControlHandlers[j];

                    for (int j = i + 1; j < controlHandlerCount; ++j)
                        newControlHandlers[j - 1] = _registeredControlHandlers[j];

                    _registeredControlHandlers = newControlHandlers;

                    return;
                }
            }
        }

        // private string _lastStatusText = "";

        // private void SetStatusText(string statusText)
        // {
        //     if (statusText == _lastStatusText)
        //         return;
        //
        //     _lastStatusText = statusText;
        //
        //     foreach (TimelineControlHandler handler in _registeredControlHandlers)
        //         handler.SetStatusText(statusText);
        // }

        private void SetUIPaused(bool paused)
        {
            foreach (TimelineControlHandler handler in _registeredControlHandlers)
                handler.SetPaused(paused);
        }
        
        private void SendUIOwnerUpdate()
        {
            foreach (TimelineControlHandler handler in _registeredControlHandlers)
                handler.OnTimelinePlayerOwnerTransferred();
        }

        // private void SetUILooping(bool looping)
        // {
        //     foreach (TimelineControlHandler handler in _registeredControlHandlers)
        //         handler.SetLooping(looping);
        // }
#endregion
        
#region Utilities

        private void LogError(string message)
        {
            Debug.LogError("[<color=#FF00FF>USharpVideo</color>] " + message, this);
        }

#endregion
    }
}
