using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace _Main.Scripts.Collectibles
{
	public class TimelineService : ITimelineService, IService
	{
		private readonly Dictionary<string, PlayableDirector> _lookup = new();
		private PlayableDirector _gameStartDirector = new();
		private PlayableDirector _gameEndDirector = new();

		
		public void SetTimelineDirectors(List<NamedTimeline> directors, PlayableDirector gameStartDirector, PlayableDirector gameEndDirector)
		{
			foreach (var t in directors)
			{
				if (t.Director && !_lookup.ContainsKey(t.Name))
					_lookup.Add(t.Name, t.Director);
			}

			_gameStartDirector = gameStartDirector;
			_gameEndDirector = gameEndDirector;
		}

		public void PlayGameStartTimeline() => PlayDirector(_gameStartDirector);

		public void PlayGameEndTimeline() => PlayDirector(_gameEndDirector);

		public void PlayHomeBuildTimeline(string timelineName)
		{
			if (_lookup.TryGetValue(timelineName, out var dir))
				PlayDirector(dir);
			else
				Debug.LogWarning($"[TimelineService] No timeline named '{timelineName}'");
		}

		private void PlayDirector(PlayableDirector director)
		{
			if (!director)
			{
				Debug.LogWarning("[TimelineService] Missing PlayableDirector reference.");
				return;
			}

			director.time = 0;
			director.Stop();
			director.Evaluate();
			director.Play();
		}

		public void Dispose()
		{
		}
	}
}