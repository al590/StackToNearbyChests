using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;

namespace StackToNearbyChests
{
	static class StackLogic
	{
		internal static void StackToNearbyChests(int radius)
		{

			StardewValley.Farmer farmer = Game1.player;
			IEnumerable<Chest> chests = ModEntry.Config.SearchMode == ChestSearchMode.CURRENT_AREA
				? GetChestsAroundFarmer(farmer, radius) 
				: GetAllChests();

			foreach (Chest chest in chests)
			{
				List<Item> itemsToRemoveFromPlayer = new List<Item>();
				bool movedAtLeastOne = false;

				//Find remaining stack size of CHEST item. check if player has the item, then remove as much as possible
				//need to compare quality
				foreach (Item chestItem in chest.items)
				{
					if (chestItem != null)
					{
						foreach (Item playerItem in farmer.Items)
						{
							if (playerItem != null)
							{
								int remainingStackSize = chestItem.getRemainingStackSpace();
								if (!(itemsToRemoveFromPlayer.Contains(playerItem)) && playerItem.canStackWith(chestItem) && playerItem.CompareTo(chestItem) == 0)
								{
									movedAtLeastOne = true;
									int amountToRemove = Math.Min(remainingStackSize, playerItem.Stack);
									chestItem.Stack += amountToRemove;

									if (playerItem.Stack > amountToRemove)
									{
										playerItem.Stack -= amountToRemove;
									}
									else
									{
										itemsToRemoveFromPlayer.Add(playerItem);
									}
								}
							}
						}
					}
				}

				foreach (Item removeItem in itemsToRemoveFromPlayer)
					farmer.removeItemFromInventory(removeItem);



				//List of sounds: https://gist.github.com/gasolinewaltz/46b1473415d412e220a21cb84dd9aad6
				if (movedAtLeastOne)
					Game1.playSound(Game1.soundBank.GetCue("pickUpItem").Name);

			}



		}

		private static IEnumerable<Chest> GetChestsAroundFarmer(StardewValley.Farmer farmer, int radius)
		{
			Vector2 farmerLocation = farmer.getTileLocation();

			//Normal object chests
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dy = -radius; dy <= radius; dy++)
				{
					Vector2 checkLocation = Game1.tileSize * (farmerLocation + new Vector2(dx, dy));
					StardewValley.Object blockObject = farmer.currentLocation.getObjectAt((int)checkLocation.X, (int)checkLocation.Y);
					if (blockObject is Chest chest && IsStackingAllowed(chest))
					{
						yield return chest;
					}
				}
			}

			//Fridge
			if (farmer?.currentLocation is FarmHouse farmHouse && farmHouse.upgradeLevel >= 1) //Lvl 1,2,3 is where you have fridge upgrade
			{
				Point fridgeLocation = farmHouse.getKitchenStandingSpot();
				fridgeLocation.X += 2; fridgeLocation.Y += -1; //Fridge spot relative to kitchen spot

				if (Math.Abs(farmerLocation.X - fridgeLocation.X) <= radius && Math.Abs(farmerLocation.Y - fridgeLocation.Y) <= radius)
				{
					if (farmHouse.fridge.Value != null)
					{
						if (IsStackingAllowed(farmHouse.fridge.Value))
							yield return farmHouse.fridge.Value;
					}
					else
						Console.WriteLine("StackToNearbyChests: could not find fridge!");
				}
			}

			//Mills and Junimo Huts
			if (farmer.currentLocation is BuildableGameLocation buildableGameLocation)
			{
				foreach (Building building in buildableGameLocation.buildings)
				{
					if (Math.Abs(building.tileX.Value - farmerLocation.X) <= radius && Math.Abs(building.tileY.Value - farmerLocation.Y) <= radius)
					{
						if (building is JunimoHut junimoHut && IsStackingAllowed(junimoHut.output.Value))
							yield return junimoHut.output.Value;

						if (building is Mill mill && IsStackingAllowed(mill.output.Value))
							yield return mill.output.Value;
					}
				}
			}
		}

		public static IEnumerable<Chest> GetAllChests()
		{
			foreach (var location in ModEntry.Helper.Multiplayer.GetActiveLocations())
			{
				var mapWidth = location.Map.DisplayWidth / 64;
				var mapHeight = location.Map.DisplayHeight / 64;

				for (var x = 0; x < mapWidth; x++)
					for (var y = 0; y < mapHeight; y++)
						if (location.getObjectAtTile(x, y) is Chest chest && IsStackingAllowed(chest))
							yield return chest;

				if (location is FarmHouse farmHouse && farmHouse.upgradeLevel >= 1)
				{
					var fridgeLocation = farmHouse.getKitchenStandingSpot();
					fridgeLocation.X += 2; 
					fridgeLocation.Y += -1;

					if (farmHouse.fridge.Value != null) 
					{
						if (IsStackingAllowed(farmHouse.fridge.Value))
							yield return farmHouse.fridge.Value;
					}
					else
						Console.WriteLine("StackToNearbyChests (global): could not find fridge!");
				}

				if (location is BuildableGameLocation buildableGameLocation)
				{
					foreach (var building in buildableGameLocation.buildings)
					{
						if (building is JunimoHut junimoHut && IsStackingAllowed(junimoHut.output.Value))
							yield return junimoHut.output.Value;

						if (building is Mill mill && IsStackingAllowed(mill.output.Value))
							yield return mill.output.Value;
					}
				}
			}
		}

		private static bool IsStackingAllowed(Chest chest)
		{
			// If we lack any filters then any chest is suitable
			if (ModEntry.Config.AllowedCategories.Count == 0)
				return true;

			var category = GetChestCategory(chest);
			return !string.IsNullOrEmpty(category) && ModEntry.Config.AllowedCategories.Contains(category);
		}

		private static string GetChestCategory(Chest chest)
		{
			var match = Regex.Match(chest.Name, @"\|cat\:(.*?)\|");
			return match.Success ? match.Groups[1].Value.ToLower() : null;
		}
	}
}
