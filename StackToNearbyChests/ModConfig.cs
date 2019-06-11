using System.Collections.Generic;

namespace StackToNearbyChests
{
	class ModConfig
	{
		public int Radius { get; set; } = 5;
		public List<string> AllowedCategories = new List<string>();
		public ChestSearchMode SearchMode = ChestSearchMode.CURRENT_AREA;
	}
}
