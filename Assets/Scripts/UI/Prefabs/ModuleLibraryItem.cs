using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ModuleLibraryItem : MonoBehaviour, IPointerClickHandler
{
    public SpellModuleSO module;                 // 关联的模块数据
    public Image iconImage;
    public TMP_Text moduleNameText;

    public void Init(SpellModuleSO moduleData)
    {
        module = moduleData;
        if (iconImage != null) iconImage.sprite = moduleData.icon;
        if (moduleNameText != null) moduleNameText.text = moduleData.moduleName;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SpellCraftingPanel.Instance.OnLibraryItemClicked(this);
    }
}