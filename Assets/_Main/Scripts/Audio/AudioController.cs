using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure.Lifecycle;
using PlatformCore.Services.Audio;

namespace _Main.Scripts.Audio
{
	public class AudioController : IBaseController, IActivatable
	{
		private const string ForrestSound = "event:/ForrestTheme";
		private readonly IAudioService _audioService;
		
		public AudioController(IAudioService audioService)
		{
			_audioService = audioService;
		}

		public void Activate()
		{
			_audioService.PlayMusicAsync(ForrestSound, 1);
		}

		public void Deactivate()
		{
			_audioService.StopMusicAsync().Forget();
		}
	}
}