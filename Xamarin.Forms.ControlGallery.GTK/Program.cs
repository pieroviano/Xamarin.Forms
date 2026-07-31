using System;
using GLib;
using Xamarin.Forms;
using Xamarin.Forms.ControlGallery.GTK;
using Xamarin.Forms.Controls;
using Xamarin.Forms.Platform.GTK;
using Xamarin.Forms.Platform.GTK.Renderers;

[assembly: ExportRenderer(typeof(DisposePage), typeof(DisposePageRenderer))]
[assembly: ExportRenderer(typeof(DisposeLabel), typeof(DisposeLabelRenderer))]
namespace Xamarin.Forms.ControlGallery.GTK
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			ExceptionManager.UnhandledException += OnUnhandledException;

			// GtkOpenGL.Init() is gone for good: it existed only to bootstrap OpenTK's
			// Toolkit, and OpenGLView now runs on Gtk.GLArea, which needs no initialisation
			// (plan M6 §9.2).
			GtkThemes.Init();
			Gtk.Application.Init();

			// The Maps renderer lives in Xamarin.Forms.Maps.GTK, so that assembly has to be
			// loaded before Registrar scans - naming a type in it is what forces the load.
			Forms.Init(new[] { typeof(Xamarin.Forms.Maps.GTK.MapRenderer).Assembly });
			Xamarin.Forms.Maps.GTK.FormsMaps.Init();
			var app = new App();
			var window = new FormsWindow();
			window.LoadApplication(app);
			window.SetApplicationTitle("Xamarin.Forms GTK# Backend");
			window.SetApplicationIcon("xamarinlogo.png");
			window.Show();
			Gtk.Application.Run();
		}

		private static void OnUnhandledException(UnhandledExceptionArgs args)
		{
			System.Diagnostics.Debug.WriteLine($"Unhandled GTK# exception: {args.ExceptionObject}");
		}
	}

	public class DisposePageRenderer : PageRenderer
	{
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				((DisposePage)Element).SendRendererDisposed();
			}

			base.Dispose(disposing);
		}
	}

	public class DisposeLabelRenderer : LabelRenderer
	{
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				((DisposeLabel)Element).SendRendererDisposed();
			}

			base.Dispose(disposing);
		}
	}
}