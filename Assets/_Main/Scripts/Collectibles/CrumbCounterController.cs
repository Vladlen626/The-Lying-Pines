using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using UnityEngine;

namespace _Main.Scripts.Collectibles
{
	public class CrumbCounterController : IBaseController, IActivatable
	{
		public int Crumbs { get; private set; }

		public void Activate()   => Signals.CrumbCollected += OnCrumb;
		public void Deactivate() => Signals.CrumbCollected -= OnCrumb;

		private void OnCrumb(int amount)
		{
			Crumbs += amount;
			Debug.Log($"[CRUMBS] +{amount} → {Crumbs}");
		}
	}
}