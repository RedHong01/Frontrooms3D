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
        // The leaf's act is a body pushing it back to its frame (push-shut, 11-04): its squeeze, or its eased-out swing.
        public bool pushed;
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
        if (leaf.away >= .5f)
        {
            leaf.episode = false;
            if (!leaf.moving) leaf.pushed = false;
        }
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

        // A partial leaf can be pushed open only while crossing its doorway. Push-shut (Red 2026-11-04, the map doors' rule): a
        // press on the face a leaf hangs toward takes it back to its frame. SLOW follows it there (it stays where it is left and
        // clicks shut near the frame), a walk pushes it ahead of the body, a run into its face slams it; a run into the doorway
        // still goes through. Touch-close (decision 14B): a press on the face the body pushed of a leaf open at rest, facing it,
        // closes it after the walk's lean (0.30 s slow) as the pull does; with no step hook here the leaf waits on the body.
        var crossing = closedAlong >= 0f && closedAlong <= width && Mathf.Abs(Vector3.Dot(moveDirection, closedNormal)) >= .5f;
        var partialCrossing = Mathf.Abs(leaf.angle) < 70f && crossing;
        var blind = Vector3.Dot(viewForward, moveDirection) < .5f;
        var barge = !blind && (sprinting || sinceSprint <= .15f && speed >= 3.19f);
        var ahead = Mathf.Abs(leaf.angle) >= 3f && Ahead(leaf.angle, hinge, closedDirection, feet);
        var pushShut = pressing && ahead && (leaf.pushed || !leaf.moving && !leaf.shutting && !(barge && crossing));
        var touchClose = pressing && !ahead && Mathf.Abs(leaf.angle) >= 70f && !leaf.moving && !leaf.following
            && Vector3.Dot(viewForward, -normal * Mathf.Sign(planeDistance)) >= .5f;
        if (touchClose)
        {
            leaf.offContact = 0f;
            leaf.lean += dt;
            if (leaf.lean >= (slow ? .30f : .12f))
            {
                var slam = !slow && speed >= 3.19f && leaf.time - leaf.sprintPass <= 2f;
                Start(leaf, 0f, slam ? .30f : .55f, true);
                leaf.lean = 0f;
                leaf.episode = true;
                verb = slam ? Verb.Slam : Verb.Shut;
            }
        }
        else if (pushShut)
        {
            leaf.offContact = 0f;
            leaf.lean = 0f;
            if (!leaf.pushed)
            {
                verb = barge && !slow ? Verb.Slam : Verb.Shut;
                leaf.episode = true;
            }
            leaf.pushed = true;
            if (slow && !leaf.moving)
            {
                leaf.following = true;
                var minimum = ClearAngle(leaf.angle, 0f, hinge, closedDirection, width, predicted);
                leaf.angle = Mathf.MoveTowards(leaf.angle, minimum, 40f);
                if (Mathf.Abs(leaf.angle) < 3f)
                {
                    leaf.angle = 0f;
                    leaf.following = leaf.pushed = leaf.episode = false;
                    return Verb.Latched;
                }
            }
            else
            {
                if (!leaf.moving)
                {
                    // The verb's ease-out back to the frame, quickened when the body's straight path needs it (planned in the
                    // far side's frame, where the leaf runs from minus its angle up to 0), so the leaf stays ahead of the body.
                    var leafSide = Mathf.Sign(leaf.angle);
                    var seconds = PlannedSeconds(leaf.angle, -leafSide, 0f,
                        Mathf.Max(MinSwingSeconds, (barge ? .30f : .55f) * Mathf.Abs(leaf.angle) / stop),
                        hinge, closedDirection, width, feet, inputMove / dt, dt);
                    Start(leaf, 0f, seconds, true);
                    leaf.pushed = true;
                }
                var floor = ClearAngle(leaf.angle, 0f, hinge, closedDirection, width, predicted);
                if (Mathf.Abs(floor) < Mathf.Abs(leaf.angle)) leaf.angle = floor;
            }
        }
        else if (pressing && !leaf.shutting && (Mathf.Abs(leaf.angle) < 3f || partialCrossing))
        {
            leaf.offContact = 0f;
            if (!leaf.moving)
            {
                var delay = blind ? .40f : slow ? .30f : .12f;
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
                        // The verb's ease-out, quickened when the body's straight path needs it (a hit near the hinge), so the
                        // floor below never has to pop the leaf ahead of the body (the map doors' law, 2026-11-01).
                        var seconds = PlannedSeconds(leaf.angle, sign, stop, barge ? .30f : .55f, hinge, closedDirection, width,
                            feet, inputMove / dt, dt);
                        Start(leaf, sign * stop, seconds, false);
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
        leaf.following = leaf.pushed = false;
    }

    // The longest swing, up to the verb's seconds and down to MinSwingSeconds, whose ease-out from the leaf's angle keeps it past a
    // body going on at its velocity from feet (Reach plus PlanSlack from the leaf), sampled every PlanStep for PlanMetres. Each
    // sample looks 0.12 m further on, as Tick's floor (ClearAngle on the predicted point) does, so the floor never leads the curve.
    const float MinSwingSeconds = .12f, PlanSlack = .015f, PlanStep = .01f, PlanMetres = 1.6f;

    static float PlannedSeconds(float from, float sign, float stop, float seconds, Vector3 hinge, Vector3 closedDirection,
        float width, Vector3 feet, Vector3 velocity, float dt)
    {
        velocity = Flat(velocity);
        var speed = velocity.magnitude;
        if (speed < .01f || sign == 0f) return seconds;
        var normal = Vector3.Cross(Vector3.up, closedDirection) * sign;
        var start = from * sign;
        var span = stop - start;
        if (span <= 1f) return seconds;
        var until = dt + Mathf.Clamp(PlanMetres / speed, .15f, .6f);
        var ahead = velocity / speed * .12f;
        var planned = seconds;
        for (var t = dt; t <= until + 1e-4f; t += PlanStep)
        {
            var offset = Flat(feet + velocity * t + ahead - hinge);
            var p = new Vector2(Vector3.Dot(offset, closedDirection), Vector3.Dot(offset, normal));
            var r = p.magnitude;
            // The slack shrinks to nothing for a body passing right by the hinge, where a full swing only just clears the reach.
            var reach = Mathf.Max(Reach, Mathf.Min(Reach + PlanSlack, r - .005f));
            if (r >= width + reach) continue;
            var need = 90f;
            if (r > reach)
            {
                var bearing = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                var half = r <= width ? Mathf.Asin(reach / r) * Mathf.Rad2Deg
                    : Mathf.Acos(Mathf.Clamp((width * width + r * r - reach * reach) / (2f * width * r), -1f, 1f)) * Mathf.Rad2Deg;
                // A body ahead of the leaf as planned so far is one it stops on, not one it clears.
                var u = Mathf.Min(1f, t / planned);
                if (bearing - half > start + span * u * (2f - u)) continue;
                need = Mathf.Min(bearing + half, 90f);
            }
            var e = (need - start) / span;
            if (e <= 0f) continue;
            // The ease-out reaches share e of its span at u = 1 - sqrt(1 - e).
            var share = 1f - Mathf.Sqrt(1f - Mathf.Min(e, .999f));
            planned = Mathf.Min(planned, t / share);
        }
        return Mathf.Clamp(planned, MinSwingSeconds, seconds);
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
        // A pull eases in and out; an open and a body's push back to the frame ease out.
        var ease = leaf.shutting && !leaf.pushed ? u * u * (3f - 2f * u) : 1f - (1f - u) * (1f - u);
        var candidate = Mathf.Lerp(leaf.from, leaf.target, ease);
        // A predicted floor (an open's, or a push-shut's) can already be further than this frame's clock.
        if ((!leaf.shutting || leaf.pushed) && Mathf.Abs(leaf.target - candidate) > Mathf.Abs(leaf.target - leaf.angle)) candidate = leaf.angle;
        var currentDistance = Distance(feet, hinge, Direction(closedDirection, leaf.angle), width);
        var nextDistance = Distance(feet, hinge, Direction(closedDirection, candidate), width);
        if (leaf.shutting && nextDistance < Reach && nextDistance < currentDistance - .0001f) return Verb.None;
        leaf.elapsed = nextElapsed;
        leaf.angle = candidate;
        if (u < 1f) return Verb.None;
        leaf.angle = leaf.target;
        leaf.moving = false;
        if (!leaf.shutting) return Verb.None;
        // Latched: pressing on into it is a new act (its lean, then an open with its own verb).
        leaf.shutting = leaf.pushed = leaf.episode = false;
        return Verb.Latched;
    }

    // The face a leaf out of the frame hangs toward: the body's bearing from the hinge is past the leaf's angle, on its side.
    static bool Ahead(float angle, Vector3 hinge, Vector3 closedDirection, Vector3 feet)
    {
        var bearing = Vector3.SignedAngle(closedDirection, Flat(feet - hinge), Vector3.up);
        return angle > 0f ? bearing > angle : bearing < angle;
    }
}
