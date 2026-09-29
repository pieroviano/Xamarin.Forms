# Running WPF Code on Linux and macOS: `System.Windows` Reimplemented over GTK 4

WPF is Windows-only, and always has been. `PresentationFramework` sits on `milcore`, which sits on
DirectX, which does not exist anywhere else. That is why the standard advice for a WPF codebase that
needs Linux is *rewrite it* — in Avalonia, in MAUI, in Uno.

This article describes a different route taken in this repository: **keep the WPF source, replace the
framework underneath it.** The `Net4x.Xamarin.Forms.Wpf` package implements the `System.Windows.*` API
over Xamarin.Forms, which the `Xamarin.Forms.Platform.GTK` backend renders with **GTK 4** through
`GtkSharp4`. WPF C# and WPF XAML compile and run unchanged on Linux, macOS and Windows.

## The shape of it

| Layer | What it is |
|---|---|
| your code | WPF: `Window`, `Grid`, `Button`, `{Binding}`, `Dispatcher`, `*.xaml` |
| `Xamarin.Forms.Wpf` | the `System.Windows.*` API — ~16,600 lines, `netstandard2.0` |
| `Xamarin.Forms.Core` + `.Xaml` | the property system, layout, XAML loader and compiler |
| `Xamarin.Forms.Platform.GTK` | one renderer per control |
| `GtkSharp4` → GTK 4 | the native widgets, on any OS GTK builds for |

The assembly is named `Xamarin.Forms.Wpf` but its root namespace is `System.Windows`. It is not a
shim layer with adapter types: a public type in a `System.Windows.*` namespace here is always a WPF
type with WPF's name, so code written against real WPF resolves the same names.

## Install

```
dotnet add package Net4x.Xamarin.Forms.Wpf
dotnet add package Net4x.Xamarin.Forms.Build.Tasks   # compiles the XAML — its targets are not transitive
dotnet add package GtkSharp4                         # fetches the GTK runtime on Windows — not transitive
```

Both companions are deliberate: MSBuild `.targets` and native runtime acquisition do not flow through
a transitive `PackageReference`, so they have to be named by the consuming project.

## Hello, GTK

```csharp
using System;
using System.Windows;
using System.Windows.Controls;

class Program
{
    [STAThread]
    static int Main()
    {
        var app = new Application();
        var button = new Button { Content = "_Click me" };
        button.Click += (s, e) => MessageBox.Show("Hello from GTK", "Greeting");

        return app.Run(new Window
        {
            Title = "WPF on GTK 4",
            Content = button,
        });
    }
}
```

There is no `Forms.Init()`, no `Gtk.Application.Init()`, no GTK type in sight. Constructing the first
WPF element bootstraps both frameworks on that thread (`GtkHost.EnsureInitialized`) — which matches
WPF's own rule that the thread creating the first window becomes the UI thread. `[STAThread]` is
required for the same reason it is in WPF: on Windows, GTK 4's GDK backend calls `OleInitialize`
during `gtk_init`, and that needs a single-threaded apartment.

## Unchanged XAML, literally

The test suite's fixture is the interesting part of the claim. `Fixtures/FrmMain.xaml` is the output
of a VB6-to-C# converter's WPF target, committed byte for byte — nobody adjusted it to suit this
implementation:

```xml
<Window x:Class="Showcase.Forms.frmMain"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Title="Showcase"
    SizeToContent="WidthAndHeight"
    WindowStartupLocation="CenterScreen"
    Background="{DynamicResource {x:Static SystemColors.ControlBrushKey}}">
  <Grid x:Name="LayoutRoot" Width="408" Height="297">
    <GroupBox x:Name="fraMode" Header="Mode" Width="161" Height="65">
      <Grid Margin="-6,-17,-6,-6">
        <RadioButton x:Name="optA" Content="Simple" IsChecked="True" />
        <RadioButton x:Name="optB" Content="Detailed" />
      </Grid>
    </GroupBox>
    <Button x:Name="cmdRun" Content="_Run" IsDefault="True" />
    <Button x:Name="cmdClose" Content="_Close" IsCancel="True" />
  </Grid>
</Window>
```

That file compiles with `InitializeComponent()` code-behind, and the same fixture is also exercised
with `[XamlCompilation(XamlCompilationOptions.Skip)]` so both paths are covered — XamlC-compiled IL
and the runtime XAML loader.

The trick that makes it work is one line per namespace in `AssemblyInfo.cs`:

```csharp
[assembly: XmlnsDefinition(WpfXaml.PresentationUri, "System.Windows.Controls")]
```

The WPF presentation URI is mapped to these namespaces, so Xamarin.Forms' own XAML compiler resolves
`<Button>` in a WPF document to `System.Windows.Controls.Button` here. The `x:` URI needed no work —
WPF and Xamarin.Forms already share it. Markup extensions (`{Binding}`, `{StaticResource}`,
`{DynamicResource}`) are mapped to the URI as well, so they are Xamarin.Forms' own implementations
answering WPF syntax.

## How each WPF concept lands

