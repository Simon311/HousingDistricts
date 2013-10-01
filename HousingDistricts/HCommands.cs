using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace HousingDistricts
{
    public class HCommands
    {
        public static void House(CommandArgs args)
        {
            string AdminHouse = "house.admin"; // Seems right to keep the actual permision name in one place, for easy editing
            string cmd = "help";
            var ply = args.Player; // Makes the code shorter
            if (args.Parameters.Count > 0)
            {
                cmd = args.Parameters[0].ToLower();
            }
            var player = HTools.GetPlayerByID(args.Player.Index);
            switch (cmd)
            {
                case "name":
                    {
                        {
                            ply.SendMessage("Hit a block to get the name of the house", Color.Yellow);
                            player.AwaitingHouseName = true;
                        }
                        break;
                    }
                case "set":
                    {
                        int choice = 0;
                        if (args.Parameters.Count == 2 &&
                            int.TryParse(args.Parameters[1], out choice) &&
                            choice >= 1 && choice <= 2)
                        {
                            if (choice == 1)
                                ply.SendMessage("Now hit the TOP-LEFT block of the area to be protected.", Color.Yellow);
                            if (choice == 2)
                                ply.SendMessage("Now hit the BOTTOM-RIGHT block of the area to be protected.", Color.Yellow);
                            ply.AwaitingTempPoint = choice;
                        }
                        else
                        {
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house set [1/2]");
                        }
                        break;
                    }
                case "add":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            List<int> userOwnedHouses = new List<int>();
                            foreach (House house in HousingDistricts.Houses)
                            {
                                foreach (string owner in house.Owners)
                                {
                                    if (ply.UserID.ToString() == owner)
                                    {
                                        userOwnedHouses.Add(house.ID);
                                        break;
                                    }

                                }
                            }
                            if (userOwnedHouses.Count < HousingDistricts.HConfig.MaxHousesByUsername || ply.Group.HasPermission("house.bypasscount"))
                            {
                                if (!ply.TempPoints.Any(p => p == Point.Zero))
                                {
                                    string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));

                                    var x = Math.Min(ply.TempPoints[0].X, ply.TempPoints[1].X);
                                    var y = Math.Min(ply.TempPoints[0].Y, ply.TempPoints[1].Y);
                                    var width = Math.Abs(ply.TempPoints[0].X - ply.TempPoints[1].X) + 1;
                                    var height = Math.Abs(ply.TempPoints[0].Y - ply.TempPoints[1].Y) + 1;
                                    if (((width * height) <= HousingDistricts.HConfig.MaxHouseSize && width >= HousingDistricts.HConfig.MinHouseWidth && height >= HousingDistricts.HConfig.MinHouseHeight) || ply.Group.HasPermission("house.bypasssize"))
                                    {
                                        Rectangle newHouseR = new Rectangle(x, y, width, height);
                                        foreach (House house in HousingDistricts.Houses)
                                        {
                                            if (house.WorldID == Main.worldID.ToString() && (newHouseR.Intersects(house.HouseArea) && !userOwnedHouses.Contains(house.ID)) && !HousingDistricts.HConfig.OverlapHouses)
                                            { // user is allowed to intersect their own house
                                                ply.SendErrorMessage("Your selected area overlaps another players' house, which is not allowed.");
                                                return;
                                            }
                                        }
                                        if (HouseTools.AddHouse(x, y, width, height, houseName, ply.UserID.ToString(), 0, 0))
                                        {
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                            ply.SendMessage("You have created new house " + houseName, Color.Yellow);
                                            HouseTools.AddNewUser(houseName, ply.UserID.ToString());
                                        }
                                        else
                                        {
                                            ply.SendErrorMessage("House " + houseName + " already exists");
                                        }
                                    }
                                    else
                                    {
                                        if ((width * height) >= HousingDistricts.HConfig.MaxHouseSize)
                                        {
                                            ply.SendErrorMessage("Your house exceeds the maximum size of " + HousingDistricts.HConfig.MaxHouseSize.ToString() + "blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else if (width < HousingDistricts.HConfig.MinHouseWidth)
                                        {
                                            ply.SendErrorMessage("Your house width is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseWidth.ToString() + "blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else
                                        {
                                            ply.SendErrorMessage("Your house height is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseHeight.ToString() + "blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                    }
                                }
                                else
                                {
                                    ply.SendErrorMessage("Points not set up yet");
                                }
                            }
                            else
                            {
                                ply.SendErrorMessage("House add failed: You have too many houses!");
                            }
                        }
                        else
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house add [name]");
                        break;
                    }
                case "allow":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(String.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count - 2)));
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (HouseTools.AddNewUser(houseName, playerID.ID.ToString()))
                                    {
                                        ply.SendMessage("Added user " + playerName + " to " + houseName, Color.Yellow);
                                    }
                                    else
                                        ply.SendErrorMessage("House " + houseName + " not found");
                                }
                                else
                                {
                                    ply.SendErrorMessage("Player " + playerName + " not found");
                                }
                            }
                            else
                            {
                                ply.SendErrorMessage("You do not own house: " + houseName);
                            }
                        }
                        else
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house allow [name] [house]");
                        break;
                    }
                case "delete":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                            var house = HouseTools.GetHouseByName(houseName);
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                List<SqlValue> where = new List<SqlValue>();
                                where.Add(new SqlValue("Name", "'" + houseName.Replace("'", "''") + "'"));
                                HousingDistricts.SQLWriter.DeleteRow("HousingDistrict", where);
                                HousingDistricts.Houses.Remove(house);
                                ply.SendMessage("House: " + houseName + " deleted", Color.Yellow);
                                break;
                            }
                            else
                            {
                                ply.SendErrorMessage("You do not own house: " + houseName);
                                break;
                            }
                        }
                        else
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house delete [house]");
                        break;
                    }
                case "clear":
                    {
                        ply.TempPoints[0] = Point.Zero;
                        ply.TempPoints[1] = Point.Zero;
                        ply.AwaitingTempPoint = 0;
                        ply.SendMessage("Cleared temp area", Color.Yellow);
                        break;
                    }
                case "list":
                    {
                        //How many regions per page
                        const int pagelimit = 15;
                        //How many regions per line
                        const int perline = 5;
                        //Pages start at 0 but are displayed and parsed at 1
                        int page = 0;


                        if (args.Parameters.Count > 1)
                        {
                            if (!int.TryParse(args.Parameters[1], out page) || page < 1)
                            {
                                ply.SendErrorMessage(string.Format("Invalid page number ({0})", page));
                                return;
                            }
                            page--; //Substract 1 as pages are parsed starting at 1 and not 0
                        }

                        List<House> houses = new List<House>();

                        foreach (House house in HousingDistricts.Houses)
                        {
                            if (house.WorldID == Main.worldID.ToString())
                            {
                                houses.Add(house);
                            }
                        }

                        // Are there even any houses to display?
                        if (houses.Count == 0)
                        {
                            ply.SendErrorMessage("There are currently no houses defined.");
                            return;
                        }

                        //Check if they are trying to access a page that doesn't exist.
                        int pagecount = houses.Count / pagelimit;
                        if (page > pagecount)
                        {
                            ply.SendErrorMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1));
                            return;
                        }

                        //Display the current page and the number of pages.
                        ply.SendMessage(string.Format("Current Houses ({0}/{1}):", page + 1, pagecount + 1), Color.Green);

                        //Add up to pagelimit names to a list
                        var nameslist = new List<string>();
                        for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < houses.Count; i++)
                        {
                            nameslist.Add(houses[i].Name);
                        }

                        //convert the list to an array for joining
                        var names = nameslist.ToArray();
                        for (int i = 0; i < names.Length; i += perline)
                        {
                            ply.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
                        }

                        if (page < pagecount)
                        {
                            ply.SendMessage(string.Format("Type /house list {0} for more houses.", (page + 2)), Color.Yellow);
                        }
                        break;
                    }
                case "redefine":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            if (!ply.TempPoints.Any(p => p == Point.Zero))
                            {
                                string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                if (HTools.OwnsHouse(ply.UserID.ToString(), houseName) || ply.Group.HasPermission(AdminHouse))
                                {
                                    var x = Math.Min(ply.TempPoints[0].X, ply.TempPoints[1].X);
                                    var y = Math.Min(ply.TempPoints[0].Y, ply.TempPoints[1].Y);
                                    var width = Math.Abs(ply.TempPoints[0].X - ply.TempPoints[1].X) + 1;
                                    var height = Math.Abs(ply.TempPoints[0].Y - ply.TempPoints[1].Y) + 1;

                                    if ((width * height) <= HousingDistricts.HConfig.MaxHouseSize && width >= HousingDistricts.HConfig.MinHouseWidth && height >= HousingDistricts.HConfig.MinHouseHeight)
                                    {
                                        Rectangle newHouseR = new Rectangle(x, y, width, height);
                                        foreach (House house in HousingDistricts.Houses)
                                        {
                                            if (house.WorldID == Main.worldID.ToString() && (newHouseR.Intersects(house.HouseArea) && !house.Owners.Contains(ply.UserID.ToString())) && !HousingDistricts.HConfig.OverlapHouses)
                                            { // user is allowed to intersect their own house
                                                ply.SendErrorMessage("Your selected area overlaps another players' house, which is not allowed.");
                                                return;
                                            }
                                        }
                                        if (HouseTools.RedefineHouse(x, y, width, height, houseName))
                                        {
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                            ply.SendMessage("Redefined house " + houseName, Color.Yellow);
                                        }
                                        else
                                        {
                                            ply.SendErrorMessage("Error redefining house " + houseName);
                                        }
                                    }
                                    else
                                    {
                                        if ((width * height) >= HousingDistricts.HConfig.MaxHouseSize)
                                        {
                                            ply.SendErrorMessage("Your house exceeds the maximum size of " + HousingDistricts.HConfig.MaxHouseSize.ToString() + "blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else if (width < HousingDistricts.HConfig.MinHouseWidth)
                                        {
                                            ply.SendErrorMessage("Your house width is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseWidth.ToString() + "blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else
                                        {
                                            ply.SendErrorMessage("Your house height is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseHeight.ToString() + "blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                    }
                                }
                                else
                                {
                                    ply.SendErrorMessage("You do not own house: " + houseName);
                                }
                            }
                            else
                            {
                                ply.SendErrorMessage("Points not set up yet");
                            }
                        }
                        else
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house redefine [name]");
                        break;
                    }
                case "debug":
                    { // This is just here for... I have no idea really :)
                        if (args.Parameters.Count > 1)
                        {
                            var house = HouseTools.GetHouseByName(args.Parameters[1]);
                            ply.SendMessage("House '" + house.Name + "':", Color.LawnGreen);
                            ply.SendMessage("Chat enabled: " + house.ChatEnabled.ToString(), Color.LawnGreen);
                            ply.SendMessage("Owner IDs: " + String.Join(", ",house.Owners.ToArray()), Color.LawnGreen);
                        }
                        break;
                    }
                case "owner":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            var house = HouseTools.GetHouseByName(args.Parameters[1]);
                            string OwnerNames = "";
                            foreach (string ID in house.Owners)
                            {
                                if (OwnerNames == "") { OwnerNames = OwnerNames + TShock.Users.GetUserByID(System.Convert.ToInt32(ID)).Name; }
                                else { OwnerNames = OwnerNames + ", " + TShock.Users.GetUserByID(System.Convert.ToInt32(ID)).Name; }
                            }
                            ply.SendMessage(String.Format("House '{0}' owners: {1}", house.Name, OwnerNames), Color.Lime);
                        }
                        else
                        {
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house owner [house-name]");
                        }
                        break;
                    }
                case "chat":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            var house = HouseTools.GetHouseByName(args.Parameters[1]);
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name))
                            {
                                if (args.Parameters.Count > 2)
                                {
                                    if (args.Parameters[2].ToLower() == "on")
                                    {
                                        HouseTools.ToggleChat(house, 1);
                                        ply.SendMessage(house.Name + " chat is now enabled.", Color.Lime);
                                    }
                                    else if (args.Parameters[2].ToLower() == "off")
                                    {
                                        HouseTools.ToggleChat(house, 0);
                                        ply.SendMessage(house.Name + " chat is now disabled.", Color.Lime);
                                    }
                                    else
                                    {
                                        ply.SendErrorMessage("Invalid syntax! Use /house chat <housename> (on|off)");
                                    }
                                }
                                else
                                {
                                    HouseTools.ToggleChat(house, (house.ChatEnabled == 0 ? 1 : 0));
                                    ply.SendMessage(house.Name + " chat is now " + (house.ChatEnabled == 0 ? "disabled." : "enabled."), Color.Lime);
                                }
                            }
                            else
                            {
                                ply.SendErrorMessage("You do not own " + house.Name + ".");
                            }
                        }
                        else
                        {
                            ply.SendErrorMessage("Invalid syntax! Use /house chat <housename> (on|off)");
                        }
                        break;
                    }
                case "addvisitor":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(args.Parameters[2]);
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (HouseTools.AddNewVisitor(house, playerID.ID.ToString()))
                                    {
                                        ply.SendMessage("Added user " + playerName + " to " + houseName + "as a visitor.", Color.Yellow);
                                    }
                                    else
                                        ply.SendErrorMessage("House " + houseName + " not found");
                                }
                                else
                                {
                                    ply.SendErrorMessage("Player " + playerName + " not found");
                                }
                            }
                            else
                            {
                                ply.SendErrorMessage("You do not own house: " + houseName);
                            }
                        }
                        else
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house addvisitor [name] [house]");
                        break;
                    }
                default:
                    {
                        ply.SendMessage("To create a house, use these commands:", Color.Lime);
                        ply.SendMessage("/house set 1", Color.Lime);
                        ply.SendMessage("/house set 2", Color.Lime);
                        ply.SendMessage("/house add HouseName", Color.Lime);
                        ply.SendMessage("Other /house commands: list, allow, name, delete, clear, owner", Color.Lime);
                        break;
                    }
            }
        }

        public static void TellAll(CommandArgs args)
        {
            if (HousingDistricts.HConfig.HouseChatEnabled)
            {
                if (args.Player != null)
                {
                    var tsplr = args.Player;
                    if (args.Parameters.Count < 1)
                    {
                        tsplr.SendErrorMessage("Invalid syntax! Proper syntax: /all [message]");
                        return;
                    }
                    string text = String.Join(" ", args.Parameters);
                    if (!tsplr.mute)
                    {
                        TShock.Utils.Broadcast(
                            String.Format(TShock.Config.ChatFormat, tsplr.Group.Name, tsplr.Group.Prefix, tsplr.Name, tsplr.Group.Suffix, text),
                            tsplr.Group.R, tsplr.Group.G, tsplr.Group.B);
                    }
                    else
                    {
                        tsplr.SendErrorMessage("You are muted!");
                    }
                }
            }
            else { args.Player.SendErrorMessage("No need for that ;)"); }
        }

        public static void Convert(CommandArgs args)
        {
            List<SqlValue> value = new List<SqlValue>();
            value.Add(new SqlValue("WorldID", "'" + Main.worldID.ToString() + "'"));
            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", value, new List<SqlValue>());

            foreach (House house in HousingDistricts.Houses)
            {
                house.WorldID = Main.worldID.ToString();
            }
        }

        public static void ChangeLock(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                string houseName = "";

                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (houseName == "")
                    {
                        houseName = args.Parameters[i];
                    }
                    else
                    {
                        houseName = houseName + " " + args.Parameters[i];
                    }
                }

                if (HTools.OwnsHouse(args.Player.UserID.ToString(), houseName))
                {
                    bool locked = HouseTools.ChangeLock(houseName);
                    args.Player.SendMessage("House: " + houseName + (locked ? " locked" : " unlocked"), Color.Yellow);
                }
                else
                    args.Player.SendErrorMessage("You do not own House: " + houseName);
            }
            else
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /changelock [house]");
        }

        public static void HouseReload(CommandArgs args)
        {
            HTools.SetupConfig();
            Log.Info("House Reload Initiated");
            args.Player.SendMessage("House Reload Initiated", Color.Lime);
        }
    }
}
