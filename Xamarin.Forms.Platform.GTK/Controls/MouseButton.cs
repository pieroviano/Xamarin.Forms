namespace Xamarin.Forms.Platform.GTK.Controls
{
	/// <summary>
	/// Which pointer button raised an item-tapped event.
	/// </summary>
	/// <remarks>
	/// Replaces the former dependency on OpenTK.Input.MouseButton, which the GTK3 port
	/// dropped along with OpenTK. The member values are deliberately identical to
	/// OpenTK's (Left = 0, Middle = 1, Right = 2) because call sites translate a GDK
	/// button number - which is 1-based - with <c>(MouseButton)evt.Button - 1</c>.
	/// </remarks>
	public enum MouseButton
	{
		Left = 0,
		Middle = 1,
		Right = 2
	}
}
