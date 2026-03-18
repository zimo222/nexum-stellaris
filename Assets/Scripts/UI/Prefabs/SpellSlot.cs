using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SpellSlot : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex;                       // 槽位索引（0~n-1）
    public Image iconImage;                      // 显示模块图标的Image
    private SpellModuleSO currentModule;         // 当前槽位中的模块

    private void Start()
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(false);
    }

    // 放入模块
    public void SetModule(SpellModuleSO module)
    {
        currentModule = module;
        if (module != null && module.icon != null)
        {
            iconImage.sprite = module.icon;
            iconImage.gameObject.SetActive(true);
        }
        else
        {
            iconImage.gameObject.SetActive(false);
        }
    }

    // 清空槽位
    public void ClearSlot()
    {
        currentModule = null;
        iconImage.gameObject.SetActive(false);
    }

    public SpellModuleSO GetModule() => currentModule;

    // 点击槽位：如果是空槽，尝试从当前选中的模块库项中放入；如果已有模块，则取出到选中？
    // 简化：我们用拖拽或双击方式。先实现：点击模块库项，再点击空槽放入。
    // 这里我们使用外部管理器处理逻辑，槽位仅提供点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        SpellCraftingPanel.Instance.OnSlotClicked(this);
    }
}