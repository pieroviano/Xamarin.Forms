# Net4x.Xamarin.Forms.Wpf

The WPF API (`System.Windows.*`) implemented over Xamarin.Forms and rendered by `Xamarin.Forms.Platform.GTK` (GTK 4):
WPF code and WPF XAML compile and run unchanged on Linux, macOS and Windows.

```
dotnet add package Net4x.Xamarin.Forms.Wpf
dotnet add package Net4x.Xamarin.Forms.Build.Tasks   # compiles the XAML (its targets are not transitive)
dotnet add package GtkSharp4                        # fetches the GTK runtime on Windows (not transitive)
```

## How it works

| WPF | Here |
|---|---|
| `DependencyObject` | a Xamarin.Forms `Element`; values in `BindableProperty`s, so XAML, bindings and `{DynamicResource}` are Xamarin.Forms' own |
| `DependencyProperty` | a view of a `BindableProperty` (`XxxProperty` fields are `BindableProperty`, implicitly `DependencyProperty`) |
| `UIElement` | owns a Xamarin.Forms `View` (created on first use), kept in step with its WPF properties |
| logical tree | the WPF one: routed events, inherited fonts/colors, `IsEnabled`, resources |
| `Window` | a GTK window of its own (`FormsWindow.LoadPage`); any number open; `ShowDialog` = nested loop |
| `Application` | brings GTK and Xamarin.Forms up; `Run` until `Shutdown` (`ShutdownMode` honoured) |
| input | GTK event controllers per window: `Preview*` tunnel, then bubble; handled events stop at the widget |
| `Dispatcher` | the GLib main context; `DispatcherTimer` = GLib timeout |
| XAML namespace | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` maps to these namespaces (`XmlnsDefinition`) |

## Boundaries

| Not modelled | Consequence |
|---|---|
| templates, visual tree | `Template`/`DataTemplate` kept, never applied; `VisualTreeHelper` walks the logical tree |
| window position, `Topmost`, `ShowInTaskbar` | GTK 4 leaves placement to the window manager |
| rich text | `RichTextBox` holds plain text; RTF loads as its text |
| `WindowsFormsHost` | absent |

A public type in a `System.Windows.*` namespace here is always a WPF type: nothing is added that WPF does not have,
so code that compiles against WPF resolves the same names.
