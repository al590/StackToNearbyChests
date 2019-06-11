using System;
using Harmony;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace StackToNearbyChests
{
	/// <summary>The mod entry class loaded by SMAPI.</summary>
	class ModEntry : Mod
	{
		internal static ModConfig Config { get; private set; }
		internal new static IModHelper Helper { get; private set; }

		/// <summary>The mod entry point, called after the mod is first loaded.</summary>
		/// <param name="helper">Provides simplified APIs for writing mods.</param>
		public override void Entry(IModHelper helper)
		{
			Config = helper.ReadConfig<ModConfig>();
			Helper = helper;
			ButtonHolder.ButtonIcon = helper.Content.Load<Texture2D>(@"icon.png");

			helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
			helper.Events.GameLoop.ReturnedToTitle += (sender, e) => helper.WriteConfig(Config);
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

			// Commands for adding/removing filters
			// TODO: Move to a new UI
			helper.ConsoleCommands.Add("stnc_add", "Adds a category to the list of filters used by Stack to Nearby Chests.", ManageCategories);
			helper.ConsoleCommands.Add("stnc_del", "Removes a category to the list of filters used by Stack to Nearby Chests.", ManageCategories);
			helper.ConsoleCommands.Add("stnc_list", "Lists the categories being used for filtering.", (cmd, args) =>
			{
				var categories = Config.AllowedCategories.Count == 0 
					? "none!"
					: string.Join(", ", Config.AllowedCategories);
				Monitor.Log($"Categories being filtered: {categories}");
			});
			helper.ConsoleCommands.Add("stnc_radius", "Sets the radius for Stack to Nearby Chests.", (cmd, args) => 
			{
				Config.Radius = int.Parse(args[0]);
				Monitor.Log($"Set radius to {Config.Radius}.");
			});
			helper.ConsoleCommands.Add("stnc_mode", "Sets the search mode for Stack to Nearby Chests (either CURRENT_AREA/ANY_AREA)", (cmd, args) =>
			{
				if (!Enum.TryParse(args[0].ToUpper(), out ChestSearchMode mode))
				{
					Monitor.Log("Couldn't parse that mode name (available: CURRENT_AREA/ANY_AREA).");
					return;
				}

				Config.SearchMode = mode;
				Monitor.Log($"Set search mode to {Config.SearchMode}.");
			});
		}

		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			if (Config.SearchMode == ChestSearchMode.ANY_AREA && !Context.IsMainPlayer)
			{
				Monitor.Log("WARNING: Search mode is set to ANY_AREA but we're not the main player in multiplayer. Due to limitations, only chests in the CURRENT area can be used.", LogLevel.Warn);
			}
		}

		private void ManageCategories(string command, string[] arguments)
		{
			if (arguments == null || arguments.Length == 0)
			{
				Monitor.Log("Missing category name.", LogLevel.Warn);
				return;
			}

			var category = arguments[0].ToLower();

			if (command.Equals("stnc_add", StringComparison.OrdinalIgnoreCase))
			{
				if (Config.AllowedCategories.Contains(category))
				{
					Monitor.Log($"{category} is already being used for filtering.");
				}
				else
				{
					Config.AllowedCategories.Add(category);
					Monitor.Log($"Added {category} to filtering.");
				}
			}
			else if (command.Equals("stnc_del", StringComparison.OrdinalIgnoreCase))
			{
				if (Config.AllowedCategories.Contains(category))
				{
					Config.AllowedCategories.Remove(category);
					Monitor.Log($"Removed {category} from filtering.");
				}
				else
				{
					Monitor.Log($"{category} is already being used for filtering.");
				}
			}
		}

		/// <summary>Raised after the game is launched, right before the first update tick. This happens once per game session (unrelated to loading saves). All mods are loaded and initialised at this point, so this is a good time to set up mod integrations.</summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event data.</param>
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			Patch.PatchAll(HarmonyInstance.Create("me.ilyaki.StackToNearbyChests"));
		}
	}
}
