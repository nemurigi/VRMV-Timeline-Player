using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace NmrgLibrary.TimelinePlayer
{
    // audio sourceやvideo playerに差し替えることを考慮し抽象クラスを定義しておく
    public abstract class AbstractTimelineManager : UdonSharpBehaviour
    {
        public abstract void Initialize(GameObject mediaObj, UdonSharpBehaviour receiver);
        public abstract bool IsPlaying();
        public abstract bool IsStopped();
        public abstract bool IsPaused();
        public abstract float GetDuration();
        public abstract float GetTime();
        public abstract void SetTime(float time);
        public abstract void Play();
        public abstract void Pause();
        public abstract void OnMediaEnd();
        public abstract void OnMediaStart();
    }
}
