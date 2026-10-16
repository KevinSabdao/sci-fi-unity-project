using UnityEngine;
using UnityEngine.EventSystems;

internal enum OptionType
{
    Delete,
    DropAll,
    DropOne
}

public class ItemOption : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private OptionType optionType;
    private Inventory inventory;

    public void Start()
    {
        this.inventory = Inventory.Instance;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            switch (this.optionType)
            {
                case OptionType.Delete: this.inventory.DeleteSelectedItem(); break;
                case OptionType.DropAll: this.inventory.DropSelectedItemAll(); break;
                case OptionType.DropOne: this.inventory.DropSelectedItemOne(); break;
            }
        }
    }
}
