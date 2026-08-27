using UnityEngine.UIElements;

// Pill "icon + text" dung chung cho Gold/Gem (Inventory, Main Menu) va Level (Main Menu).
// Trong UI Builder, sau khi keo component nay vao canvas, mo panel Inspector ben phai se thay
// truong "Icon Class" - go ten class USS (vi du "icon-gold", "icon-gem", "icon-level") ma
// KHONG can viet code, component tu ap dung class do vao phan icon.
public class IconLabelElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<IconLabelElement, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private readonly UxmlStringAttributeDescription _iconClass =
            new UxmlStringAttributeDescription { name = "icon-class", defaultValue = "icon-gold" };

        private readonly UxmlStringAttributeDescription _initialText =
            new UxmlStringAttributeDescription { name = "text", defaultValue = "0" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var el = (IconLabelElement)ve;
            el.SetIconClass(_iconClass.GetValueFromBag(bag, cc));
            el.SetText(_initialText.GetValueFromBag(bag, cc));
        }
    }

    private readonly VisualElement _icon;
    private readonly Label _label;
    private string _currentIconClass;

    public IconLabelElement()
    {
        AddToClassList("icon-label");

        _icon = new VisualElement();
        _icon.AddToClassList("icon-label__icon");
        Add(_icon);

        _label = new Label("0");
        _label.AddToClassList("icon-label__text");
        Add(_label);
    }

    public void SetIconClass(string ussClass)
    {
        if (!string.IsNullOrEmpty(_currentIconClass))
            _icon.RemoveFromClassList(_currentIconClass);

        _currentIconClass = ussClass;
        if (!string.IsNullOrEmpty(ussClass))
            _icon.AddToClassList(ussClass);
    }

    public void SetText(string text)
    {
        _label.text = text;
    }

    // Dung khi gan gia tri so (Gold/Gem/Level) - dinh dang co dau phay ngan cach hang nghin.
    public void SetAmount(int amount)
    {
        _label.text = amount.ToString("N0");
    }
}
