using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Int ids for Unity objects, the role Object.GetInstanceID() played before Unity 6.6 removed it. Its replacement,
/// EntityId, is 64-bit with no int form, so each EntityId gets a stable nonzero int for the session (0 stays free as
/// the "no object" key), and every int-keyed table and hash keeps working unchanged.
/// </summary>
public static class FrontRoomsObjectId
{
#if UNITY_6000_6_OR_NEWER
    static readonly Dictionary<EntityId, int> ids = new Dictionary<EntityId, int>();

    public static int Of(Object o)
    {
        var entity = o.GetEntityId();
        if (!ids.TryGetValue(entity, out var id)) { id = ids.Count + 1; ids[entity] = id; }
        return id;
    }
#else
    public static int Of(Object o) => o.GetInstanceID();
#endif
}
