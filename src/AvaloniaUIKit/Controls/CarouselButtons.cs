using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// The Previous or Next control of GPUI Kit's Carousel (carousel.rs
/// carousel_control): an outline, fully rounded icon button (a chevron
/// along the carousel's axis, unless the app gives it content) that moves
/// <see cref="Carousel"/> one page. It is disabled at the carousel's ends
/// unless the carousel wraps (WrapSelection). Put it in the same Panel cell
/// as the carousel: the theme sets it 16px outside the pages, centered on
/// them (above and below a vertical carousel). After a pointer click the
/// carousel takes focus without a ring, so the arrow keys go on paging.
/// </summary>
public abstract class CarouselButton : Button
{
    /// <summary>The carousel the button moves.</summary>
    public static readonly StyledProperty<Carousel?> CarouselProperty =
        AvaloniaProperty.Register<CarouselButton, Carousel?>(nameof(Carousel));

    private readonly PathIcon _icon = new();
    private Carousel? _observed;
    private bool _pointerClick;

    /// <summary>Creates the button with GPUI's look and its default icon.</summary>
    protected CarouselButton()
    {
        Classes.Add("outline");
        Classes.Add("rounded-full");
        Classes.Add("icon-only");
        Content = _icon;
        UpdateIcon();
    }

    /// <inheritdoc cref="CarouselProperty"/>
    public Carousel? Carousel
    {
        get => GetValue(CarouselProperty);
        set => SetValue(CarouselProperty, value);
    }

    /// <summary>Whether the button moves to the next page (otherwise the previous one).</summary>
    protected abstract bool IsNext { get; }

    /// <inheritdoc />
    protected override bool IsEnabledCore => base.IsEnabledCore && CanMove;

    private bool CanMove
    {
        get
        {
            if (Carousel is not { } carousel || carousel.ItemCount == 0)
            {
                return false;
            }
            // state.rs has_previous / has_next: a looping carousel always can.
            if (carousel.WrapSelection && carousel.ItemCount > 1)
            {
                return true;
            }
            return IsNext ? carousel.SelectedIndex < carousel.ItemCount - 1 : carousel.SelectedIndex > 0;
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CarouselProperty)
        {
            Observe(this.IsAttachedToVisualTree() ? Carousel : null);
        }
        else if (change.Property == ContentProperty)
        {
            // Content of the app's replaces the icon (GPUI: children instead of the icon).
            Classes.Set("icon-only", ReferenceEquals(change.NewValue, _icon));
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Observe(Carousel);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Observe(null);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _pointerClick = true;
        try
        {
            base.OnPointerReleased(e);
        }
        finally
        {
            _pointerClick = false;
        }
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        if (Carousel is { } carousel && CanMove)
        {
            if (IsNext)
            {
                carousel.Next();
            }
            else
            {
                carousel.Previous();
            }
            // carousel.rs focus_after_pointer_click: a keyboard activation keeps the focus here.
            if (_pointerClick && carousel.Focusable && !carousel.IsKeyboardFocusWithin)
            {
                carousel.Focus(NavigationMethod.Pointer);
            }
        }
        base.OnClick();
    }

    private void Observe(Carousel? carousel)
    {
        if (_observed is not null)
        {
            _observed.PropertyChanged -= OnCarouselPropertyChanged;
        }
        _observed = carousel;
        if (carousel is not null)
        {
            carousel.PropertyChanged += OnCarouselPropertyChanged;
        }
        UpdateIcon();
        UpdateIsEffectivelyEnabled();
    }

    private void OnCarouselPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SelectingItemsControl.SelectedIndexProperty || e.Property == ItemsControl.ItemCountProperty ||
            e.Property == SelectingItemsControl.WrapSelectionProperty)
        {
            UpdateIsEffectivelyEnabled();
        }
        else if (e.Property == Carousel.PageTransitionProperty)
        {
            UpdateIcon();
        }
    }

    /// <summary>The chevron along the axis, and :vertical for the theme to place the button.</summary>
    private void UpdateIcon()
    {
        var vertical = Carousel?.PageTransition is PageSlide { Orientation: PageSlide.SlideAxis.Vertical };
        PseudoClasses.Set(":vertical", vertical);
        var key = (vertical, IsNext) switch
        {
            (false, false) => "UIKit.Icon.ChevronLeft",
            (false, true) => "UIKit.Icon.ChevronRight",
            (true, false) => "UIKit.Icon.ChevronUp",
            (true, true) => "UIKit.Icon.ChevronDown",
        };
        _icon.Bind(PathIcon.DataProperty, this.GetResourceObservable(key));
    }
}

/// <summary>GPUI Kit's CarouselPrevious: the <see cref="CarouselButton"/> that moves to the previous page.</summary>
public class CarouselPrevious : CarouselButton
{
    /// <inheritdoc />
    protected override bool IsNext => false;
}

/// <summary>GPUI Kit's CarouselNext: the <see cref="CarouselButton"/> that moves to the next page.</summary>
public class CarouselNext : CarouselButton
{
    /// <inheritdoc />
    protected override bool IsNext => true;
}
