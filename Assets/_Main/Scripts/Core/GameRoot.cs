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
		protected override void RegisterServices(PlatformCore.Core.GameContext context)
		{
			Debug.Log("[GameRoot] Register services...");

			var logger = new LoggerService();
			var inputService = new InputBaseService();
			var resourcesService = new ResourceService(logger);
			var objectFactory = new ObjectFactory(resourcesService, logger);
			var cameraService = new CameraService(objectFactory);
			var audioService = new AudioBaseService(logger);
			var uiService = new UIBaseService(logger, resourcesService, context.StaticCanvas, context.DynamicCanvas);
			var cursorService = new CursorService(uiService);
			var splashScreenService = new SplashScreenService(uiService);
			var sceneService = new SceneService(logger);
			var inventoryService = new InventoryService();


			Services.Register<ILoggerService>(logger);
			Services.Register<ICursorService>(cursorService);
			Services.Register<IInputService>(inputService);
			Services.Register<IAudioService>(audioService);
			Services.Register<IResourceService>(resourcesService);
			Services.Register<IObjectFactory>(objectFactory);
			Services.Register<ICameraService>(cameraService);
			Services.Register<ICameraShakeService>(cameraService);
			Services.Register<IUIService>(uiService);
			Services.Register<ISplashScreenService>(splashScreenService);
			Services.Register<ISceneService>(sceneService);
			Services.Register<IInventoryService>(inventoryService);

			Debug.Log("[GameRoot] Services finally registered.!");
		}

		protected override async UniTask InitializeServicesAsync()
		{
			var services = new IService[]
			{
				Services.Get<ILoggerService>(),
				Services.Get<ICameraService>(),
				Services.Get<IAudioService>(),
				Services.Get<IInputService>(),
				Services.Get<IResourceService>(),
				Services.Get<IObjectFactory>(),
				Services.Get<IUIService>(),
				Services.Get<ICursorService>(),
				Services.Get<ISplashScreenService>(),
				Services.Get<ISceneService>(),
				Services.Get<IInventoryService>(),
			};

			foreach (var service in services)
			{
				await service.InitializeAsync(ApplicationCancellationToken);
			}
		}

		protected override async UniTask LaunchGameAsync(GameContext context)
		{
			var gameModel = new GameStateModel();
			var splashScreenService = Services.Get<ISplashScreenService>();
			var inputService = Services.Get<IInputService>();
			var sceneService = Services.Get<ISceneService>();
			var inventoryService = Services.Get<IInventoryService>();

			inputService.DisableAllInputs();
			splashScreenService.FadeInAsync(0).Forget();

			await sceneService.LoadSceneAsync(SceneNames.Hub, ApplicationCancellationToken);
			var mainControllers = new List<IBaseController>()
			{
				new PauseController(gameModel, Services),
				new GameStateController(gameModel, inputService),
				new AudioController(Services.Get<IAudioService>()),
			};

			var playerSpawnPosition = Vector3.zero;
			if (sceneService.TryGetSceneContext(SceneNames.Hub, out var context1))
			{
				playerSpawnPosition = context1.PlayerSpawnPos;
			}

			var playerModel = new PlayerModel();
			var playerFactory = new PlayerFactory(Services);
			var playerView = await playerFactory.CreatePlayerView(playerSpawnPosition);
			mainControllers.AddRange(playerFactory.GetPlayerBaseControllers(playerModel, playerView));
			mainControllers.Add(new CrumbCounterController());

			foreach (var controller in mainControllers)
			{
				await Lifecycle.RegisterAsync(controller);
			}

			await CollectibleModule.BindSceneCollectibles(Lifecycle, inventoryService, playerView);
			await UniTask.Delay(500, DelayType.UnscaledDeltaTime, cancellationToken: ApplicationCancellationToken);
			await splashScreenService.FadeOutAsync();

			inputService.EnableAllInputs();
		}
	}
}