using UnityEngine;

// Gan attribute nay cung voi [SerializeReference] tren field/list interface hoac abstract class
// de Inspector hien dropdown chon class cu the (xem SerializeReferenceDropdownDrawer.cs trong Editor/).
//
// Vi du dung trong IEffect.cs:
//   [SerializeReference, SerializeReferenceDropdown]
//   public List<IEffect<IAttackable>> effects = new();
public class SerializeReferenceDropdownAttribute : PropertyAttribute
{
    
}