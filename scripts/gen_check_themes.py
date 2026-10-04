#!/usr/bin/env python3
"""Generates the CheckBox and RadioButton control themes.

GPUI Kit Checkbox (crates/component/src/checkbox.rs) and Radio (radio.rs) share
their layout: an indicator of 12/14/16/18px, 8px from the label, nudged down
by an eighth of its size when there is a label, and a Lucide check mark of
8/10/12/14px fading in with the `spring_control` motion token. They differ in
the indicator's corners and colors.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

# size: indicator, mark, text size, label line height (1.25em)
SIZES = {
    "xsmall": (12, 8, 12, 15),
    "small": (14, 10, 14, 17.5),
    "medium": (16, 12, 14, 17.5),
    "large": (18, 14, 16, 20),
}
RING = 3.0
ROOT_RADIUS = 3.0  # radius * 0.5 (checkbox.rs / radio.rs root)

# spring_control: response 180ms, critically damped, epsilon 0.001 (theme/motion.rs),
# run by Motion.Spring so a mark reversed mid-fade keeps its velocity as GPUI's does.
import math


def snapped(v: float) -> float:
    """GPUI snaps layout edges to device pixels (scale 2), rounding halves toward zero
    (gpui taffy.rs). Offsets are pre-snapped here as GPUI places them on an integer
    origin, so Avalonia's own rounding (halves to even) cannot land elsewhere (R9)."""
    d = v * 2
    r = math.floor(d) if d - math.floor(d) <= 0.5 else math.ceil(d)
    return r / 2


def num(v: float) -> str:
    return f"{v:g}" if v == int(v) else f"{v:.4f}".rstrip("0").rstrip(".")


