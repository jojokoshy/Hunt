#region Usings

using System;
using Hunt.Common;
using Lottie.Forms;
using Microsoft.Azure.Mobile;
using Microsoft.Azure.Mobile.Analytics;
using Microsoft.Azure.Mobile.Crashes;
using Microsoft.Azure.Mobile.Push;
using Newtonsoft.Json;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

#endregion

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Hunt.Mobile.Common
{
	public partial class App : Application
	{
		#region Properties

		public event EventHandler AppResumed;
		public event EventHandler AppBackgrounded;
		public event EventHandler<NotificationEventArgs> AppNotificationReceived;

		public Size ScreenSize { get; set; } = new Size(380, 540);

		static App _instance;
		public static App Instance => _instance;

		public Player Player { get; set; }
		public Game CurrentGame { get; set; }

		AzureFunctionService _dataService;
		public AzureFunctionService DataService => _dataService ?? (_dataService = new AzureFunctionService());

		StorageService _storageService;
		public StorageService StorageService => _storageService ?? (_storageService = new StorageService());

		public bool IsDesignMode { get; set; }
		public string Assemblies { get; set; }

		#endregion

		public App()
		{
			_instance = this;
			IsDesignMode = Type.GetType("MonoTouch.Design.Parser,MonoTouch.Design") != null;
			if(IsDesignMode)
			{
				//Instance.CurrentGame = Mocker.GetGame(5, 4, true, true, true, true, true);
				//Instance.CurrentGame.StartDate = null;

				////Has game started
				////Instance.CurrentGame.StartDate = DateTime.Now;

				////Has game ended
				//Instance.CurrentGame.EndDate = DateTime.Now;
				//Instance.CurrentGame.StartDate = DateTime.Now;
				//Instance.CurrentGame.WinnningTeamId = Instance.CurrentGame.Teams[1].Id;

				////Are you a player
				//Player = Instance.CurrentGame.Teams[1].Players[0];

				////Are you the coordinator
				//Player = Instance.CurrentGame.Coordinator.Clone(); //Jon
				//Instance.CurrentGame.Coordinator = Player;

			}

			Push.Instance.OnNotificationReceived += OnNotificationReceived;
			InitializeComponent();
		}

		#region Lifecycle

		protected override void OnStart()
		{
			MobileCenter.Start($"android={Keys.MobileCenter.AndroidToken};ios={Keys.MobileCenter.iOSToken}",
				typeof(Analytics), typeof(Crashes));

			CrossConnectivity.Current.ConnectivityChanged += OnConnectivityChanged;
			if(false) { var l = new AnimationView(); }//Warm up the library

			base.OnStart();

			LoadRegisteredPlayer();
			SetMainPage();

			EvaluateConnectivity();
			WakeUpServer();
		}

		//Forms.ContentPage.OnResume does not seem to fire properly across platforms so we gone roll our own
		public void OnHuntResume()
		{
			AppResumed?.Invoke(this, new EventArgs());
			EvaluateConnectivity();
		}

		protected override void OnSleep()
		{
			base.OnSleep();
			AppBackgrounded?.Invoke(this, new EventArgs());
		}

		void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
		{
			EvaluateConnectivity();
		}

		void OnNotificationReceived(object sender, NotificationEventArgs e)
		{
			AppNotificationReceived?.Invoke(this, e);
		}

		public void EvaluateConnectivity()
		{
			if(!CrossConnectivity.Current.IsConnected)
			{
				Hud.Instance.ShowToast(Keys.Constants.NoConnectionMessage);
			}
		}

		void WakeUpServer()
		{
			#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			DataService.WakeUpServer();
			#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		#endregion

		#region Player Related

		public void SetMainPage()
		{
			var regPage = new RegistrationPage();
			MainPage = regPage.ToNav();

			if(Player != null)
			{
				regPage.Navigation.PushAsync(new LoadingDataPage(), false);
			}
		}

		void LoadRegisteredPlayer()
		{
			if(!string.IsNullOrWhiteSpace(Settings.Player))
			{
				var player = JsonConvert.DeserializeObject<Player>(Settings.Player);

				if(player == null)
					return;
	
				Player = player;
			}
		}

		public void SetPlayer(Player player)
		{
			if(player == null)
				Settings.Player = null;
			else
				Settings.Player = JsonConvert.SerializeObject(player);

			Player = player;
		}

		#endregion
	}
}