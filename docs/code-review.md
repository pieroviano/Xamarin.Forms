# Code review — Xamarin.Forms (GTK fork)

Branch `code-review-remediation`, reviewed 2026-08-04 on Windows 11.

## What this repository is

A maintenance fork of `xamarin/Xamarin.Forms` 5.0.0 narrowed to the **GTK backend only**. The
Android, iOS, UAP, WPF, Tizen, MacOS and DualScreen legs are gone, along with the Cake build and
the Azure pipelines. `Xamarin.Forms.Gtk.sln` is the only solution; CI is a single GitHub Actions
workflow, `.github/workflows/linux-gtk.yml`. Libraries target `netstandard2.0`; the three test
projects target `net10.0` and run xUnit v3.

Roughly 345k lines, the great majority frozen upstream code. The genuinely fork-owned surface is
`Xamarin.Forms.Platform.GTK` (~25k lines), the GTK renderer test suite, and the build/CI
configuration, and that is where every finding below lands.

## Baseline

The tree builds clean in Debug and all three suites pass: **6092 passed / 10 skipped / 0 failed**
(193 GTK renderer, 4856 Core, 1043 XAML). So the findings below are about what a green build does
not reveal — teardown paths nothing exercises, property mappings wired to empty method bodies,
lifecycle events that never fire, and subscriptions detached from the wrong object.

One caveat on the baseline: this was measured on Windows only. WSL is currently unavailable on this
machine (`Wsl/0x80070422`, the service is disabled), so the Linux leg — which the GTK suite is known
to disagree with in places — was not re-measured. Nothing below is Linux-specific, but a Linux run
is still owed.

**Method.** Read directly, one reviewer, concentrated on the fork-owned surface: every renderer's
`OnElementChanged`/`Dispose` pair, every `GLib.Idle` callback, the cell and page hierarchies, the
window/platform bootstrap, and the parts of `Xamarin.Forms.Core` this fork has modified. Every
finding cites `file:line` and was read in the source. Finding 1 was additionally confirmed by
running it.

Severity is about consequence to a running app, not effort to fix:

- **High** — crashes, or ships visibly broken.
- **Medium** — behaviour a user would see as wrong, or a real leak / teardown fault.
- **Low** — narrow, cosmetic, or a gap in what the suite can catch.

| # | Sev | Finding | Location |
|---|---|---|---|
| 1 | HIGH | TabbedPageRenderer.Dispose ignores `disposing` and dereferences a nulled Element — throws on the finalizer thread, which kills the process | `Xamarin.Forms.Platform.GTK/Renderers/TabbedPageRenderer.cs:95` |
| 2 | MEDIUM | RadioButtonRenderer maps six properties onto empty method bodies, so TextColor/Font/Border/Padding silently do nothing | `Xamarin.Forms.Platform.GTK/Renderers/RadioButtonRenderer.cs:135` |
| 3 | MEDIUM | TabbedPage and CarouselPage renderers detach their `e.OldElement` handlers from the NEW element, so the old one keeps them | `Xamarin.Forms.Platform.GTK/Renderers/TabbedPageRenderer.cs:23` |
| 4 | MEDIUM | AbstractPageRenderer.Dispose detaches PropertyChanged but not BatchCommitted, so every page retains its destroyed renderer | `Xamarin.Forms.Platform.GTK/Renderers/AbstractPageRenderer.cs:151` |
| 5 | MEDIUM | PickerRenderer has no `e.OldElement` path at all — a reused renderer keeps observing the previous Picker's Items and never observes the new one's | `Xamarin.Forms.Platform.GTK/Renderers/PickerRenderer.cs:35` |
| 6 | MEDIUM | FormsWindow compares the `[Flags]` Gdk.WindowState with `==`, so minimizing a maximized window raises OnResume instead of OnSleep | `Xamarin.Forms.Platform.GTK/FormsWindow.cs:136` |
| 7 | MEDIUM | Page.IsBusy is inert on GTK: nothing subscribes to BusySetSignalName, though Platform.Dispose unsubscribes from it | `Xamarin.Forms.Platform.GTK/Platform.cs:117` |
| 8 | LOW | LayoutRenderer is the one idle callback in the backend with no disposal guard | `Xamarin.Forms.Platform.GTK/Renderers/LayoutRenderer.cs:105` |
| 9 | LOW | RadioButtonRenderer's group ghost widget is created and never destroyed | `Xamarin.Forms.Platform.GTK/Renderers/RadioButtonRenderer.cs:36` |
| 10 | LOW | UriImageSource abandons an open FileStream when the freshly downloaded copy is zero-length | `Xamarin.Forms.Core/UriImageSource.cs:331` |
| 11 | LOW | CarouselPageRenderer appends to `_pages` with a per-batch index where the widget inserts at the collection index | `Xamarin.Forms.Platform.GTK/Renderers/CarouselPageRenderer.cs:132` |
| 12 | LOW | Platform.SetPage answers "a page is already set" with a bare NotImplementedException | `Xamarin.Forms.Platform.GTK/Platform.cs:132` |
| 13 | LOW | RadioButton is registered but has no property-mapping test, which is why finding 2 is invisible | `Xamarin.Forms.Platform.GTK.UnitTests/RendererRegistrationTests.cs:126` |
| 14 | LOW | PickerRenderer's property-changed chain mixes four bare `if`s with a trailing `else if` | `Xamarin.Forms.Platform.GTK/Renderers/PickerRenderer.cs:54` |

