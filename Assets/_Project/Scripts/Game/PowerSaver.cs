using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PFN
{
    /// <summary>
    /// Battery saver for a mostly static UI. The game loop runs at a low target frame rate,
    /// and the GPU only renders every Nth frame unless something asked for full rate
    /// (a touch, a screen change, an animation). Input is still read every loop.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PowerSaver : MonoBehaviour
    {
        [Tooltip("Game loop rate. Input and timers run at this rate.")]
        [SerializeField, Min(10)] int targetFps = 30;

        [Tooltip("When idle, render every Nth loop (30 fps / 6 = 5 rendered fps).")]
        [SerializeField, Min(1)] int idleFrameInterval = 6;

        [Tooltip("How long to render at full rate after a touch.")]
        [SerializeField, Min(0)] float touchWakeSeconds = 0.5f;

        static PowerSaver instance;
        float fullRateUntil;

        /// <summary>Render every frame for the given number of seconds (e.g. during an animation).</summary>
        public static void Wake(float seconds)
        {
            if (instance == null) return;
            instance.fullRateUntil = Mathf.Max(instance.fullRateUntil, Time.unscaledTime + seconds);
            OnDemandRendering.renderFrameInterval = 1;
        }

        void Awake()
        {
            instance = this;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFps;
        }

        void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            OnDemandRendering.renderFrameInterval = 1;
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed) Wake(touchWakeSeconds);

            OnDemandRendering.renderFrameInterval =
                Time.unscaledTime < fullRateUntil ? 1 : idleFrameInterval;
        }
    }
}
