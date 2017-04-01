﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using SDV = StardewValley;

namespace CJBAutomation
{
    public class Automation
    {
        /*********
        ** Properties
        *********/
        private static Dictionary<int, int> CropData;


        /*********
        ** Public methods
        *********/
        public static List<Chest> GetConnectedChests(GameLocation location, Vector2 tile)
        {
            List<Chest> chests = new List<Chest>();

            if (location == null || tile == null)
                return chests;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if ((CJBAutomation.Config.Diagonal || (x == 0 || y == 0)) && !(x == 0 && y == 0))
                    {
                        Vector2 index = new Vector2(tile.X - x, tile.Y - y);
                        if (location.objects.ContainsKey(index))
                        {
                            StardewValley.Object o = location.objects[index];
                            if (o is Chest)
                                chests.Add((Chest)o);
                        }
                    }
                }
            }
            return chests;
        }

        public static IEnumerable<T> FindItemTypes<T>(GameLocation location)
            where T : SDV.Object
        {
            return location.objects.Values.Where(o => o is T).Select(m => (T)m);
        }

        public static IEnumerable<Chest> GetChestsInLocation(GameLocation location)
        {
            return FindItemTypes<Chest>(location);
        }

        public static bool DoChestsHaveItem(List<Chest> chests, int index, int stack)
        {
            foreach (Chest chest in chests)
            {
                foreach (Item item in chest.items)
                {
                    if (item.parentSheetIndex == index && item.Stack >= stack)
                        return true;
                }
            }
            return false;
        }

        public static void DecreaseStack(Chest chest, Item stack, int amount = 1)
        {
            stack.Stack -= amount;
            if (stack.Stack <= 0)
                chest.items.Remove(stack);
            chest.clearNulls();
        }

        public static bool RemoveItemFromChests(List<Chest> chests, int index, int stack = 1)
        {
            foreach (Chest chest in chests)
            {
                foreach (Item item in chest.items)
                {
                    if (item.parentSheetIndex == index && item.Stack >= stack)
                    {
                        item.Stack -= stack;
                        if (item.Stack <= 0)
                            chest.items.Remove(item);
                        chest.clearNulls();
                        return true;
                    }
                }
            }
            return false;
        }

        public static Item GetItemFromChestsByCategory(List<Chest> chests, int category, int excludeID)
        {
            foreach (Chest chest in chests)
            {
                foreach (Item item in chest.items)
                {
                    if (item.category == category && item.parentSheetIndex != excludeID)
                        return item.getOne();
                }
            }
            return null;
        }

        public static void RemoveItemFromChestsCategory(List<Chest> chests, int category, int excludeID)
        {
            foreach (Chest chest in chests)
            {
                foreach (Item item in chest.items)
                {
                    if (item.category == category && item.parentSheetIndex != excludeID)
                    {
                        item.Stack -= 1;
                        if (item.Stack <= 0)
                            chest.items.Remove(item);
                        chest.clearNulls();
                        return;
                    }
                }
            }
            return;
        }

        public static Item GetItemFromChestsByName(List<Chest> chests, string name, int excludeID)
        {
            foreach (Chest chest in chests)
            {
                foreach (Item item in chest.items)
                {
                    if (item.Name == null) continue;
                    if (item.Name == name && item.parentSheetIndex != excludeID)
                        return item.getOne();
                }
            }
            return null;
        }

        public static bool DoChestsHaveEnoughItemsByName(List<Chest> chests, string name, int excludeID, int stack)
        {
            int itemsFound = 0;
            foreach (Chest chest in chests)
            {
                foreach (Item item in chest.items)
                {
                    if (item.Name == null)
                        continue;
                    if (item.Name == name && item.parentSheetIndex != excludeID)
                        itemsFound += item.Stack;
                    if (itemsFound >= stack)
                        return true;
                }
            }
            return false;
        }

        public static bool RemoveItemFromChestsByName(List<Chest> chests, string name, int excludeID, int stack = 1)
        {
            if (stack > 1 && !Automation.DoChestsHaveEnoughItemsByName(chests, name, excludeID, stack))
                return false;

            foreach (Chest chest in chests)
            {
                var toRemove = new List<Item>();
                foreach (Item item in chest.items)
                {
                    if (item.Name == null) continue;
                    if (item.Name == name && item.parentSheetIndex != excludeID)
                    {
                        int remove = Math.Min(stack, item.Stack);
                        item.Stack -= remove;
                        stack -= remove;
                        if (item.Stack <= 0)
                            toRemove.Add(item);
                        if (stack <= 0)
                        {
                            foreach (var victim in toRemove)
                                chest.items.Remove(victim);
                            chest.clearNulls();
                            return true;
                        }
                    }
                }
                foreach (var victim in toRemove)
                    chest.items.Remove(victim);
                chest.clearNulls();
            }
            return false;
        }

        public static int RemoveItemFromChestsIfCrop(List<Chest> chests)
        {
            foreach (Chest chest in chests)
            {
                foreach (Item item in chest.items)
                {
                    if (item.Name == null) continue;
                    int seedID = Automation.GetSeedIdFromCrop(item.parentSheetIndex);
                    if (seedID != -1)
                    {
                        item.Stack -= 1;
                        if (item.Stack <= 0)
                            chest.items.Remove(item);
                        chest.clearNulls();
                        return seedID;
                    }
                }
            }
            return -1;
        }

        public static int GetSeedIdFromCrop(int cropID)
        {

            if (Automation.CropData == null)
            {
                Automation.CropData = new Dictionary<int, int>();
                Dictionary<int, string> cropData = Game1.content.Load<Dictionary<int, string>>("Data\\Crops");
                foreach (KeyValuePair<int, string> entry in cropData)
                {
                    Automation.CropData.Add(Convert.ToInt32(entry.Value.Split(new char[] { '/' })[3]), entry.Key);
                }
            }

            if (Automation.CropData.ContainsKey(cropID))
            {
                return Automation.CropData[cropID];
            }

            return -1;
        }

        public static int GetMinutesForCrystalarium(int gemID)
        {
            switch (gemID)
            {
                case 60:
                    return 3000;
                case 62:
                    return 2240;
                case 64:
                    return 3000;
                case 66:
                    return 1360;
                case 68:
                    return 1120;
                case 70:
                    return 2400;
                case 72:
                    return 7200;
                case 80:
                    return 420;
                case 82:
                    return 1300;
                case 84:
                    return 1120;
                case 86:
                    return 800;
                default:
                    return 5000;
            }
        }
    }
}