---

## High severity

### 1. TabbedPageRenderer.Dispose ignores `disposing` and dereferences a nulled Element — throws on the finalizer thread, which kills the process

`Xamarin.Forms.Platform.GTK/Renderers/TabbedPageRenderer.cs:93`

```csharp
protected override void Dispose(bool disposing)
{
    Page.PagesChanged -= OnPagesChanged;
    Page.ChildAdded -= OnPageAdded;
    Page.ChildRemoved -= OnPageRemoved;

    if (Widget != null)
    {
        Widget.NoteBook.SwitchPage -= OnNotebookPageSwitched;
    }

    base.Dispose(disposing);
}
```

There is no `if (disposing)` guard, and `Page` is `Element as TPage`
(`AbstractPageRenderer.cs:33`) — which `AbstractPageRenderer.Dispose(bool)` has already set to null
(`AbstractPageRenderer.cs:158`). Every other page renderer guards both: `CarouselPageRenderer.cs:35`
opens with `if (disposing)` and null-checks `Page`, `FlyoutPageRenderer.cs:49` and
`ShellRenderer.cs:168` likewise.

The finalizer reaches this, and the chain is not hypothetical:

- `GLib.Object` declares its own `Finalize`, whose only real call is `Dispose(bool)` — verified by
  reading the IL in `glib-sharp.dll`.
- `GLib.Object.Dispose(bool)` does **not** call `GC.SuppressFinalize` — only the public
  parameterless `Dispose()` does, also verified in the IL.
- `AbstractPageRenderer.Destroy()` calls the protected `Dispose(true)` **directly**
  (`AbstractPageRenderer.cs:102`), never the public `Dispose()`. So the teardown path
  `Platform.DisposeModelAndChildrenRenderers` uses — `(renderer as Widget)?.Destroy()`
  (`Platform.cs:72`) — leaves the object still registered for finalization, with `Element` already
  null.

So the next GC after any TabbedPage teardown runs `Dispose(false)`, hits line 95, and throws
`NullReferenceException` on the finalizer thread. An unhandled exception there terminates the
process — no `catch`, no `AppDomain.UnhandledException` recovery.

Confirmed by running it. Invoking `Dispose(false)` by reflection on a `TabbedPageRenderer` that had
been `SetElement`-ed and then `Destroy()`-ed:

```
System.NullReferenceException: Object reference not set to an instance of an object.
   at Xamarin.Forms.Platform.GTK.Renderers.TabbedPageRenderer.Dispose(Boolean disposing)
      in Xamarin.Forms.Platform.GTK\Renderers\TabbedPageRenderer.cs:line 95
```

The same probe against `CarouselPageRenderer` completes cleanly, which isolates the cause to the
missing guard rather than to the teardown order.

