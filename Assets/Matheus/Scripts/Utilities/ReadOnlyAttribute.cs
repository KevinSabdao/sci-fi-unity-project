using UnityEngine;

namespace COMP602
{
    // marks a serialized field as visible but not editable in the inspector
    // drawn by ReadOnlyDrawer in the Editor folder
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}