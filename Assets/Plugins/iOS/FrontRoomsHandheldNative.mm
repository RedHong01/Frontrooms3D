// FrontRooms handheld: the iOS side of FrontRoomsHandheld and FrontRoomsMobileHaptics.
// UIKit only (no extra framework to link). Called on Unity's main thread.

#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *FRImpactGenerators[5];
static UISelectionFeedbackGenerator *FRSelectionGenerator;

static UIImpactFeedbackGenerator *FRImpactGenerator(int style)
{
    if (style < 0 || style > 4) style = 0;
    if (FRImpactGenerators[style] == nil)
    {
        UIImpactFeedbackStyle s = UIImpactFeedbackStyleLight;
        switch (style)
        {
            case 1: s = UIImpactFeedbackStyleMedium; break;
            case 2: s = UIImpactFeedbackStyleHeavy; break;
            case 3: s = UIImpactFeedbackStyleSoft; break;
            case 4: s = UIImpactFeedbackStyleRigid; break;
            default: s = UIImpactFeedbackStyleLight; break;
        }
        FRImpactGenerators[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    }
    return FRImpactGenerators[style];
}

extern "C"
{
    // Pixels per point (3 on iPhone Pro, 2 on iPad).
    float FRNativeScale(void)
    {
        return (float)[UIScreen mainScreen].nativeScale;
    }

    int FRReduceMotionNative(void)
    {
        return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
    }

    int FRIsPadNative(void)
    {
        return [UIDevice currentDevice].userInterfaceIdiom == UIUserInterfaceIdiomPad ? 1 : 0;
    }

    // style: 0 light, 1 medium, 2 heavy, 3 soft, 4 rigid; intensity 0..1.
    void FRHapticImpact(int style, float intensity)
    {
        UIImpactFeedbackGenerator *generator = FRImpactGenerator(style);
        CGFloat i = intensity < 0.0f ? 0.0f : (intensity > 1.0f ? 1.0f : intensity);
        [generator impactOccurredWithIntensity:i];
        [generator prepare];
    }

    void FRHapticSelection(void)
    {
        if (FRSelectionGenerator == nil) FRSelectionGenerator = [[UISelectionFeedbackGenerator alloc] init];
        [FRSelectionGenerator selectionChanged];
        [FRSelectionGenerator prepare];
    }
}
