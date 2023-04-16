using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Markup;

namespace MGUI.Core.UI.XAML
{
    [ContentProperty(nameof(Tabs))]
    public class TabControl2 : Element
    {
        public override MGElementType ElementType => MGElementType.TabControl2;

        public Border Border { get; set; } = new();

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public BorderBrush BorderBrush { get => Border.BorderBrush; set => Border.BorderBrush = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public BorderBrush BB { get => BorderBrush; set => BorderBrush = value; }

        [Category("Border")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Thickness? BorderThickness { get => Border.BorderThickness; set => Border.BorderThickness = value; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Browsable(false)]
        public Thickness? BT { get => BorderThickness; set => BorderThickness = value; }

        [Category("Appearance")]
        public StackPanel HeadersPanel { get; set; } = new();
        [Category("Appearance")]
        public FillBrush HeaderAreaBackground { get; set; }

        [Category("Appearance")]
        public Button SelectedTabHeaderTemplate { get; set; }
        [Category("Appearance")]
        public Button UnselectedTabHeaderTemplate { get; set; }

        [Category("Appearance")]
        public Dock TabStripPlacement { get; set; } = Dock.Top;

        [Category("Data")]
        public List<TabItem2> Tabs { get; set; } = new();

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent) => new MGTabControl2(Window, TabStripPlacement);

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGDesktop Desktop = Element.GetDesktop();

            MGTabControl2 TabControl = Element as MGTabControl2;
            Border.ApplySettings(Parent, TabControl.BorderComponent.Element, false);
            HeadersPanel.ApplySettings(TabControl, TabControl.HeadersPanelComponent.Element, false);

            if (HeaderAreaBackground != null)
                TabControl.HeaderAreaBackground.NormalValue = HeaderAreaBackground.ToFillBrush(Desktop);

            if (SelectedTabHeaderTemplate != null)
            {
                TabControl.SelectedTabHeaderTemplate = (TabItem) =>
                {
                    MGButton Button = new(TabItem.SelfOrParentWindow, x => TabItem.IsTabSelected = true);
                    TabControl.ApplyDefaultSelectedTabHeaderStyle(Button);
                    SelectedTabHeaderTemplate.ApplySettings(TabItem, Button, true);

                    //  When a Tab is selected, the wrapper Button is implcitly set to IsSelected=true.
                    //  If the user specifies a Background but not a SelectedBackground, they probably
                    //  meant to specify a SelectedBackground since the regular Background would do nothing
                    if (SelectedTabHeaderTemplate.Background != null && SelectedTabHeaderTemplate.SelectedBackground == null)
                        Button.BackgroundBrush.SelectedValue = Button.BackgroundBrush.NormalValue;
                    //  Same as above but for TextForeground
                    if (SelectedTabHeaderTemplate.TextForeground != null && SelectedTabHeaderTemplate.SelectedTextForeground == null)
                        Button.DefaultTextForeground.SelectedValue = Button.DefaultTextForeground.NormalValue;

                    return Button;
                };
            }

            if (UnselectedTabHeaderTemplate != null)
            {
                TabControl.UnselectedTabHeaderTemplate = (TabItem) =>
                {
                    MGButton Button = new(TabItem.SelfOrParentWindow, x => TabItem.IsTabSelected = true);
                    TabControl.ApplyDefaultUnselectedTabHeaderStyle(Button);
                    UnselectedTabHeaderTemplate.ApplySettings(TabItem, Button, true);
                    return Button;
                };
            }

            if (IncludeContent)
            {
                foreach (TabItem2 Child in Tabs)
                {
                    _ = Child.ToElement<MGTabItem2>(TabControl.ParentWindow, TabControl);
                }
            }
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            yield return Border;
            yield return HeadersPanel;

            foreach (TabItem2 Tab in Tabs)
                yield return Tab;
        }
    }

    public class TabItem2 : SingleContentHost
    {
        public TabItem2() { }

        public override MGElementType ElementType => MGElementType.TabItem2;

        public Element Header { get; set; }

        [Category("Behavior")]
        public bool? IsTabSelected { get; set; }

        protected override MGElement CreateElementInstance(MGWindow Window, MGElement Parent)
        {
            if (Parent is MGTabControl2 TabControl)
            {
                MGElement HeaderElement = Header?.ToElement<MGElement>(Window, null);
                MGElement ContentElement = Content?.ToElement<MGElement>(Window, null);
                return TabControl.AddTab(HeaderElement, ContentElement);
            }
            else
                throw new InvalidOperationException($"The {nameof(Parent)} {nameof(MGElement)} of an {nameof(MGTabItem2)} should be of type {nameof(MGTabControl2)}");
        }

        protected internal override void ApplyDerivedSettings(MGElement Parent, MGElement Element, bool IncludeContent)
        {
            MGTabItem2 TabItem = Element as MGTabItem2;

            if (IsTabSelected.HasValue)
                TabItem.IsTabSelected = IsTabSelected.Value;

            //base.ApplyDerivedSettings(Parent, Element, IncludeContent);
        }

        protected internal override IEnumerable<Element> GetChildren()
        {
            foreach (Element Element in base.GetChildren())
                yield return Element;

            if (Header != null)
                yield return Header;
        }
    }
}
