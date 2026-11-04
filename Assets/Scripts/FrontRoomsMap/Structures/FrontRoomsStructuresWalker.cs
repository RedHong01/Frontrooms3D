using FrontRooms.Map;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The structures scene's walker (scene 4, PLAN 2.3): the game's player, ported, so Red judges every structure at the game's
/// own size and speed. Not the Level Designer's FrontRoomsMapWalker (its Shift sprints; changing it would change the
/// designer). CharacterController 1.75 m, r 0.3, step 0.3; eye 1.62 through FrontRoomsCameraRig; input from
/// FrontRoomsInput.ReadFrame (the game's bindings: WASD or arrows, mouse look in degrees, LeftCtrl run, Shift slow, E use);
/// gait as FrontRooms3DGame (walk 3.2, run 5.5 ahead only within 70 degrees, slow 0.45 x walk, 5 s of stamina, back after
/// 1 s at 1/s, winded until 1 s is back); gravity -9.81; pitch +-75. Camera as the game's: FOV 76, near 0.06, far 80
/// (a district may raise far), the film look's camera setup, tag MainCamera. Click locks the cursor, Esc frees it.
/// E goes to the host (pads, the map's doors in P). C swaps in the Relay's 2.05 m capsule (70 section 10, R6).
/// Frozen (SP1's ride, later) stops the body and leaves the look.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class FrontRoomsStructuresWalker : MonoBehaviour
{
    public const float Walk = 3.2f, Run = 5.5f, Reach = 2.4f, Eye = ModuleUnits.PlayerEye;
    const float StaminaSeconds = 5f, RecoverDelay = 1f, RecoverRate = 1f, SlowTilt = .5f;

    FrontRoomsStructures host;
    CharacterController body;
    Camera view;
    FrontRoomsCameraRig rig;
    float yaw, pitch, fallSpeed, sinceSprint, stamina = StaminaSeconds;
    bool winded, sprinting, relayBody, riding;
    float rideHeading, rideLook;

    /// <summary>The collider in the crosshair within reach, the hit point, and the host's prompt for it.</summary>
    public Collider Aimed { get; private set; }
    public Vector3 AimPoint { get; private set; }
    public string Prompt { get; private set; }
    public float HoldProgress { get; private set; }
    public bool Frozen { get; set; }
    /// <summary>Riding (SP1's slide): the district moves the body along the riding line; the mouse looks within 70 degrees of its heading.</summary>
    public bool Riding => riding;
    public Camera View => view;
    public float Stamina01 => stamina / StaminaSeconds;
    public bool RelayBody => relayBody;
    public float Yaw => yaw;
    public float Pitch => pitch;
    public Vector3 EyePosition => rig != null ? rig.BaseEye.position : transform.position + Vector3.up * Eye;

    public static FrontRoomsStructuresWalker Create(FrontRoomsStructures host, Vector3 position, float yaw)
    {
        var go = new GameObject("Structures walker");
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var body = go.AddComponent<CharacterController>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = new Vector3(0f, ModuleUnits.PlayerHeight * .5f, 0f);
        body.stepOffset = .3f;
        body.skinWidth = .03f;
        body.slopeLimit = 45f;

        var cameraObject = new GameObject("Structures camera");
        cameraObject.tag = "MainCamera";
        var view = cameraObject.AddComponent<Camera>();
        view.fieldOfView = 76f;
        view.nearClipPlane = .06f;
        view.farClipPlane = 80f;
        view.allowHDR = true;
        view.allowMSAA = true;
        view.useOcclusionCulling = true;
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = FrontRoomsLook.FogColor;
        cameraObject.AddComponent<AudioListener>();
        try { FrontRoomsPostStack.ConfigureCamera(view); }
        catch (System.Exception e) { FrontRoomsPoolMaterials.LogOnce("camera", "the film camera setup failed: " + e.Message); }

        var walker = go.AddComponent<FrontRoomsStructuresWalker>();
        walker.host = host;
        walker.body = body;
        walker.view = view;
        walker.rig = FrontRoomsCameraRig.Attach(go.transform, view, Eye);
        walker.yaw = yaw;
        return walker;
    }

    /// <summary>Put the walker on a floor point, facing yaw (degrees), looking pitch (positive down); the body stops falling.</summary>
    public void Teleport(Vector3 floor, float newYaw, float newPitch = 0f)
    {
        body.enabled = false;
        transform.SetPositionAndRotation(floor + Vector3.up * .02f, Quaternion.Euler(0f, newYaw, 0f));
        yaw = newYaw;
        pitch = Mathf.Clamp(newPitch, -75f, 75f);
        fallSpeed = 0f;
        if (rig != null) { rig.ResetLayers(); rig.SetBase(pitch); }
        body.enabled = !Frozen;
        Physics.SyncTransforms();
    }

    /// <summary>Start a ride: the controller off, no move, the look free within 70 degrees of the riding line's heading (61 P3).</summary>
    public void BeginRide(float heading)
    {
        riding = true;
        Frozen = true;
        body.enabled = false;
        rideHeading = heading;
        rideLook = 0f;
        yaw = heading;
        pitch = Mathf.Clamp(pitch, -70f, 70f);
    }

    /// <summary>One frame of the ride: the feet at a point (the eye 1.62 above it) and the line's heading (degrees).</summary>
    public void RideTo(Vector3 feet, float heading)
    {
        if (!riding) return;
        rideHeading = heading;
        transform.position = feet;
        yaw = rideHeading + rideLook;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>End the ride standing at a floor point facing yaw.</summary>
    public void EndRide(Vector3 floor, float newYaw)
    {
        riding = false;
        Frozen = false;
        Teleport(floor, newYaw, 0f);
    }

    /// <summary>The camera's far plane (a district raises it where its main view needs it).</summary>
    public void SetFar(float far)
    {
        if (view != null) view.farClipPlane = Mathf.Max(20f, far);
    }

    /// <summary>C: the Relay's capsule (2.05 m, r 0.3) or the player's (1.75 m). Eye height stays 1.62 so views compare.</summary>
    public void SetRelayBody(bool on)
    {
        relayBody = on;
        var h = on ? ModuleUnits.RelayHeight : ModuleUnits.PlayerHeight;
        body.height = h;
        body.center = new Vector3(0f, h * .5f, 0f);
    }

    void Update()
    {
        var dt = Mathf.Min(Time.deltaTime, .1f);
        var mouse = Mouse.current;
        var keys = Keyboard.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (keys != null && keys.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        var input = FrontRoomsInput.ReadFrame();
        if (Cursor.lockState == CursorLockMode.Locked && (rig == null || !rig.LookLocked))
        {
            if (riding)
            {
                rideLook = Mathf.Clamp(rideLook + input.Look.x, -70f, 70f);
                yaw = rideHeading + rideLook;
                pitch = Mathf.Clamp(pitch - input.Look.y, -70f, 70f);
            }
            else
            {
                yaw += input.Look.x;
                pitch = Mathf.Clamp(pitch - input.Look.y, -75f, 75f);
            }
        }
        else if (riding) yaw = rideHeading + rideLook;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (rig != null) rig.SetBase(pitch);

        if (Frozen)
        {
            sprinting = false;
            Aim(input, dt);
            return;
        }

        var local = input.Move;
        sprinting = input.SprintHeld && RunAhead(local, sprinting) && local.sqrMagnitude >= SlowTilt * SlowTilt && stamina > 0f && !winded;
        local = GaitMove(local, input.SlowHeld && !sprinting);
        var wish = transform.right * local.x + transform.forward * local.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        sprinting &= wish.sqrMagnitude > .01f;
        if (sprinting)
        {
            stamina = Mathf.Max(0f, stamina - dt);
            sinceSprint = 0f;
            if (stamina <= 0f) winded = true;
        }
        else
        {
            sinceSprint += dt;
            if (sinceSprint > RecoverDelay) stamina = Mathf.Min(StaminaSeconds, stamina + RecoverRate * dt);
            if (winded && stamina >= 1f) winded = false;
        }
        fallSpeed = body.isGrounded ? -1f : Mathf.Max(fallSpeed - 9.81f * dt, -40f);
        if (body.enabled) body.Move((wish * (sprinting ? Run : Walk) + Vector3.up * fallSpeed) * dt);
        Aim(input, dt);
    }

    // As FrontRooms3DGame.RunAhead: the run starts within 70 degrees of straight ahead and holds while the move goes forward.
    static bool RunAhead(Vector2 move, bool running)
    {
        if (move.sqrMagnitude <= .01f) return false;
        return running ? move.y > .01f : Vector2.Angle(Vector2.up, move) <= FrontRoomsTouchControls.RunHalfAngle;
    }

    // As FrontRooms3DGame.GaitMove: slow held, or a small tilt, is the slow gait (0.45 of the walk).
    static Vector2 GaitMove(Vector2 move, bool slow)
    {
        var size = move.magnitude;
        if (size <= .001f) return Vector2.zero;
        return move / size * (slow || size < SlowTilt ? FrontRoomsTouchControls.SlowSpeedShare : 1f);
    }

    void Aim(FrontRoomsInput.FrameSnapshot input, float dt)
    {
        var previous = Aimed;
        Aimed = null;
        Prompt = null;
        var holdPrompt = false;
        var eye = rig != null ? rig.BaseEye : new Pose(transform.position + Vector3.up * Eye, transform.rotation);
        var ray = new Ray(eye.position, eye.rotation * Vector3.forward);
        RaycastHit hit = default;
        if (Physics.Raycast(ray, out hit, Reach, ~0, QueryTriggerInteraction.Ignore) && host != null)
        {
            var p = host.Describe(hit.collider, out holdPrompt);
            if (p != null) { Aimed = hit.collider; AimPoint = hit.point; Prompt = p; }
        }
        // A pad under the feet answers E too.
        if (Aimed == null && host != null)
        {
            var under = host.PadPrompt(transform.position);
            if (under != null) Prompt = under;
        }
        if (previous != null && previous != Aimed && host != null) host.ReleaseHold(previous);
        if (host == null) return;
        if (Aimed == null)
        {
            HoldProgress = 0f;
            if (input.UseDown && Prompt != null) host.UsePadUnder(transform.position);
            return;
        }
        if (!holdPrompt)
        {
            HoldProgress = 0f;
            if (input.UseDown) host.Use(Aimed);
            return;
        }
        if (input.UseHeld)
        {
            if (host.Hold(Aimed, hit.point, dt, out var progress)) { Aimed = null; HoldProgress = 0f; }
            else HoldProgress = progress;
        }
        else
        {
            host.ReleaseHold(Aimed);
            HoldProgress = 0f;
        }
    }
}
