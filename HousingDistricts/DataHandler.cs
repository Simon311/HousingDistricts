using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Terraria;
using TShockAPI;
using TShockAPI.Net;
using System.IO.Streams;

namespace HousingDistricts
{
    public delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
    public class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public Player TPlayer
        {
            get { return Player.TPlayer; }
        }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }
    public static class GetDataHandlers
    {
        static string EditHouse = "house.edit";
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

        public static void InitGetDataHandler()
        {
            GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.Tile, HandleTile},
                {PacketTypes.TileSendSquare, HandleSendTileSquare},
                {PacketTypes.TileKill, HandleTileKill},
                {PacketTypes.LiquidSet, HandleLiquidSet},
            };
        }

        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (GetDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return false;
        }

        private static bool HandleSendTileSquare(GetDataHandlerArgs args)
        {
            short size = args.Data.ReadInt16();
            int tilex = args.Data.ReadInt32();
            int tiley = args.Data.ReadInt32();

            if (!args.Player.Group.HasPermission(EditHouse))
            {
                lock (HousingDistricts.HPlayers)
                {
                    foreach (House house in HousingDistricts.Houses)
                    {
                        if (house != null && house.HouseArea.Intersects(new Rectangle(tilex, tiley, 1, 1)) && house.WorldID == Main.worldID.ToString())
                        {
                            if (!HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name))
                            {
                                args.Player.SendTileSquare(tilex, tiley);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool HandleTile(GetDataHandlerArgs args)
        {
            byte type = args.Data.ReadInt8();
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            byte tiletype = args.Data.ReadInt8();
            var player = HTools.GetPlayerByID(args.Player.Index);

            int tilex = Math.Abs(x);
            int tiley = Math.Abs(y);

            if (player.AwaitingHouseName)
            {
                if (HTools.InAreaHouseName(x, y) == null)
                {
                    args.Player.SendMessage("Tile is not in any House", Color.Yellow);
                }
                else
                {
                    args.Player.SendMessage("House Name: " + HTools.InAreaHouseName(x, y), Color.Yellow);
                }
                args.Player.SendTileSquare(x, y);
                player.AwaitingHouseName = false;
                return true;
            }

            if (args.Player.AwaitingTempPoint > 0)
            {
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].X = x;
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].Y = y;
                if (args.Player.AwaitingTempPoint == 1)
                {
                    args.Player.SendMessage("Top-left corner of protection area has been set!", Color.Yellow);
                }
                if (args.Player.AwaitingTempPoint == 2)
                {
                    args.Player.SendMessage("Bottom-right corner of protection area has been set!", Color.Yellow);
                }

                args.Player.SendTileSquare(x, y);
                args.Player.AwaitingTempPoint = 0;
                return true;
            }

            if (!args.Player.Group.HasPermission(EditHouse))
            {
                lock (HousingDistricts.HPlayers)
                {
                    foreach (House house in HousingDistricts.Houses)
                    {
                        if (house != null && house.HouseArea.Intersects(new Rectangle(tilex, tiley, 1, 1)) && house.WorldID == Main.worldID.ToString())
                        {
                            if (!HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name))
                            {
                                args.Player.SendTileSquare(tilex, tiley);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool HandleLiquidSet(GetDataHandlerArgs args)
        {
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            int plyX = Math.Abs(args.Player.TileX);
            int plyY = Math.Abs(args.Player.TileY);
            int tilex = Math.Abs(x);
            int tiley = Math.Abs(y);

            if (!args.Player.Group.HasPermission(EditHouse))
            {
                lock (HousingDistricts.HPlayers)
                {
                    foreach (House house in HousingDistricts.Houses)
                    {
                        if (house != null && house.HouseArea.Intersects(new Rectangle(tilex, tiley, 1, 1)) && house.WorldID == Main.worldID.ToString())
                        {
                            if (!HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name))
                            {
                                args.Player.SendTileSquare(tilex, tiley);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool HandleTileKill(GetDataHandlerArgs args)
        {
            int x = args.Data.ReadInt32();
            int y = args.Data.ReadInt32();
            int tilex = Math.Abs(x);
            int tiley = Math.Abs(y);
            var player = HTools.GetPlayerByID(args.Player.Index);

            if (player.AwaitingHouseName)
            {
                if (HTools.InAreaHouseName(x, y) == null)
                {
                    args.Player.SendMessage("Tile is not in any House", Color.Yellow);
                }
                else
                {
                    args.Player.SendMessage("House Name: " + HTools.InAreaHouseName(x, y), Color.Yellow);
                }
                args.Player.SendTileSquare(x, y);
                player.AwaitingHouseName = false;
                return true;
            }

            if (args.Player.AwaitingTempPoint > 0)
            {
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].X = x;
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].Y = y;
                if (args.Player.AwaitingTempPoint == 1)
                {
                    args.Player.SendMessage("Top-left corner of protection area has been set!", Color.Yellow);
                }
                if (args.Player.AwaitingTempPoint == 2)
                {
                    args.Player.SendMessage("Bottom-right corner of protection area has been set!", Color.Yellow);
                }

                args.Player.SendTileSquare(x, y);
                args.Player.AwaitingTempPoint = 0;
                return true;
            }

            if (!args.Player.Group.HasPermission(EditHouse))
            {
                lock (HousingDistricts.HPlayers)
                {
                    foreach (House house in HousingDistricts.Houses)
                    {
                        if (house != null && house.HouseArea.Intersects(new Rectangle(tilex, tiley, 1, 1)) && house.WorldID == Main.worldID.ToString())
                        {
                            if (!HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name))
                            {
                                args.Player.SendTileSquare(tilex, tiley);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}