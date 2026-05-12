using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ModuleLibraryItem : MonoBehaviour, IPointerClickHandler
{
    public SpellModuleSO module;
    public Image iconImage;
    public TMP_Text moduleNameText;
    public SpellCraftingPanel craftingPanel;

    private Vector3 originalScale;   // 记录原始缩放

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Init(SpellModuleSO moduleData)
    {
        module = moduleData;
        if (iconImage != null) iconImage.sprite = moduleData.icon;
        this.name = moduleData.id;
        //if (moduleNameText != null) moduleNameText.text = moduleData.moduleName;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (craftingPanel != null)
            craftingPanel.OnLibraryItemClicked(this);
    }

    // 设置高亮状态（缩放变化）
    public void SetHighlight(bool highlight)
    {
        if (highlight)
            transform.localScale = originalScale * 1.2f;   // 放大1.2倍
        else
            transform.localScale = originalScale;
    }
}