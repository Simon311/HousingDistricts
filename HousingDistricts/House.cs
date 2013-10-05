using System;
using System.Collections.Generic;
using System.Text;
using TShockAPI.DB;
using TShockAPI;
using Terraria;

namespace HousingDistricts
{
    public class House
    {
        public Rectangle HouseArea { get; set; }
        public List<string> Owners { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string WorldID { get; set; }
        public int Locked { get; set; }
        public int ChatEnabled { get; set; }
        public List<string> Visitors { get; set; }

        public House(Rectangle housearea, List<string> owners, int id, string name, string worldid, int locked, int chatenabled, List<string> visitors)
        {
            HouseArea = housearea;
            Owners = owners;
            ID = id;
            Name = name;
            WorldID = worldid;
            Locked = locked;
            ChatEnabled = chatenabled;
            Visitors = visitors;
        }
    }

    public class HouseTools
    {
        public static bool AddHouse(int tx, int ty, int width, int height, string housename, string owner, int locked, int chatenabled)
        {
            List<string> cols = new List<string>();
            cols.Add("Name");
            cols.Add("TopX");
            cols.Add("TopY");
            cols.Add("BottomX");
            cols.Add("BottomY");
            cols.Add("Owners");
            cols.Add("WorldID");
            cols.Add("Locked");
            cols.Add("ChatEnabled");
            cols.Add("Visitors");
            if (GetHouseByName(housename) != null) { return false; }
            try
            {
                TShock.DB.Query("INSERT INTO HousingDistrict (" + String.Join(", ", cols) + ") VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9);", housename.Replace("'", "''"), tx, ty, width, height, "0", Main.worldID.ToString(), locked, chatenabled, "0");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            HousingDistricts.Houses.Add(new House(new Rectangle(tx, ty, width, height), new List<string>(), (HousingDistricts.Houses.Count + 1), housename, Main.worldID.ToString(), locked, chatenabled, new List<string>()));
            return true;
        }

        public static bool AddNewUser(string houseName, string id)
        {
            var house = GetHouseByName(houseName);
            if (house == null) { return false; }
            StringBuilder sb = new StringBuilder();
            int count = 0;
            house.Owners.Add(id);
            foreach(string owner in house.Owners)
            {
                count++;
                sb.Append(owner);
                if(count != house.Owners.Count)
                    sb.Append(",");
            }
            sb.Replace("'", "''");

            try
            {
                string query = "UPDATE HousingDistrict SET Owners=@0 WHERE Name=@1";

                TShock.DB.Query(query, sb.ToString(), houseName.Replace("'", "''"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

            return true;
        }

        public static bool DeleteUser(string houseName, string id)
        {
            var house = GetHouseByName(houseName);
            if (house == null) { return false; }
            StringBuilder sb = new StringBuilder();
            int count = 0;
            while (house.Owners.Contains(id))
            {
                house.Owners.Remove(id);
            }
            foreach (string owner in house.Owners)
            {
                count++;
                sb.Append(owner);
                if (count != house.Owners.Count)
                    sb.Append(",");
            }
            sb.Replace("'", "''");

            try
            {
                string query = "UPDATE HousingDistrict SET Owners=@0 WHERE Name=@1";

                TShock.DB.Query(query, sb.ToString(), houseName.Replace("'", "''"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

            return true;
        }

        public static bool WorldMismatch(House house)
        {
            return (house.WorldID != Main.worldID.ToString());
        }

        public static bool AddNewVisitor(House house, string id)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            house.Visitors.Add(id);
            foreach (string visitor in house.Visitors)
            {
                count++;
                sb.Append(visitor);
                if (count != house.Visitors.Count)
                    sb.Append(",");
            }
            sb.Replace("'", "''");

            try
            {
                string query = "UPDATE HousingDistrict SET Owners=@0 WHERE Name=@1";

                TShock.DB.Query(query, sb.ToString(), house.Name.Replace("'", "''"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

            return true;
        }

        public static bool DeleteVisitor(House house, string id)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            while (house.Visitors.Contains(id))
            {
                house.Visitors.Remove(id);
            }
            foreach (string visitor in house.Visitors)
            {
                count++;
                sb.Append(visitor);
                if (count != house.Visitors.Count)
                    sb.Append(",");
            }
            sb.Replace("'", "''");

            try
            {
                string query = "UPDATE HousingDistrict SET Owners=@0 WHERE Name=@1";

                TShock.DB.Query(query, sb.ToString(), house.Name.Replace("'", "''"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

            return true;
        }

        public static bool ToggleChat(House house, int onOrOff)
        {
            house.ChatEnabled = onOrOff;

            try
            {
                string query = "UPDATE HousingDistrict SET ChatEnabled=@0 WHERE Name=@1";
                TShock.DB.Query(query, house.ChatEnabled.ToString(), house.Name.Replace("'", "''"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

            return true;
        }
        public static bool ChangeLock(House house)
        {
            bool locked = false;

            if (house.Locked == 0)
                locked = true;
            else
                locked = false;

            house.Locked = locked ? 1 : 0;

            try
            {
                string query = "UPDATE HousingDistrict SET Locked=@0 WHERE Name=@1";

                TShock.DB.Query(query, locked ? 1 : 0, house.Name.Replace("'", "''"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

            return locked;
        }
        public static bool RedefineHouse(int tx, int ty, int width, int height, string housename)
        {
            try
            {
                var house = GetHouseByName(housename);
                var houseName = house.Name;

                try
                {
                    string query = "UPDATE HousingDistrict SET TopX=@0, TopY=@1, BottomX=@2, BottomY=@3, WorldID=@4 WHERE Name=@1";

                    TShock.DB.Query(query, tx, ty, width, height, house.Name.Replace("'", "''"), Main.worldID.ToString());
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return false;
                }

                house.HouseArea = new Rectangle(tx, ty, width, height);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error on redefining house: \n" + ex);
                return false;
            }
        }


        public static House GetHouseByName(string name)
        {
            if (name == null || name == String.Empty)
            {
                Log.Info("Null housename detected @ GetHouseByName");
                while (HousingDistricts.Houses.Contains(null))
                {
                    HousingDistricts.Houses.Remove(null);
                }
                return null;
            }
            foreach (House house in HousingDistricts.Houses)
            {
                if (house.Name == name)
                    return house;
            }
            return null;
        }
    }
}
