using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    private const int MaxLevel = 100;

    private int level = 1;
    private int experience;

    public int Level => level;
    public int Experience => experience;

    public int RequiredExperience => level >= MaxLevel ? 0 : level * 100;


    public void AddExperience(int amount)
    {
        if (amount <= 0 || level >= MaxLevel)
            return;

        experience += amount;

        while(level < MaxLevel && experience >= RequiredExperience)
        {
            experience -= RequiredExperience;

            LevelUp();
        }

        if (level >= MaxLevel)
            experience = 0;
    }

    private void LevelUp()
    {
        level++;

        Debug.Log("현재 플레이어 레벨 : " +  level);
    }
}
