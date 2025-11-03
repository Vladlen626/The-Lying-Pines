using System.Collections.Generic;
using System.Threading;
using _Main.Scripts.Audio;
using _Main.Scripts.Collectibles;
using _Main.Scripts.Core.Services;
using _Main.Scripts.Global.Controllers;
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
using UnityEngine.SceneManagement;

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

			input.DisableAllInputs();
			splash.FadeInAsync(0).Forget();

			// === 1. Загружаем сцену и готовим сервисы ПАРАЛЛЕЛЬНО ===
			var sceneTask = scene.LoadSceneAsync(SceneNames.Hub, ApplicationCancellationToken);
			var preloadUiTask = Services.Get<IUIService>().PreloadAsync<UIPlayerCrosshair>().AsTask();
			await UniTask.WhenAll(sceneTask.AsAsyncUnitUniTask().AsUniTask(), preloadUiTask.AsUniTask());

			// === 2. После загрузки сцены: контекст ===
			Vector3 spawn = Vector3.zero;
			if (scene.TryGetSceneContext(SceneNames.Hub, out var ctx))
			{
				spawn = ctx.PlayerSpawnPos;
				await HomeModule.BindFromContext(Lifecycle, Services, ctx, defaultCrumbsCost: 50);
			}

			var playerFactory = new PlayerFactory(Services);
			var playerModel = new PlayerModel();
			var playerViewTask = playerFactory.CreatePlayerView(spawn);

			var mainControllers = new List<IBaseController>
			{
				new PauseController(gameModel, Services),
				new GameStateController(gameModel, input),
				new AudioController(audio)
			};

			var playerView = await playerViewTask;
			mainControllers.AddRange(playerFactory.GetPlayerBaseControllers(playerModel, playerView));
			mainControllers.Add(new CrumbCounterController());

			await UniTask.WhenAll(mainControllers.Select(c => Lifecycle.RegisterAsync(c)));

			var collectiblesTask = CollectibleModule.BindSceneCollectibles(Lifecycle, inventory, playerView);
			var fadeOutTask = splash.FadeOutAsync();

			await UniTask.WhenAll(collectiblesTask, fadeOutTask);

			input.EnableAllInputs();
		}
	}
}