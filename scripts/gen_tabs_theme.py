#!/usr/bin/env python3
"""Generates src/AvaloniaUIKit/Themes/Controls/Tabs.axaml and
src/AvaloniaUIKit.Tabalonia/Themes/Tabs.axaml.

GPUI Kit's TabBar / Tab (crates/component/src/tab) have five variants and
four sizes, and nearly every metric depends on both. XAML cannot compute
them, so every combination is written out here, for TabStrip / TabStripItem,
for the strip of TabControl / TabItem and for Tabalonia's TabsControl /
DragTabItem alike.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "src/AvaloniaUIKit/Themes/Controls/Tabs.axaml"
TABALONIA_OUT = ROOT / "src/AvaloniaUIKit.Tabalonia/Themes/Tabs.axaml"

VARIANTS = ["tab", "outline", "pill", "segmented", "underline"]
SIZES = ["xsmall", "small", "medium", "large"]
OWNERS = [("TabStrip", "TabStripItem"), ("TabControl", "TabItem")]
TABALONIA_OWNERS = [("tab|TabsControl", "tab|DragTabItem")]

# tab.rs TabVariant::height / inner_height / inner_paddings / inner_margins.
HEIGHT = {v: [20, 24, 32, 36] for v in VARIANTS} | {"underline": [26, 30, 36, 44]}
INNER_HEIGHT = {
    "tab": [18, 22, 30, 36],
    "outline": [18, 22, 26, 36],
    "pill": [18, 22, 26, 36],
    "segmented": [16, 18, 24, 28],
    "underline": [20, 22, 26, 32],
}
PADDING_X = [8, 10, 12, 16]
UNDERLINE_MARGINS = [(1, 2), (2, 3), (3, 4), (5, 6)]
FONT_SIZE = [12, 14, 14, 16]
LINE_HEIGHT = [15, 17.5, 17.5, 20]  # the inner line_height(relative(1.25))
ICON_SIZE = [10, 14, 16, 16]
BORDER = {"tab": "1,0,1,0", "outline": "1", "pill": "0", "segmented": "0", "underline": "0,0,0,2"}
BORDER_WIDTH = {"tab": 1, "outline": 1, "pill": 0, "segmented": 0, "underline": 2}
# radius(): radius_full (clamped to half the height) or the bar's radius.
RADIUS = {
    "tab": [0, 0, 0, 0],
    "outline": [10, 12, 16, 18],
    "pill": [10, 12, 16, 18],
    "segmented": [6, 6, 8, 8],
    "underline": [0, 0, 0, 0],
}
SEGMENTED_INNER_RADIUS = [4, 4, 6, 5]
# tab_bar.rs TabBar::render: background, horizontal padding and gap.
BAR_BACKGROUND = {"tab": "{DynamicResource UIKit.TabBar}", "segmented": "{DynamicResource UIKit.TabBarSegmented}"}
SEGMENTED_PADDING = [2, 3, 4, 4]
GAP = {
    "tab": [0, 0, 0, 0],
    "outline": [8, 8, 12, 16],
    "pill": [4, 4, 4, 4],
    "segmented": [2, 2, 2, 2],
    "underline": [10, 12, 16, 20],
}
BASELINE = {"tab", "underline"}

T = "Transparent"
# (foreground, background, border) by state (tab.rs normal / hovered / selected / disabled).
COLORS = {
    "tab": {
        "normal": ("UIKit.TabForeground", T, T),
        "hover": ("UIKit.TabActiveForeground", T, T),
        "selected": ("UIKit.TabActiveForeground", "UIKit.TabActive", "UIKit.Border"),
        "disabled": ("UIKit.MutedForeground", T, T),
        "disabled-selected": ("UIKit.MutedForeground", T, "UIKit.Border"),
    },
    "outline": {
        "normal": ("UIKit.TabForeground", T, "UIKit.Border"),
        "hover": ("UIKit.SecondaryForeground", "UIKit.SecondaryHover", "UIKit.Border"),
        "selected": ("UIKit.Primary", T, "UIKit.Primary"),
        "disabled": ("UIKit.MutedForeground", T, "UIKit.Border"),
        "disabled-selected": ("UIKit.MutedForeground", T, "UIKit.Primary"),
    },
    # The selected pill, segment and underline paint nothing: the indicator does.
    "pill": {
        "normal": ("UIKit.Foreground", T, T),
        "hover": ("UIKit.SecondaryForeground", "UIKit.Secondary", T),
        "selected": ("UIKit.PrimaryForeground", T, T),
        "disabled": ("UIKit.MutedForeground", T, T),
        "disabled-selected": ("UIKit.Tab.Pill.Disabled.SelectedForeground", "UIKit.Tab.Pill.Disabled.SelectedBackground", T),
    },
    "segmented": {
        "normal": ("UIKit.TabForeground", T, T),
        "hover": ("UIKit.TabActiveForeground", T, T),
        "selected": ("UIKit.TabActiveForeground", T, T),
        "disabled": ("UIKit.MutedForeground", "UIKit.TabBar", T),
        "disabled-selected": ("UIKit.MutedForeground", "UIKit.TabBar", T),
    },
    "underline": {
        "normal": ("UIKit.TabForeground", T, T),
        "hover": ("UIKit.TabActiveForeground", T, T),
        "selected": ("UIKit.TabActiveForeground", T, T),
        "disabled": ("UIKit.MutedForeground", T, T),
        "disabled-selected": ("UIKit.MutedForeground", T, "UIKit.Border"),
    },
}
# Later states win: hover only on an enabled, unselected tab; a disabled tab keeps its look.
STATES = [
    ("normal", ""),
    ("hover", ":pointerover:not(:selected):not(:disabled)"),
    ("selected", ":selected"),
    ("disabled", ":disabled"),
    ("disabled-selected", ":disabled:selected"),
]

VARIANT_SEL = {
    "tab": ":not(.outline):not(.pill):not(.segmented):not(.underline)",
    "outline": ".outline",
    "pill": ".pill",
    "segmented": ".segmented",
    "underline": ".underline",
}
SIZE_SEL = {"xsmall": ".xsmall", "small": ".small", "medium": ":not(.xsmall):not(.small):not(.large)", "large": ".large"}


def num(value: float) -> str:
    return f"{value:g}"


def brush(key: str) -> str:
    return key if key == T else f"{{DynamicResource {key}}}"


def setters(props: dict, indent: str) -> str:
    return "\n".join(f'{indent}<Setter Property="{k}" Value="{v}" />' for k, v in props.items())


def style(selector: str, props: dict, indent: str = "  ") -> str:
    return f'{indent}<Style Selector="{selector}">\n{setters(props, indent + "  ")}\n{indent}</Style>'


def items(variant: str, size: str | None = None, suffix: str = "", owners=OWNERS) -> str:
    size_sel = SIZE_SEL[size] if size else ""
    return ", ".join(f"{owner}{VARIANT_SEL[variant]}{size_sel} > {item}{suffix}" for owner, item in owners)


def item_theme(item: str, header: bool) -> str:
    source = "Header" if header else "Content"
    return f"""    <ControlTheme x:Key="{{x:Type {item}}}" TargetType="{item}">
      <Setter Property="Foreground" Value="{{DynamicResource UIKit.TabForeground}}" />
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="BorderBrush" Value="Transparent" />
      <Setter Property="ClipToBounds" Value="False" />
      <Setter Property="Height" Value="32" />
      <Setter Property="FontSize" Value="14" />
      <Setter Property="TextBlock.LineHeight" Value="17.5" />
      <Setter Property="Template">
        <ControlTemplate>
          <Panel>
            <!--
              The background to the outer edge and the border over it, as GPUI
              paints a quad: two exact rounded rectangles (Avalonia's border
              geometry for a background under the border strays at large radii).
            -->
            <Border Name="PART_Background" Background="{{TemplateBinding Background}}" />
            <Border Name="PART_LayoutRoot"
                    BorderBrush="{{TemplateBinding BorderBrush}}"
                    BorderThickness="{{TemplateBinding BorderThickness}}"
                    CornerRadius="{{TemplateBinding CornerRadius}}"
                    ClipToBounds="True">
              <Border Name="PART_Inner" VerticalAlignment="Center">
                <ContentPresenter Name="PART_ContentPresenter"
                                  Content="{{TemplateBinding {source}}}"
                                  ContentTemplate="{{TemplateBinding {source}Template}}"
                                  Foreground="{{TemplateBinding Foreground}}"
                                  HorizontalContentAlignment="Center"
                                  VerticalContentAlignment="Center"
                                  TextWrapping="NoWrap"
                                  TextTrimming="CharacterEllipsis" />
              </Border>
            </Border>
            <!-- Avalonia-only keyboard focus (GPUI tabs take no focus): the 3px ring band. -->
            <Border Name="PART_FocusRing" Margin="-3" BorderThickness="3"
                    BorderBrush="{{DynamicResource UIKit.FocusRing}}"
                    IsVisible="False" IsHitTestVisible="False" />
          </Panel>
        </ControlTemplate>
      </Setter>
      <Style Selector="^:focus-visible /template/ Border#PART_FocusRing">
        <Setter Property="IsVisible" Value="True" />
      </Style>
    </ControlTheme>"""


def panel_gap(selector: str, gap: float) -> str:
    """The gap between tabs: the spacing of the strip's StackPanel."""
    return style(f"{selector} /template/ StackPanel#PART_TabsPanel", {"Spacing": num(gap)}, "      ")


