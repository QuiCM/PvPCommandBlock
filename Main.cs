using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace PvPCommandBlock
{
    [ApiVersion(1, 14)]
    public class Maincs : TerrariaPlugin
    {
        public static Config config { get; set; }
        public static rConfig rConfig { get; set; }
        public static string directoryPath { get { return Path.Combine(TShock.SavePath, "PvPBlock"); } }
        public static string configPath { get { return Path.Combine(directoryPath, "PvPCmdConfig.json"); } }
        public static string regionPath { get { return Path.Combine(directoryPath, "PvPRegions.json"); } }

        public static List<TShockAPI.DB.Region> confirmedRegions = new List<TShockAPI.DB.Region>();
        
        public static List<regionObj> regions = new List<regionObj>() 
        { new regionObj("example", false) };

        public override string Author
        { get { return "WhiteX, aMoka"; } }

        public override string Description
        { get { return "Blocks commands while in PvP"; } }

        public override string Name
        { get { return "PvPCmdBlock"; } }

        public override Version Version
        { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var Hook = ServerApi.Hooks;

                Hook.ServerChat.Deregister(this, OnChat);
                Hook.GameInitialize.Deregister(this, onInitialize);
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            var Hook = ServerApi.Hooks;

            Hook.ServerChat.Register(this, OnChat);
            Hook.GameInitialize.Register(this, onInitialize);
        }


        public Maincs(Main game)
            : base(game)
        {
            Order = 100;

            config = new Config();
            rConfig = new rConfig();
        }

        #region Initialize
        public void onInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("pvp.block", Toggle, "toggleblock"));
            Commands.ChatCommands.Add(new Command("pvp.reload", Reload, "reloadexempts"));
            Commands.ChatCommands.Add(new Command("pvp.region", BlockRegion, "blockreg"));

            SetUpConfig();

            foreach (TShockAPI.DB.Region region in TShock.Regions.Regions)
            {
                for (int i = 0; i < rConfig.RegionConfiguration.Count; i++)
                {
                    for (int j = 0; j < rConfig.RegionConfiguration[i].regionList.Count; j++)
                    {
                        if (rConfig.RegionConfiguration[i].regionList[j].Name == region.Name)
                        {
                            confirmedRegions.Add(region);
                        }
                    }
                }
            }
        }
        #endregion

        #region OnChat
        public void OnChat(ServerChatEventArgs args)
        {
            var player = TShock.Players[args.Who];

            if (config.toggleAll)
            {
                if (args.Text.StartsWith("/") && !player.Group.HasPermission("pvp.block") &&
                    player.TPlayer.hostile && !config.ExemptCommands.Contains(args.Text))
                {
                    player.SendErrorMessage("That command is blocked while in PvP!");
                    args.Handled = true;
                }
            }
            else if (config.toggleRegions)
            {
                Region reg = TShock.Regions.GetTopRegion(TShock.Regions.InAreaRegion(player.TileX, player.TileY));

                if (reg != null)
                {
                    if (getRegionObj(reg) != null)
                    {
                        var objReg = getRegionObj(reg);

                        if (objReg.BlockCmds)
                            if (args.Text.StartsWith("/") && !player.Group.HasPermission("pvp.block") &&
                                player.TPlayer.hostile && !config.ExemptCommands.Contains(args.Text))
                            {
                                player.SendErrorMessage("That command is blocked while in PvP in this region!");
                                args.Handled = true;
                            }
                    }
                }
            }
        }
        #endregion

        #region getRegionObj
        public regionObj getRegionObj(Region region)
        {
            for (int i = 0; i < rConfig.RegionConfiguration.Count; i++)
            {
                foreach (regionObj obj in rConfig.RegionConfiguration[i].regionList)
                {
                    if (obj.Name == region.Name)
                        return obj;
                }
            }
            return null;
        }
        #endregion

        #region ToggleGlobal
        public void Toggle(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                config.toggleAll = !config.toggleAll;

                args.Player.SendSuccessMessage("Globally " + (config.toggleAll ? "b" : "unb") + "locked commands while in PvP");
            }
            else if (args.Parameters.Count > 0)
            {
                if (args.Parameters[0] == "all")
                {
                    config.toggleAll = !config.toggleAll;

                    args.Player.SendSuccessMessage("Globally " + (config.toggleAll ? "b" : "unb") + "locked commands while in PvP");
                }
                else if (args.Parameters[0] == "region")
                {
                    config.toggleRegions = !config.toggleRegions;

                    args.Player.SendSuccessMessage("Regionally " + (config.toggleRegions ? "b" : "unb") + "locked commands while in PvP");
                }
            }
        }
        #endregion

        #region BlockRegion
        public void BlockRegion(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax. Try /blockreg <add/del/toggle> <regionName> [optional(true/false)]");
                return;
            }
            else
            {
                #region Add
                if (args.Parameters[0] == "add")
                {
                    try
                    {
                        string getReg = args.Parameters[1];

                        if (TShock.Regions.GetRegionByName(getReg) != null)
                        {
                            Region newReg = TShock.Regions.GetRegionByName(getReg);
                            regionObj newRegObj = getRegionObj(newReg);

                            bool toggle;
                            if (bool.TryParse(args.Parameters[2], out toggle))
                            {
                                newRegObj.Name = getReg;
                                newRegObj.BlockCmds = toggle;
                                regions.Add(newRegObj);

                                SetUpConfig();
                            }
                            args.Player.SendSuccessMessage("Added a new PvP command blocking region");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Invalid region: Non-existant");
                        }
                    }

                    catch (Exception x)
                    {
                        Log.ConsoleError(x.ToString());
                        args.Player.SendErrorMessage("Something broke. Check logs or console for more info");
                    }
                }
                #endregion

                #region Del
                else if (args.Parameters[0] == "del")
                {
                    try
                    {
                        string getReg = args.Parameters[1];

                        if (TShock.Regions.GetRegionByName(getReg) != null)
                        {
                            regions.RemoveAll(r => r.Name == getReg);

                            SetUpConfig();

                            args.Player.SendSuccessMessage("Deleted PvP command blocking region");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Invalid region: Non-existant");
                        }
                    }

                    catch (Exception x)
                    {
                        Log.ConsoleError(x.ToString());
                        args.Player.SendErrorMessage("Something broke. Check logs or console for more info");
                    }
                }
                #endregion

                #region Toggle
                else if (args.Parameters[0] == "toggle")
                {
                    try
                    {
                        string getReg = args.Parameters[1];

                        if (TShock.Regions.GetRegionByName(getReg) != null)
                        {
                            regionObj changeReg = getRegionObj(TShock.Regions.GetRegionByName(getReg));

                            if (regions.Contains(changeReg))
                            {
                                changeReg.BlockCmds = !changeReg.BlockCmds;

                                SetUpConfig();
                            }
                            else
                            {
                                args.Player.SendErrorMessage("Invalid region: Not defined in PvPRegions.json");
                            }
                        }
                        else
                            args.Player.SendErrorMessage("Invalid region: Non-existant");
                    }
                    catch (Exception x)
                    {
                        Log.ConsoleError(x.ToString());
                        args.Player.SendErrorMessage("Something broke. Check logs or console for more info");
                    }
                }
                #endregion
            }
        }
        #endregion

        public void Reload(CommandArgs args)
        {
            SetUpConfig();
            args.Player.SendInfoMessage("Attempted to reload the config file");
        }

        public void SetUpConfig()
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                if (File.Exists(configPath))
                    config = Config.Read(configPath);
                else
                    config.Write(configPath);

                if (File.Exists(regionPath))
                    rConfig = rConfig.Read(regionPath);
                else
                    rConfig.Write(regionPath);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in PvPBlock.json!");
                Console.ResetColor();
            }
        }
    }
}
