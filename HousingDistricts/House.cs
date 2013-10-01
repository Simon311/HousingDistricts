using System;
using System.Collections.Generic;
using System.Linq;
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
            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Name", "'" + housename.Replace("'", "''") + "'"));
            values.Add(new SqlValue("TopX", tx));
            values.Add(new SqlValue("TopY", ty));
            values.Add(new SqlValue("BottomX", width));
            values.Add(new SqlValue("BottomY", height));
            values.Add(new SqlValue("Owners", "0"));
            values.Add(new SqlValue("WorldID", "'" + Main.worldID.ToString() + "'"));
            values.Add(new SqlValue("Locked", locked));
            values.Add(new SqlValue("ChatEnabled", chatenabled));
            values.Add(new SqlValue("Visitors", "0"));
            HousingDistricts.SQLEditor.InsertValues("HousingDistrict", values);
            HousingDistricts.Houses.Add(new House(new Rectangle(tx, ty, width, height), new List<string>(), (HousingDistricts.Houses.Count + 1), housename, Main.worldID.ToString(), locked, chatenabled, new List<string>()));
            return true;
        }

        public static bool AddNewUser(string houseName, string id)
        {
            var house = GetHouseByName(houseName);
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

            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Owners", "'" + sb.ToString() + "'"));

            List<SqlValue> wheres = new List<SqlValue>();
            wheres.Add(new SqlValue("Name", "'" + houseName.Replace("'", "''") + "'"));

            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
            return true;
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

            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Visitors", "'" + sb.ToString() + "'"));

            List<SqlValue> wheres = new List<SqlValue>();
            wheres.Add(new SqlValue("Name", "'" + house.Name.Replace("'", "''") + "'"));

            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
            return true;
        }
        public static bool ToggleChat(House house, int onOrOff)
        {
            house.ChatEnabled = onOrOff;

            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("ChatEnabled", "'" + house.ChatEnabled.ToString() + "'"));

            List<SqlValue> wheres = new List<SqlValue>();
            wheres.Add(new SqlValue("Name", "'" + house.Name.Replace("'", "''") + "'"));

            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
            return true;
        }
        public static bool ChangeLock(string houseName)
        {
            var house = GetHouseByName(houseName);
            bool locked = false;

            if (house.Locked == 0)
                locked = true;
            else
                locked = false;

            house.Locked = locked ? 1 : 0;

            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Locked", locked ? 1 : 0));
            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, new List<SqlValue>());
            return locked;
        }
        public static bool RedefineHouse(int tx, int ty, int width, int height, string housename)
        {
            try
            {
                var house = GetHouseByName(housename);
                var houseName = house.Name;

                HousingDistricts.SQLEditor.UpdateValues(
                    "HousingDistrict", 
                    new List<SqlValue> {
                        new SqlValue("TopX", tx),
                        new SqlValue("TopY", ty),
                        new SqlValue("BottomX", width),
                        new SqlValue("BottomY", height),
                    }, 
                    new List<SqlValue> {
                      /* Note: TShock generates an invalid sql expression when multiple wheres are used. */
                        new SqlValue("Name", "'" + houseName.Replace("'", "''") + "'"),
                    }
                );

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
            foreach (House house in HousingDistricts.Houses)
            {
                if (house.Name == name)
                    return house;
            }
            return null;
        }
    }
}