Note that `ButtonRenderer.cs:29` and `RadioButtonRenderer.cs:13` also omit `if (disposing)`, but
both null-check what they touch, so they are merely wrong in principle rather than in effect.

**Fix.** Wrap the body in `if (disposing)` and null-check `Page` and `Widget.NoteBook`, matching
`CarouselPageRenderer.Dispose`. Worth a test in `LifecycleTests` that destroys a page renderer and
then drives `Dispose(false)`, for all four page renderers at once — the probe above is three lines.

---

## Medium severity

### 2. RadioButtonRenderer maps six properties onto empty method bodies

`Xamarin.Forms.Platform.GTK/Renderers/RadioButtonRenderer.cs:135`

```csharp
void UpdateTextColor() { }

void UpdateFont() { }

void UpdateBorderColor() { }

void UpdateBorderWidth() { }

void UpdateBorderRadius() { }

void UpdatePadding() { }
```

`OnElementPropertyChanged` (lines 65-103) routes `TextColor`, `FontFamily`, `FontSize`,
`FontAttributes`, `BorderColor`, `BorderWidth`, `CornerRadius` and `Padding` into these. Every one
of them is a no-op. `<RadioButton TextColor="Red" FontSize="20"/>` renders in the default theme
colour and size, and a trigger or binding that changes any of them does nothing at all — with no
warning, no exception and no log line.

This is the exact defect `VisualElementTrackerTests`' class remarks (line 11) say that suite exists
to prevent: *"Opacity and InputTransparent were previously wired into the property-change pipeline
with EMPTY implementations - setting either did nothing at all, silently, and no test noticed. A
silent no-op is worse than a NotImplementedException, because the only way to discover it is to look
at the running app."* These six are the same pattern, still live, and they are the only empty
`Update*()` bodies left in the backend — every other renderer implements what it maps.

And the native side is already built. `Controls/RadioButton.cs` — the widget this renderer drives —
exposes exactly the setters these six want:

```csharp
public Gtk.Label LabelWidget => _label;          // line 46
public void SetForegroundColor(Gdk.Color color)  // line 82
public void SetBorderWidth(uint width)           // line 89
public void SetBorderColor(Gdk.Color? color)     // line 95
```

So this is not "GTK cannot do it". `UpdateTextColor`, `UpdateBorderColor` and `UpdateBorderWidth`
are one line each against methods that already exist and are already used by
`UpdateBackgroundColor` in the same renderer (lines 105-122); the font properties go through
`LabelWidget` the way `LabelRenderer` does. The wiring was laid and never connected.

**Fix.** Implement the four that have native setters waiting, plus the font properties against
`LabelWidget`. For `CornerRadius` and `Padding`, if GTK genuinely has nothing to map them to, remove
the branches from `OnElementPropertyChanged` and log the limitation once — leaving a mapped property
pointing at `{ }` is the one option that should not survive.

### 3. TabbedPage and CarouselPage renderers detach their `e.OldElement` handlers from the NEW element

`Xamarin.Forms.Platform.GTK/Renderers/TabbedPageRenderer.cs:17`

```csharp
protected override void OnElementChanged(VisualElementChangedEventArgs e)
{
    base.OnElementChanged(e);

    if (e.OldElement != null)
    {
        Page.ChildAdded -= OnPageAdded;
        Page.ChildRemoved -= OnPageRemoved;
        Page.PagesChanged -= OnPagesChanged;
    }
```

`Page` is `Element as TPage`, and `AbstractPageRenderer.SetElement` assigns
`Element = element` **before** raising `OnElementChanged` (`AbstractPageRenderer.cs:55-57`). So
inside the `e.OldElement != null` branch, `Page` is already the *new* page. The three `-=` calls
therefore detach handlers from a page that was never subscribed — a no-op — while the old page keeps
all three.

`CarouselPageRenderer.cs:55-58` has the same shape for `PagesChanged`.

Two consequences. The discarded page holds the renderer alive for its whole life, and — worse —
mutating that page's `Children` still drives this renderer, which is now showing a different page:
`OnPagesChanged` calls `Widget.AddPage`/`Widget.RemovePage` and `e.Apply(Page.Children, ...)`, where
`Page` resolves to the *current* element. So adding a tab to the old TabbedPage adds a tab to the new
one.

