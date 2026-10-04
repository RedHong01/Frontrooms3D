using UnityEngine;

/// <summary>
/// Lightweight first-person rig used only by FrontRoomsScreenAdsDemo.unity.
/// It keeps the ad test scene independent from FrontRooms3DGame's title, map,
/// hunter and room-stream systems.
/// </summary>
public sealed class FrontRoomsScreenAdsDemoRig : MonoBehaviour
{
    [SerializeField] float moveSpeed = 1.6f;
    [SerializeField] float sprintMultiplier = 1.6f;
    [SerializeField] float lookSensitivity = .06f;
    [SerializeField] float minHeight = .7f;
    [SerializeField] float maxHeight = 2.2f;
    [SerializeField] Vector2 xBounds = new Vector2(-2.4f, 2.4f);
    [SerializeField] Vector2 zBounds = new Vector2(.2f, 4.4f);

    float yaw;
    float pitch;

    void Awake()
    {
        var euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizeAngle(euler.x);
    }

    void Update()
    {
        var input = FrontRoomsInput.Snapshot;
        yaw += input.Look.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - input.Look.y * lookSensitivity, -65f, 65f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        var forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        var right = transform.right;
        right.y = 0f;
        right.Normalize();
        var speed = moveSpeed * (input.SprintHeld ? sprintMultiplier : 1f);
        var delta = (forward * input.Move.y + right * input.Move.x) * speed * Time.unscaledDeltaTime;
        var position = transform.position + delta;
        position.x = Mathf.Clamp(position.x, xBounds.x, xBounds.y);
        position.y = Mathf.Clamp(position.y, minHeight, maxHeight);
        position.z = Mathf.Clamp(position.z, zBounds.x, zBounds.y);
        transform.position = position;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        var oldColor = GUI.color;
        var oldStyle = GUI.skin.label.fontSize;
        GUI.color = new Color(1f, .96f, .78f, .92f);
        GUI.skin.label.fontSize = 15;
        GUI.Label(new Rect(18f, 18f, 520f, 48f), "SCREEN ADS DEMO\nWASD move · mouse look · E toggle screen");
        GUI.color = oldColor;
        GUI.skin.label.fontSize = oldStyle;
    }

    static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
