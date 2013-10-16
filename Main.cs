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

namespace PvPCommandBlock
{
    [ApiVersion(1, 14)]
    public class Maincs : TerrariaPlugin
    {
        public static bool isToggled;
        public static Config config { get; set; }
        public static string configPath { get { return Path.Combine(TShock.SavePath, "PvPCmdConfig.json"); } }

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
        }

        public void onInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("pvp.block", Toggle, "toggleblock"));
            Commands.ChatCommands.Add(new Command("pvp.reload", Reload, "reloadexempts"));

            SetUpConfig();
        }

        public void OnChat(ServerChatEventArgs args)
        {
            if (isToggled)
                if (args.Text.StartsWith("/") && !TShock.Players[args.Who].Group.HasPermission("pvp.block") &&
                    TShock.Players[args.Who].TPlayer.hostile && !config.ExemptCommands.Contains(args.Text))
                {
                    TShock.Players[args.Who].SendErrorMessage("That command is blocked while in PvP!");
                    args.Handled = true;
                }
        }

        public void Toggle(CommandArgs args)
        {
            isToggled = !isToggled;

            args.Player.SendSuccessMessage((isToggled ? "B" : "Unb") + "locked commands while in PvP");
        }

        public void Reload(CommandArgs args)
        {
            SetUpConfig();
            args.Player.SendInfoMessage("Attempted to reload the config file");
        }

        public void SetUpConfig()
        {
            try
            {
                if (File.Exists(configPath))
                    config = Config.Read(configPath);
                else
                    config.Write(configPath);
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
