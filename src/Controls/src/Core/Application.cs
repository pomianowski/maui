#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;

namespace Microsoft.Maui.Controls
{
	/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="Type[@FullName='Microsoft.Maui.Controls.Application']/Docs" />
	public partial class Application : Element, IResourcesProvider, IApplicationController, IElementConfiguration<Application>, IVisualTreeElement
	{
		readonly WeakEventManager _weakEventManager = new WeakEventManager();
		Task<IDictionary<string, object>>? _propertiesTask;
		readonly Lazy<PlatformConfigurationRegistry<Application>> _platformConfigurationRegistry;
		readonly Lazy<IResourceDictionary> _systemResources;

		IAppIndexingProvider? _appIndexProvider;
		ReadOnlyCollection<Element>? _logicalChildren;
		bool _isStarted;

		static readonly SemaphoreSlim SaveSemaphore = new SemaphoreSlim(1, 1);

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='.ctor']/Docs" />
		public Application() : this(true)
		{
		}

		internal Application(bool setCurrentApplication)
		{
			if (setCurrentApplication)
				SetCurrentApplication(this);

			_systemResources = new Lazy<IResourceDictionary>(() =>
			{
				var systemResources = DependencyService.Get<ISystemResourcesProvider>().GetSystemResources();
				systemResources.ValuesChanged += OnParentResourcesChanged;
				return systemResources;
			});
			_platformConfigurationRegistry = new Lazy<PlatformConfigurationRegistry<Application>>(() => new PlatformConfigurationRegistry<Application>(this));
		}

