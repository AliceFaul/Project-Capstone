using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

// Tab switcher dung chung - dung ngay cho Upgrade/Feed, nhung viet generic de tai dung o cac
// man sau (vi du settings co nhieu tab). Moi tab = 1 nut + 1 content panel tuong ung, chi hien
// dung 1 content panel tai 1 thoi diem.
public class TabGroupElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<TabGroupElement, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits { }

    private readonly VisualElement _tabBar;
    private readonly VisualElement _contentContainer;
    private readonly List<(Button button, VisualElement content)> _tabs = new();
    private int _activeIndex = -1;

    public event Action<int> TabChanged;

    public TabGroupElement()
    {
        AddToClassList("tab-group");

        _tabBar = new VisualElement();
        _tabBar.AddToClassList("tab-group__bar");
        Add(_tabBar);

        _contentContainer = new VisualElement();
        _contentContainer.AddToClassList("tab-group__content");
        Add(_contentContainer);
    }

    // Goi 1 lan luc khoi tao man hinh, vi du:
    //   tabGroup.AddTab("Upgrade", upgradePanelElement);
    //   tabGroup.AddTab("Feed", feedPanelElement);
    public void AddTab(string label, VisualElement content)
    {
        var button = new Button { text = label };
        button.AddToClassList("tab-group__tab-button");

        int index = _tabs.Count;
        button.clicked += () => SelectTab(index);

        _tabBar.Add(button);
        content.style.display = DisplayStyle.None;
        _contentContainer.Add(content);

        _tabs.Add((button, content));

        if (_activeIndex < 0)
        {
            SelectTab(0);
        }
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= _tabs.Count || index == _activeIndex)
            return;

        if (_activeIndex >= 0)
        {
            _tabs[_activeIndex].content.style.display = DisplayStyle.None;
            _tabs[_activeIndex].button.RemoveFromClassList("tab-group__tab-button--active");
        }

        _activeIndex = index;
        _tabs[_activeIndex].content.style.display = DisplayStyle.Flex;
        _tabs[_activeIndex].button.AddToClassList("tab-group__tab-button--active");

        TabChanged?.Invoke(index);
    }
}
