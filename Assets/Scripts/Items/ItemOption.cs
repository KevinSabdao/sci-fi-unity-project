using UnityEngine;
using UnityEngine.EventSystems;

internal enum OptionType
{
    Delete,
    Drop
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
                case OptionType.Drop: this.inventory.DropSelectedItem(); break;
            }
        }
    }
}
