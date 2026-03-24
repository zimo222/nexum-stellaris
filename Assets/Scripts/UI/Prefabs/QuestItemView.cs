using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestItemView : MonoBehaviour
{
    public TMP_Text questNameText;
    public TMP_Text chapterNameText;


    public TMP_Text categoryText;
    public TMP_Text chapterNumText;
    public TMP_Text questNumText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateUI(QuestDefineSO questData)
    {
        if (questNameText != null) questNameText.text = questData.questName;
        if (chapterNameText != null) chapterNameText.text = questData.chapterName;

        if (categoryText != null) categoryText.text = questData.category == QuestCategory.Main ? "主线" : "世界";
        if (chapterNumText != null) chapterNumText.text = "第" + questData.chapterNum + "章";
        if (questNumText != null) questNumText.text = "第" + questData.questNum + "幕";
    }


}