		internal void PlatformServicesSet()
		{
			_lastAppTheme = RequestedTheme;
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='Quit']/Docs" />
		public void Quit()
		{
			Handler?.Invoke(ApplicationHandler.TerminateCommandKey);
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='AppLinks']/Docs" />
		public IAppLinks AppLinks
		{
			get
			{
				if (_appIndexProvider == null)
					throw new ArgumentException("No IAppIndexingProvider was provided");
				if (_appIndexProvider.AppLinks == null)
					throw new ArgumentException("No AppLinks implementation was found, if in Android make sure you installed the Microsoft.Maui.Controls.AppLinks");
				return _appIndexProvider.AppLinks;
			}
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='SetCurrentApplication']/Docs" />
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void SetCurrentApplication(Application value) => Current = value;

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='Current']/Docs" />
		public static Application? Current { get; set; }

		Page? _pendingMainPage;

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='MainPage']/Docs" />
		public Page? MainPage
		{
			get
			{
				if (Windows.Count == 0)
					return _pendingMainPage;

				return Windows[0].Page;
			}
			set
			{
				if (MainPage == value)
					return;

				OnPropertyChanging();

				if (Windows.Count == 0)
				{
					_pendingMainPage = value;
				}
				else
				{
					Windows[0].Page = value;
				}

				OnPropertyChanged();
			}
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='Properties']/Docs" />
		[Obsolete("Properties API is obsolete, use Essentials.Preferences instead.")]
		public IDictionary<string, object> Properties
		{
			get
			{
				if (_propertiesTask == null)
				{
					_propertiesTask = GetPropertiesAsync();
				}

				return _propertiesTask.Result;
			}
		}

		internal override IReadOnlyList<Element> LogicalChildrenInternal =>
			_logicalChildren ??= new ReadOnlyCollection<Element>(InternalChildren);

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='NavigationProxy']/Docs" />
		[EditorBrowsable(EditorBrowsableState.Never)]
		public NavigationProxy? NavigationProxy { get; private set; }

		internal IResourceDictionary SystemResources => _systemResources.Value;

		ObservableCollection<Element> InternalChildren { get; } = new ObservableCollection<Element>();

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='SetAppIndexingProvider']/Docs" />
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void SetAppIndexingProvider(IAppIndexingProvider provider)
		{
			_appIndexProvider = provider;
		}

		ResourceDictionary? _resources;
		bool IResourcesProvider.IsResourcesCreated => _resources != null;

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='Resources']/Docs" />
		public ResourceDictionary Resources
		{
			get
			{
				if (_resources != null)
					return _resources;

				_resources = new ResourceDictionary();
				((IResourceDictionary)_resources).ValuesChanged += OnResourcesChanged;
				return _resources;
			}
			set
			{
				if (_resources == value)
					return;
				OnPropertyChanging();
				if (_resources != null)
					((IResourceDictionary)_resources).ValuesChanged -= OnResourcesChanged;
				_resources = value;
				OnResourcesChanged(value);
				if (_resources != null)
					((IResourceDictionary)_resources).ValuesChanged += OnResourcesChanged;
				OnPropertyChanged();
			}
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='UserAppTheme']/Docs" />
		public OSAppTheme UserAppTheme
		{
			get => _userAppTheme;
			set
			{
				_userAppTheme = value;
				TriggerThemeChangedActual(new AppThemeChangedEventArgs(value));
			}
		}
		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='RequestedTheme']/Docs" />
		public OSAppTheme RequestedTheme => UserAppTheme == OSAppTheme.Unspecified ? Device.PlatformServices.RequestedTheme : UserAppTheme;

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='AccentColor']/Docs" />
		public static Color? AccentColor { get; set; }

		public event EventHandler<AppThemeChangedEventArgs> RequestedThemeChanged
		{
			add => _weakEventManager.AddEventHandler(value);
			remove => _weakEventManager.RemoveEventHandler(value);
		}

		bool _themeChangedFiring;
		OSAppTheme _lastAppTheme;
		OSAppTheme _userAppTheme = OSAppTheme.Unspecified;


		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='TriggerThemeChanged']/Docs" />
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void TriggerThemeChanged(AppThemeChangedEventArgs args)
		{
			if (UserAppTheme != OSAppTheme.Unspecified)
				return;
			TriggerThemeChangedActual(args);
		}

		void TriggerThemeChangedActual(AppThemeChangedEventArgs args)
		{
			// On iOS the event is triggered more than once.
			// To minimize that for us, we only do it when the theme actually changes and it's not currently firing
			if (_themeChangedFiring || RequestedTheme == _lastAppTheme)
				return;

			try
			{
				_themeChangedFiring = true;
				_lastAppTheme = RequestedTheme;

				_weakEventManager.HandleEvent(this, args, nameof(RequestedThemeChanged));
			}
			finally
			{
				_themeChangedFiring = false;
			}
		}

		public event EventHandler<ModalPoppedEventArgs>? ModalPopped;

		public event EventHandler<ModalPoppingEventArgs>? ModalPopping;

		public event EventHandler<ModalPushedEventArgs>? ModalPushed;

		public event EventHandler<ModalPushingEventArgs>? ModalPushing;

		internal void NotifyOfWindowModalEvent(EventArgs eventArgs)
		{
			switch (eventArgs)
			{
				case ModalPoppedEventArgs poppedEvents:
					ModalPopped?.Invoke(this, poppedEvents);
					break;
				case ModalPoppingEventArgs poppingEvents:
					ModalPopping?.Invoke(this, poppingEvents);
					break;
				case ModalPushedEventArgs pushedEvents:
					ModalPushed?.Invoke(this, pushedEvents);
					break;
				case ModalPushingEventArgs pushingEvents:
					ModalPushing?.Invoke(this, pushingEvents);
					break;
				default:
					break;
			}
		}

		public event EventHandler<Page>? PageAppearing;

		public event EventHandler<Page>? PageDisappearing;

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='SavePropertiesAsync']/Docs" />
		[Obsolete("Properties API is obsolete, use Essentials.Preferences instead.")]
		public Task SavePropertiesAsync() =>
			Dispatcher.DispatchIfRequiredAsync(async () =>
			{
				try
				{
					await SetPropertiesAsync();
				}
				catch (Exception exc)
				{
					this.FindMauiContext()?.CreateLogger<Application>()?.LogWarning(exc, "Exception while saving Application Properties");
				}
			});

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='On']/Docs" />
		public IPlatformElementConfiguration<T, Application> On<T>() where T : IConfigPlatform
		{
			return _platformConfigurationRegistry.Value.On<T>();
		}

		protected virtual void OnAppLinkRequestReceived(Uri uri)
		{
		}

		protected override void OnParentSet()
		{
			throw new InvalidOperationException("Setting a Parent on Application is invalid.");
		}

		protected virtual void OnResume()
		{
		}

		protected virtual void OnSleep()
		{
		}

		protected virtual void OnStart()
		{
		}

		internal static void ClearCurrent() => Current = null;

		internal static bool IsApplicationOrNull(object? element) =>
			element == null || element is IApplication;

		internal static bool IsApplicationOrWindowOrNull(object? element) =>
			element == null || element is IApplication || element is IWindow;

		internal override void OnParentResourcesChanged(IEnumerable<KeyValuePair<string, object>> values)
		{
			if (!((IResourcesProvider)this).IsResourcesCreated || Resources.Count == 0)
			{
				base.OnParentResourcesChanged(values);
				return;
			}

			var innerKeys = new HashSet<string>();
			var changedResources = new List<KeyValuePair<string, object>>();
			foreach (KeyValuePair<string, object> c in Resources)
				innerKeys.Add(c.Key);
			foreach (KeyValuePair<string, object> value in values)
			{
				if (innerKeys.Add(value.Key))
					changedResources.Add(value);
			}
			OnResourcesChanged(changedResources);
		}

		/// <include file="../../docs/Microsoft.Maui.Controls/Application.xml" path="//Member[@MemberName='SendOnAppLinkRequestReceived']/Docs" />
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void SendOnAppLinkRequestReceived(Uri uri)
		{
			OnAppLinkRequestReceived(uri);
		}

		internal void SendResume()
		{
			Current = this;
			OnResume();
		}

		internal void SendSleep()
		{
			OnSleep();
#pragma warning disable CS0618 // Type or member is obsolete
			SavePropertiesAsync().FireAndForget();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		internal Task SendSleepAsync()
		{
			OnSleep();
#pragma warning disable CS0618 // Type or member is obsolete
			return SavePropertiesAsync();
#pragma warning restore CS0618 // Type or member is obsolete
		}

		internal void SendStart()
		{
			if (_isStarted)
				return;

			_isStarted = true;
			OnStart();
		}

		async Task<IDictionary<string, object>> GetPropertiesAsync()
		{
			var deserializer = DependencyService.Get<IDeserializer>();
			if (deserializer == null)
			{
				Current?.FindMauiContext()?.CreateLogger<Application>()?.LogWarning("No IDeserializer was found registered");
				return new Dictionary<string, object>(4);
			}

			IDictionary<string, object> properties = await deserializer.DeserializePropertiesAsync().ConfigureAwait(false);
			if (properties == null)
				properties = new Dictionary<string, object>(4);

			return properties;
		}

		internal void OnPageAppearing(Page page)
			=> PageAppearing?.Invoke(this, page);

		internal void OnPageDisappearing(Page page)
			=> PageDisappearing?.Invoke(this, page);


		async Task SetPropertiesAsync()
		{
			await SaveSemaphore.WaitAsync();
			try
			{
#pragma warning disable CS0618 // Type or member is obsolete
				await DependencyService.Get<IDeserializer>().SerializePropertiesAsync(Properties);
#pragma warning restore CS0618 // Type or member is obsolete
			}
			finally
			{
				SaveSemaphore.Release();
			}

		}

		protected internal virtual void CleanUp()
		{
			// Unhook everything that's referencing the main page so it can be collected
			// This only comes up if we're disposing of an embedded Forms app, and will
			// eventually go away when we fully support multiple windows
			// TODO MAUI
			//if (_mainPage != null)
			//{
			//	InternalChildren.Remove(_mainPage);
			//	_mainPage.Parent = null;
			//	_mainPage = null;
			//}

			NavigationProxy = null;
		}

		IReadOnlyList<Maui.IVisualTreeElement> IVisualTreeElement.GetVisualChildren() => this.Windows;
	}
}