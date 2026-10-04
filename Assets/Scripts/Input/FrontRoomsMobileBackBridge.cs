using System;
using System.Threading;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Platform boundary for the Android system Back action.
///
/// Android 13+ exposes a predictive-back dispatcher instead of routing every
/// back gesture through a legacy key event. This component registers a small
/// <c>OnBackInvokedCallback</c> when that API is available and only transfers
/// the callback to Unity's main thread. Older Android versions, iOS, the
/// Editor, and WebGL remain safe: callers can use the regular Escape action or
/// invoke <see cref="SimulateBackPressed"/> in an Editor test.
///
/// The bridge intentionally publishes an event and a per-frame edge rather
/// than changing game state itself. The game can map BackPressed to its pause
/// state without making the Android Java callback know about scene ownership.
/// </summary>
[DefaultExecutionOrder(-600)]
public sealed class FrontRoomsMobileBackBridge : MonoBehaviour
{
    /// <summary>Raised on Unity's main thread for one complete Back action.</summary>
    public static event Action BackPressed;

    /// <summary>
    /// Lets an Editor/device simulator test exercise the same path with Escape.
    /// It is disabled by default so an ordinary desktop Escape does not create
    /// a second pause edge beside FrontRoomsInput.
    /// </summary>
    [SerializeField] bool editorEscapeFallback;

    /// <summary>
    /// Android versions before API 33 may report hardware Back as Escape. This
    /// fallback is enabled only on a mobile player by default.
    /// </summary>
    [SerializeField] bool mobileEscapeFallback = true;

    /// <summary>True for one Unity frame after a Back callback is delivered.</summary>
    public bool BackPressedThisFrame { get; private set; }

    /// <summary>Whether a Java predictive-back callback was registered.</summary>
    public bool PredictiveBackRegistered { get; private set; }

    static FrontRoomsMobileBackBridge instance;
    int pendingBack;
    bool attemptedRegistration;

#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject dispatcher;
    AndroidJavaObject callback;

    sealed class BackInvokedCallback : AndroidJavaProxy
    {
        readonly FrontRoomsMobileBackBridge owner;

        public BackInvokedCallback(FrontRoomsMobileBackBridge owner)
            : base("android.window.OnBackInvokedCallback")
        {
            this.owner = owner;
        }

        // Android's interface method is intentionally lower camel case.
        public void onBackInvoked()
        {
            owner?.QueueBackFromPlatform();
        }
    }
#endif

    /// <summary>
    /// Returns the scene bridge, if a game scene has installed one. This is
    /// useful to a bootstrapper that wants to avoid a duplicate component.
    /// </summary>
    public static FrontRoomsMobileBackBridge Instance => instance;

    /// <summary>
    /// Editor-safe test hook. It queues the same main-thread edge as Android.
    /// </summary>
    public static void SimulateBackPressed()
    {
        if (instance == null)
            return;
        instance.QueueBackFromPlatform();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    void OnEnable()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Keep the process alive when Android hands us a Back edge. The game
        // owns the resulting pause/resume decision.
        if (Application.isMobilePlatform)
            Application.backButtonLeavesApp = false;
#endif
        TryRegisterPredictiveBack();
    }

    void OnDisable()
    {
        UnregisterPredictiveBack();
        if (instance == this)
            instance = null;
        Interlocked.Exchange(ref pendingBack, 0);
        BackPressedThisFrame = false;
    }

    void Update()
    {
        BackPressedThisFrame = false;

        var back = Interlocked.Exchange(ref pendingBack, 0) != 0;
        if (!back && ShouldUseEscapeFallback())
        {
#if ENABLE_INPUT_SYSTEM
            back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
        }

        if (!back)
            return;

        BackPressedThisFrame = true;
        BackPressed?.Invoke();
    }

    bool ShouldUseEscapeFallback()
    {
        if (Application.isMobilePlatform)
            return mobileEscapeFallback && !PredictiveBackRegistered;
        return Application.isEditor && editorEscapeFallback;
    }

    void QueueBackFromPlatform()
    {
        Interlocked.Exchange(ref pendingBack, 1);
    }

    void TryRegisterPredictiveBack()
    {
        if (attemptedRegistration)
            return;
        attemptedRegistration = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                var sdk = version.GetStatic<int>("SDK_INT");
                if (sdk < 33)
                    return;
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null)
                    return;

                dispatcher = activity.Call<AndroidJavaObject>("getOnBackInvokedDispatcher");
                if (dispatcher == null)
                    return;

                callback = new BackInvokedCallback(this);
                // PRIORITY_DEFAULT is zero. Registering at the default level
                // lets the app consume Back while preserving system gestures.
                dispatcher.Call("registerOnBackInvokedCallback", 0, callback);
                PredictiveBackRegistered = true;
            }
        }
        catch (Exception error)
        {
            // A vendor ROM or an older Unity activity can expose API 33 while
            // omitting the dispatcher. Escape fallback remains available.
            PredictiveBackRegistered = false;
            Debug.LogWarning("FrontRooms mobile Back callback unavailable: " + error.Message);
        }
#endif
    }

    void UnregisterPredictiveBack()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!PredictiveBackRegistered || dispatcher == null || callback == null)
            return;

        try
        {
            dispatcher.Call("unregisterOnBackInvokedCallback", callback);
        }
        catch (Exception error)
        {
            Debug.LogWarning("FrontRooms mobile Back callback unregister failed: " + error.Message);
        }
        finally
        {
            PredictiveBackRegistered = false;
            dispatcher = null;
            callback = null;
        }
#else
        PredictiveBackRegistered = false;
#endif
    }
}
