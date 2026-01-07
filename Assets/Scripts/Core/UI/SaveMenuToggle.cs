using UnityEngine;

public class SaveMenuToggle : MonoBehaviour
{
    [SerializeField] GameObject panel;




    public void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
    }
}