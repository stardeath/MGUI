using MGUI.Core.UI.Brushes.Border_Brushes;
using MGUI.Core.UI.Containers;
using MGUI.Shared.Helpers;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace MGUI.Core.UI
{
    public class MGTabControl2 : MGSingleContentHost
    {
        protected override void SetContentVirtual(MGElement Value)
        {
            if (_Content != Value)
            {
                if (!CanChangeContent)
                    throw new InvalidOperationException($"Cannot set {nameof(MGSingleContentHost)}.{nameof(Content)} while {nameof(CanChangeContent)} is false.");

                //_Content?.SetParent(null);
                //InvokeContentRemoved(_Content);
                _Content = Value;
                //_Content?.SetParent(this);
                LayoutChanged(this, true);
                //InvokeContentAdded(_Content);
                NPC(nameof(Content));
                NPC(nameof(HasContent));
            }
        }

        public override IEnumerable<MGElement> GetVisualTreeChildren(bool IncludeInactive, bool IncludeActive)
        {
            if (IncludeInactive)
            {
                foreach (MGTabItem2 Item in _Tabs.Where(x => !x.IsTabSelected))
                    yield return Item;
            }

            if (IncludeActive && SelectedTab != null)
                yield return SelectedTab;
        }


        #region Border
        /// <summary>Provides direct access to this element's border.</summary>
        public MGComponent<MGBorder> BorderComponent { get; }
        private MGBorder BorderElement { get; }
        public override MGBorder GetBorder() => BorderElement;

        public IBorderBrush BorderBrush
        {
            get => BorderElement.BorderBrush;
            set => BorderElement.BorderBrush = value;
        }

        public Thickness BorderThickness
        {
            get => BorderElement.BorderThickness;
            set => BorderElement.BorderThickness = value;
        }
        #endregion Border

        #region Tab Headers
        /// <summary>Provides direct access to to the stackpanel component that the tab headers are placed in at the top of this tabcontrol.</summary>
        public MGComponent<MGStackPanel> HeadersPanelComponent { get; }
        private MGStackPanel HeadersPanelElement { get; }

        public Dock TabStripPlacement { get; }
        private TabAlignmentData TabStripData => TabAlignmentDatas[(int)TabStripPlacement];

        /// <summary>The background brush of the entire header region of this <see cref="MGTabControl"/>. This is rendered behind the tab headers.<para/>
        /// To change the background of a specific tab, consider setting the <see cref="UnselectedTabHeaderTemplate"/> and <see cref="SelectedTabHeaderTemplate"/>.</summary>
        public VisualStateFillBrush HeaderAreaBackground
        {
            get => HeadersPanelElement.BackgroundBrush;
            set
            {
                if (HeadersPanelElement.BackgroundBrush != value)
                {
                    HeadersPanelElement.BackgroundBrush = value;
                    NPC(nameof(HeaderAreaBackground));
                }
            }
        }

        private bool ManagedAddHeadersPanelChild(MGSingleContentHost NewItem)
        {
            using (HeadersPanelElement.AllowChangingContentTemporarily())
            {
                return HeadersPanelElement.TryAddChild(NewItem);
            }
        }

        private bool ManagedReplaceHeadersPanelChild(MGSingleContentHost OldItem, MGSingleContentHost NewItem)
        {
            using (HeadersPanelElement.AllowChangingContentTemporarily())
            {
                return HeadersPanelElement.TryReplaceChild(OldItem, NewItem);
            }
        }

        private bool ManagedRemoveHeadersPanelChild(MGSingleContentHost ToRemove)
        {
            using (HeadersPanelElement.AllowChangingContentTemporarily())
            {
                return HeadersPanelElement.TryRemoveChild(ToRemove);
            }
        }

        public void ApplyDefaultSelectedTabHeaderStyle(MGButton Button)
        {
            Button.BorderThickness = TabStripData.DefaultSelectedTabHeaderStyle_Button_BorderThickness;
            Button.BorderBrush = MGUniformBorderBrush.Black;
            Button.Padding = new(8, 5, 8, 5);
            Button.BackgroundBrush = GetTheme().SelectedTabHeaderBackground.GetValue(true);
            //Button.DefaultTextForeground.SetAll(Color.Black);
            Button.VerticalAlignment = VerticalAlignment.Bottom;
        }

        public void ApplyDefaultUnselectedTabHeaderStyle(MGButton Button)
        {
            Button.BorderThickness = TabStripData.DefaultUnselectedTabHeaderStyle_Button_BorderThickness;
            Button.BorderBrush = MGUniformBorderBrush.Gray;
            Button.Padding = new(8, 3, 8, 3);
            Button.BackgroundBrush = GetTheme().UnselectedTabHeaderBackground.GetValue(true);
            //Button.DefaultTextForeground.SetAll(Color.Black);
            Button.VerticalAlignment = VerticalAlignment.Bottom;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<MGTabItem2, MGButton> _SelectedTabHeaderTemplate;
        /// <summary>Creates the wrapper element that hosts the given <see cref="MGTabItem"/>'s <see cref="MGTabItem.Header"/> for the selected tab.<para/>
        /// Default style: <see cref="ApplyDefaultSelectedTabHeaderStyle(MGButton)"/><para/>
        /// See also: <see cref="UnselectedTabHeaderTemplate"/></summary>
        public Func<MGTabItem2, MGButton> SelectedTabHeaderTemplate
        {
            get => _SelectedTabHeaderTemplate;
            set
            {
                if (_SelectedTabHeaderTemplate != value)
                {
                    _SelectedTabHeaderTemplate = value;
                    foreach (KeyValuePair<MGTabItem2, MGButton> KVP in ActualTabHeaders.ToList())
                    {
                        MGTabItem2 Tab = KVP.Key;
                        if (Tab.IsTabSelected)
                            UpdateHeaderWrapper(Tab);
                    }
                    NPC(nameof(SelectedTabHeaderTemplate));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<MGTabItem2, MGButton> _UnselectedTabHeaderTemplate;
        /// <summary>Creates the wrapper element that hosts the given <see cref="MGTabItem"/>'s <see cref="MGTabItem.Header"/> for tabs that aren't selected.<para/>
        /// Default style: <see cref="ApplyDefaultUnselectedTabHeaderStyle(MGButton)"/><para/>
        /// See also: <see cref="SelectedTabHeaderTemplate"/></summary>
        public Func<MGTabItem2, MGButton> UnselectedTabHeaderTemplate
        {
            get => _UnselectedTabHeaderTemplate;
            set
            {
                if (_UnselectedTabHeaderTemplate != value)
                {
                    _UnselectedTabHeaderTemplate = value;
                    foreach (KeyValuePair<MGTabItem2, MGButton> KVP in ActualTabHeaders.ToList())
                    {
                        MGTabItem2 Tab = KVP.Key;
                        if (!Tab.IsTabSelected)
                            UpdateHeaderWrapper(Tab);
                    }
                    NPC(nameof(UnselectedTabHeaderTemplate));
                }
            }
        }

        private void UpdateHeaderWrapper(MGTabItem2 Tab)
        {
            if (Tab != null && ActualTabHeaders.TryGetValue(Tab, out MGButton OldHeaderWrapper))
            {
                MGButton NewHeaderWrapper = Tab.IsTabSelected ? SelectedTabHeaderTemplate(Tab) : UnselectedTabHeaderTemplate(Tab);
                if (ManagedReplaceHeadersPanelChild(OldHeaderWrapper, NewHeaderWrapper))
                {
                    NewHeaderWrapper.IsSelected = Tab.IsTabSelected;
                    OldHeaderWrapper.SetContent(null as MGElement);
                    NewHeaderWrapper.SetContent(Tab.Header);
                    ActualTabHeaders[Tab] = NewHeaderWrapper;
                }
            }
        }

        private Dictionary<MGTabItem2, MGButton> ActualTabHeaders { get; }
        #endregion Tab Headers

        #region Tabs
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<MGTabItem2> _Tabs { get; }
        public IReadOnlyList<MGTabItem2> Tabs => _Tabs;

        public void RemoveTab(MGTabItem2 Tab)
        {
            if (_Tabs.Contains(Tab))
            {
                MGButton TabHeader = ActualTabHeaders[Tab];
                ActualTabHeaders.Remove(Tab);
                int TabIndex = _Tabs.IndexOf(Tab);

                _Tabs.Remove(Tab);
                InvokeContentRemoved(Tab);

                ManagedRemoveHeadersPanelChild(TabHeader);

                if (SelectedTab == Tab)
                {
                    int NewSelectedTabIndex = Math.Max(0, TabIndex - 1); // Focus to left of the closed tab
                    _ = TrySelectTabAtIndex(NewSelectedTabIndex);
                }
            }
        }

        public MGTabItem2 AddTab(string TabHeader, MGElement TabContent)
            => AddTab(new MGTextBlock(ParentWindow, TabHeader), TabContent);

        public MGTabItem2 AddTab(MGElement TabHeader, MGElement TabContent)
        {
            MGTabItem2 Tab = new(this, TabHeader, TabContent);

            MGButton HeaderWrapper = UnselectedTabHeaderTemplate(Tab);
            HeaderWrapper.SetContent(TabHeader);
            ActualTabHeaders.Add(Tab, HeaderWrapper);

            _Tabs.Add(Tab);
            InvokeContentAdded(Tab);

            ManagedAddHeadersPanelChild(HeaderWrapper);

            if (SelectedTab == null)
                _ = TrySelectTab(Tab);

            return Tab;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MGTabItem2 _SelectedTab;
        public MGTabItem2 SelectedTab { get => _SelectedTab; }
        public int SelectedTabIndex => _SelectedTab == null ? -1 : _Tabs.IndexOf(_SelectedTab);

        /// <summary>Invoked just before <see cref="SelectedTab"/> changes. Argument value is the new tab being selected. This event allows cancellation.</summary>
        public event EventHandler<CancelEventArgs<MGTabItem2>> SelectedTabChanging;
        /// <summary>Invoked when <see cref="SelectedTab"/> changes to a different <see cref="MGTabItem"/></summary>
        public event EventHandler<EventArgs<MGTabItem2>> SelectedTabChanged;

        /// <summary>Attempts to set the given <paramref name="Tab"/> as the <see cref="SelectedTab"/>.<para/>
        /// To deselect a tab, use <see cref="TryDeselectTab(MGTabItem, bool)"/> rather than a null <paramref name="Tab"/> parameter.</summary>
        /// <param name="Tab">Cannot be null, and should be a tab that belongs to this <see cref="MGTabControl"/> (I.E. it was created via <see cref="AddTab(MGElement, MGElement)"/>)</param>
        /// <returns>False if unable to select the given <paramref name="Tab"/>, such as if the value was null, or it belongs to a different <see cref="MGTabControl"/>, or the action was cancelled by <see cref="SelectedTabChanging"/> event.</returns>
        public bool TrySelectTab(MGTabItem2 Tab)
        {
            if (Tab != null && _Tabs.Contains(Tab) && Tab.TabControl == this && Tab != SelectedTab)
            {
                if (SelectedTabChanging != null)
                {
                    CancelEventArgs<MGTabItem2> CancelArgs = new(Tab);
                    SelectedTabChanging.Invoke(this, CancelArgs);
                    if (CancelArgs.Cancel)
                        return false;
                }

                MGTabItem2 Previous = SelectedTab;
                _SelectedTab = Tab;

                UpdateHeaderWrapper(Previous);
                UpdateHeaderWrapper(SelectedTab);

                SetContent(SelectedTab);
                NPC(nameof(SelectedTab));
                NPC(nameof(SelectedTabIndex));
                Previous?.NPC(nameof(MGTabItem2.IsTabSelected));
                _SelectedTab?.NPC(nameof(MGTabItem2.IsTabSelected));
                SelectedTabChanged?.Invoke(this, new(Previous, SelectedTab));
                return true;
            }
            else
                return false;
        }

        public bool TrySelectTabAtIndex(int Index)
        {
            if (Index >= 0 && Index < _Tabs.Count)
                return TrySelectTab(_Tabs[Index]);
            else
                return false;
        }

        /// <summary>Attempts to deselect the given <paramref name="Tab"/>. Does nothing if the <paramref name="Tab"/> is not already selected or if there are no other tabs to select in place of it.</summary>
        /// <param name="Tab">The tab to deselect.</param>
        /// <param name="FocusTabToRight">If true, will attempt to select the tab to the right of the tab being deselected.<br/>
        /// If false, will attempt to select the tab to the left of the tab being deselected.</param>
        public bool TryDeselectTab(MGTabItem2 Tab, bool FocusTabToRight)
        {
            if (Tab == null || Tab != SelectedTab || _Tabs.Count <= 1)
                return false;

            int TabIndex = _Tabs.IndexOf(Tab);
            if (TabIndex < 0)
                return false;

            int DesiredIndex = FocusTabToRight ? TabIndex + 1 : TabIndex - 1;
            int ActualIndex = (DesiredIndex + _Tabs.Count) % _Tabs.Count;
            return TrySelectTabAtIndex(ActualIndex);
        }
        #endregion Tabs

        public MGTabControl2(MGWindow Window, Dock tabStripPlacement) : base(Window, MGElementType.TabControl)
        {
            using (BeginInitializing())
            {
                this.TabStripPlacement = tabStripPlacement;

                this.HeadersPanelElement = new(Window, TabStripData.HeadersPanelElement_Orientation);
                this.HeadersPanelElement.CanChangeContent = false;
                this.HeadersPanelElement.Spacing = 0;
                this.HeadersPanelElement.VerticalAlignment = TabStripData.HeadersPanelElement_VerticalAlignment;
                this.HeadersPanelElement.HorizontalContentAlignment = TabStripData.HeadersPanelElement_HorizontalContentAlignment;
                this.HeadersPanelComponent = new
                (
                    HeadersPanelElement, // Element
                    ComponentUpdatePriority.BeforeContents, // UpdatePriority
                    ComponentDrawPriority.AfterContents, // DrawPriority
                    TabStripData.HeadersPanelComponent_IsWidthSharedWithContent, // IsWidthSharedWithContent
                    TabStripData.HeadersPanelComponent_IsHeightSharedWithContent, // IsHeightSharedWithContent
                    TabStripData.HeadersPanelComponent_ConsumesLeftSpace, // ConsumesLeftSpace
                    TabStripData.HeadersPanelComponent_ConsumesTopSpace, // ConsumesTopSpace
                    TabStripData.HeadersPanelComponent_ConsumesRightSpace, // ConsumesRightSpace
                    TabStripData.HeadersPanelComponent_ConsumesBottomSpace, // ConsumesBottomSpace
                    false, // UsesOwnersPadding
                    TabStripData.HeadersPanelArrangeFunc // Arrange
                );
                AddComponent(HeadersPanelComponent);

                this.BorderElement = new(Window);
                this.BorderComponent = MGComponentBase.Create(BorderElement);
                AddComponent(BorderComponent);

                BorderElement.OnBorderBrushChanged += (sender, e) => { NPC(nameof(BorderBrush)); };
                BorderElement.OnBorderThicknessChanged += (sender, e) => { NPC(nameof(BorderThickness)); };

                this.Padding = new(12);

                this.ActualTabHeaders = new();

                this._Tabs = new();
                _Tabs.CollectionChanged += (sender, e) =>
                {
                    if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace)
                    {
                        if (e.NewItems != null)
                        {
                            foreach (MGTabItem2 Item in e.NewItems)
                            {
                                Item.HeaderChanged += Tab_HeaderChanged;
                            }
                        }
                    }

                    if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Reset)
                    {
                        if (e.OldItems != null)
                        {
                            foreach (MGTabItem2 Item in e.OldItems)
                            {
                                Item.HeaderChanged -= Tab_HeaderChanged;
                            }
                        }
                    }
                };

                this.SelectedTabHeaderTemplate = (MGTabItem2 TabItem) =>
                {
                    MGButton Button = new(TabItem.SelfOrParentWindow, x => TabItem.IsTabSelected = true);
                    ApplyDefaultSelectedTabHeaderStyle(Button);
                    return Button;
                };

                this.UnselectedTabHeaderTemplate = (MGTabItem2 TabItem) =>
                {
                    MGButton Button = new(TabItem.SelfOrParentWindow, x => TabItem.IsTabSelected = true);
                    ApplyDefaultUnselectedTabHeaderStyle(Button);
                    return Button;
                };
            }
        }

        private void Init_DependsOnTabStripPlacement()
        {

        }

        private void Tab_HeaderChanged(object sender, EventArgs<MGElement> e)
        {
            MGTabItem2 TabItem = sender as MGTabItem2;
            this.ActualTabHeaders[TabItem].SetContent(e.NewValue);
        }

        public override void DrawBackground(ElementDrawArgs DA, Rectangle LayoutBounds)
        {
            //  The background only spans the content region of this TabControl,
            //  Does not fill the region with the tab headers
            Rectangle TabHeadersBounds = HeadersPanelElement.LayoutBounds;
            //Rectangle TabContentBounds = new( TabHeadersBounds.Right, LayoutBounds.Top, LayoutBounds.Width - TabHeadersBounds.Width, LayoutBounds.Height );
            Rectangle TabContentBounds = TabStripData.TabContentBoundsFunc(TabHeadersBounds, LayoutBounds);
            base.DrawBackground(DA, TabContentBounds);
        }

        public override void DrawSelf(ElementDrawArgs DA, Rectangle LayoutBounds) { }

        private Rectangle HeadersPanelArrangeFunc(Rectangle rectangle, Thickness thickness)
        {
            return ApplyAlignment(rectangle, HorizontalAlignment.Left, VerticalAlignment.Stretch, thickness.Size);
        }

        private struct TabAlignmentData
        {
            public Thickness DefaultSelectedTabHeaderStyle_Button_BorderThickness { get; init; }
            public Thickness DefaultUnselectedTabHeaderStyle_Button_BorderThickness { get; init; }

            public Orientation HeadersPanelElement_Orientation { get; init; }
            public VerticalAlignment HeadersPanelElement_VerticalAlignment { get; init; }
            public HorizontalAlignment HeadersPanelElement_HorizontalContentAlignment { get; init; }

            public bool HeadersPanelComponent_IsWidthSharedWithContent { get; init; }
            public bool HeadersPanelComponent_IsHeightSharedWithContent { get; init; }
            public bool HeadersPanelComponent_ConsumesLeftSpace { get; init; }
            public bool HeadersPanelComponent_ConsumesTopSpace { get; init; }
            public bool HeadersPanelComponent_ConsumesRightSpace { get; init; }
            public bool HeadersPanelComponent_ConsumesBottomSpace { get; init; }
            public Func<Rectangle, Thickness, Rectangle> HeadersPanelArrangeFunc { get; init; }

            public Func<Rectangle, Rectangle, Rectangle> TabContentBoundsFunc { get; init; }
        }

        // TODO : add Right and Bottom TabStripPlacement
        TabAlignmentData[] TabAlignmentDatas = new TabAlignmentData[]
        {
			// Dock.Left
			new TabAlignmentData
            {
                DefaultSelectedTabHeaderStyle_Button_BorderThickness = new( 1, 1, 0, 1 ),
                DefaultUnselectedTabHeaderStyle_Button_BorderThickness = new( 1, 1, 0, 1 ),
                HeadersPanelElement_Orientation = Orientation.Vertical,
                HeadersPanelElement_VerticalAlignment = VerticalAlignment.Top,
                HeadersPanelElement_HorizontalContentAlignment = HorizontalAlignment.Left,
                HeadersPanelComponent_IsWidthSharedWithContent = false,
                HeadersPanelComponent_IsHeightSharedWithContent = true,
                HeadersPanelComponent_ConsumesLeftSpace = true,
                HeadersPanelComponent_ConsumesTopSpace = true,
                HeadersPanelComponent_ConsumesRightSpace = true,
                HeadersPanelComponent_ConsumesBottomSpace = true,
                HeadersPanelArrangeFunc = ( AvailableBounds, ComponentSize ) => ApplyAlignment( AvailableBounds, HorizontalAlignment.Left, VerticalAlignment.Stretch, ComponentSize.Size ),
                TabContentBoundsFunc = ( TabHeadersBounds, LayoutBounds ) => new( TabHeadersBounds.Right, LayoutBounds.Top, LayoutBounds.Width - TabHeadersBounds.Width, LayoutBounds.Height )
            },
			// Dock.Top
			new TabAlignmentData
            {
                DefaultSelectedTabHeaderStyle_Button_BorderThickness = new( 1, 1, 1, 0 ),
                DefaultUnselectedTabHeaderStyle_Button_BorderThickness = new( 1, 1, 1, 0 ),
                HeadersPanelElement_Orientation = Orientation.Horizontal,
                HeadersPanelElement_VerticalAlignment = VerticalAlignment.Bottom,
                HeadersPanelElement_HorizontalContentAlignment = HorizontalAlignment.Left,
                HeadersPanelComponent_IsWidthSharedWithContent = true,
                HeadersPanelComponent_IsHeightSharedWithContent = false,
                HeadersPanelComponent_ConsumesLeftSpace = true,
                HeadersPanelComponent_ConsumesTopSpace = true,
                HeadersPanelComponent_ConsumesRightSpace = false,
                HeadersPanelComponent_ConsumesBottomSpace = false,
                HeadersPanelArrangeFunc = ( AvailableBounds, ComponentSize ) => ApplyAlignment( AvailableBounds, HorizontalAlignment.Stretch, VerticalAlignment.Top, ComponentSize.Size ),
                TabContentBoundsFunc = ( TabHeadersBounds, LayoutBounds ) => new( LayoutBounds.Left, TabHeadersBounds.Bottom, LayoutBounds.Width, LayoutBounds.Height - TabHeadersBounds.Height )
            }
        };
    }
}
