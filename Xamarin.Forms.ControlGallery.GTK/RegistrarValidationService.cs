using Xamarin.Forms;
using Xamarin.Forms.ControlGallery.GTK;
using Xamarin.Forms.Controls;
using Xamarin.Forms.Platform.GTK;

[assembly: Dependency(typeof(RegistrarValidationService))]
namespace Xamarin.Forms.ControlGallery.GTK
{
	public class RegistrarValidationService : IRegistrarValidationService
	{
		public bool Validate(VisualElement element, out string message)
		{
			message = "Success";

			if (element == null)
				return true;

#if !ENABLE_GTK_OPENGL
			// OpenGLView has no renderer while it is quarantined behind EnableGtkOpenGL in
			// Xamarin.Forms.Platform.GTK (restored in M6). Without this the gallery aborts
			// during start-up validation before a single page is shown.
			if (element is OpenGLView)
				return true;
#endif

			var renderer = Platform.GTK.Platform.CreateRenderer(element);

			if (renderer == null
				|| renderer.GetType().Name == "DefaultRenderer"
				)
			{
				message = $"Failed to load proper GTK renderer for {element.GetType().Name}";
				return false;
			}

			return true;
		}
	}
}