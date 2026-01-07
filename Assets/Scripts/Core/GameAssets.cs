using UnityEngine;

public class GameAssets : MonoBehaviour
{
    public static GameAssets i;

    [Header("PopUps")]
    public Transform pfMessagePopUp;

    private void Awake()
    {
        if (i != null && i != this)
        {
            Destroy(gameObject);
            return;
        }

        i = this;
    }
}