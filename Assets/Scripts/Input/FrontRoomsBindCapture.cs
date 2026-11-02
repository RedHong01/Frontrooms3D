using UnityEngine;
using UnityEngine.InputSystem;
using Bind = FrontRoomsInput.Bind;
using BindCheck = FrontRoomsInput.BindCheck;

/// <summary>
/// The touch sheet's CONTROLLER and KEYBOARD lists listen through this (交互设计, 2026-11-01; Red: "如果玩家用手柄正在进行操作，
/// 在设置界面应该是手柄相关的建委mapping默认排在最上方默认第一个选择的选项", and "这部分如果使用的是键盘，也应该是一样的"): on an
/// iPhone or an iPad a pad's or a keyboard's own list heads CONTROLS while it is the scheme in hand, one row per target whose
/// button is its control as bound. A or a tap on a row listens (its button reads PRESS A BUTTON / PRESS A KEY); the next
/// control binds; a control another target has asks SWAP WITH BACK? (A, Enter or Space swaps; B, Esc or MENU cancels); a
/// fixed or locked one says so for a moment (D-PAD UP IS FIXED, LS IS SPRINT'S) and listening goes on. The rules are the
/// desktop key map's (FrontRoomsInput.CheckBind, SwapInto); the desktop map pages keep their own capture
/// (FrontRoomsKeyMapView.UpdateCapture), which this never touches. While it listens the game reads no physical input
/// (SuspendGameplay), so the press being bound never also pauses, uses or goes back.
/// </summary>
public static class FrontRoomsBindCapture
{
    public enum Phase { Idle, Listening, Asking }
    public static Phase State { get; private set; }
    public static bool Busy => State != Phase.Idle;
    /// <summary>The row being changed (also after a failed <see cref="Begin"/>: its row says why).</summary>
    public static Bind Target { get; private set; }
    static Bind owner;
    static string askPath, message;
    static float messageUntil;
    static bool armed;
    // As long as the map pages' flashes (FrontRoomsKeyMapView.Flash).
    const float MessageSeconds = 1.4f;

    /// <summary>The row of <paramref name="b"/> listens. False, with a message on its row, when its device is not in: no pad
    /// for a pad target (a browser shows a pad only after a press), no keyboard for a key target.</summary>
    public static bool Begin(Bind b)
    {
        Cancel();
        Target = b;
        message = null;
        var pad = FrontRoomsInput.IsPad(b);
        if (pad && Gamepad.current == null) { Say(FrontRoomsLanguage.T(Application.platform == RuntimePlatform.WebGLPlayer ? "PRESS A BUTTON" : "CONNECT A CONTROLLER")); return false; }
        if (!pad && Keyboard.current == null) { Say(FrontRoomsLanguage.T("CONNECT A KEYBOARD")); return false; }
        State = Phase.Listening;
        armed = false;
        FrontRoomsInput.SuspendGameplay = true;
        return true;
    }

    /// <summary>Stops listening or asking; the row reads its control again.</summary>
    public static void Cancel()
    {
        if (State == Phase.Idle) return;
        State = Phase.Idle;
        FrontRoomsInput.SuspendGameplay = false;
    }

    /// <summary>One frame of listening, read from the devices themselves (the sheet sends nothing meanwhile); true when the
    /// row's words changed (bound, asked, refused, cancelled).</summary>
    public static bool Update()
    {
        if (State == Phase.Idle) return false;
        var kb = Keyboard.current;
        var ms = Mouse.current;
        var gp = Gamepad.current;
        var pad = FrontRoomsInput.IsPad(Target);
        // Its device went while it listened or asked: it ends and says why.
        if (pad && gp == null) { Cancel(); Say(FrontRoomsLanguage.T("CONNECT A CONTROLLER")); return true; }
        if (!pad && kb == null) { Cancel(); Say(FrontRoomsLanguage.T("CONNECT A KEYBOARD")); return true; }
        // Esc or MENU (never bound) cancel; B too while asking, and on a key target (no pad button binds there).
        var padBack = gp != null && gp.buttonEast.wasPressedThisFrame && (!pad || State == Phase.Asking);
        if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame) || padBack) { Cancel(); return true; }
        if (!armed)
        {
            // Listening starts once the press that opened it is released (never capture the opening press).
            var padDown = pad ? FrontRoomsInput.PadButtonHeld(gp) : gp != null && gp.buttonSouth.isPressed;
            var held = (kb != null && kb.anyKey.isPressed) || (ms != null && (ms.leftButton.isPressed || ms.rightButton.isPressed || ms.middleButton.isPressed)) || padDown;
            if (!held) armed = true;
            return false;
        }
        if (State == Phase.Asking)
        {
            var yes = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (gp != null && gp.buttonSouth.wasPressedThisFrame);
            if (!yes) return false;
            FrontRoomsInput.SwapInto(Target, askPath);
            Cancel();
            return true;
        }
        var path = pad ? FrontRoomsInput.PressedPadPath(gp) : FrontRoomsInput.PressedKeyPath(kb, ms);
        if (path == null) return false;
        switch (FrontRoomsInput.CheckBind(Target, path, out var taken, out var lockedName))
        {
            case BindCheck.Reserved: Say(FrontRoomsLanguage.F("{0} IS FIXED", FrontRoomsInput.KeyName(path))); return true;
            case BindCheck.Locked: Say(FrontRoomsLanguage.F("{0} IS {1}'S", FrontRoomsInput.KeyName(path)).Replace("{1}", FrontRoomsLanguage.T(lockedName))); return true;
            case BindCheck.Taken:
            {
                // A swap would hand this target's control to the owner, and a locked one may not go there: say whose it is.
                var mine = FrontRoomsInput.PathOf(Target);
                if (FrontRoomsInput.CheckBind(taken, mine, out _, out var holder) == BindCheck.Locked)
                {
                    Say(FrontRoomsLanguage.F("{0} IS {1}'S", FrontRoomsInput.KeyName(mine)).Replace("{1}", FrontRoomsLanguage.T(holder)));
                    return true;
                }
                owner = taken; askPath = path; message = null;
                State = Phase.Asking;
                return true;
            }
            case BindCheck.Same: Cancel(); return true;
            default: FrontRoomsInput.Assign(Target, path); Cancel(); return true;
        }
    }

    /// <summary>A bind row's button: its control as bound, PRESS A BUTTON / PRESS A KEY while it listens, SWAP WITH BACK? while
    /// it asks, or for a moment why a control was refused.</summary>
    public static string CellText(Bind b)
    {
        if (b != Target) return FrontRoomsInput.ControlName(b);
        if (message != null && Time.unscaledTime < messageUntil) return message;
        if (State == Phase.Listening) return FrontRoomsLanguage.T(FrontRoomsInput.IsPad(b) ? "PRESS A BUTTON" : "PRESS A KEY");
        if (State == Phase.Asking) return FrontRoomsLanguage.F("SWAP WITH {0}?", FrontRoomsLanguage.T(FrontRoomsKeyMapView.ActionOf(owner)));
        return FrontRoomsInput.ControlName(b);
    }

    static void Say(string text) { message = text; messageUntil = Time.unscaledTime + MessageSeconds; }
}
