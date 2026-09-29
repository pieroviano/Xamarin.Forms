using System.Collections.Generic;

namespace System.Windows
{
	public enum RoutingStrategy
	{
		Tunnel,
		Bubble,
		Direct,
	}

	public sealed class RoutedEvent
	{
		internal RoutedEvent(string name, RoutingStrategy strategy, Type handlerType, Type ownerType)
		{
			Name = name;
			RoutingStrategy = strategy;
			HandlerType = handlerType;
			OwnerType = ownerType;
		}

		public string Name { get; }

		public RoutingStrategy RoutingStrategy { get; }

		public Type HandlerType { get; }

		public Type OwnerType { get; }

		public RoutedEvent AddOwner(Type ownerType) => this;

		public override string ToString() => OwnerType.Name + "." + Name;
	}

	public static class EventManager
	{
		static readonly Dictionary<RoutedEvent, List<(Type Type, Delegate Handler, bool HandledEventsToo)>> s_classHandlers =
			new Dictionary<RoutedEvent, List<(Type, Delegate, bool)>>();

		public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType) =>
			new RoutedEvent(name, routingStrategy, handlerType, ownerType);

		public static void RegisterClassHandler(Type classType, RoutedEvent routedEvent, Delegate handler) =>
			RegisterClassHandler(classType, routedEvent, handler, false);

		public static void RegisterClassHandler(Type classType, RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
		{
			lock (s_classHandlers)
			{
				if (!s_classHandlers.TryGetValue(routedEvent, out var list))
					s_classHandlers.Add(routedEvent, list = new List<(Type, Delegate, bool)>());

				list.Add((classType, handler, handledEventsToo));
			}
		}

		internal static void InvokeClassHandlers(object target, RoutedEventArgs e)
		{
			(Type Type, Delegate Handler, bool HandledEventsToo)[] handlers;
			lock (s_classHandlers)
			{
				if (!s_classHandlers.TryGetValue(e.RoutedEvent, out var list))
					return;

				handlers = list.ToArray();
			}

			foreach (var (type, handler, handledToo) in handlers)
			{
				if (type.IsInstanceOfType(target) && (!e.Handled || handledToo))
					e.InvokeHandler(handler, target);
			}
		}
	}

	public class RoutedEventArgs : EventArgs
	{
		object _source;

		public RoutedEventArgs()
		{
		}

		public RoutedEventArgs(RoutedEvent routedEvent) => RoutedEvent = routedEvent;

		public RoutedEventArgs(RoutedEvent routedEvent, object source)
		{
			RoutedEvent = routedEvent;
			_source = OriginalSource = source;
		}

		public RoutedEvent RoutedEvent { get; set; }

		public bool Handled { get; set; }

		public object Source
		{
			get => _source;
			set
			{
				_source = value;
				if (OriginalSource == null)
					OriginalSource = value;
			}
		}

		public object OriginalSource { get; private set; }

		protected virtual void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is RoutedEventHandler routed)
				routed(genericTarget, this);
			else
				genericHandler.DynamicInvoke(genericTarget, this);
		}

		protected virtual void OnSetSource(object source)
		{
		}

		internal void InvokeHandler(Delegate handler, object target) => InvokeEventHandler(handler, target);

		internal void SetSource(object source)
		{
			OnSetSource(source);
			_source = source;
			if (OriginalSource == null)
				OriginalSource = source;
		}
	}

	public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

	public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs
	{
		public RoutedPropertyChangedEventArgs(T oldValue, T newValue)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		public RoutedPropertyChangedEventArgs(T oldValue, T newValue, RoutedEvent routedEvent) : this(oldValue, newValue) =>
			RoutedEvent = routedEvent;

		public T OldValue { get; }

		public T NewValue { get; }

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is RoutedPropertyChangedEventHandler<T> typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void RoutedPropertyChangedEventHandler<T>(object sender, RoutedPropertyChangedEventArgs<T> e);

	public class SizeChangedEventArgs : RoutedEventArgs
	{
		internal SizeChangedEventArgs(UIElement element, Size previousSize, Size newSize)
			: base(FrameworkElement.SizeChangedEvent, element)
		{
			PreviousSize = previousSize;
			NewSize = newSize;
		}

		public Size PreviousSize { get; }

		public Size NewSize { get; }

		public bool WidthChanged => !PreviousSize.Width.Equals(NewSize.Width);

		public bool HeightChanged => !PreviousSize.Height.Equals(NewSize.Height);

		protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
		{
			if (genericHandler is SizeChangedEventHandler typed)
				typed(genericTarget, this);
			else
				base.InvokeEventHandler(genericHandler, genericTarget);
		}
	}

	public delegate void SizeChangedEventHandler(object sender, SizeChangedEventArgs e);

	public interface IInputElement
	{
		bool Focusable { get; set; }

		bool IsEnabled { get; }

		bool IsKeyboardFocused { get; }

		bool IsKeyboardFocusWithin { get; }

		bool IsMouseOver { get; }

		bool Focus();

		void AddHandler(RoutedEvent routedEvent, Delegate handler);

		void RemoveHandler(RoutedEvent routedEvent, Delegate handler);

		void RaiseEvent(RoutedEventArgs e);
	}
}
