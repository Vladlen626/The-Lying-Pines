using System.Collections.Generic;
using UnityEngine.Playables;

namespace _Main.Scripts.Collectibles
{
	public interface ITimelineService
	{
		public void PlayGameStartTimeline();
		public void PlayGameEndTimeline();
		public void PlayHomeBuildTimeline(string timeLineName);

		void SetTimelineDirectors(List<NamedTimeline> directors, PlayableDirector gameStartDirector,
			PlayableDirector gameEndDirector);
	}
}