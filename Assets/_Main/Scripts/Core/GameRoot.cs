using System.Collections.Generic;
using _Main.Scripts.Audio;
using _Main.Scripts.Collectibles;
using _Main.Scripts.Core.Services;
using _Main.Scripts.Global.Controllers;
using _Main.Scripts.Home;
using _Main.Scripts.Inventory;
using _Main.Scripts.Player;
using _Main.Scripts.UI;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Factory;
using PlatformCore.Services.UI;
using PlatformCore.Services.UI.SplashScreen;
using UnityEngine;

namespace _Main.Scripts.Core
{
	public class GameRoot : BaseGameRoot
	{
		protected override void RegisterServices(GameContext context)
		{
			Debug.Log("[GameRoot] Register services...");

			var logger = new LoggerService();
			var inputService = new InputBaseService();
			var resourcesService = new ResourceService(logger);
			var objectFactory = new ObjectFactory(resourcesService, logger);
			var cameraService = new CameraAsyncService(objectFactory);
			var audioService = new AudioBaseService(logger);
			var uiService = new UIBaseService(logger, resourcesService, context.StaticCanvas, context.DynamicCanvas);
			var cursorService = new CursorService(uiService);
			var splashScreenService = new SplashScreenService(uiService);
			var sceneService = new SceneService(logger);
			var inventoryService = new InventoryService();
			var timelineService = new TimelineService();

			Services.Register<ILoggerService, LoggerService>(logger);
			Services.Register<IInputService, InputBaseService>(inputService);
			Services.Register<IResourceService, ResourceService>(resourcesService);
			Services.Register<IObjectFactory, ObjectFactory>(objectFactory);
			Services.Register<ICameraService, CameraAsyncService>(cameraService);
			Services.Register<ICameraShakeService, CameraAsyncService>(cameraService);
			Services.Register<IAudioService, AudioBaseService>(audioService);
			Services.Register<IUIService, UIBaseService>(uiService);
			Services.Register<ICursorService, CursorService>(cursorService);
			Services.Register<ISplashScreenService, SplashScreenService>(splashScreenService);
			Services.Register<ISceneService, SceneService>(sceneService);
			Services.Register<IInventoryService, InventoryService>(inventoryService);
			Services.Register<ITimelineService, TimelineService>(timelineService);

			Debug.Log("[GameRoot] Services finally registered.!");
		}

		protected override async UniTask LaunchGameAsync(GameContext context)
		{
			var gameModel = new GameStateModel();
			var splash = Services.Get<ISplashScreenService>();
			var input = Services.Get<IInputService>();
			var scene = Services.Get<ISceneService>();
			var inventory = Services.Get<IInventoryService>();
			var audio = Services.Get<IAudioService>();
			var timelineService = Services.Get<ITimelineService>();
			var ui = Services.Get<IUIService>();
			var cursor = Services.Get<ICursorService>();
			
			input.DisableAllInputs();
			cursor.UnlockCursor(); 
			gameModel.SetInMenu(true);
			var settingsController = new SettingsController(Services);
			var audioController = new AudioController(audio, gameModel);
			await Lifecycle.RegisterAsync(settingsController);
			await Lifecycle.RegisterAsync(audioController);
			
			await ui.PreloadAsync<UICreditsSplash>();
			await ui.ShowAsync<UICreditsSplash>(0.3f);
			await UniTask.Delay(3200);
			await ui.HideAsync<UICreditsSplash>(0.25f);

			var mainMenuController = new MainMenuController(Services, settingsController);
			await Lifecycle.RegisterAsync(mainMenuController);
			await mainMenuController.WaitForStartAsync();
			gameModel.SetInMenu(false);
			await splash.FadeInAsync(0.25f);
			var sceneTask = scene.LoadSceneAsync(SceneNames.Hub, ApplicationCancellationToken);
			var preloadUiTask = ui.PreloadAsync<UIPlayerCrosshair>().AsTask();
			await UniTask.WhenAll(sceneTask.AsAsyncUnitUniTask().AsUniTask(), preloadUiTask.AsUniTask());

			Vector3 spawn = Vector3.zero;
			var sceneControllers = new List<IBaseController>();
			var models = new List<HomeModel>();
			if (scene.TryGetSceneContext(SceneNames.Hub, out var ctx))
			{
				// SETUP SCENE CONTEXT
				spawn = ctx.PlayerSpawnPos;

				foreach (var slot in ctx.Homes)
				{
					if (!slot.Home) continue;

					var cost = slot.CrumbsCost > 0 ? slot.CrumbsCost : 50;
					var req = new PurchaseRequirements(new Cost(CollectibleKind.Crumb, cost));

					var model = new HomeModel(req);
					models.Add(model);
					var homeCtrl = new HomeController(model, slot.Home);
					sceneControllers.Add(homeCtrl);
					if (slot.Builder)
					{
						sceneControllers.Add(new CockroachController(slot.Builder, model, inventory, input));
					}
				}

				timelineService.SetTimelineDirectors(ctx.HomeTimeLines, ctx.GameStartDirector, ctx.GameEndDirector);
			}

			var playerFactory = new PlayerFactory(Services);
			var playerModel = new PlayerModel();
			var playerViewTask = playerFactory.CreatePlayerView(spawn);
			var mainControllers = new List<IBaseController>
			{
				settingsController,
				new PauseController(gameModel, Services, settingsController),
				new GameStateController(gameModel, input),
				new PlayerHudController(inventory, ui)
			};

			var playerView = await playerViewTask;
			var gameProgressModel = new GameProgressModel(ctx.Homes.Length);

			sceneControllers.Add(new GameProgressController(gameProgressModel, models.ToArray(), playerView));
			mainControllers.AddRange(sceneControllers);
			mainControllers.AddRange(playerFactory.GetPlayerBaseControllers(playerModel, playerView));

			await UniTask.WhenAll(mainControllers.Select(c => Lifecycle.RegisterAsync(c)));

			var collectiblesTask = CollectibleModule.BindSceneCollectibles(Lifecycle, inventory, playerView);
			var fadeOutTask = splash.FadeOutAsync();

			await UniTask.WhenAll(collectiblesTask, fadeOutTask);

			input.EnableAllInputs();
			cursor.LockCursor(); 
		}
	}
}