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
        private bool _canAfford;
        private bool _tooltipVisible;
        private HomeState _lastHomeState;
        private bool _lastAffordState;
        private string _buildPrompt;
        private string _lastTooltipText;

        private enum TooltipMode
        {
                None,
                Greeting,
                Build,
                Missing
        }

        private TooltipMode _tooltipMode = TooltipMode.None;

        public CockroachController(CockroachView view, HomeModel home, IInventoryService inventory, IInputService input)
        {
                _view = view;
                _home = home;
		_inventory = inventory;
		_input = input;
	}

        public void Activate()
        {
                _input.OnInteractPressed += OnInteractHandler;
                _view.PlayerEnter += OnEnter;
                _view.PlayerExit += OnExit;
                _buildPrompt = $"[E] — Give {_home.Requirements.ToShortText()} to build";
                _lastHomeState = _home.State;
                HideTooltip();
        }

	public void Deactivate()
	{
		_input.OnInteractPressed -= OnInteractHandler;
		_view.PlayerEnter -= OnEnter;
		_view.PlayerExit -= OnExit;
		HideTooltip();
	}

        public void OnUpdate(float dt)
        {
                if (!_inside || _home.State == HomeState.Built)
                {
                        if (_tooltipVisible)
                                HideTooltip();
                        return;
                }

                _canAfford = _home.State == HomeState.Broken &&
                             PurchaseUtils.CanAfford(_inventory, _home.Requirements.Costs);

                if (_canAfford != _lastAffordState || _home.State != _lastHomeState || _tooltipMode == TooltipMode.Greeting)
                {
                        ShowBuildState(_canAfford);
                        _lastAffordState = _canAfford;
                        _lastHomeState = _home.State;
                }
        }

        private void OnEnter()
        {
                _inside = true;
                ShowTooltip("mmm... crumbs...", TooltipMode.Greeting);
        }

        private void OnExit()
        {
                _inside = false;
                HideTooltip();
        }

        private void ShowTooltip(string text, TooltipMode mode = TooltipMode.None)
        {
                if (_view.Tooltip)
                {
                        _view.Tooltip.SetText(text);
                        _lastTooltipText = text;
                        _view.Tooltip.gameObject.SetActive(true);
                        _tooltipVisible = true;
                        _tooltipMode = mode;
                }
        }

        private void HideTooltip()
        {
                if (_view.Tooltip)
                {
                        _view.Tooltip.gameObject.SetActive(false);
                }
                _tooltipVisible = false;
                _tooltipMode = TooltipMode.None;
                _lastTooltipText = string.Empty;
        }

        private void OnInteractHandler()
        {
                if (_inside && _canAfford)
                {
                        if (_home.TryBuild(_inventory))
                        {
                                HideTooltip();
                        }
                }
        }

        private void ShowBuildState(bool canAfford)
        {
                var tooltip = _view.Tooltip;
                if (!tooltip)
                        return;

                if (!_tooltipVisible)
                {
                        tooltip.gameObject.SetActive(true);
                        _tooltipVisible = true;
                }

                if (canAfford)
                {
                        if (_tooltipMode != TooltipMode.Build || _lastTooltipText != _buildPrompt)
                        {
                                tooltip.SetText(_buildPrompt);
                                _tooltipMode = TooltipMode.Build;
                                _lastTooltipText = _buildPrompt;
                        }
                }
                else
                {
                        string missing = PurchaseUtils.MissingText(_inventory, _home.Requirements.Costs);
                        if (_tooltipMode != TooltipMode.Missing || _lastTooltipText != missing)
                        {
                                tooltip.SetText(missing);
                                _tooltipMode = TooltipMode.Missing;
                                _lastTooltipText = missing;
                        }
                }
        }
}