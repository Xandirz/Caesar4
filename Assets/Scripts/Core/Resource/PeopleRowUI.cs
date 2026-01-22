using TMPro;
using UnityEngine;

public class PeopleRowUI : MonoBehaviour
{
    [Header("Only numbers (names are static in prefab)")]
    [SerializeField] private TMP_Text total_amount;
    [SerializeField] private TMP_Text workers_amount;
    [SerializeField] private TMP_Text idle_amount;
    [SerializeField] private TMP_Text mood_amount;

    public void SetAmounts(int total, int workers, int idle, int moodPercent)
    {
        if (total_amount != null) total_amount.text = total.ToString();
        if (workers_amount != null) workers_amount.text = workers.ToString();

        if (idle_amount != null)
        {
            // сохраняем старую логику окраски idle: green если >0 иначе red
            string idleColor = idle > 0 ? "green" : "red";
            idle_amount.text = $"<color={idleColor}>{idle}</color>";
        }
        if (mood_amount != null)
        {
            // если у тебя Mood хранится как 0..100 — просто выводим %
            mood_amount.text = $"{moodPercent}%";
        }
    }
}