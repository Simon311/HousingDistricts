﻿using System;
using System.Collections.Generic;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using System.IO;
using System.Reflection;

namespace HousingDistricts
{
    [ApiVersion(1, 14)]
    public class HousingDistricts : TerrariaPlugin
    {
        public static HConfigFile HConfig { get; set; }
        public static List<House> Houses = new List<House>();
        public static List<HPlayer> HPlayers = new List<HPlayer>();

        public override string Name
        {
            get { return "HousingDistricts"; }
        }
        public override string Author
        {
            get { return "Twitchy, Dingo, radishes, CoderCow and B4"; }
        }
        public override string Description
        {
			get { return "Housing Districts v." + Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }
        public override Version Version
        {
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

		// Done: Update Timer
		// Done: Manage permissions inside /house
		// Done: Use for on Lists instead of foreach
		// Note: Do NOT replace for, its faster for Lists than Foreach. Yes, there are studies proving that. No, there is no difference for arrays.

		// Done: Added house.count.[int], house.size.[int] #tested
		// Done: Added NotifyOwner, NotifyUser, NotifySelf #tested
		// Done: Check if a house Intersects any regions. #tested


		// TODO: Add UUID support --Not sure when will implement this
		// TODO: Add owners+builders instead of just owners --Same for this
		// TODO: Add Caching User Info?


		static readonly System.Timers.Timer Update = new System.Timers.Timer(500);

        public override void Initialize()
        {
            HTools.SetupConfig();

            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize, -5);
            ServerApi.Hooks.ServerChat.Register(this, OnChat, 5);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer, -5);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave, 5);
            ServerApi.Hooks.NetGetData.Register(this, GetData);
            GetDataHandlers.InitGetDataHandler();
			Update.Elapsed += OnUpdate;
			Update.Start();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.NetGetData.Deregister(this, GetData);
				Update.Dispose();
            }
            base.Dispose(disposing);
        }
        public HousingDistricts(Main game)
            : base(game)
        {
            HConfig = new HConfigFile();
            Order = 5;
        }

        public void OnInitialize(EventArgs e)
        {
            #region Setup
            bool sethouse = false;
            bool edithouse = false;
            bool enterlocked = false;
            bool adminhouse = false;
            bool bypasssize = false;
            bool bypasscount = false;
            bool hlock = false;

            foreach (Group group in TShock.Groups.groups)
            {
                if (group.Name != "superadmin")
                {
                    if (group.HasPermission("house.use"))
                        sethouse = true;
                    if (group.HasPermission("house.edit"))
                        edithouse = true;
                    if (group.HasPermission("house.enterlocked"))
                        enterlocked = true;
                    if (group.HasPermission("house.admin"))
                        adminhouse = true;
                    if (group.HasPermission("house.bypasscount"))
                        bypasscount = true;
                    if (group.HasPermission("house.bypasssize"))
                        bypasssize = true;
                    if (group.HasPermission("house.lock"))
                        hlock = true;
                }
            }

            List<string> trustedperm = new List<string>();
            List<string> defaultperm = new List<string>();

            if (!sethouse)
                defaultperm.Add("house.use");
            if (!edithouse)
                trustedperm.Add("house.edit");
            if (!enterlocked)
                trustedperm.Add("house.enterlocked");
            if (!adminhouse)
                trustedperm.Add("house.admin");
            if (!bypasscount)
                trustedperm.Add("house.bypasscount");
            if (!bypasssize)
                trustedperm.Add("house.bypasssize");
            if (!hlock)
                defaultperm.Add("house.lock"); 
            TShock.Groups.AddPermissions("trustedadmin", trustedperm);
            TShock.Groups.AddPermissions("default", defaultperm);

            var table = new SqlTable("HousingDistrict",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.VarChar, 255) { Unique = true },
                new SqlColumn("TopX", MySqlDbType.Int32),
                new SqlColumn("TopY", MySqlDbType.Int32),
                new SqlColumn("BottomX", MySqlDbType.Int32),
                new SqlColumn("BottomY", MySqlDbType.Int32),
                new SqlColumn("Owners", MySqlDbType.Text),
                new SqlColumn("WorldID", MySqlDbType.Text),
                new SqlColumn("Locked", MySqlDbType.Int32),
                new SqlColumn("ChatEnabled", MySqlDbType.Int32),
                new SqlColumn("Visitors", MySqlDbType.Text)
            );
            var SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter.EnsureExists(table);
        	var reader = TShock.DB.QueryReader("Select * from HousingDistrict");
			while( reader.Read() )
			{
				int id = reader.Get<int>("ID");
				string[] list = reader.Get<string>("Owners").Split(',');
				List<string> owners = new List<string>();
				foreach( string i in list)
					owners.Add( i );
				int locked = reader.Get<int>("Locked");
                int chatenabled;
                if (reader.Get<int>("ChatEnabled") == 1) { chatenabled = 1; }
                else { chatenabled = 0; }
				list = reader.Get<string>("Visitors").Split(',');
                List<string> visitors = new List<string>();
                foreach (string i in list)
                    visitors.Add(i);
				Houses.Add( new House( new Rectangle( reader.Get<int>("TopX"),reader.Get<int>("TopY"),reader.Get<int>("BottomX"),reader.Get<int>("BottomY") ), 
					owners, id, reader.Get<string>("Name"), reader.Get<string>("WorldID"), locked, chatenabled, visitors));
			}
            #endregion

			List<string> perms = new List<string>();
			perms.Add("house.use");
			perms.Add("house.lock");
			perms.Add("house.root");

            #region Commands
            Commands.ChatCommands.Add(new Command(perms, HCommands.House, "house"));
            Commands.ChatCommands.Add(new Command("tshock.canchat", HCommands.TellAll, "all"));
            Commands.ChatCommands.Add(new Command("house.root", HCommands.HouseReload, "housereload"));
            Commands.ChatCommands.Add(new Command("house.root", HCommands.HouseWipe, "housewipe"));
            #endregion
        }

		public void OnUpdate(object sender, ElapsedEventArgs e)
        {
			if (Main.worldID == 0) return;
			var Start = DateTime.Now;
			if (Main.rand == null) Main.rand = new Random();
                lock (HPlayers)
                {
					var I = HousingDistricts.HPlayers.Count;
					for (int i = 0; i < I; i++)
					{
						if (Timeout(Start, 2000)) return;
						var player = HousingDistricts.HPlayers[i];
                        List<string> NewCurHouses = new List<string>(player.CurHouses);
                        int HousesNotIn = 0;
                        try
                        {
							var J = HousingDistricts.Houses.Count;
							for (int j = 0; j < J; j++)
							{
								if (Timeout(Start, 2000)) return;
								var house = HousingDistricts.Houses[j];
                                try
                                {
                                        if (house.HouseArea.Intersects(new Rectangle(player.TSPlayer.TileX, player.TSPlayer.TileY, 1, 1)) && !HouseTools.WorldMismatch(house))
                                        {
                                            if (house.Locked == 1 && !player.TSPlayer.Group.HasPermission("house.enterlocked"))
                                            {
												if (!HTools.CanVisitHouse(player.TSPlayer.UserID.ToString(), house))
												{
													player.TSPlayer.Teleport((int)player.LastTilePos.X * 16, (int)player.LastTilePos.Y * 16);
													player.TSPlayer.SendMessage("House: '" + house.Name + "' Is locked", Color.LightSeaGreen);
												}
												else
												{
													if (!player.CurHouses.Contains(house.Name) && HConfig.NotifyOnEntry)
													{
														NewCurHouses.Add(house.Name);
														if (HTools.OwnsHouse(player.TSPlayer.UserID.ToString(), house.Name))
														{
															if (HConfig.NotifySelf)
															{
																player.TSPlayer.SendMessage(HConfig.NotifyOnOwnHouseEntryString.Replace("$HOUSE_NAME", house.Name), Color.LightSeaGreen);
															}
														}
														else
														{
															if (HConfig.NotifyVisitor)
															{
																player.TSPlayer.SendMessage(HConfig.NotifyOnEntryString.Replace("$HOUSE_NAME", house.Name), Color.LightSeaGreen);
															}
															if (HConfig.NotifyOwner)
															{
																HTools.BroadcastToHouseOwners(house.Name, HConfig.NotifyOnOtherEntryString.Replace("$PLAYER_NAME", player.TSPlayer.Name).Replace("$HOUSE_NAME", house.Name));
															}
														}
													}
												}
                                            }
                                            else
                                            {
												if (!player.CurHouses.Contains(house.Name) && HConfig.NotifyOnEntry)
												{
													NewCurHouses.Add(house.Name);
													if (HTools.OwnsHouse(player.TSPlayer.UserID.ToString(), house.Name))
													{
														if(HConfig.NotifySelf)
														{
														player.TSPlayer.SendMessage(HConfig.NotifyOnOwnHouseEntryString.Replace("$HOUSE_NAME", house.Name), Color.LightSeaGreen);
														}
													}
													else
													{
														if (HConfig.NotifyVisitor)
														{
															player.TSPlayer.SendMessage(HConfig.NotifyOnEntryString.Replace("$HOUSE_NAME", house.Name), Color.LightSeaGreen);
														}
														if (HConfig.NotifyOwner)
														{
															HTools.BroadcastToHouseOwners(house.Name, HConfig.NotifyOnOtherEntryString.Replace("$PLAYER_NAME", player.TSPlayer.Name).Replace("$HOUSE_NAME", house.Name));
														}
													}
												}
                                            }
                                        }
                                        else
                                        {
                                            NewCurHouses.Remove(house.Name);
                                            HousesNotIn++;
                                        }
                                    
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.ToString());
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            continue;
                        }

                        if (HConfig.NotifyOnExit)
                        {
                            {
								var K = player.CurHouses.Count;
								for (int k = 0; k < K; k++)
								{
									var cHouse = player.CurHouses[k];
									if (!NewCurHouses.Contains(cHouse))
									{
										if (HTools.OwnsHouse(player.TSPlayer.UserID.ToString(), cHouse))
										{
											if (HConfig.NotifySelf)
											{
												player.TSPlayer.SendMessage(HConfig.NotifyOnOwnHouseExitString.Replace("$HOUSE_NAME", cHouse), Color.LightSeaGreen);
											}
										}
										else
										{
											if (HConfig.NotifyVisitor)
											{
												player.TSPlayer.SendMessage(HConfig.NotifyOnExitString.Replace("$HOUSE_NAME", cHouse), Color.LightSeaGreen);
											}
											if (HConfig.NotifyOwner)
											{
												HTools.BroadcastToHouseOwners(cHouse, HConfig.NotifyOnOtherExitString.Replace("$PLAYER_NAME", player.TSPlayer.Name).Replace("$HOUSE_NAME", cHouse));
											}
										}
									}
								}
                            }
                            
                        }
                        player.CurHouses = NewCurHouses;
                        player.LastTilePos = new Vector2(player.TSPlayer.TileX, player.TSPlayer.TileY);
                    }
                }
            

        }
        public void OnChat(ServerChatEventArgs e)
        {
			var Start = DateTime.Now;
            var msg = e.Buffer;
            var ply = e.Who;
            var text = e.Text;

            if (!e.Handled)
            {
                if (HConfig.HouseChatEnabled)
                {
                    if (text[0] == '/')
                        return;

                    var tsplr = TShock.Players[msg.whoAmI];
					var I = HousingDistricts.Houses.Count;
					for (int i = 0; i < I; i++)
					{
						if (Timeout(Start)) return;
						var house = HousingDistricts.Houses[i];
						if (!HouseTools.WorldMismatch(house) && house.ChatEnabled == 1 && house.HouseArea.Intersects(new Rectangle(tsplr.TileX, tsplr.TileY, 1, 1)))
                        {
                            HTools.BroadcastToHouse(house, text, tsplr.Name);
                            e.Handled = true;
                        }
                    }
                }
            }
        }
        public void OnGreetPlayer( GreetPlayerEventArgs e)
        {
            lock (HPlayers)
                HPlayers.Add(new HPlayer(e.Who, new Vector2(TShock.Players[e.Who].TileX, TShock.Players[e.Who].TileY)));
        }
        public void OnLeave(LeaveEventArgs args)
        {
			var Start = DateTime.Now;
            lock (HPlayers)
            {
				var I = HPlayers.Count;
                for (int i = 0; i < I; i++)
                {
					if (Timeout(Start)) return;
                    if (HPlayers[i].Index == args.Who)
                    {
                        HPlayers.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        private void GetData(GetDataEventArgs e)
        {
            PacketTypes type = e.MsgID;
            var player = TShock.Players[e.Msg.whoAmI];
            if (player == null)
            {
                e.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                e.Handled = true;
                return;
            }

            using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
            {
                try
                {
                    if (GetDataHandlers.HandlerGetData(type, player, data))
                        e.Handled = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
		public static bool Timeout(DateTime Start, int ms = 1000)
		{
			bool ret = (Start - DateTime.Now).TotalMilliseconds > ms;
			if (ret) 
			{ 
				TSPlayer.Server.SendErrorMessage("Hook timeout detected in HousingDisricts. You might want to report this.");
				Log.Error("Hook timeout detected in HousingDisricts. You might want to report this.");
			}
			return ret;
		}
    }
}