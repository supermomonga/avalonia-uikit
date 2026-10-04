using System.Collections.ObjectModel;

namespace AvaloniaUIKit.Demos;

/// <summary>
/// The Tabalonia demos' tabs: TabsControl adds, closes and reorders the items
/// of its ItemsSource, which must be a list it can change.
/// </summary>
public sealed class TabList : ObservableCollection<object>;
