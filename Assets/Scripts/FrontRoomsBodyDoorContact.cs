using UnityEngine;

/// <summary>Body-contact motion for the two non-map doorway types. No input action opens or shuts a leaf.</summary>
public static class FrontRoomsBodyDoorContact
{
    public const float Reach = .355f;
    public const float PullAimSeconds = .30f;
    public const float PullStepSeconds = .12f;
    public enum Verb { None, Ease, Barge, Shut, Slam, Latched }

    public sealed class Leaf
    {
        public float angle;
        public bool moving;
        public bool shutting;
        public bool following;
        public float elapsed;
        public float from;
        public float target;
        public float seconds;
        public float lean;
        public float offContact;
        public float aim;
        public float back;
        public float time;
        public float sprintPass = -100f;
        public float previousSide;
        public bool sideKnown;
        public bool episode;
        public float away;
    }

    public static Vector3 Direction(Vector3 closedDirection, float angle) => Quaternion.AngleAxis(angle, Vector3.up) * closedDirection;

    public static float Distance(Vector3 feet, Vector3 hinge, Vector3 direction, float width)
    {
        var along = Mathf.Clamp(Vector3.Dot(Flat(feet - hinge), direction), 0f, width);
        return (Flat(feet - hinge) - direction * along).magnitude;
    }

    /// <summary>True when the requested step still presses into this leaf's capsule clearance.
    /// Read after Tick: a released swing can clear the path in the same frame.</summary>
    public static bool BlocksMove(Vector3 hinge, Vector3 closedDirection, float angle, float width,
        Vector3 feet, Vector3 inputMove)
    {
        inputMove = Flat(inputMove);
        if (inputMove.sqrMagnitude < .0000001f) return false;
        var direction = Direction(Flat(closedDirection).normalized, angle);
        var normal = Vector3.Cross(Vector3.up, direction);
        var side = Vector3.Dot(Flat(feet - hinge), normal);
        if (Vector3.Dot(inputMove, normal) * side >= 0f) return false;
        var predicted = feet + inputMove;
        var along = Vector3.Dot(Flat(predicted - hinge), direction);
        return along >= -.03f && along <= width + .03f && Distance(predicted, hinge, direction, width) <= Reach;
    }

    static Vector3 Flat(Vector3 value) { value.y = 0f; return value; }