def strip_part_styles(gap_style=panel_gap) -> str:
    out = []
    for v in VARIANTS:
        vs = VARIANT_SEL[v]
        out.append(style(f"^{vs} /template/ Border#PART_Baseline", {"IsVisible": str(v in BASELINE)}, "      "))
        bar = {"Background": BAR_BACKGROUND.get(v, T)}
        out.append(style(f"^{vs} /template/ Border#PART_Bar", bar, "      "))
        fill = {
            "tab": {"IsVisible": "False"},
            "outline": {"IsVisible": "False"},
            "pill": {"Background": "{DynamicResource UIKit.Primary.Fill}"},
            "segmented": {
                "Background": "{DynamicResource UIKit.Background}",
                "BoxShadow": "{StaticResource UIKit.Shadow.Raised}",
                "VerticalAlignment": "Center",
            },
            "underline": {"Background": "{DynamicResource UIKit.Primary.Fill}", "Height": "2", "VerticalAlignment": "Bottom"},
        }[v]
        out.append(style(f"^{vs} /template/ Border#PART_IndicatorFill", fill, "      "))
        for i, s in enumerate(SIZES):
            sel = f"^{vs}{SIZE_SEL[s]}"
            out.append(gap_style(sel, GAP[v][i]))
            if v == "segmented":
                out.append(style(f"{sel} /template/ Border#PART_Bar", {
                    "Padding": f"{num(SEGMENTED_PADDING[i])},0",
                    "CornerRadius": num(RADIUS[v][i]),
                }, "      "))
                out.append(style(f"{sel} /template/ Border#PART_IndicatorFill", {
                    "Height": num(INNER_HEIGHT[v][i]),
                    "CornerRadius": num(SEGMENTED_INNER_RADIUS[i]),
                }, "      "))
            if v == "pill":
                out.append(style(f"{sel} /template/ Border#PART_IndicatorFill", {"CornerRadius": num(RADIUS[v][i])}, "      "))
    return "\n".join(out)


