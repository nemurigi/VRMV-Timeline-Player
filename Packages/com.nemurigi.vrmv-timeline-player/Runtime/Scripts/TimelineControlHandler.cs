
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace NmrgLibrary.TimelinePlayer
{
    [DefaultExecutionOrder(10)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TimelineControlHandler : UdonSharpBehaviour
    {
        [PublicAPI, NotNull]
        public VRMVTimelinePlayer targetTimelinePlayer;
        
#pragma warning disable CS0649

        [Header("Status text")]
        [SerializeField]
        private Text statusTextField;

        [SerializeField]
        private Text statusTextDropShadow;

        [Header("Video progress bar")]
        [SerializeField]
        private Slider progressSlider;

        [SerializeField]
        private Text ownerField;
        
        [Header("Play/Pause/Stop buttons")]
        [SerializeField]
        private GameObject pauseStopObject;
        
        [SerializeField]
        private GameObject playObject;

        // [SerializeField]
        // private GameObject pauseIcon, stopIcon;
#pragma warning restore CS0649

        private void OnEnable()
        {
            targetTimelinePlayer.RegisterControlHandler(this);
            // UpdateMaster();
            UpdateTimelineOwner();
        }
        
        private void OnDisable()
        {
            targetTimelinePlayer.UnregisterControlHandler(this);
        }
        
        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return false;
        }

        private void Update()
        {
            RunUIUpdate();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // UpdateMaster();
        }
        
        public void OnTimelinePlayerOwnerTransferred()
        {
            UpdateTimelineOwner();
        }

        private void UpdateTimelineOwner()
        {
#if !UNITY_EDITOR
            if (ownerField)
                ownerField.text = Networking.GetOwner(targetTimelinePlayer.gameObject).displayName;
#endif
        }
        
        public void SetPaused(bool paused)
        {
            if (playObject) playObject.SetActive(paused);
            if (pauseStopObject) pauseStopObject.SetActive(!paused);
        }

         [PublicAPI]
         public void SetControlledVideoPlayer(VRMVTimelinePlayer newPlayer)
         {
             if (newPlayer == targetTimelinePlayer)
                 return;
             
             UpdateTimelineOwner();

             SetStatusText("");
             _draggingSlider = false;
         }

        string _currentStatusText = "";
        
        public void SetStatusText(string newStatus)
        {
            _currentStatusText = newStatus;
            if (statusTextField) statusTextField.text = _currentStatusText;
            if (statusTextDropShadow) statusTextDropShadow.text = _currentStatusText;
            _lastTime = int.MaxValue;
        }

        private int _lastTime = int.MaxValue;
        
        private void RunUIUpdate()
        {
            var manager = targetTimelinePlayer.GetMediaManager();
            float duration = manager.GetDuration();
    
            if (_draggingSlider)
            {
                float currentProgress = progressSlider.value;
                float currentTime = duration * currentProgress;
    
                targetTimelinePlayer.SeekTo(currentProgress);
    
                string currentTimeStr = GetFormattedTime(System.TimeSpan.FromSeconds(currentTime));
    
                if (statusTextField) statusTextField.text = currentTimeStr;
                if (statusTextDropShadow) statusTextDropShadow.text = currentTimeStr;
            }
            else
            {
                float currentTime = manager.GetTime();
    
                if (progressSlider)
                {
                    if (duration > 0f)
                    {
                        progressSlider.gameObject.SetActive(true);
                        progressSlider.value = Mathf.Clamp01(currentTime / duration);
                    }
                    else
                    {
                        progressSlider.gameObject.SetActive(false);
                    }
                }
    
                int currentTimeInt = Mathf.RoundToInt(currentTime);
                if (currentTimeInt != _lastTime)
                {
                    _lastTime = currentTimeInt;
    
                    if (!float.IsInfinity(duration) & duration != float.MaxValue)
                    {
                        if (string.IsNullOrEmpty(_currentStatusText))
                        {
                            System.TimeSpan durationTimespan = System.TimeSpan.FromSeconds(duration);
                            System.TimeSpan currentTimeTimespan = System.TimeSpan.FromSeconds(currentTime);
    
                            string totalTimeStr = GetFormattedTime(durationTimespan);
                            string currentTimeStr = GetFormattedTime(currentTimeTimespan);
    
                            string statusStr = currentTimeStr + "/" + totalTimeStr;
    
                            if (statusTextField) statusTextField.text = statusStr;
                            if (statusTextDropShadow) statusTextDropShadow.text = statusStr;
                        }
                    }
                }
            }
        }
        
        private string GetFormattedTime(System.TimeSpan time)
        {
            return ((int)time.TotalHours).ToString("D2") + time.ToString(@"\:mm\:ss");
        }
        
        public void OnPlayButtonPress()
        {
            targetTimelinePlayer.SetPaused(!targetTimelinePlayer.IsPaused());
        }
        
        // public void OnPlayButtonPress()
        // {
        //     targetTimelinePlayer.SetPaused(false);
        // }
        //
        // public void OnStopButtonPress()
        // {
        //     targetTimelinePlayer.SetPaused(true);
        // }

        public void OnTakeOwnershipButtonPress()
        {
            targetTimelinePlayer.TakeOwnership();
        }
        
        public void OnSeekSliderChanged()
        {
            // if (!_draggingSlider)
            //     return;
        }
        
        private bool _draggingSlider;

        public void OnSeekSliderBeginDrag()
        {
            if (Networking.IsOwner(targetTimelinePlayer.gameObject))
                _draggingSlider = true;
        }
        
        public void OnSeekSliderEndDrag()
        {
            _draggingSlider = false;
        }
    }
}
