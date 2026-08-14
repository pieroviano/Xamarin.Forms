using System.Linq;
using Gtk;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Helpers
{
	internal static class DialogHelper
	{
		public static void ShowAlert(PlatformRenderer platformRender, AlertArguments arguments)
		{
			MessageDialog messageDialog = new MessageDialog(
					platformRender.Toplevel as Window,
					DialogFlags.DestroyWithParent,
					MessageType.Other,
					GetAlertButtons(arguments),
					arguments.Message);

			SetDialogTitle(arguments.Title, messageDialog);
			SetButtonText(arguments.Accept, ButtonsType.Ok, messageDialog);
			SetButtonText(arguments.Cancel, ButtonsType.Cancel, messageDialog);

			// The Response signal, not Run(). Gtk 4 removed gtk_dialog_run because it spun a NESTED
			// main loop to block until the user answered, and re-entering the main loop from inside
			// an event handler is what made modal dialogs deadlock.
			//
			// Nothing is lost by not blocking: arguments.SetResult completes the TaskCompletionSource
			// that Forms' DisplayAlert is already awaiting, so the wait simply happens where it
			// always belonged - in the caller's await, not in a second main loop.
			messageDialog.Response += (o, args) =>
			{
				arguments.SetResult(args.ResponseId == ResponseType.Ok);
				messageDialog.Destroy();
			};

			messageDialog.Present();
		}

		public static void ShowActionSheet(PlatformRenderer platformRender, ActionSheetArguments arguments)
		{
			MessageDialog messageDialog = new MessageDialog(
			   platformRender.Toplevel as Window,
			   DialogFlags.DestroyWithParent,
			   MessageType.Other,
			   ButtonsType.Cancel,
			   null);

			SetDialogTitle(arguments.Title, messageDialog);
			SetButtonText(arguments.Cancel, ButtonsType.Cancel, messageDialog);
			SetDestructionButton(arguments.Destruction, messageDialog);
			AddExtraButtons(arguments, messageDialog);

			// See ShowAlert for why this is a signal rather than a blocking Run().
			messageDialog.Response += (o, args) =>
			{
				var response = args.ResponseId;

				if (response == ResponseType.Cancel)
				{
					arguments.SetResult(arguments.Cancel);
				}
				else if (response == ResponseType.Reject)
				{
					arguments.SetResult(arguments.Destruction);
				}

				messageDialog.Destroy();
			};

			messageDialog.Present();
		}

		private static void SetDialogTitle(string title, MessageDialog messageDialog)
		{
			messageDialog.Title = title ?? string.Empty;
		}

		/// <remarks>
		/// GetWidgetForResponse, not a descendant walk for a button whose label is the stock id
		/// "gtk-ok". Gtk 4 removed GtkHButtonBox (the dialog's action area is an ordinary box now)
		/// AND the stock system that supplied those labels, so the old search had lost both its
		/// container and its needle. Asking the dialog which widget carries a response is what the
		/// walk was approximating, and it does not depend on the theme's wording.
		/// </remarks>
		private static void SetButtonText(string text, ButtonsType type, MessageDialog messageDialog)
		{
			ResponseType response;

			switch (type)
			{
				case ButtonsType.Ok:
					response = ResponseType.Ok;
					break;
				case ButtonsType.Cancel:
					response = ResponseType.Cancel;
					break;
				default:
					return;
			}

			var targetButton = messageDialog.GetWidgetForResponse((int)response) as Gtk.Button;

			if (targetButton == null)
				return;

			if (string.IsNullOrEmpty(text))
			{
				targetButton.Visible = false;
			}
			else
			{
				targetButton.Label = text;
			}
		}

		private static void SetDestructionButton(string destruction, MessageDialog messageDialog)
		{
			if (!string.IsNullOrEmpty(destruction))
			{
				var destructionButton =
					messageDialog.AddButton(destruction, ResponseType.Reject) as Gtk.Button;

				var destructionColor = Color.Red.ToGtkColor();
				destructionButton.Child.SetForegroundColor(destructionColor, StateType.Normal);
				destructionButton.Child.SetForegroundColor(destructionColor, StateType.Prelight);
				destructionButton.Child.SetForegroundColor(destructionColor, StateType.Active);
			}
		}

		private static void AddExtraButtons(ActionSheetArguments arguments, MessageDialog messageDialog)
		{
			var vbox = messageDialog.ContentArea;

			// As we are not showing any message in this dialog, we just 
			// hide default container and avoid it from using space
			var firstChild = vbox.Children.FirstOrDefault();

			if (firstChild != null)
				firstChild.Visible = false;

			if (arguments.Buttons.Any())
			{
				for (int i = 0; i < arguments.Buttons.Count(); i++)
				{
					var button = new Gtk.Button();
					button.Label = arguments.Buttons.ElementAt(i);
					button.Clicked += (o, e) =>
					{
						arguments.SetResult(button.Label);
						messageDialog.Destroy();
					};
					button.Visible = true;

					vbox.PackStart(button, false, false, 0);
				}
			}
		}

		private static ButtonsType GetAlertButtons(AlertArguments arguments)
		{
			bool hasAccept = !string.IsNullOrEmpty(arguments.Accept);
			bool hasCancel = !string.IsNullOrEmpty(arguments.Cancel);

			ButtonsType type = ButtonsType.None;

			if (hasAccept && hasCancel)
			{
				type = ButtonsType.OkCancel;
			}
			else if (hasAccept && !hasCancel)
			{
				type = ButtonsType.Ok;
			}
			else if (!hasAccept && hasCancel)
			{
				type = ButtonsType.Cancel;
			}

			return type;
		}
	}
}