def strip_template() -> str:
    return """          <Border Name="PART_Bar"%s>
            <Panel>
              <!-- Tab and Underline: a 1px border line along the bar's bottom, under the tabs. -->
              <Border Name="PART_Baseline" BorderThickness="0,0,0,1"
                      BorderBrush="{DynamicResource UIKit.Border}" IsHitTestVisible="False" />
              <!--
                The selected tab's indicator (Pill, Segmented, Underline), under the
                tabs and clipped to the bar's content as GPUI clips its tab row.
              -->
              <Canvas Name="PART_IndicatorLayer" ClipToBounds="True" IsHitTestVisible="False">
                <Panel Name="PART_Indicator" uikit:Tabs.Indicator="True"
                       uikit:Motion.Spring="{StaticResource UIKit.Spring.Move}" uikit:Motion.SpringsCanvasLeft="True">
                  <Border Name="PART_IndicatorFill"
                          uikit:Motion.Spring="{StaticResource UIKit.Spring.Move}"
                          Width="{Binding $self.(uikit:Motion.SpringValue)}" />
                </Panel>
              </Canvas>
              <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" />
            </Panel>
          </Border>"""


ITEMS_PANEL = """      <Setter Property="ItemsPanel">
        <ItemsPanelTemplate>
          <StackPanel Name="PART_TabsPanel" Orientation="Horizontal" />
        </ItemsPanelTemplate>
      </Setter>"""


