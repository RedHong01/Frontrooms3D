// FrontRooms: a hardware keyboard on iPhone and iPad, the native half of FrontRoomsHardwareKeyboard.cs.
// Unity's Input System has no iOS keyboard, so GameController's GCKeyboard (iOS 14 and later) is read here, once per
// input update on Unity's main thread, for the keys the C# side asks about (HID usage codes, which GCKeyCode is).
// GameController.framework is already linked by Unity's iOS project (its pad support).

#import <GameController/GameController.h>

// Writes 1 into pressed[i] for each codes[i] held down; returns 1 while a keyboard is connected, else 0 (and writes nothing).
extern "C" int FRKeyboardPoll(const int *codes, unsigned char *pressed, int count)
{
    if (@available(iOS 14.0, *))
    {
        GCKeyboard *keyboard = GCKeyboard.coalescedKeyboard;
        if (keyboard == nil) return 0;
        GCKeyboardInput *input = keyboard.keyboardInput;
        if (input == nil) return 0;
        for (int i = 0; i < count; i++)
        {
            GCControllerButtonInput *key = [input buttonForKeyCode:(GCKeyCode)codes[i]];
            pressed[i] = (key != nil && key.isPressed) ? 1 : 0;
        }
        return 1;
    }
    return 0;
}
