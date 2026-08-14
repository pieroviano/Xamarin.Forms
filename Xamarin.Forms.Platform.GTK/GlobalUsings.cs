// Compilation-wide type aliases.
//
// Gtk 4 introduced three type names that collide with names this backend uses constantly, in
// every one of the ~20 files that has a `using Gtk;` (or `using Gdk;`) at the top:
//
//   Gtk.EventArgs / Gtk.EventHandler   the arguments and handler of GtkEventControllerLegacy's
//                                      "event" signal. Inside a file that opens namespace Gtk
//                                      these WIN name resolution over System.EventArgs and
//                                      System.EventHandler, so every ordinary `EventArgs e`
//                                      became CS0104 - ambiguous, not merely shadowed, because
//                                      System is open too.
//   Gdk.EventArgs / Gdk.EventHandler   the same pair again in Gdk, so a file opening both
//                                      namespaces got the ambiguity even without System.
//   GLib.ListStore                     Gtk 4 binds GListStore, which collides with the
//                                      GtkListStore that Controls/ListView.cs and
//                                      Renderers/PickerRenderer.cs use.
//
// GtkSharp hit the same wall in its own source and solved it by qualifying every occurrence -
// see the comment on Gtk.Application.Invoke. That is right for a handful of call sites inside
// the binding; here it would mean touching ~50 declarations across 20 files to say what is true
// in all of them, so the alias is stated once instead.
//
// These are aliases, not usings: they bind the SIMPLE name, so `EventArgs` means System.EventArgs
// throughout this assembly. Anything that genuinely wants the Gtk 4 types must spell them out as
// Gtk.EventArgs / Gtk.EventHandler - nothing here does, and a file that starts to should be
// explicit about it anyway.
//
// Requires C# 10; the project sets LangVersion=latest, which is independent of its
// netstandard2.0 target framework.

global using EventArgs = System.EventArgs;
global using EventHandler = System.EventHandler;
global using ListStore = Gtk.ListStore;
