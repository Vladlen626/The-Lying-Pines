using _Main.Scripts.Collectibles;
using _Main.Scripts.Inventory;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.UI;

public class PlayerHudController : IBaseController, IActivatable, IPreloadable
{
	private readonly IUIService _uiService;
	private readonly IInventoryService _inventory;
	private UIPlayerHud _uiPlayerHud;

	public PlayerHudController(IInventoryService inventory, IUIService uiService)
	{
		_inventory = inventory;
		_uiService = uiService;
	}

	public async UniTask PreloadAsync()
	{
		await _uiService.PreloadAsync<UIPlayerHud>();
	}

	public void Activate()
	{
		_uiService.ShowAsync<UIPlayerHud>();
		_uiPlayerHud = _uiService.Get<UIPlayerHud>();
		_inventory.Changed += OnInventoryChanged;
		_uiPlayerHud.SetCrumbsCount(_inventory.Get(CollectibleKind.Crumb));
	}

	public void Deactivate()
	{
		_inventory.Changed -= OnInventoryChanged;
	}

	private void OnInventoryChanged(CollectibleKind kind, int total, int delta)
	{
		if (kind == CollectibleKind.Crumb)
		{
			_uiPlayerHud.SetCrumbsCount(total);
		}
	}
}