| WPF | Here |
|---|---|
| `DependencyObject` | a Xamarin.Forms `Element`; values live in `BindableProperty`s |
| `DependencyProperty` | a *view* of a `BindableProperty` — `XxxProperty` fields are declared as `BindableProperty` (what XamlC looks for) and convert implicitly |
| `UIElement` | owns a Xamarin.Forms `View`, created on first use and kept in step |
| logical tree | the WPF one: routed events, inherited fonts and colours, `IsEnabled`, resource lookup |
| `Window` | a GTK window of its own, content hosted as a `ContentPage`; any number open |
| `ShowDialog` | a nested `DispatcherFrame` — a real modal loop, not a flag |
| `Application` | one per AppDomain; `Run` until `Shutdown`, honouring `ShutdownMode` |
| `Dispatcher` | the GLib main context; `DispatcherPriority` mapped onto GLib priorities; `DispatcherTimer` is a GLib timeout |
| input | GTK event controllers per window: `Preview*` tunnels down, then bubbles; handled stops propagation |
| unhandled exceptions | a GLib handler defers them to the dispatcher, so they surface from the message loop as WPF does, instead of aborting the process from inside GLib |

Because dependency properties *are* bindable properties rather than a parallel store, bindings,
styles, dynamic resources and XAML all work through machinery that already existed and was already
tested — rather than a second property system written to imitate one.

## What is implemented

Roughly 90 public control types across `System.Windows.Controls`, `.Primitives`, `.Shapes`,
`.Documents`, `.Media` and `.Input`:

> `Border` `Button` `Calendar` `Canvas` `CheckBox` `ComboBox` `ContentControl` `ContextMenu`
> `DataGrid` (+ columns, rows, cells) `DatePicker` `DockPanel` `Ellipse` `Grid` `GridView`
> `GroupBox` `Image` `ItemsControl` `Label` `Line` `ListBox` `ListView` `Menu` `MenuItem`
> `PasswordBox` `ProgressBar` `RadioButton` `Rectangle` `RepeatButton` `RichTextBox` `ScrollBar`
> `ScrollViewer` `Selector` `Separator` `Slider` `StackPanel` `StatusBar` `TabControl` `TextBlock`
> `TextBox` `ToggleButton` `ToolBar` `ToolTip` `TreeView` `UserControl` `WrapPanel` …

## Boundaries

Stated plainly, because a compatibility layer that hides its gaps is worse than one that does not:

| Not modelled | Consequence |
|---|---|
| templates and the visual tree | `Template` / `DataTemplate` are kept but never applied; `VisualTreeHelper` walks the logical tree |
| window position, `Topmost`, `ShowInTaskbar` | kept as properties, not applied — GTK 4 leaves placement to the window manager (on Wayland the compositor owns geometry outright) |
| rich text | `RichTextBox` holds plain text; RTF loads as its text |
| `WindowsFormsHost` | absent |

## Why this is portable at all

Two facts about the implementation, both checkable:

- **No `DllImport` anywhere in the layer.** Not one P/Invoke in ~16,600 lines. Every native call goes
  through `GtkSharp4`, which is where the per-OS problem already got solved.
- **No OS branching.** No `RuntimeInformation.IsOSPlatform`, no `#if WINDOWS`. There is one code path.

So portability is not a feature this layer implements — it is a property it inherits. The same
`netstandard2.0` assembly runs wherever GTK 4 and .NET both run.

## Verification status

Honest accounting, measured rather than assumed:

| | Status |
|---|---|
| `Xamarin.Forms.Wpf.UnitTests` | **171 tests, all passing** on Windows (xUnit v3, `net10.0`) |
| coverage | controls (22), items/selection (21), windows (15), input (14), values (13), dependency properties (11), routed events (10), layout (10), emitted XAML (7), XAML fixtures (5) |
| test style | real GTK 4 widgets on a dedicated STA thread, via the backend suite's `GtkTestHost` (linked, not copied) |
| Linux | the layer is OS-agnostic and the backend suite runs under Xvfb; **this suite is not yet in CI** |
| macOS | follows from the same backend and the same single code path; **not covered by CI** |

CI (`.github/workflows/linux-gtk.yml`, Ubuntu) currently gates Core, XAML and the GTK renderer suite.
Adding the WPF suite behind `xvfb-run` is the obvious next step, and until that lands the Linux and
macOS claims rest on the architecture rather than on a green run.

## When this is the right tool

**A good fit**

- A WPF application, or a generated one (VB6/WinForms migration output), that must reach Linux — where
  the alternative is a rewrite.
- Line-of-business UI: forms, grids, dialogs, menus, data binding.
- Keeping one source tree that still builds against real WPF on Windows if you want it to.

**A bad fit**

- Heavy custom `ControlTemplate` / `Style` visual work — templates are not applied.
- Anything leaning on `VisualTreeHelper`, pixel-exact placement, or `WindowsFormsHost`.
- Greenfield cross-platform work, where Avalonia or MAUI is the better-supported starting point.

The value here is narrow and real: for a codebase whose WPF-ness is an accident of history rather
than a design goal, changing the framework underneath is far cheaper than changing the code on top.
