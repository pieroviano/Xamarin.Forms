using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace System.Windows.Threading
{
	public enum DispatcherPriority
	{
		Invalid = -1,
		Inactive = 0,
		SystemIdle = 1,
		ApplicationIdle = 2,
		ContextIdle = 3,
		Background = 4,
		Input = 5,
		Loaded = 6,
		Render = 7,
		DataBind = 8,
		Normal = 9,
		Send = 10,
	}

	public enum DispatcherOperationStatus
	{
		Pending,
		Aborted,
		Completed,
		Executing,
	}

	/// <summary>
	/// WPF's dispatcher over the GLib main context GTK runs on: work posted here is a GLib idle source, and a nested
	/// message loop (<see cref="PushFrame"/>, what <c>ShowDialog</c> and VB6 <c>DoEvents</c> rely on) is a loop of
	/// main-context iterations.
	/// </summary>
	public sealed class Dispatcher
	{
		static readonly Dictionary<Thread, Dispatcher> s_dispatchers = new Dictionary<Thread, Dispatcher>();

		readonly List<DispatcherFrame> _frames = new List<DispatcherFrame>();
		System.Runtime.ExceptionServices.ExceptionDispatchInfo _pending;

		Dispatcher(Thread thread) => Thread = thread;

		/// <summary>
		/// An exception from a callback the message loop ran: rethrown by the loop (<see cref="PushFrame"/>) once the
		/// callback has returned, as WPF surfaces it from Run - rather than ending the process from inside GLib.
		/// </summary>
		internal void Defer(Exception exception)
		{
			if (exception is System.Reflection.TargetInvocationException invocation && invocation.InnerException != null)
				exception = invocation.InnerException;

			if (_pending == null && exception != null)
				_pending = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception);
		}

		/// <summary>Rethrows the exception a callback left, if any.</summary>
		internal void ThrowPending()
		{
			var pending = _pending;
			_pending = null;
			pending?.Throw();
		}

		/// <summary>The dispatcher of the calling thread, created on first use.</summary>
		public static Dispatcher CurrentDispatcher
		{
			get
			{
				var thread = Thread.CurrentThread;
				lock (s_dispatchers)
				{
					if (!s_dispatchers.TryGetValue(thread, out var dispatcher))
						s_dispatchers.Add(thread, dispatcher = new Dispatcher(thread));

					return dispatcher;
				}
			}
		}

		public static Dispatcher FromThread(Thread thread)
		{
			lock (s_dispatchers)
				return thread != null && s_dispatchers.TryGetValue(thread, out var dispatcher) ? dispatcher : null;
		}

		public Thread Thread { get; }

		public bool HasShutdownStarted { get; private set; }

		public bool HasShutdownFinished { get; private set; }

		public event EventHandler ShutdownStarted;

		public event EventHandler ShutdownFinished;

		public bool CheckAccess() => Thread == Thread.CurrentThread;

		[EditorBrowsable(EditorBrowsableState.Never)]
		public void VerifyAccess()
		{
			if (!CheckAccess())
				throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");
		}

		public DispatcherOperation BeginInvoke(Delegate method, params object[] args) =>
			BeginInvoke(method, DispatcherPriority.Normal, args);

		public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method) =>
			BeginInvoke(method, priority, Array.Empty<object>());

		public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object arg) =>
			BeginInvoke(method, priority, new[] { arg });

		public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object arg, params object[] args)
		{
			var all = new object[(args?.Length ?? 0) + 1];
			all[0] = arg;
			args?.CopyTo(all, 1);
			return BeginInvoke(method, priority, all);
		}

		public DispatcherOperation BeginInvoke(Delegate method, DispatcherPriority priority, params object[] args)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));

			var operation = new DispatcherOperation(this, priority, () => method.DynamicInvoke(args));
			Post(operation);
			return operation;
		}

		public DispatcherOperation InvokeAsync(Action callback) => InvokeAsync(callback, DispatcherPriority.Normal);

		public DispatcherOperation InvokeAsync(Action callback, DispatcherPriority priority)
		{
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			var operation = new DispatcherOperation(this, priority, () =>
			{
				callback();
				return null;
			});
			Post(operation);
			return operation;
		}

		public void Invoke(Action callback) => Invoke(callback, DispatcherPriority.Send);

		public void Invoke(Action callback, DispatcherPriority priority)
		{
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			Invoke(() =>
			{
				callback();
				return (object)null;
			}, priority);
		}

		public TResult Invoke<TResult>(Func<TResult> callback) => Invoke(callback, DispatcherPriority.Send);

		public TResult Invoke<TResult>(Func<TResult> callback, DispatcherPriority priority)
		{
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			if (CheckAccess())
				return callback();

			var operation = new DispatcherOperation(this, priority, () => callback());
			Post(operation);
			operation.Wait();
			return (TResult)operation.Result;
		}

		public object Invoke(Delegate method, params object[] args) => Invoke(method, DispatcherPriority.Send, args);

		public object Invoke(DispatcherPriority priority, Delegate method) => Invoke(method, priority, Array.Empty<object>());

		public object Invoke(DispatcherPriority priority, Delegate method, object arg) => Invoke(method, priority, new[] { arg });

		public object Invoke(Delegate method, DispatcherPriority priority, params object[] args)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));

			return Invoke(() => method.DynamicInvoke(args), priority);
		}

		/// <summary>Runs a nested message loop until <paramref name="frame"/> stops it.</summary>
		public static void PushFrame(DispatcherFrame frame)
		{
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));

			var dispatcher = CurrentDispatcher;
			dispatcher._frames.Add(frame);
			try
			{
				while (frame.Continue && !dispatcher.HasShutdownStarted)
				{
					GLib.MainContext.Iteration(true);
					dispatcher.ThrowPending();
				}
			}
			finally
			{
				dispatcher._frames.Remove(frame);
			}
		}

		/// <summary>Stops every nested message loop of the calling thread.</summary>
		public static void ExitAllFrames()
		{
			foreach (var frame in CurrentDispatcher._frames.ToArray())
				frame.Continue = false;
		}

		public static void Run() => PushFrame(new DispatcherFrame());

		public void InvokeShutdown()
		{
			if (HasShutdownStarted)
				return;

			HasShutdownStarted = true;
			ShutdownStarted?.Invoke(this, EventArgs.Empty);
			foreach (var frame in _frames.ToArray())
				frame.Continue = false;
			GLib.MainContext.Default.Wakeup();
			HasShutdownFinished = true;
			ShutdownFinished?.Invoke(this, EventArgs.Empty);
		}

		public void BeginInvokeShutdown(DispatcherPriority priority) => BeginInvoke(priority, new Action(InvokeShutdown));

		/// <summary>Queues <paramref name="operation"/> on the GLib main context at a priority matching WPF's order.</summary>
		void Post(DispatcherOperation operation)
		{
			GLib.Idle.Add(GLibPriority(operation.Priority), () =>
			{
				operation.Run();
				return false;
			});
			GLib.MainContext.Default.Wakeup();
		}

		/// <summary>
		/// WPF runs higher priorities first, and everything from <see cref="DispatcherPriority.Render"/> up before
		/// input; GLib runs lower numbers first, with its own redraw at 120 and idle work at 200.
		/// </summary>
		static int GLibPriority(DispatcherPriority priority)
		{
			switch (priority)
			{
				case DispatcherPriority.Send:
				case DispatcherPriority.Normal:
				case DispatcherPriority.DataBind:
					return (int)GLib.Priority.Default;
				case DispatcherPriority.Render:
				case DispatcherPriority.Loaded:
					return (int)GLib.Priority.HighIdle;
				case DispatcherPriority.Input:
				case DispatcherPriority.Background:
					return (int)GLib.Priority.DefaultIdle;
				default:
					return (int)GLib.Priority.Low;
			}
		}
	}

	/// <summary>A nested message loop, which runs for as long as <see cref="Continue"/> is true.</summary>
	public class DispatcherFrame
	{
		bool _continue = true;

		public DispatcherFrame()
		{
		}

		public DispatcherFrame(bool exitWhenRequested)
		{
		}

		public bool Continue
		{
			get => _continue;
			set
			{
				_continue = value;
				if (!value)
					GLib.MainContext.Default.Wakeup();
			}
		}
	}

	public sealed class DispatcherOperation
	{
		readonly Func<object> _work;
		readonly TaskCompletionSource<object> _completion = new TaskCompletionSource<object>();

		internal DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, Func<object> work)
		{
			Dispatcher = dispatcher;
			Priority = priority;
			_work = work;
		}

		public Dispatcher Dispatcher { get; }

		public DispatcherPriority Priority { get; set; }

		public DispatcherOperationStatus Status { get; private set; }

		public object Result { get; private set; }

		public Task Task => _completion.Task;

		public event EventHandler Completed;

		public event EventHandler Aborted;

		public bool Abort()
		{
			if (Status != DispatcherOperationStatus.Pending)
				return false;

			Status = DispatcherOperationStatus.Aborted;
			_completion.TrySetCanceled();
			Aborted?.Invoke(this, EventArgs.Empty);
			return true;
		}

		/// <summary>Waits for the operation: on the dispatcher's own thread by running its message loop.</summary>
		public DispatcherOperationStatus Wait()
		{
			if (Dispatcher.CheckAccess())
			{
				var frame = new DispatcherFrame();
				Completed += (s, e) => frame.Continue = false;
				Aborted += (s, e) => frame.Continue = false;
				if (Status == DispatcherOperationStatus.Pending || Status == DispatcherOperationStatus.Executing)
					Dispatcher.PushFrame(frame);
			}
			else
			{
				try
				{
					_completion.Task.Wait();
				}
				catch (AggregateException)
				{
				}
			}

			return Status;
		}

		public System.Runtime.CompilerServices.TaskAwaiter<object> GetAwaiter() => _completion.Task.GetAwaiter();

		internal void Run()
		{
			if (Status != DispatcherOperationStatus.Pending)
				return;

			Status = DispatcherOperationStatus.Executing;
			try
			{
				Result = _work();
				Status = DispatcherOperationStatus.Completed;
				_completion.TrySetResult(Result);
			}
			catch (Exception e)
			{
				Status = DispatcherOperationStatus.Completed;
				_completion.TrySetException(e);
				Completed?.Invoke(this, EventArgs.Empty);

				// WPF: Application.DispatcherUnhandledException may handle it; otherwise the loop rethrows it.
				var inner = e is System.Reflection.TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
				if (Application.Current?.RaiseDispatcherUnhandledException(inner) != true)
					Dispatcher.Defer(inner);
				return;
			}

			Completed?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>A timer on the dispatcher's thread: its <see cref="Tick"/> runs in the message loop, as WPF's does.</summary>
	public class DispatcherTimer
	{
		TimeSpan _interval;
		bool _enabled;
		uint _source;
		int _generation;

		public DispatcherTimer() : this(DispatcherPriority.Background)
		{
		}

		public DispatcherTimer(DispatcherPriority priority) : this(priority, Dispatcher.CurrentDispatcher)
		{
		}

		public DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher)
		{
			Priority = priority;
			Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		}

		public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback, Dispatcher dispatcher)
			: this(priority, dispatcher)
		{
			Interval = interval;
			Tick += callback ?? throw new ArgumentNullException(nameof(callback));
			Start();
		}

		public Dispatcher Dispatcher { get; }

		public DispatcherPriority Priority { get; }

		public object Tag { get; set; }

		public event EventHandler Tick;

		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value.TotalMilliseconds < 0 || value.TotalMilliseconds > int.MaxValue)
					throw new ArgumentOutOfRangeException(nameof(value));

				_interval = value;
				if (_enabled)
					Restart();
			}
		}

		public bool IsEnabled
		{
			get => _enabled;
			set
			{
				if (value)
					Start();
				else
					Stop();
			}
		}

		public void Start()
		{
			if (_enabled)
				return;

			_enabled = true;
			Restart();
		}

		public void Stop()
		{
			if (!_enabled)
				return;

			_enabled = false;
			Cancel();
		}

		void Restart()
		{
			Cancel();

			var generation = ++_generation;
			_source = GLib.Timeout.Add((uint)Math.Max(1, _interval.TotalMilliseconds), () =>
			{
				// A tick queued before a Stop or an Interval change belongs to a timer that no longer exists.
				if (!_enabled || generation != _generation)
					return false;

				Tick?.Invoke(this, EventArgs.Empty);

				// A handler that stopped or restarted the timer has already removed this source.
				return _enabled && generation == _generation;
			});
		}

		void Cancel()
		{
			_generation++;
			if (_source != 0)
			{
				GLib.Source.Remove(_source);
				_source = 0;
			}
		}
	}
}