def strip_theme() -> str:
    return f"""    <ControlTheme x:Key="{{x:Type TabStrip}}" TargetType="TabStrip">
      <Setter Property="HorizontalAlignment" Value="Left" />
{ITEMS_PANEL}
      <Setter Property="Template">
        <ControlTemplate>
{strip_template() % ''}
        </ControlTemplate>
      </Setter>
{strip_part_styles()}
    </ControlTheme>"""


def control_theme() -> str:
    bar = strip_template() % ' DockPanel.Dock="{TemplateBinding TabStripPlacement}"'
    bar = "\n".join("    " + line for line in bar.split("\n"))
    return f"""    <ControlTheme x:Key="{{x:Type TabControl}}" TargetType="TabControl">
      <Setter Property="Padding" Value="0" />
{ITEMS_PANEL}
      <Setter Property="Template">
        <ControlTemplate>
          <Border Background="{{TemplateBinding Background}}"
                  BorderBrush="{{TemplateBinding BorderBrush}}"
                  BorderThickness="{{TemplateBinding BorderThickness}}"
                  CornerRadius="{{TemplateBinding CornerRadius}}">
            <DockPanel>
{bar}
              <Panel>
                <ContentPresenter Name="PART_SelectedContentHost2"
                                  Margin="{{TemplateBinding Padding}}"
                                  HorizontalContentAlignment="{{TemplateBinding HorizontalContentAlignment}}"
                                  VerticalContentAlignment="{{TemplateBinding VerticalContentAlignment}}"
                                  IsVisible="False" />
                <ContentPresenter Name="PART_SelectedContentHost"
                                  Margin="{{TemplateBinding Padding}}"
                                  HorizontalContentAlignment="{{TemplateBinding HorizontalContentAlignment}}"
                                  VerticalContentAlignment="{{TemplateBinding VerticalContentAlignment}}" />
              </Panel>
            </DockPanel>
          </Border>
        </ControlTemplate>
      </Setter>
{strip_part_styles()}
      <!-- GPUI has no vertical tab bar: tabs stack, with no indicator or baseline. -->
      <Style Selector="^[TabStripPlacement=Left] /template/ StackPanel#PART_TabsPanel, ^[TabStripPlacement=Right] /template/ StackPanel#PART_TabsPanel">
        <Setter Property="Orientation" Value="Vertical" />
      </Style>
      <Style Selector="^[TabStripPlacement=Left] /template/ Canvas#PART_IndicatorLayer, ^[TabStripPlacement=Right] /template/ Canvas#PART_IndicatorLayer, ^[TabStripPlacement=Left] /template/ Border#PART_Baseline, ^[TabStripPlacement=Right] /template/ Border#PART_Baseline">
        <Setter Property="IsVisible" Value="False" />
      </Style>
    </ControlTheme>"""


