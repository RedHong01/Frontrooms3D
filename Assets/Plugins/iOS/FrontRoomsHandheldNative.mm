// FrontRooms handheld: the iOS side of FrontRoomsHandheld and FrontRoomsMobileHaptics.
// UIKit only (no extra framework to link). Called on Unity's main thread.

#import <UIKit/UIKit.h>

// Unity's rendered view, including its current window when displays change.
extern "C" UIView *UnityGetGLView(void);

static UIImpactFeedbackGenerator *FRImpactGenerators[5];
static UISelectionFeedbackGenerator *FRSelectionGenerator;
// A hidden, non-interactive view filling Unity's view with container-concentric corners: its effective radii are the
// window's (on a full-screen phone, the display's) corner radii, per corner, in the current orientation.
static UIView *FRCornerProbe;
static NSString *FRLastNativeLayout;

static UIView *FRCornerProbeFor(UIView *view)
{
    if (@available(iOS 26.0, *))
    {
        if (FRCornerProbe == nil || FRCornerProbe.superview != view)
        {
            [FRCornerProbe removeFromSuperview];
            FRCornerProbe = [[UIView alloc] initWithFrame:view.bounds];
            FRCornerProbe.userInteractionEnabled = NO;
            FRCornerProbe.alpha = 0.0;
            FRCornerProbe.autoresizingMask = UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;
            FRCornerProbe.cornerConfiguration = [UICornerConfiguration configurationWithRadius:[UICornerRadius containerConcentricRadius]];
            [view addSubview:FRCornerProbe];
        }
        FRCornerProbe.frame = view.bounds;
        return FRCornerProbe;
    }
    return nil;
}

