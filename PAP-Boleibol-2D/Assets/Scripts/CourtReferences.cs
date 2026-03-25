using UnityEngine;

public static class CourtReferences
{
    public static Transform FindNetPosition()
    {
        GameObject netObject = GameObject.Find("net");
        if (netObject != null)
        {
            Transform netCheck = netObject.transform.Find("netcheck");
            return netCheck != null ? netCheck : netObject.transform;
        }

        GameObject netCheckObject = GameObject.Find("netcheck");
        return netCheckObject != null ? netCheckObject.transform : null;
    }

    public static bool IsPlayableNetPosition(Transform netTransform)
    {
        if (netTransform == null)
            return false;

        if (netTransform.name == "net")
            return true;

        return netTransform.name == "netcheck"
            && netTransform.parent != null
            && netTransform.parent.name == "net";
    }

    public static Transform FindBoundary(string boundaryObjectName)
    {
        GameObject boundaryObject = GameObject.Find(boundaryObjectName);
        return boundaryObject != null ? boundaryObject.transform : null;
    }

    public static bool TryGetBoundaryInnerX(Transform boundary, bool isLeftBoundary, out float innerX)
    {
        innerX = 0f;

        if (boundary == null)
            return false;

        Collider2D boundaryCollider = boundary.GetComponent<Collider2D>();
        if (boundaryCollider != null)
        {
            Bounds bounds = boundaryCollider.bounds;
            innerX = isLeftBoundary ? bounds.max.x : bounds.min.x;
            return true;
        }

        innerX = boundary.position.x;
        return true;
    }
}