def item_styles(owners=OWNERS, resource: str = "StaticResource") -> str:
    def items(variant: str, size: str | None = None, suffix: str = "") -> str:
        return globals()["items"](variant, size, suffix, owners)

    out = []
    for v in VARIANTS:
        out.append(f"  <!-- {v} -->")
        out.append(style(items(v), {"BorderThickness": BORDER[v]}))
        for state, suffix in STATES:
            fg, bg, bc = COLORS[v][state]
            out.append(style(items(v, suffix=suffix), {"Foreground": brush(fg), "Background": brush(bg), "BorderBrush": brush(bc)}))
        if v == "pill":
            out.append(style(items(v), {
                "uikit:Tabs.SelectionFade": f"{{{resource} UIKit.Tab.PillFade}}",
                "uikit:Tabs.SelectionFadeFrom": "{DynamicResource UIKit.Foreground}",
            }))
        for i, s in enumerate(SIZES):
            r = RADIUS[v][i]
            border = BORDER_WIDTH[v]
            # Avalonia corner radii sit on the border's middle (see gen_button_theme.py).
            corner = num(r - border / 2) if r and v == "outline" else num(r)
            out.append(style(items(v, s), {
                "Height": num(HEIGHT[v][i]),
                "FontSize": num(FONT_SIZE[i]),
                "TextBlock.LineHeight": num(LINE_HEIGHT[i]),
                "CornerRadius": corner,
            }))
            out.append(style(items(v, s, " /template/ Border#PART_Background"), {"CornerRadius": num(r)}))
            top, bottom = UNDERLINE_MARGINS[i] if v == "underline" else (0, 0)
            pad = 0 if v == "underline" else PADDING_X[i]
            inner = {
                "Height": num(INNER_HEIGHT[v][i]),
                "Padding": f"{num(pad)},0",
                "Margin": f"0,{num(top)},0,{num(bottom)}",
                "CornerRadius": num(SEGMENTED_INNER_RADIUS[i] if v == "segmented" else 0),
            }
            out.append(style(items(v, s, " /template/ Border#PART_Inner"), inner))
            # An icon tab's inner box is 1.25 times as wide as it is tall, unpadded.
            out.append(style(items(v, s, ".icon-only /template/ Border#PART_Inner"), {
                "Width": num(INNER_HEIGHT[v][i] * 1.25),
                "Padding": "0",
            }))
            out.append(style(items(v, s, " /template/ Border#PART_FocusRing"), {"CornerRadius": num(r + 1.5) if r else "1.5"}))
    for i, s in enumerate(SIZES):
        sel = ", ".join(f"{owner}{SIZE_SEL[s]} > {item} PathIcon" for owner, item in owners)
        out.append(style(sel, {"Width": num(ICON_SIZE[i]), "Height": num(ICON_SIZE[i])}))
    return "\n".join(out)


HEADER = """<!--
  DO NOT EDIT. Generated by scripts/gen_tabs_theme.py.

  GPUI Kit TabBar / Tab (crates/component/src/tab) -> TabStrip / TabStripItem,
  and the strip of TabControl / TabItem:

    <TabStrip Classes="pill small" SelectedIndex="0">
      <TabStripItem Content="Account" />
      <TabStripItem Classes="icon-only"><PathIcon Data="{StaticResource UIKit.Icon.Copy}" /></TabStripItem>
    </TabStrip>

  Classes on the TabStrip / TabControl: variant outline pill segmented underline
  (GPUI's default Tab without one), size xsmall small large (medium without
  one); icon-only on an item whose content is an icon. Pill, Segmented and
  Underline show the selection with an indicator that springs to the selected
  tab (the Tabs.Indicator behavior); a pill's label fades in as it slides.
  GPUI's overflow menu, prefix and suffix, closing and reordering are not part
  of the controls. Tabs show an Avalonia-only ring on keyboard focus.
-->"""


RESOURCES = """    <!-- styled.rs raised_shadow(): the segmented indicator. -->
    <BoxShadows x:Key="UIKit.Shadow.Raised">0 1 3.4641 0 #1A000000, 0 1 1.7321 -1 #1A000000</BoxShadows>
    <!-- tab.rs: the newly selected pill's label, 200ms ease-in-out-cubic. -->
    <uikit:ColorTransition x:Key="UIKit.Tab.PillFade" Duration="0:0:0.2" Easing="CubicEaseInOut" Properties="Foreground" />"""


def offset_gap(selector: str, gap: float) -> str:
    """The gap between tabs: TabsControl's offset between adjacent tabs (its TabsPanel)."""
    return style(selector, {"AdjacentHeaderItemOffset": num(gap)}, "      ")


