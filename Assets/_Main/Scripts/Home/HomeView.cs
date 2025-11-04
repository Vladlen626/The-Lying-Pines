using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Main.Scripts.Home
{
	public class HomeView : MonoBehaviour
	{
		[SerializeField] private GameObject _broken;
		[SerializeField] private GameObject _built;

		
		public void SetState(HomeState state)
		{
			if (_broken)
			{
				_broken.SetActive(state == HomeState.Broken);
			}

			if (_built)
			{
				_built.SetActive(state == HomeState.Built);
			}
		}
	}
}