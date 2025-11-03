using _Main.Scripts.Collectibles;
using _Main.Scripts.Core.Services;
using _Main.Scripts.Home;
using _Main.Scripts.Inventory;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;

public sealed class CockroachController : IBaseController, IActivatable, IUpdatable
{
	private readonly CockroachView _view;
	private readonly HomeModel _home;
	private readonly IInventoryService _inventory;
	private readonly IInputService _input;

	private bool _inside;

	public CockroachController(CockroachView view, HomeModel home, IInventoryService inventory, IInputService input)
	{
		_view = view;
		_home = home;
		_inventory = inventory;
		_input = input;
	}

	public void Activate()
	{
		_view.PlayerEnter += OnEnter;
		_view.PlayerExit += OnExit;
	}

	public void Deactivate()
	{
		_view.PlayerEnter -= OnEnter;
		_view.PlayerExit -= OnExit;
		HideTooltip();
	}

	public void OnUpdate(float dt)
	{
		if (!_inside) return;

		bool canAfford = _home.State == HomeState.Broken &&
		                 PurchaseUtils.CanAfford(_inventory, _home.Requirements.Costs);

		var tooltip = _view.Tooltip;
		if (tooltip)
		{
			string text = canAfford
				? $"[E] — Give {_home.Requirements.ToShortText()} to build"
				: PurchaseUtils.MissingText(_inventory, _home.Requirements.Costs);
			tooltip.SetText(text);
		}

		if (canAfford && _input.IsInteract)
		{
			if (_home.TryBuild(_inventory))
			{
				HideTooltip();
			}
		}
	}

	private void OnEnter()
	{
		_inside = true;
		ShowTooltip("Come closer...");
	}

	private void OnExit()
	{
		_inside = false;
		HideTooltip();
	}

	private void ShowTooltip(string text)
	{
		if (_view.Tooltip)
		{
			_view.Tooltip.SetText(text);
			_view.Tooltip.gameObject.SetActive(true);
		}
	}

	private void HideTooltip()
	{
		if (_view.Tooltip)
			_view.Tooltip.gameObject.SetActive(false);
	}
}