def drag_item_theme() -> str:
    return """    <!-- The drag handle over a tab's label: hit-testable, paints nothing. -->
    <ControlTheme x:Key="UIKitDragTabThumb" TargetType="tab:LeftPressedThumb">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="Template">
        <ControlTemplate>
          <Border Background="{TemplateBinding Background}" />
        </ControlTemplate>
      </Setter>
    </ControlTheme>
    <!-- The window-moving thumbs beside the tabs: GPUI's bar does not move the window. -->
    <ControlTheme x:Key="UIKitTabsWindowThumb" TargetType="Thumb">
      <Setter Property="IsHitTestVisible" Value="False" />
      <Setter Property="Template">
        <ControlTemplate>
          <Panel />
        </ControlTemplate>
      </Setter>
    </ControlTheme>
    <tabconv:ShowDefaultCloseButtonConverter x:Key="UIKitTabsShowsClose" />
    <ControlTheme x:Key="{x:Type tab:DragTabItem}" TargetType="tab:DragTabItem">
      <Setter Property="Foreground" Value="{DynamicResource UIKit.TabForeground}" />
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="BorderBrush" Value="Transparent" />
      <Setter Property="ClipToBounds" Value="False" />
      <Setter Property="Height" Value="32" />
      <Setter Property="FontSize" Value="14" />
      <Setter Property="TextBlock.LineHeight" Value="17.5" />
      <Setter Property="Template">
        <ControlTemplate>
          <Panel>
            <!-- As TabItem's (Tabs.axaml): the background, and the border over it. -->
            <Border Name="PART_Background" Background="{TemplateBinding Background}" />
            <Border Name="PART_LayoutRoot"
                    BorderBrush="{TemplateBinding BorderBrush}"
                    BorderThickness="{TemplateBinding BorderThickness}"
                    CornerRadius="{TemplateBinding CornerRadius}"
                    Padding="{TemplateBinding Padding}"
                    ClipToBounds="True">
              <!-- tab.rs: the label fills the tab, then gap_1 and the suffix (the close button). -->
              <Grid ColumnDefinitions="*,Auto">
                <Border Name="PART_Inner" VerticalAlignment="Center">
                  <!-- tab.rs under max_width: an ellipsized label starts at its box's start. -->
                  <ContentPresenter Name="PART_ContentPresenter"
                                    Content="{TemplateBinding Header}"
                                    ContentTemplate="{TemplateBinding HeaderTemplate}"
                                    Foreground="{TemplateBinding Foreground}"
                                    HorizontalContentAlignment="Center"
                                    VerticalContentAlignment="Center"
                                    TextWrapping="NoWrap"
                                    TextTrimming="CharacterEllipsis"
                                    uikit:TextLines.StartsTrimmedText="True" />
                </Border>
                <tab:LeftPressedThumb Name="PART_Thumb" Theme="{StaticResource UIKitDragTabThumb}" />
                <!-- The tabs story's close suffix: a ghost xsmall button with the 12px Close icon. -->
                <Button Name="PART_CloseButton" Grid.Column="1" Classes="ghost xsmall icon-only"
                        Margin="4,0,0,0" VerticalAlignment="Center"
                        Command="{Binding $parent[tab:TabsControl].CloseItemCommand}"
                        CommandParameter="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                  <Button.IsVisible>
                    <MultiBinding Converter="{StaticResource UIKitTabsShowsClose}">
                      <Binding Path="ShowDefaultCloseButton" RelativeSource="{RelativeSource FindAncestor, AncestorType={x:Type tab:TabsControl}}" />
                      <Binding Path="FixedHeaderCount" RelativeSource="{RelativeSource FindAncestor, AncestorType={x:Type tab:TabsControl}}" />
                      <Binding Path="LogicalIndex" RelativeSource="{RelativeSource TemplatedParent}" />
                    </MultiBinding>
                  </Button.IsVisible>
                  <PathIcon Classes="uikit-tab-button" Data="{DynamicResource UIKit.Icon.Close}" />
                </Button>
              </Grid>
            </Border>
            <!-- Avalonia-only keyboard focus (GPUI tabs take no focus): the 3px ring band. -->
            <Border Name="PART_FocusRing" Margin="-3" BorderThickness="3"
                    BorderBrush="{DynamicResource UIKit.FocusRing}"
                    IsVisible="False" IsHitTestVisible="False" />
          </Panel>
        </ControlTemplate>
      </Setter>
      <Style Selector="^:focus-visible /template/ Border#PART_FocusRing">
        <Setter Property="IsVisible" Value="True" />
      </Style>
    </ControlTheme>"""


