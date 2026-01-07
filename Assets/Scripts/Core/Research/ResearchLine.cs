using UnityEngine;

public class ResearchLine : MonoBehaviour
{
    public RectTransform RectTransform => (RectTransform)transform;

    public void Connect(RectTransform from, RectTransform to)
    {
        Vector2 start = from.anchoredPosition;
        Vector2 end   = to.anchoredPosition;
        Vector2 dir   = end - start;
        float dist    = dir.magnitude;

        RectTransform rt = RectTransform;
        rt.sizeDelta = new Vector2(dist, rt.sizeDelta.y);
        rt.anchoredPosition = start + dir * 0.5f;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
    }
}