    /// <summary>The input is one world-space horizontal movement step before CharacterController.Move.</summary>
    public static Verb Tick(Leaf leaf, Vector3 hinge, Vector3 closedDirection, float width, float stop,
        Vector3 feet, Vector3 inputMove, Vector3 viewForward, float dt, bool sprinting, bool slow,
        float sinceSprint, bool ready, bool sealedShut = false)
    {
        dt = Mathf.Clamp(dt, 0f, .1f);
        if (dt <= 0f || leaf == null) return Verb.None;
        leaf.time += dt;
        closedDirection = Flat(closedDirection).normalized;
        inputMove = Flat(inputMove);
        viewForward = Flat(viewForward).normalized;
        var closedNormal = Vector3.Cross(Vector3.up, closedDirection);
        var side = Vector3.Dot(Flat(feet - hinge), closedNormal);
        var closedAlong = Vector3.Dot(Flat(feet - hinge), closedDirection);
        if (leaf.sideKnown && side * leaf.previousSide < 0f && closedAlong >= 0f && closedAlong <= width && sprinting)
            leaf.sprintPass = leaf.time;
        if (Mathf.Abs(side) > .01f) { leaf.previousSide = side; leaf.sideKnown = true; }

        if (sealedShut) { leaf.lean = leaf.aim = leaf.back = 0f; return Advance(leaf, hinge, closedDirection, width, feet, dt); }
        if (!ready) { leaf.lean = leaf.aim = leaf.back = 0f; return Verb.None; }

        var direction = Direction(closedDirection, leaf.angle);
        leaf.away = Distance(feet, hinge, direction, width) > .5f ? leaf.away + dt : 0f;
        if (leaf.away >= .5f) leaf.episode = false;
        var normal = Vector3.Cross(Vector3.up, direction);
        var speed = inputMove.magnitude / dt;
        var moveDirection = speed > .001f ? inputMove.normalized : Vector3.zero;
        var predicted = feet + inputMove + moveDirection * .12f;
        var relative = Flat(predicted - hinge);
        var alongLeaf = Vector3.Dot(relative, direction);
        var planeDistance = Vector3.Dot(Flat(feet - hinge), normal);
        var into = Mathf.Abs(Vector3.Dot(moveDirection, normal));
        var pressing = speed >= 1f && alongLeaf >= .15f && alongLeaf <= width + .03f &&
            Distance(predicted, hinge, direction, width) <= Reach && into >= .5f &&
            Vector3.Dot(inputMove, normal) * planeDistance < 0f;
        var verb = Verb.None;

        // Only a deliberate look at the latch half followed by a backwards step pulls a leaf.
        if (!leaf.moving && !leaf.following && Mathf.Abs(leaf.angle) >= 3f)
        {
            var rayDenom = Vector3.Dot(viewForward, normal);
            var rayT = Mathf.Abs(rayDenom) > .0001f ? -planeDistance / rayDenom : -1f;
            var rayAlong = rayT > 0f ? Vector3.Dot(Flat(feet + viewForward * rayT - hinge), direction) : -1f;
            var handle = hinge + direction * (width * .85f);
            var doorSide = Vector3.Dot(closedDirection * (width * .5f) - direction * (width * .5f), normal);
            var canAim = rayT > 0f && rayT <= 1.4f && rayAlong >= width * .5f && rayAlong <= width &&
                Flat(feet - handle).sqrMagnitude <= 1.4f * 1.4f && planeDistance * doorSide >= -.001f;
            leaf.aim = canAim ? leaf.aim + dt : 0f;
            var backing = speed >= 1f && Vector3.Dot(moveDirection, -viewForward) >= .7660444f;
            leaf.back = leaf.aim >= PullAimSeconds && backing ? leaf.back + dt : 0f;
            if (leaf.back >= PullStepSeconds)
            {
                var slam = !slow && leaf.time - leaf.sprintPass <= 2f;
                Start(leaf, 0f, slam ? .30f : .55f, true);
                leaf.aim = leaf.back = 0f;
                verb = slam ? Verb.Slam : Verb.Shut;
            }
        }
        else leaf.aim = leaf.back = 0f;

        // Open leaves at rest are walls. A partial leaf can be pushed only while crossing its doorway.
        var partialCrossing = Mathf.Abs(leaf.angle) < 70f && closedAlong >= 0f && closedAlong <= width &&
            Mathf.Abs(Vector3.Dot(moveDirection, closedNormal)) >= .5f;
        if (pressing && !leaf.shutting && (Mathf.Abs(leaf.angle) < 3f || partialCrossing))
        {
            leaf.offContact = 0f;
            if (!leaf.moving)
            {
                var blind = Vector3.Dot(viewForward, moveDirection) < .5f;
                var delay = blind ? .40f : slow ? .30f : .12f;
                var barge = !blind && (sprinting || sinceSprint <= .15f && speed >= 3.19f);
                leaf.lean += dt;
                if (barge || leaf.lean >= delay || Mathf.Abs(leaf.angle) >= 3f)
                {
                    var sign = Mathf.Sign(Vector3.Dot(moveDirection, closedNormal));
                    if (slow)
                    {
                        if (!leaf.episode) verb = Verb.Ease;
                        leaf.episode = true;
                        leaf.following = true;
                        var minimum = ClearAngle(leaf.angle, sign * stop, hinge, closedDirection, width, predicted);
                        leaf.angle = Mathf.MoveTowards(leaf.angle, minimum, 40f);
                        if (Mathf.Abs(leaf.angle) < 3f && Mathf.Abs(leaf.from) < 3f) leaf.angle = sign * 3f;
                    }
                    else
                    {
                        Start(leaf, sign * stop, barge ? .30f : .55f, false);
                        if (!leaf.episode) verb = barge ? Verb.Barge : Verb.Ease;
                        leaf.episode = true;
                    }
                }
            }
            if (leaf.moving && !leaf.shutting)
            {
                var minimum = ClearAngle(leaf.angle, leaf.target, hinge, closedDirection, width, predicted);
                if (Mathf.Abs(leaf.target - minimum) < Mathf.Abs(leaf.target - leaf.angle)) leaf.angle = minimum;
            }
        }
        else
        {
            leaf.offContact += dt;
            if (leaf.offContact >= .10f) leaf.lean = 0f;
            leaf.following = false;
        }
        var finish = Advance(leaf, hinge, closedDirection, width, feet, dt);
        return finish != Verb.None ? finish : verb;
    }

    public static void Start(Leaf leaf, float target, float seconds, bool shutting)
    {
        leaf.from = leaf.angle;
        leaf.target = target;
        leaf.elapsed = 0f;
        leaf.seconds = Mathf.Max(.05f, seconds);
        leaf.moving = true;
        leaf.shutting = shutting;
        leaf.following = false;
    }

    static float ClearAngle(float from, float target, Vector3 hinge, Vector3 closedDirection, float width, Vector3 predicted)
    {
        var sign = Mathf.Sign(target - from);
        var angle = from;
        for (var i = 0; i < 200 && sign * (target - angle) > .001f; i++)
        {
            if (Distance(predicted, hinge, Direction(closedDirection, angle), width) >= Reach + .005f) break;
            angle = Mathf.MoveTowards(angle, target, .5f);
        }
        return angle;
    }

    static Verb Advance(Leaf leaf, Vector3 hinge, Vector3 closedDirection, float width, Vector3 feet, float dt)
    {
        if (!leaf.moving) return Verb.None;
        var nextElapsed = Mathf.Min(leaf.seconds, leaf.elapsed + dt);
        var u = nextElapsed / leaf.seconds;
        var ease = leaf.shutting ? u * u * (3f - 2f * u) : 1f - (1f - u) * (1f - u);
        var candidate = Mathf.Lerp(leaf.from, leaf.target, ease);
        // A predicted opening floor can already be further than this frame's clock.
        if (!leaf.shutting && Mathf.Abs(leaf.target - candidate) > Mathf.Abs(leaf.target - leaf.angle)) candidate = leaf.angle;
        var currentDistance = Distance(feet, hinge, Direction(closedDirection, leaf.angle), width);
        var nextDistance = Distance(feet, hinge, Direction(closedDirection, candidate), width);
        if (leaf.shutting && nextDistance < Reach && nextDistance < currentDistance - .0001f) return Verb.None;
        leaf.elapsed = nextElapsed;
        leaf.angle = candidate;
        if (u < 1f) return Verb.None;
        leaf.angle = leaf.target;
        leaf.moving = false;
        if (!leaf.shutting) return Verb.None;
        leaf.shutting = false;
        return Verb.Latched;
    }
}