def theme(target: str, round_box: bool, disabled_checked_bg: str) -> list[str]:
    out = []
    w = out.append
    w(f'  <ControlTheme x:Key="{{x:Type {target}}}" TargetType="{target}">')
    w('    <Setter Property="Foreground" Value="{DynamicResource UIKit.Foreground}" />')
    w('    <Setter Property="Background" Value="Transparent" />')
    w(f'    <Setter Property="FontSize" Value="{SIZES["medium"][2]}" />')
    w('    <Setter Property="ClipToBounds" Value="False" />')
    w('    <Setter Property="HorizontalAlignment" Value="Left" />')
    w('    <Setter Property="VerticalAlignment" Value="Top" />')
    w('    <Setter Property="Template">')
    w('      <ControlTemplate>')
    w('        <Grid ColumnDefinitions="Auto,*" Background="{TemplateBinding Background}">')
    w('          <!-- The ring wraps the whole row (focus_ring_style on the root, radius * 0.5). -->')
    w(f'          <Border Name="PART_FocusRing" Grid.ColumnSpan="2" Margin="-{num(RING)}" BorderThickness="{num(RING)}"')
    w(f'                  CornerRadius="{num(ROOT_RADIUS + RING - RING / 2)}" BorderBrush="{{DynamicResource UIKit.FocusRing}}"')
    w('                  IsVisible="False" IsHitTestVisible="False" />')
    ind, mark, _, _ = SIZES["medium"]
    radius = ind / 2 - 0.5 if round_box else 4 - 0.5
    w(f'          <Border Name="PART_Indicator" Width="{ind}" Height="{ind}" VerticalAlignment="Top"')
    w(f'                  Margin="0,{num(snapped(ind * 0.125))},0,0"')
    w('                  Classes.bare="{TemplateBinding Content, Converter={x:Static ObjectConverters.IsNull}}"')
    w(f'                  BorderThickness="1" CornerRadius="{num(radius)}" BackgroundSizing="OuterBorderEdge"')
    w('                  Background="{DynamicResource UIKit.InputBackground}" BorderBrush="{DynamicResource UIKit.Input}">')
    w(f'            <PathIcon Name="PART_Mark" Width="{mark}" Height="{mark}" Margin="1"')
    w('                      HorizontalAlignment="Left" VerticalAlignment="Top"')
    w('                      Data="{StaticResource UIKit.Icon.Check}" Foreground="{DynamicResource UIKit.PrimaryForeground}"')
    w('                      uikit:Motion.Spring="{StaticResource UIKit.Spring.Control}" uikit:Motion.SpringTarget="0"')
    w('                      Opacity="{Binding $self.(uikit:Motion.SpringValue)}">')
    w('            </PathIcon>')
    w('          </Border>')
    w('          <ContentPresenter Name="PART_ContentPresenter" Grid.Column="1" Margin="8,0,0,0"')
    w('                            IsVisible="{TemplateBinding Content, Converter={x:Static ObjectConverters.IsNotNull}}"')
    w('                            Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"')
    w('                            Foreground="{TemplateBinding Foreground}" FontSize="{TemplateBinding FontSize}"')
    w(f'                            LineHeight="{num(SIZES["medium"][3])}" RecognizesAccessKey="True" />')
    w('        </Grid>')
    w('      </ControlTemplate>')
    w('    </Setter>')
    w('')
    w('    <Style Selector="^ /template/ Border#PART_Indicator.bare">')
    w('      <Setter Property="Margin" Value="0" />')
    w('    </Style>')
    for size, (ind, mark, text, line) in SIZES.items():
        if size == "medium":
            continue
        radius = ind / 2 - 0.5 if round_box else 4 - 0.5
        w(f'    <Style Selector="^.{size}">')
        w(f'      <Setter Property="FontSize" Value="{text}" />')
        w('    </Style>')
        w(f'    <Style Selector="^.{size} /template/ Border#PART_Indicator">')
        w(f'      <Setter Property="Width" Value="{ind}" />')
        w(f'      <Setter Property="Height" Value="{ind}" />')
        w(f'      <Setter Property="Margin" Value="0,{num(snapped(ind * 0.125))},0,0" />')
        w(f'      <Setter Property="CornerRadius" Value="{num(radius)}" />')
        w('    </Style>')
        w(f'    <Style Selector="^.{size} /template/ Border#PART_Indicator.bare">')
        w('      <Setter Property="Margin" Value="0" />')
        w('    </Style>')
        w(f'    <Style Selector="^.{size} /template/ PathIcon#PART_Mark">')
        w(f'      <Setter Property="Width" Value="{mark}" />')
        w(f'      <Setter Property="Height" Value="{mark}" />')
        w('    </Style>')
        w(f'    <Style Selector="^.{size} /template/ ContentPresenter#PART_ContentPresenter">')
        w(f'      <Setter Property="LineHeight" Value="{num(line)}" />')
        w('    </Style>')
    w('')
    w('    <Style Selector="^:checked /template/ Border#PART_Indicator">')
    w('      <Setter Property="Background" Value="{DynamicResource UIKit.Primary.Fill}" />')
    w('      <Setter Property="BorderBrush" Value="{DynamicResource UIKit.Primary}" />')
    w('    </Style>')
    w('    <Style Selector="^:checked /template/ PathIcon#PART_Mark">')
    w('      <Setter Property="uikit:Motion.SpringTarget" Value="1" />')
    w('    </Style>')
    w('    <Style Selector="^:disabled">')
    w('      <Setter Property="Foreground" Value="{DynamicResource UIKit.MutedForeground}" />')
    w('    </Style>')
    w('    <Style Selector="^:disabled /template/ Border#PART_Indicator">')
    w('      <Setter Property="BorderBrush" Value="{DynamicResource UIKit.Check.Disabled.Border}" />')
    w('    </Style>')
    w('    <Style Selector="^:disabled:checked /template/ Border#PART_Indicator">')
    w(f'      <Setter Property="Background" Value="{{DynamicResource {disabled_checked_bg}}}" />')
    w('      <Setter Property="BorderBrush" Value="{DynamicResource UIKit.Check.Disabled.Checked}" />')
    w('    </Style>')
    w('    <Style Selector="^:disabled /template/ PathIcon#PART_Mark">')
    w('      <Setter Property="Foreground" Value="{DynamicResource UIKit.Check.Disabled.Mark}" />')
    w('    </Style>')
    w('    <!-- Avalonia only: the indeterminate state shows a minus mark (not compared, R16). -->')
    w('    <Style Selector="^:indeterminate /template/ Border#PART_Indicator">')
    w('      <Setter Property="Background" Value="{DynamicResource UIKit.Primary.Fill}" />')
    w('      <Setter Property="BorderBrush" Value="{DynamicResource UIKit.Primary}" />')
    w('    </Style>')
    w('    <Style Selector="^:indeterminate /template/ PathIcon#PART_Mark">')
    w('      <Setter Property="Data" Value="{StaticResource UIKit.Icon.Minus}" />')
    w('      <Setter Property="uikit:Motion.SpringTarget" Value="1" />')
    w('    </Style>')
    w('    <!-- Keyboard focus only: GPUI prevents a pointer press from focusing the control. -->')
    w('    <Style Selector="^:focus-visible /template/ Border#PART_FocusRing">')
    w('      <Setter Property="IsVisible" Value="True" />')
    w('    </Style>')
    w('  </ControlTheme>')
    return out


for target, round_box, name, source in [
    ("CheckBox", False, "CheckBox", "checkbox.rs"),
    ("RadioButton", True, "RadioButton", "radio.rs"),
]:
    lines = [
        "<!--",
        "  DO NOT EDIT. Generated by scripts/gen_check_themes.py.",
        "",
        f"  GPUI Kit {'Checkbox' if target == 'CheckBox' else 'Radio'} -> {target} (crates/component/src/{source}).",
        "  Classes: xsmall small large (medium is the default).",
        "-->",
        '<ResourceDictionary xmlns="https://github.com/avaloniaui"',
        '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"',
        '                    xmlns:uikit="using:AvaloniaUIKit">',
    ]
    lines += theme(target, round_box, "UIKit.Check.Disabled.Checked")
    lines.append("</ResourceDictionary>")
    out = ROOT / f"src/AvaloniaUIKit/Themes/Controls/{name}.axaml"
    out.write_text("\n".join(lines) + "\n")
    print(f"wrote {out.relative_to(ROOT)}")
