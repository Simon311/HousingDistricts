using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace HousingDistricts
{
    public class HCommands
    {
        public static void House(CommandArgs args)
        {
            string AdminHouse = "house.admin"; // Seems right to keep the actual permission names in one place, for easy editing
			string UseHouse = "house.use";
			string LockHouse = "house.lock";
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
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
                        if (!ply.IsLoggedIn || ply.UserID == 0)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
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
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
                        if (!ply.IsLoggedIn || ply.UserID == 0)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (args.Parameters.Count > 1)
                        {
                            List<int> userOwnedHouses = new List<int>();
							var maxHouses = HTools.MaxCount(ply);
							for (int i = 0; i < HousingDistricts.Houses.Count; i++)
							{
								var house = HousingDistricts.Houses[i];
								if (HTools.OwnsHouse(ply.UserID.ToString(), house))
								{
									userOwnedHouses.Add(house.ID);
								}
                            }
                            if (userOwnedHouses.Count < maxHouses || ply.Group.HasPermission("house.bypasscount"))
                            {
                                if (!ply.TempPoints.Any(p => p == Point.Zero))
                                {
                                    string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));

                                    if (String.IsNullOrEmpty(houseName))
                                    {
                                        ply.SendErrorMessage("House name cannot be empty.");
                                        return;
                                    }

                                    var x = Math.Min(ply.TempPoints[0].X, ply.TempPoints[1].X);
                                    var y = Math.Min(ply.TempPoints[0].Y, ply.TempPoints[1].Y);
                                    var width = Math.Abs(ply.TempPoints[0].X - ply.TempPoints[1].X) + 1;
                                    var height = Math.Abs(ply.TempPoints[0].Y - ply.TempPoints[1].Y) + 1;
									var maxSize = HTools.MaxSize(ply);
									if (((width * height) <= maxSize && width >= HousingDistricts.HConfig.MinHouseWidth && height >= HousingDistricts.HConfig.MinHouseHeight) || ply.Group.HasPermission("house.bypasssize"))
                                    {
                                        Rectangle newHouseR = new Rectangle(x, y, width, height);
										for (int i = 0; i < HousingDistricts.Houses.Count; i++)
										{
											var house = HousingDistricts.Houses[i];
											if (!HouseTools.WorldMismatch(house) && (newHouseR.Intersects(house.HouseArea) && !userOwnedHouses.Contains(house.ID)) && !HousingDistricts.HConfig.OverlapHouses)
                                            {
                                                ply.SendErrorMessage("Your selected area overlaps another players' house, which is not allowed.");
                                                return;
                                            }
                                        }
                                        if (newHouseR.Intersects(new Rectangle(Main.spawnTileX, Main.spawnTileY, 1, 1)))
                                        {
                                                ply.SendErrorMessage("Your selected area overlaps spawnpoint, which is not allowed.");
                                                return;
                                        }
										for (int i = 0; i < TShock.Regions.Regions.Count; i++)
										{
											var Region = TShock.Regions.Regions[i];
											if (newHouseR.Intersects(Region.Area) && !Region.HasPermissionToBuildInRegion(ply))
											{
												ply.SendErrorMessage(string.Format("Your selected area overlaps region '{0}', which is not allowed.", Region.Name));
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
                                            var WM = HouseTools.WorldMismatch(HouseTools.GetHouseByName(houseName)) ? " with a different WorldID!" : "";
                                            ply.SendErrorMessage("House " + houseName + " already exists" + WM);
                                        }
                                    }
                                    else
                                    {
										if ((width * height) >= maxSize)
                                        {
                                            ply.SendErrorMessage("Your house exceeds the maximum size of " + maxSize.ToString() + " blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else if (width < HousingDistricts.HConfig.MinHouseWidth)
                                        {
                                            ply.SendErrorMessage("Your house width is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseWidth.ToString() + " blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else
                                        {
                                            ply.SendErrorMessage("Your house height is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseHeight.ToString() + " blocks.");
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
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(String.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count - 2)));
                            if (house == null) { ply.SendErrorMessage("No such house!"); return; }
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (!HTools.OwnsHouse(playerID.ID.ToString(), house))
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
                                        ply.SendErrorMessage("Player " + playerName + " is already allowed to build in '" + house.Name + "'.");
                                    }
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
                case "disallow":
                    {
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(String.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count - 2)));
                            if (house == null) { ply.SendErrorMessage("No such house!"); return; }
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (HouseTools.DeleteUser(houseName, playerID.ID.ToString()))
                                    {
                                        ply.SendMessage("Deleted user " + playerName + " from " + houseName, Color.Yellow);
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
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house disallow [name] [house]");
                        break;
                    }
                case "delete":
                    {
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (args.Parameters.Count > 1)
                        {
                            string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                            var house = HouseTools.GetHouseByName(houseName);
                            if (house == null) { ply.SendErrorMessage("No such house!"); return; }
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                try
                                {
                                    TShock.DB.Query("DELETE FROM HousingDistrict WHERE Name=@0", houseName);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.ToString());
                                }
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
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
                        ply.TempPoints[0] = Point.Zero;
                        ply.TempPoints[1] = Point.Zero;
                        ply.AwaitingTempPoint = 0;
                        ply.SendMessage("Cleared points!", Color.Yellow);
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

						for (int i = 0; i < HousingDistricts.Houses.Count; i++)
						{
							var house = HousingDistricts.Houses[i];
							if (!HouseTools.WorldMismatch(house))
                            {
                                houses.Add(house);
                            }
                        }

                        // Are there even any houses to display?
                        if (houses.Count == 0)
                        {
                            ply.SendMessage("There are currently no houses defined.", Color.Yellow);
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
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
                        if (!ply.IsLoggedIn || ply.UserID == 0)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
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
									var maxSize = HTools.MaxSize(ply);

									if ((width * height) <= maxSize && width >= HousingDistricts.HConfig.MinHouseWidth && height >= HousingDistricts.HConfig.MinHouseHeight)
                                    {
                                        Rectangle newHouseR = new Rectangle(x, y, width, height);
										for (int i = 0; i < HousingDistricts.Houses.Count; i++)
										{
											var house = HousingDistricts.Houses[i];
											if (!HouseTools.WorldMismatch(house) && (newHouseR.Intersects(house.HouseArea) && !house.Owners.Contains(ply.UserID.ToString())) && !HousingDistricts.HConfig.OverlapHouses)
                                            { // user is allowed to intersect their own house
                                                ply.SendErrorMessage("Your selected area overlaps another players' house, which is not allowed.");
                                                return;
                                            }
                                        }
                                        if (newHouseR.Intersects(new Rectangle(Main.spawnTileX, Main.spawnTileY, 1, 1)))
                                        {
                                            ply.SendErrorMessage("Your selected area overlaps spawnpoint, which is not allowed.");
                                            return;
                                        }
										for (int i = 0; i < TShock.Regions.Regions.Count; i++)
										{
											var Region = TShock.Regions.Regions[i];
											if (newHouseR.Intersects(Region.Area) && !Region.HasPermissionToBuildInRegion(ply))
											{
												ply.SendErrorMessage(string.Format("Your selected area overlaps region '{0}', which is not allowed.", Region.Name));
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
                                        if ((width * height) >= maxSize)
                                        {
                                            ply.SendErrorMessage("Your house exceeds the maximum size of " + maxSize.ToString() + " blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else if (width < HousingDistricts.HConfig.MinHouseWidth)
                                        {
                                            ply.SendErrorMessage("Your house width is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseWidth.ToString() + " blocks.");
                                            ply.SendErrorMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.");
                                            ply.TempPoints[0] = Point.Zero;
                                            ply.TempPoints[1] = Point.Zero;
                                        }
                                        else
                                        {
                                            ply.SendErrorMessage("Your house height is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseHeight.ToString() + " blocks.");
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
                case "info":
                    {
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer || !ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
                        if (args.Parameters.Count > 1)
                        {
                            var house = HouseTools.GetHouseByName(args.Parameters[1]);
                            if (house == null) { ply.SendErrorMessage("No such house!"); return; }
							string OwnerNames = "";
							string VisitorNames = "";
							for (int i = 0; i < house.Owners.Count; i++)
							{
								var ID = house.Owners[i];
								try { OwnerNames += (String.IsNullOrEmpty(OwnerNames) ? "" : ", ") + TShock.Users.GetUserByID(System.Convert.ToInt32(ID)).Name;}
								catch { }
							}
							for (int i = 0; i < house.Visitors.Count; i++)
							{
								var ID = house.Visitors[i];
								try { VisitorNames += (String.IsNullOrEmpty(VisitorNames) ? "" : ", ") + TShock.Users.GetUserByID(System.Convert.ToInt32(ID)).Name; }
								catch { }
							}
                            ply.SendMessage("House '" + house.Name + "':", Color.LawnGreen);
                            ply.SendMessage("Chat enabled: " + (house.ChatEnabled == 1 ? "yes" : "no"), Color.LawnGreen);
							ply.SendMessage("Locked: " + (house.Locked == 1 ? "yes" : "no"), Color.LawnGreen);
                            ply.SendMessage("Owners: " + OwnerNames, Color.LawnGreen);
							ply.SendMessage("Visitors: " + VisitorNames, Color.LawnGreen);
                            ply.SendMessage("World Mismatch: " + HouseTools.WorldMismatch(house).ToString(), Color.LawnGreen);
                        }
						else ply.SendErrorMessage("Invalid syntax! Proper syntax: /house info [house]");
                        break;
                    }
                case "lock":
                    {
						if (!ply.Group.HasPermission(LockHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (ply.Group.HasPermission("house.lock"))
                        {
                            if (args.Parameters.Count > 1)
                            {
                                string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                var house = HouseTools.GetHouseByName(houseName);
                                if (house == null) { ply.SendErrorMessage("No such house!"); return; }

                                if (HTools.OwnsHouse(ply.UserID.ToString(), house))
                                {
                                    bool locked = HouseTools.ChangeLock(house);
                                    ply.SendMessage("House: " + houseName + (locked ? " locked" : " unlocked"), Color.Yellow);
                                }
                                else
                                    ply.SendErrorMessage("You do not own House: " + houseName);
                            }
                            else
                            {
                                ply.SendErrorMessage("Invalid syntax! Proper syntax: /house lock [house]");
                            }
                        }
                        else
                        {
                            ply.SendErrorMessage("You do not have access to that command.");
                        }
                        break;
                    }
                case "reload":
                    {
                        if (ply.Group.HasPermission("house.root")) { HouseReload(args); }
                        break;
                    }
                case "chat":
                    {
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (args.Parameters.Count > 1)
                        {
                            var house = HouseTools.GetHouseByName(args.Parameters[1]);
                            if (house == null) { ply.SendErrorMessage("No such house!"); return; }
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
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(args.Parameters[2]);
                            if (house == null) { ply.SendErrorMessage("No such house!"); return; }
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (!HTools.CanVisitHouse(playerID.ID.ToString(), house))
                                    {
                                        if (HouseTools.AddNewVisitor(house, playerID.ID.ToString()))
                                        {
                                            ply.SendMessage("Added user " + playerName + " to " + houseName + " as a visitor.", Color.Yellow);
                                        }
                                        else
                                            ply.SendErrorMessage("House " + houseName + " not found");
                                    }
                                    else
                                    {
                                        ply.SendErrorMessage("Player " + playerName + " is already allowed to visit '" + house.Name + "'.");
                                    }
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
                case "delvisitor":
					{
						if (!ply.Group.HasPermission(UseHouse))
						{
							ply.SendErrorMessage("You do not have permission to use this command!");
							return;
						}
						if ((!ply.IsLoggedIn || ply.UserID == 0) && ply.RealPlayer)
                        {
                            ply.SendErrorMessage("You must log-in to use House Protection.");
                            return;
                        }
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(args.Parameters[2]);
                            if (house == null) { ply.SendErrorMessage("No such house!"); return; }
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(ply.UserID.ToString(), house.Name) || ply.Group.HasPermission(AdminHouse))
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (HouseTools.DeleteVisitor(house, playerID.ID.ToString()))
                                    {
                                        ply.SendMessage("Added user " + playerName + " to " + houseName + " as a visitor.", Color.Yellow);
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
                            ply.SendErrorMessage("Invalid syntax! Proper syntax: /house delvisitor [name] [house]");
                        break;
                    }
                default:
                    {
                        ply.SendMessage("To create a house, use these commands:", Color.Lime);
                        ply.SendMessage("/house set 1", Color.Lime);
                        ply.SendMessage("/house set 2", Color.Lime);
                        ply.SendMessage("/house add HouseName", Color.Lime);
						ply.SendMessage("Other /house commands: list, allow, disallow, redefine, name, delete, clear, info, chat, addvisitor, delvisitor, lock, reload", Color.Lime);
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

        public static void HouseReload(CommandArgs args)
        {
            HTools.SetupConfig();
            var reader = TShock.DB.QueryReader("Select * from HousingDistrict");
            Log.Info("House Config Reloaded");
            args.Player.SendMessage("House Config Reloaded", Color.Lime);
            HousingDistricts.Houses = new List<House>();
            while (reader.Read())
            {
                int id = reader.Get<int>("ID");
                string[] list = reader.Get<string>("Owners").Split(',');
                List<string> owners = new List<string>();
                foreach (string i in list)
                    owners.Add(i);
                int locked = reader.Get<int>("Locked");
                int chatenabled;
                if (reader.Get<int>("ChatEnabled") == 1) { chatenabled = 1; }
                else { chatenabled = 0; }
				list = reader.Get<string>("Visitors").Split(',');
                List<string> visitors = new List<string>();
                foreach (string i in list)
                    visitors.Add(i);
                HousingDistricts.Houses.Add(new House(new Rectangle(reader.Get<int>("TopX"), reader.Get<int>("TopY"), reader.Get<int>("BottomX"), reader.Get<int>("BottomY")),
                    owners, id, reader.Get<string>("Name"), reader.Get<string>("WorldID"), locked, chatenabled, visitors));
            }
            Log.Info("Houses Reloaded");
            args.Player.SendMessage("Houses Reloaded", Color.Lime);
        }
        public static void HouseWipe(CommandArgs args)
        {
            if (args.Parameters.Contains("true"))
            {
                HousingDistricts.Houses.Clear();
                try
                {
                    TShock.DB.Query("DELETE FROM HousingDistrict;");
					if (TShock.DB.GetSqlType() == SqlType.Sqlite) TShock.DB.Query("DELETE FROM sqlite_sequence WHERE name = 'HousingDistrict';");
					else TShock.DB.Query("ALTER TABLE HousingDistrict AUTO_INCREMENT = 1;");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                args.Player.SendMessage("All houses deleted!", Color.Lime);
            }
            else
            {
                args.Player.SendMessage("Do '/housewipe true' to confirm wipe.", Color.Lime);
            }
        }
    }
}
