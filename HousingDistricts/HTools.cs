using System;
using System.Collections.Generic;
using System.IO;
using TShockAPI;
using Terraria;

namespace HousingDistricts
{
    class HTools
    {
        internal static string HConfigPath { get { return Path.Combine(TShock.SavePath, "hconfig.json"); } }

        public static void SetupConfig()
        {
            try
            {
                if (File.Exists(HConfigPath))
                {
                    HousingDistricts.HConfig = HConfigFile.Read(HConfigPath);
                    // Add all the missing config properties in the json file
                }
                HousingDistricts.HConfig.Write(HConfigPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("Config Exception");
                Log.Error(ex.ToString());
            }
        }

        public static void BroadcastToHouse(House house, string text, string playername)
        {
            foreach (HPlayer player in HousingDistricts.HPlayers)
            {
                if (house.HouseArea.Intersects(new Rectangle(player.TSPlayer.TileX, player.TSPlayer.TileY, 1, 1)) && house.WorldID == Main.worldID.ToString())
                {
                    player.TSPlayer.SendMessage("<House> <" + playername + ">: " + text, Color.LightSkyBlue);
                }
            }
        }

        public static string InAreaHouseName(int x, int y)
        {
            foreach (House house in HousingDistricts.Houses)
            {
                if (house.WorldID == Main.worldID.ToString() &&
                    x >= house.HouseArea.Left && x < house.HouseArea.Right &&
                    y >= house.HouseArea.Top && y < house.HouseArea.Bottom)
                {
                    return house.Name;
                }
            }
            return null;
        }

        public static void BroadcastToHouseOwners(string housename, string text)
        {
            BroadcastToHouseOwners(HouseTools.GetHouseByName(housename), text);
        }

        public static void BroadcastToHouseOwners(House house, string text)
        {
            foreach (string ID in house.Owners)
            {
                foreach (TSPlayer player in TShock.Players)
                {
                    if (player != null)
                    {
                        if (player.UserID.ToString() == ID)
                        {
                            player.SendMessage(text, Color.LightSeaGreen);
                        }
                    }
                }
            }
        }



        public static bool OwnsHouse(string UserID, string housename)
        {
            if (UserID == null || UserID == String.Empty || UserID == "0" || housename == null || housename == String.Empty)
            {
                return false;
            }
            if (HouseTools.GetHouseByName(housename) == null) { return false; }
            return OwnsHouse(UserID, HouseTools.GetHouseByName(housename));
        }

        public static bool OwnsHouse(string UserID, House house)
        {
            bool isAdmin = false;
            try { isAdmin = TShock.Groups.GetGroupByName(TShock.Users.GetUserByID(Convert.ToInt32(UserID)).Group).HasPermission("house.root"); }
            catch {}
            if (UserID != null && UserID != String.Empty && UserID != "0" && house != null)
            {
                try
                {
                    if (house.Owners.Contains(UserID) || isAdmin)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return false;
                }
            }
            return false;
        }

        public static bool CanVisitHouse(string UserID, House house)
        {
            return house.Visitors.Contains(UserID) || house.Owners.Contains(UserID); 
        }

        public static HPlayer GetPlayerByID(int id)
        {
            var retplayer = new HPlayer();

            foreach (HPlayer player in HousingDistricts.HPlayers)
            {
                if (player.Index == id)
                    return player;
            }

            return retplayer;
        }
    }
}
