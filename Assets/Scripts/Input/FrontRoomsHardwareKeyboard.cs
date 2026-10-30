using UnityEngine;
#if UNITY_IOS
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

/// <summary>
/// A hardware keyboard on iPhone and iPad (Red, 2026-10-29: "在手机/ipad上都可以用手柄/键盘游玩"). Unity's Input System has
/// no iOS keyboard, so GameController's GCKeyboard (iOS 14+, Plugins/iOS/FrontRoomsHardwareKeyboard.mm) is read before
/// every input update and fed to an Input System Keyboard: the key bindings, the menus' keys and the touch layer's switch
/// to keyboard play then work as they do on a Mac. Only the keys the game and its menus use are read. Android has its
/// own keyboard in the Input System, so this is iOS only.
/// </summary>
public static class FrontRoomsHardwareKeyboard
{
#if UNITY_IOS
    [DllImport("__Internal")] static extern int FRKeyboardPoll(int[] codes, byte[] pressed, int count);

    // HID usage codes (GCKeyCode) and the Input System keys they are.
    static readonly int[] Codes =
    {
        0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
        0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D,
        0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
        0x28, 0x29, 0x2A, 0x2B, 0x2C,
        0x4F, 0x50, 0x51, 0x52,
        0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7,
    };

    static readonly Key[] Keys =
    {
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M, Key.N, Key.O, Key.P,
        Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0,
        Key.Enter, Key.Escape, Key.Backspace, Key.Tab, Key.Space,
        Key.RightArrow, Key.LeftArrow, Key.DownArrow, Key.UpArrow,
        Key.LeftCtrl, Key.LeftShift, Key.LeftAlt, Key.LeftMeta, Key.RightCtrl, Key.RightShift, Key.RightAlt, Key.RightMeta,
    };

    static readonly byte[] pressed = new byte[Codes.Length];
    static readonly byte[] last = new byte[Codes.Length];
    static readonly List<Key> down = new List<Key>();
    static Keyboard device;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        // The Editor (an iOS build target, a touch harness) has its own keyboard and no GameController bridge.
        if (Application.isEditor) return;
        InputSystem.onBeforeUpdate -= Poll;
        InputSystem.onBeforeUpdate += Poll;
    }

    static void Poll()
    {
        if (FRKeyboardPoll(Codes, pressed, Codes.Length) == 0)
        {
            // Unplugged: the device goes, so Keyboard.current and the bindings see no keyboard.
            if (device != null) { InputSystem.RemoveDevice(device); device = null; System.Array.Clear(last, 0, last.Length); }
            return;
        }
        if (device == null) device = InputSystem.AddDevice<Keyboard>("FrontRooms Hardware Keyboard");
        var changed = false;
        for (var i = 0; i < Codes.Length; i++)
        {
            if (pressed[i] == last[i]) continue;
            last[i] = pressed[i];
            changed = true;
        }
        if (!changed) return;
        down.Clear();
        for (var i = 0; i < Codes.Length; i++) if (pressed[i] != 0) down.Add(Keys[i]);
        InputSystem.QueueStateEvent(device, new KeyboardState(down.ToArray()));
    }
#endif
}
