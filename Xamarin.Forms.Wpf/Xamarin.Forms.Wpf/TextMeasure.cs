using System;
using System.Windows;
using System.Windows.Media;

namespace Xamarin.Forms.Wpf
{
	/// <summary>The size a text takes on screen: Pango's answer when GTK is running, an estimate otherwise.</summary>
	internal static class TextMeasure
	{
		/// <param name="emSize">The font size in device-independent pixels, WPF's unit.</param>
		internal static (double Width, double Height) Measure(string text, Typeface typeface, double emSize)
		{
			if (string.IsNullOrEmpty(text))
				return (0, emSize * 1.2);

			if (Forms.IsInitialized)
			{
				try
				{
					return WithPango(text, typeface, emSize);
				}
				catch (DllNotFoundException)
				{
				}
				catch (EntryPointNotFoundException)
				{
				}
			}

			// Average advance of a proportional UI font.
			return (text.Length * emSize * 0.55, emSize * 1.2);
		}

		/// <summary>
		/// A never-shown label, kept for the life of the process: it is only a source of Pango layouts, and a GTK
		/// wrapper that is finalized after its widget is gone aborts the process (see GtkTestHost.Retire).
		/// </summary>
		static Gtk.Label s_measurer;

		static (double Width, double Height) WithPango(string text, Typeface typeface, double emSize)
		{
			{
				var label = s_measurer ?? (s_measurer = new Gtk.Label(null));
				var layout = label.CreatePangoLayout(text);
				var description = new Pango.FontDescription();
				if (typeface?.FontFamily != null)
					description.Family = typeface.FontFamily.FirstFamily;
				description.Size = (int)(Units.DipsToPoints(emSize) * Pango.Scale.PangoScale);
				if (typeface != null)
				{
					description.Weight = typeface.Weight.ToOpenTypeWeight() >= 600 ? Pango.Weight.Bold : Pango.Weight.Normal;
					description.Style = typeface.Style == FontStyles.Italic ? Pango.Style.Italic
						: typeface.Style == FontStyles.Oblique ? Pango.Style.Oblique : Pango.Style.Normal;
				}

				layout.FontDescription = description;
				layout.GetPixelSize(out var width, out var height);
				return (width, height);
			}
		}
	}

	/// <summary>WPF measures in device-independent pixels (1/96 in); Xamarin.Forms on GTK sizes fonts in points.</summary>
	internal static class Units
	{
		internal static double DipsToPoints(double dips) => dips * 72.0 / 96.0;

		internal static double PointsToDips(double points) => points * 96.0 / 72.0;
	}
}