def tabs_control_theme() -> str:
    return f"""    <ControlTheme x:Key="{{x:Type tab:TabsControl}}" TargetType="tab:TabsControl">
      <Setter Property="Padding" Value="0" />
      <Setter Property="Template">
        <ControlTemplate>
          <Border Background="{{TemplateBinding Background}}"
                  BorderBrush="{{TemplateBinding BorderBrush}}"
                  BorderThickness="{{TemplateBinding BorderThickness}}"
                  CornerRadius="{{TemplateBinding CornerRadius}}">
            <DockPanel>
              <!--
                tab_bar.rs: the bar is the prefix, the tabs (scrolling sideways when
                they overflow), the menu button and the suffix. TopPanel only looks
                its six parts up by name, so the bar's prefix and suffix are shown
                outside the scrolling tabs and its own content parts stay empty.
              -->
              <Border Name="PART_Bar" DockPanel.Dock="Top">
                <Panel>
                  <!-- Tab and Underline: a 1px border line along the bar's bottom, under the tabs. -->
                  <Border Name="PART_Baseline" BorderThickness="0,0,0,1"
                          BorderBrush="{{DynamicResource UIKit.Border}}" IsHitTestVisible="False" />
                  <Grid ColumnDefinitions="Auto,*,Auto,Auto">
                    <ContentPresenter Name="PART_Prefix" Content="{{TemplateBinding LeftContent}}"
                                      VerticalAlignment="Center" />
                    <ScrollViewer Name="PART_Scroller" Grid.Column="1"
                                  HorizontalScrollBarVisibility="Hidden" VerticalScrollBarVisibility="Disabled">
                      <Panel>
                        <!-- The selected tab's indicator (Pill, Segmented, Underline), under the tabs. -->
                        <Canvas Name="PART_IndicatorLayer" ClipToBounds="True" IsHitTestVisible="False">
                          <Panel Name="PART_Indicator" uikit:Tabs.Indicator="True" uikit:DragTabs.FollowsDrag="True"
                                 uikit:Motion.Spring="{{DynamicResource UIKit.Spring.Move}}" uikit:Motion.SpringsCanvasLeft="True">
                            <Border Name="PART_IndicatorFill"
                                    uikit:Motion.Spring="{{DynamicResource UIKit.Spring.Move}}"
                                    Width="{{Binding $self.(uikit:Motion.SpringValue)}}" />
                          </Panel>
                        </Canvas>
                        <tabpanels:TopPanel Name="PART_TopPanel">
                          <Panel Name="PART_LeftContent" />
                          <Thumb Name="PART_LeftDragWindowThumb" Theme="{{StaticResource UIKitTabsWindowThumb}}" />
                          <ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{{TemplateBinding ItemsPanel}}" />
                          <!-- Avalonia-only: Tabalonia's add button, a ghost xsmall button with the Plus icon. -->
                          <Button Name="PART_AddItemButton" Classes="ghost xsmall icon-only" Margin="4,0,0,0"
                                  Command="{{Binding AddItemCommand, RelativeSource={{RelativeSource TemplatedParent}}}}"
                                  IsVisible="{{TemplateBinding ShowDefaultAddButton}}">
                            <PathIcon Width="12" Height="12" Data="{{DynamicResource UIKit.Icon.Plus}}" />
                          </Button>
                          <Thumb Name="PART_RightDragWindowThumb" Theme="{{StaticResource UIKitTabsWindowThumb}}" />
                          <!-- tab_bar.rs last_empty_space: 12px after the tabs with a menu or a suffix. -->
                          <Panel Name="PART_RightContent" Width="12"
                                 IsVisible="{{Binding RightContent, RelativeSource={{RelativeSource TemplatedParent}}, Converter={{x:Static ObjectConverters.IsNotNull}}}}" />
                        </tabpanels:TopPanel>
                      </Panel>
                    </ScrollViewer>
                    <!-- tab_bar.rs menu(true): a ghost xsmall button with a caret that lists the tabs. -->
                    <uikit:TabsMenuButton Name="PART_MenuButton" Grid.Column="2" Classes="ghost xsmall icon-only"
                                          VerticalAlignment="Center" IsVisible="False">
                      <PathIcon Width="12" Height="12" Foreground="{{DynamicResource UIKit.Button.Ghost.Caret}}"
                                Data="{{DynamicResource UIKit.Icon.ChevronDown}}" />
                    </uikit:TabsMenuButton>
                    <ContentPresenter Name="PART_Suffix" Grid.Column="3" Content="{{TemplateBinding RightContent}}"
                                      VerticalAlignment="Center" />
                  </Grid>
                </Panel>
              </Border>
              <Panel>
                <ContentPresenter Name="PART_SelectedContentHost2"
                                  Margin="{{TemplateBinding Padding}}"
                                  HorizontalContentAlignment="{{TemplateBinding HorizontalContentAlignment}}"
                                  VerticalContentAlignment="{{TemplateBinding VerticalContentAlignment}}"
                                  IsVisible="False" />
                <ContentPresenter Name="PART_SelectedContentHost"
                                  Margin="{{TemplateBinding Padding}}"
                                  HorizontalContentAlignment="{{TemplateBinding HorizontalContentAlignment}}"
                                  VerticalContentAlignment="{{TemplateBinding VerticalContentAlignment}}" />
              </Panel>
            </DockPanel>
          </Border>
        </ControlTemplate>
      </Setter>
{strip_part_styles(offset_gap)}
      <Style Selector="^.menu /template/ Button#PART_MenuButton, ^.menu /template/ Panel#PART_RightContent">
        <Setter Property="IsVisible" Value="True" />
      </Style>
    </ControlTheme>"""


