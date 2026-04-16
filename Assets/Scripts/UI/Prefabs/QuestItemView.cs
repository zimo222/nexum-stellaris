using TMPro;
using UnityEngine;

public class QuestItemView : MonoBehaviour
{
    public TMP_Text questNameText;
    public TMP_Text chapterNameText;
    public TMP_Text chapterNumText;
    public TMP_Text questNumText;

    public void UpdateUI(QuestDefineSO questData)
    {
        if (questNameText != null)
            questNameText.text = questData.questName;
        if (chapterNameText != null)
            chapterNameText.text = questData.chapterName;
        if (chapterNumText != null)
            chapterNumText.text = "µÚ" + questData.chapterNum + "ÕÂ";
        if (questNumText != null)
            questNumText.text = "µÚ" + questData.questNum + "Ä»";
    }
}