// One line per change of the view's shape (a fold, a rotation, a camera turning on): what UIKit reports, for QA logs.
// Called from FRGetViewMetrics (once a frame at most).
static void FRLogNativeLayout(UIView *view)
{
    if (view == nil) return;
    NSMutableString *s = [NSMutableString string];
    CGSize size = view.bounds.size;
    UIEdgeInsets safe = view.safeAreaInsets;
    [s appendFormat:@"view %.1f x %.1f safe T %.1f L %.1f B %.1f R %.1f", size.width, size.height, safe.top, safe.left, safe.bottom, safe.right];
    UIWindowScene *scene = view.window.windowScene;
    if (scene != nil)
    {
        if (@available(iOS 16.0, *)) [s appendFormat:@" interface %ld", (long)scene.effectiveGeometry.interfaceOrientation];
        [s appendFormat:@" screen %.0f x %.0f", scene.screen.bounds.size.width, scene.screen.bounds.size.height];
    }
    if (@available(iOS 26.0, *))
    {
        UIView *probe = FRCornerProbeFor(view);
        if (probe != nil)
            [s appendFormat:@" radii TL %.1f TR %.1f BL %.1f BR %.1f",
                [probe effectiveRadiusForCorner:UIRectCornerTopLeft], [probe effectiveRadiusForCorner:UIRectCornerTopRight],
                [probe effectiveRadiusForCorner:UIRectCornerBottomLeft], [probe effectiveRadiusForCorner:UIRectCornerBottomRight]];
    }
    if (@available(iOS 27.1, *))
    {
        for (int division = 0; division < 2; division++)
        {
            UIViewReservedRegionKind *kind = division ? [UIViewReservedRegionKind divisionRegionKind] : [UIViewReservedRegionKind occlusionRegionKind];
            for (UIViewReservedRegion *region in [view reservedRegionsOfKind:kind options:UIViewReservedRegionQueryOptionsIncludeInactive])
            {
                CGRect f = region.frame;
                UIEdgeInsets m = region.margins;
                [s appendFormat:@" | %@ %@ x %.1f y %.1f w %.1f h %.1f margins T %.1f L %.1f B %.1f R %.1f",
                    division ? @"division" : @"occlusion", region.isActive ? @"active" : @"inactive",
                    f.origin.x, f.origin.y, f.size.width, f.size.height, m.top, m.left, m.bottom, m.right];
            }
        }
    }
    if (FRLastNativeLayout == nil || ![s isEqualToString:FRLastNativeLayout])
    {
        FRLastNativeLayout = [s copy];
        NSLog(@"[HandheldNative] %@ (UIKit top-left points)", s);
    }
}

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
    // UIKit points and trait scale of THIS view. UIScreen.main/nativeScale can describe
    // the wrong display during folding, window resizing or external-display moves.
    int FRGetViewMetrics(float *width, float *height, float *displayScale)
    {
        UIView *view = UnityGetGLView();
        CGSize size = view.bounds.size;
        CGFloat scale = view.traitCollection.displayScale;
        if (scale <= 0.0) scale = view.window.traitCollection.displayScale;
        *width = (float)size.width;
        *height = (float)size.height;
        *displayScale = scale > 0.0 ? (float)scale : 1.0f;
        FRLogNativeLayout(view);
        return size.width > 0.0 && size.height > 0.0 ? 1 : 0;
    }

    // UIKit's interface orientation of Unity's window (1 portrait, 2 upside down, 3 landscape with the device's top to
    // the left, 4 to the right); 0 when there is no window yet.
    int FRGetInterfaceOrientation(void)
    {
        UIView *view = UnityGetGLView();
        UIWindowScene *scene = view.window.windowScene;
        if (scene == nil) return 0;
        if (@available(iOS 16.0, *)) return (int)scene.effectiveGeometry.interfaceOrientation;
        return (int)scene.interfaceOrientation;
    }

    // The screen's corner radii as this view sees them, in Unity's bottom-left order: bottom-left, bottom-right,
    // top-left, top-right (points). 0 = unknown (before iOS 26, or no window yet).
    int FRGetCornerRadii(float *radii)
    {
        if (radii == NULL) return 0;
        radii[0] = radii[1] = radii[2] = radii[3] = 0.0f;
        UIView *view = UnityGetGLView();
        if (view == nil || view.window == nil) return 0;
        if (@available(iOS 26.0, *))
        {
            UIView *probe = FRCornerProbeFor(view);
            if (probe == nil) return 0;
            radii[0] = (float)[probe effectiveRadiusForCorner:UIRectCornerBottomLeft];
            radii[1] = (float)[probe effectiveRadiusForCorner:UIRectCornerBottomRight];
            radii[2] = (float)[probe effectiveRadiusForCorner:UIRectCornerTopLeft];
            radii[3] = (float)[probe effectiveRadiusForCorner:UIRectCornerTopRight];
            return 1;
        }
        return 0;
    }

    // iOS 27.1 exposes camera/hinge reservations in the current Unity view's
    // local point space. The managed layout uses these rects as local
    // avoidance regions, leaving unrelated full-bleed content untouched.
    int FRGetReservedRegions(int division, float *rects, int maxRegions)
    {
        if (rects == NULL || maxRegions <= 0) return 0;
        UIView *view = UnityGetGLView();
        if (view == nil) return 0;
        if (@available(iOS 27.1, *))
        {
            UIViewReservedRegionKind *kind = division
                ? [UIViewReservedRegionKind divisionRegionKind]
                : [UIViewReservedRegionKind occlusionRegionKind];
            NSArray<UIViewReservedRegion *> *regions = [view reservedRegionsOfKind:kind options:UIViewReservedRegionQueryOptionsNone];
            int count = 0;
            for (UIViewReservedRegion *region in regions)
            {
                if (!region.isActive || count >= maxRegions) continue;
                CGRect frame = region.frame;
                // UIKit's view coordinate origin is top-left; Unity's point
                // space is bottom-left.
                rects[count * 4 + 0] = (float)frame.origin.x;
                rects[count * 4 + 1] = (float)(view.bounds.size.height - CGRectGetMaxY(frame));
                rects[count * 4 + 2] = (float)frame.size.width;
                rects[count * 4 + 3] = (float)frame.size.height;
                count++;
            }
            return count;
        }
        return 0;
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
