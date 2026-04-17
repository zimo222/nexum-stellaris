using TMPro;
using UnityEngine;

public class QuestItemView : MonoBehaviour
{
    public TMP_Text questNameText;
    public TMP_Text chapterNameText;

    public void UpdateUI(QuestDefineSO questData)
    {
        if (questNameText != null)
            questNameText.text = questData.questName;
        if (chapterNameText != null)
            chapterNameText.text = questData.chapterName + "   µÚ" + questData.questNum + "Ä»";
    }
}