TABALONIA_HEADER = """<!--
  DO NOT EDIT. Generated by scripts/gen_tabs_theme.py.

  GPUI Kit TabBar / Tab (crates/component/src/tab) -> Tabalonia's TabsControl /
  DragTabItem, with the same classes and metrics as TabStrip and TabControl
  (Tabs.axaml in AvaloniaUIKit):

    <tab:TabsControl Classes="pill small menu" ItemsSource="{Binding Tabs}"
                     TabItemWidth="120" EnableTabDetaching="False" />

  Classes on the TabsControl: variant outline pill segmented underline (GPUI's
  default Tab without one), size xsmall small large (medium without one), menu
  for GPUI's menu(true) (a caret button listing the tabs); icon-only on a tab
  whose header is an icon. LeftContent and RightContent are the bar's prefix
  and suffix. The tabs scroll sideways when they overflow, as GPUI's do. Each
  tab is TabItemWidth wide (Tabalonia lays its tabs out at one width) with its
  label centred, the close button as the suffix GPUI's tabs story gives a
  closable tab, and Tabalonia's add button after the last tab. The indicator
  follows a dragged tab at once and springs to a newly selected one. Tabs show
  an Avalonia-only ring on keyboard focus.
-->"""


def tabalonia() -> None:
    text = f"""{TABALONIA_HEADER}
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:uikit="using:AvaloniaUIKit"
        xmlns:tab="clr-namespace:Tabalonia.Controls;assembly=Tabalonia"
        xmlns:tabconv="clr-namespace:Tabalonia.Converters;assembly=Tabalonia"
        xmlns:tabpanels="clr-namespace:Tabalonia.Panels;assembly=Tabalonia">
  <Styles.Resources>
{RESOURCES}
{drag_item_theme()}
{tabs_control_theme()}
  </Styles.Resources>

  <!-- A closable tab is padded as the tabs story's (px_2). -->
  <Style Selector="tab|TabsControl[ShowDefaultCloseButton=True] > tab|DragTabItem">
    <Setter Property="Padding" Value="8,0" />
  </Style>
{item_styles(TABALONIA_OWNERS)}
  <!-- The close button's icon is an xsmall button's (12px), whatever the tab's icon size. -->
  <Style Selector="tab|DragTabItem PathIcon.uikit-tab-button">
    <Setter Property="Width" Value="12" />
    <Setter Property="Height" Value="12" />
  </Style>
</Styles>
"""
    TABALONIA_OUT.parent.mkdir(parents=True, exist_ok=True)
    TABALONIA_OUT.write_text(text)


def main() -> None:
    text = f"""{HEADER}
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:uikit="using:AvaloniaUIKit">
  <Styles.Resources>
{RESOURCES}
{item_theme("TabStripItem", header=False)}
{item_theme("TabItem", header=True)}
{strip_theme()}
{control_theme()}
  </Styles.Resources>

{item_styles()}
</Styles>
"""
    OUT.write_text(text)
    tabalonia()


if __name__ == "__main__":
    main()