`AbstractPageRenderer.OnElementChanged` gets this right two frames up the stack — it uses
`e.OldElement.PropertyChanged -= ...` explicitly (line 168) rather than going through `Page`.

**Fix.** Bind the old element locally and use it: `if (e.OldElement is TabbedPage oldPage) { oldPage.ChildAdded -= ...; }`.
Same in `CarouselPageRenderer`.

### 4. AbstractPageRenderer.Dispose detaches PropertyChanged but not BatchCommitted

`Xamarin.Forms.Platform.GTK/Renderers/AbstractPageRenderer.cs:145`

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        if (Element != null)
        {
            Element.PropertyChanged -= OnElementPropertyChanged;
        }

        Platform.SetRenderer(Element, null);

        Control?.Destroy();
        Control = null;
        Element = null;
    }
```

`OnElementChanged` attaches two handlers to the new element (lines 180-183):

```csharp
e.NewElement.PropertyChanged += OnElementPropertyChanged;
// See UpdateChildrenLayout(): a Forms layout pass does not queue a GTK
// resize, so the page's content would otherwise keep its natural size.
e.NewElement.BatchCommitted += OnElementBatchCommitted;
```

Dispose removes the first and not the second. Since nothing in the teardown path calls
`SetElement(null)` — `Platform.DisposeModelAndChildrenRenderers` destroys the widget and clears
`RendererProperty` — the `BatchCommitted` subscription is never removed by any route, so the Forms
`Page` keeps the destroyed renderer (and its whole GTK subtree) in its invocation list for as long
as the page object lives. That is every page held in a field, every page in a `NavigationPage`'s
stack that was popped and kept, and every `MainPage` reassigned and reassigned back.

It does not crash: `Element = null` on the line below makes the handler inert, and
`UpdateChildrenLayout` returns early on a null controller. It is purely retention — but retention of
the entire native subtree, per page, forever.

`VisualElementRenderer.Dispose` (`VisualElementRenderer.cs:362`) removes all three of its element
handlers through `DetachElementHandlers`, and its comment spells out exactly why this matters. The
page hierarchy did not get the same treatment.

**Fix.** Add `Element.BatchCommitted -= OnElementBatchCommitted;` next to the PropertyChanged
detach, or better, factor an `AttachElementHandlers`/`DetachElementHandlers` pair the way
`VisualElementRenderer` does so the two lists cannot drift apart again.

### 5. PickerRenderer has no `e.OldElement` path at all

`Xamarin.Forms.Platform.GTK/Renderers/PickerRenderer.cs:19`

```csharp
protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
{
    if (e.NewElement != null)
    {
        if (Control == null)
        {
            ComboBox comboBox = new ComboBox();
            ...
            ((LockableObservableListWrapper)Element.Items)._list.CollectionChanged += OnCollectionChanged;

            SetNativeControl(comboBox);
        }
```

The subscription to the element's `Items` collection sits inside `if (Control == null)`, i.e. it
happens exactly once, for whichever element the renderer saw first. There is no
`if (e.OldElement != null)` block anywhere in the file, and `Dispose` (line 83-86) detaches using
the *current* `Element`.

Renderer reuse is a real path here, not a theoretical one: `Cells/ViewCell.cs:82-91` hands an
existing renderer a different element via `renderer.SetElement(viewCell.View)` whenever the recycled
widget's renderer type matches. A `<ViewCell><Picker/></ViewCell>` in a ListView therefore produces a
`PickerRenderer` that, after the first rebind:

- still listens to the **first** Picker's `Items`, so changing that list repopulates a ComboBox now
  showing a different Picker, and
- never listens to the current Picker's `Items`, so `picker.Items.Add(...)` on the visible row does
  nothing, and
- leaks: `Dispose` detaches from the current element, so the first element's subscription is never
  removed and roots the renderer permanently.

Every other renderer in the backend handles the old element — `SwitchRenderer.cs:12`,
`LayoutRenderer.cs:13`, `TableViewRenderer.cs:46`, `ListViewRenderer.cs:37`,
`CollectionViewRenderer.cs:285`, `ScrollViewRenderer.cs`. `PickerRenderer` is the exception.

**Fix.** Add the old-element branch (`((LockableObservableListWrapper)e.OldElement.Items)._list.CollectionChanged -= OnCollectionChanged;`)
and move the new-element subscription out of the `if (Control == null)` block so it tracks the
element rather than the widget.

### 6. FormsWindow compares the `[Flags]` Gdk.WindowState with `==`

`Xamarin.Forms.Platform.GTK/FormsWindow.cs:130`

```csharp
private void OnWindowStateEvent(object o, WindowStateEventArgs args)
{
    if (args.Event.ChangedMask == Gdk.WindowState.Iconified)
    {
        var windowState = args.Event.NewWindowState;

        if (windowState == Gdk.WindowState.Iconified)
            _application.SendSleep();
        else
            _application.SendResume();
    }
}
```

`Gdk.WindowState` is a flags enum — confirmed against the GtkSharp build this fork references:
`Withdrawn, Iconified, Maximized, Sticky, Fullscreen, Above, Below`. `NewWindowState` is the window's
*complete* state, so `== Gdk.WindowState.Iconified` is true only when iconified is the sole bit set.

Concretely: maximize the window, then minimize it. Only the iconified bit changes, so the outer
check passes; `NewWindowState` is `Maximized | Iconified`, so the inner check fails and
`SendResume()` runs. The app receives `OnResume` at the moment it is minimized — and, on restore,
`OnResume` again. `OnSleep` never fires for a maximized window, which is the state most desktop apps
spend their life in.

Anything an app does in `OnSleep` — flushing state, pausing timers, releasing a camera or a socket —
is simply never done, and `OnResume` firing while the window is hidden can restart work that then
runs against a window nobody can see.

**Fix.** Test the bits: `if ((args.Event.ChangedMask & Gdk.WindowState.Iconified) != 0)` and
`if ((args.Event.NewWindowState & Gdk.WindowState.Iconified) != 0)`.

### 7. Page.IsBusy is inert on GTK, and a stale Unsubscribe hides it

`Xamarin.Forms.Platform.GTK/Platform.cs:117`

```csharp
// Platform's constructor, lines 51-52 - two subscriptions:
MessagingCenter.Subscribe(this, Page.AlertSignalName, (Page sender, AlertArguments arguments) => DialogHelper.ShowAlert(PlatformRenderer, arguments));
MessagingCenter.Subscribe(this, Page.ActionSheetSignalName, (Page sender, ActionSheetArguments arguments) => DialogHelper.ShowActionSheet(PlatformRenderer, arguments));

// IDisposable.Dispose, lines 115-117 - three unsubscriptions:
MessagingCenter.Unsubscribe<Page, ActionSheetArguments>(this, Page.ActionSheetSignalName);
MessagingCenter.Unsubscribe<Page, AlertArguments>(this, Page.AlertSignalName);
MessagingCenter.Unsubscribe<Page, bool>(this, Page.BusySetSignalName);
```

`Page.BusySetSignalName` is unsubscribed but never subscribed. Grepping the whole backend for it
returns exactly that one line, and `IsBusy` does not appear anywhere in
`Xamarin.Forms.Platform.GTK`.

Core does send it — `Xamarin.Forms.Core/Page.cs:447`, `:470` and `:527` all
`MessagingCenter.Send(this, BusySetSignalName, ...)`. So `page.IsBusy = true`, one of the two
built-in ways a Forms app says "working", produces no activity indicator, no cursor change, nothing.
The orphaned `Unsubscribe` is what makes it look handled: it is the only trace in the backend that
this signal exists, and it reads as the other half of a subscription that is not there.

**Fix.** Either subscribe and show something — the backend already has `Controls/ActivityIndicator.cs`
and `PlatformRenderer` to host it — or delete the dead `Unsubscribe` and record in the GTK
limitations that `IsBusy` is unsupported. Right now it is neither implemented nor documented as
missing.

---

## Low severity

### 8. LayoutRenderer is the one idle callback in the backend with no disposal guard

`Xamarin.Forms.Platform.GTK/Renderers/LayoutRenderer.cs:98`

```csharp
private void LayoutChanged(object sender, System.EventArgs e)
{
    if (_resizeQueued)
        return;

    _resizeQueued = true;

    GLib.Idle.Add(() =>
    {
        _resizeQueued = false;
        QueueResize();

        return false;
    });
}
```

Every other deferred callback in this backend checks that it still has something to act on before
acting. `VisualElementRenderer.cs:266` and `:309`, `AbstractPageRenderer.cs:236`,
`ShellRenderer.cs:766` and `:1618`, `CollectionViewRenderer.cs:1568` and `:2143` and
`ListViewRenderer.cs:205`, `:252`, `:321` all test `_disposed`; `Controls/Carousel.cs:233`,
`Controls/NotebookWrapper.cs:195`, `Controls/EntryWrapper.cs:123` and
`Controls/ScrolledTextView.cs:139` additionally test `Handle != IntPtr.Zero`, with a comment on each
saying why ("A pending idle can outlive the widgets it resizes; a destroyed or disposed GtkSharp
wrapper is left holding a null handle").

This one calls `QueueResize()` on `this` unconditionally. `VisualElementRenderer` exposes exactly the
guard it needs — `public bool Disposed` at `VisualElementRenderer.cs:63` — and after
`Dispose(bool)` chains to `base.Dispose`, GtkSharp has zeroed the handle, so the call reaches
`gtk_widget_queue_resize(NULL)` and GTK logs a critical. CI runs this suite a second time under
`G_DEBUG=fatal-criticals` (`linux-gtk.yml:147`), where a critical aborts.

The window is narrow — Forms must raise `LayoutChanged` and the layout must be torn down before the
idle drains — which is presumably why the suite has never caught it. It is still the only
unguarded one.

**Fix.** `if (Disposed) return false;` at the top of the callback, matching its siblings.

### 9. RadioButtonRenderer's group ghost widget is created and never destroyed

`Xamarin.Forms.Platform.GTK/Renderers/RadioButtonRenderer.cs:36`

```csharp
// The ghost shares the button's group so that a single unchecked RadioButton is
// representable: GTK always keeps one member of a group active, so "nothing checked"
// means the ghost is the active one. UpdateCheck below decides which of the two it is.
_ghost = new Gtk.RadioButton(button);
```

The technique is sound and well explained. But `_ghost` is never parented — it exists only to hold
a group slot — and `Dispose` (lines 13-22) touches only `Control`. A widget that is never added to a
container keeps the floating reference it was born with, so nothing ever destroys it: one leaked
`GtkRadioButton` per RadioButton renderer, for the life of the process.

**Fix.** In `Dispose`, `_ghost?.Destroy(); _ghost = null;` before chaining to base — and while there,
give that method the `if (disposing)` guard it is missing (see finding 1).

### 10. UriImageSource abandons an open FileStream when the freshly downloaded copy is zero-length

`Xamarin.Forms.Core/UriImageSource.cs:330`

```csharp
Stream stream = await GetStreamAsyncUnchecked(key, uri, cancellationToken);
if (stream == null || stream.Length == 0 || !stream.CanRead)
{
    sem.Release();
    return null;
}
```

On the success path the stream is handed to a `StreamWrapper` whose `Disposed` event releases the
semaphore, so ownership is clear. On this path it is not: `GetStreamAsyncUnchecked` returns the
result of `Store.OpenFileAsync(path, FileMode.Open, FileAccess.Read)` (line 227), a live
`FileStream`, and this branch drops it on the floor. The handle stays open until the finalizer runs.

Reachable whenever a server answers with an empty body: the download succeeds, `CopyToAsync` writes
nothing, the file is re-opened, `Length == 0`, and the stream leaks. A page of broken image URLs
leaks one handle each, and the cache entry keeps returning zero length for the whole
`CacheValidity` window, so a retry leaks another.

The sibling path gets this right — `OpenLocallyCachedCopyAsync` disposes before returning null
(line 262), with a comment about exactly this case.

**Fix.** `stream?.Dispose();` before `sem.Release();`.

### 11. CarouselPageRenderer appends to `_pages` where the widget inserts

`Xamarin.Forms.Platform.GTK/Renderers/CarouselPageRenderer.cs:127`

```csharp
int index = e.NewStartingIndex;
for (int i = 0; i < e.NewItems.Count; i++)
{
    var page = e.NewItems[i] as Page;
    Widget.AddPage(index, page);
    _pages.Add(new PageContainer(page, i));
    index++;
}
```

`Widget.AddPage` inserts at the right position — `Controls/Carousel.cs:133` is
`_pages.Insert(index, ...)` — while the renderer's own list appends, and stamps
`PageContainer.Index` with `i`, the offset *within this batch*, rather than with `index`. For an
append at the end of the collection the two happen to agree, which is why this has not been noticed.

`carouselPage.Children.Insert(0, page)` breaks the agreement: the widget shows the new page first,
the renderer's `_pages` lists it last with `Index = 0`, and the `newPages` list built from it at
lines 136-140 and passed to `e.Apply(Page.Children, newPages)` is in the wrong order too. The
divergence lasts until the next `UpdateSource()`, which rebuilds `_pages` from
`Element.LogicalChildren` and quietly repairs it.

**Fix.** `_pages.Insert(index, new PageContainer(page, index));`.

### 12. Platform.SetPage answers "a page is already set" with a bare NotImplementedException

`Xamarin.Forms.Platform.GTK/Platform.cs:131`

```csharp
if (Page != null)
    throw new NotImplementedException();
```

No message, and the exception type says "this feature is unfinished" when the real meaning is "a
`Platform` instance takes one page and this one already has one". The current call path never trips
it — `FormsWindow.UpdateMainPage` builds a fresh `Platform` for each main page (`FormsWindow.cs:109`)
— so this is purely about the diagnostic anyone hits if that ever changes, and they get a stack
trace with nothing in it.

**Fix.** `throw new InvalidOperationException("This Platform already hosts a page; create a new Platform for a new root page.")`.

### 13. RadioButton is registered but has no property-mapping test

`Xamarin.Forms.Platform.GTK.UnitTests/RendererRegistrationTests.cs:126`

`RadioButton` appears exactly once in the whole GTK suite: in the list of types
`RendererRegistrationTests` checks a renderer resolves for. `PropertyMappingTests` covers Entry,
Label, Switch, ProgressBar, Picker, ImageButton and more; `CoreControlMappingTests` covers
DatePicker, TimePicker, BoxView and others. Neither covers RadioButton, which is precisely why
finding 2 — six mapped properties pointing at `{ }` — sits in a green suite.

**Fix.** Add a `RadioButtonMaps...` case to `PropertyMappingTests` in the shape the rest of that file
uses: assert at creation and again after a change, on the native widget. Written before finding 2 is
fixed it fails, which is the right order.

### 14. PickerRenderer's property-changed chain mixes bare `if`s with a trailing `else if`

`Xamarin.Forms.Platform.GTK/Renderers/PickerRenderer.cs:54`

```csharp
if (e.PropertyName == Picker.TitleProperty.PropertyName)
    UpdatePicker();
if (e.PropertyName == Picker.SelectedIndexProperty.PropertyName)
    UpdateSelectedIndex();
if (e.PropertyName == Picker.ItemsSourceProperty.PropertyName)
    UpdateItemsSource();
if (e.PropertyName == Picker.TextColorProperty.PropertyName)
    UpdateTextColor();
else if (e.PropertyName == Label.HorizontalTextAlignmentProperty.PropertyName)
    UpdateHorizontalTextAlignment();
else if (e.PropertyName == Label.VerticalTextAlignmentProperty.PropertyName)
    UpdateVerticalTextAlignment();
```

The first four are independent `if`s and the last three form a chain hanging off the fourth. Since
`e.PropertyName` can only match one of them, the behaviour is correct today — but the shape says the
opposite of what it does, and the two alignment branches are reachable only because the TextColor
test above them failed. Every other renderer in the backend writes this as one `if`/`else if` chain.

**Fix.** Make it a single chain. Purely defensive, but this is the file where a future property gets
added, and adding it to the wrong half is a silent no-op.

---
