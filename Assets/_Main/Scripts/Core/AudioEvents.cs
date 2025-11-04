namespace _Main.Scripts.Core
{
	public static class AudioEvents
	{
		public const string Jump = "event:/Jump";
		public const string SlamDive = "event:/Slam"; // старт пике
		public const string SlamPunch = "event:/Punch"; // УДАР о землю (если нет — временно "event:/BoxCrash")
		public const string Step = "event:/Step";
		public const string GetCrumb = "event:/GetCrumb";
		public const string Congratulations = "event:/Congratulations";
		public const string HomeBuild = "event:/HomeBuild";
	}
}