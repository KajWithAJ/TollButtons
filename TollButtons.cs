using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("Toll Buttons", "KajWithAJ", "0.0.7")]
    [Description("Make players pay toll to press a button using their RP points.")]
    class TollButtons : RustPlugin {

        [PluginReference]
        private Plugin ServerRewards;

        private const string PermissionUse = "tollbuttons.use";
        private const string PermissionAdmin = "tollbuttons.admin";
        private const string PermissionExclude = "tollbuttons.exclude";

        private StoredData storedData = new StoredData();

        private void Init() {
            permission.RegisterPermission(PermissionUse, this);
            permission.RegisterPermission(PermissionAdmin, this);
            permission.RegisterPermission(PermissionExclude, this);
        }

        private void OnServerInitialized() {
            if (ServerRewards == null) {
                PrintError("ServerRewards is not loaded");
            }

            SaveConfig();
            LoadData();
        }

        private void Unload()
        {
            SaveData();
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new configuration file");
            Config["TransferTollToOwner"] = false;
            Config["MaximumPrice"] = 0;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ButtonNotFound"] = "No button found",
                ["NoTollSetPermission"] = "You do not have permission to use this command.",
                ["NoButtonOwnership"] = "This button is not yours.",
                ["TollSet"] = "A toll of {0} RP was set for this button.",
                ["InsufficientFunds"] = "Insufficient fonds! You need {0} RP to press this button",
                ["TollPaid"] = "You've been charged {0} RP for pressing this button",
                ["MaximumPrice"] = "The maximum amount to configure as toll is set at {0}",
                ["InvalidNumber"] = "Provide a valid number."
            }, this);
        }

        [ChatCommand("toll")]
        private void ChatCmdCheckButton(BasePlayer player, string command, string[] args)
        {
            RaycastHit hit;
            var raycast = Physics.Raycast(player.eyes.HeadRay(), out hit, 2f, 2097409);
            BaseEntity button = raycast ? hit.GetEntity() : null;
            if (button == null || button as PressButton == null) {
                string message = lang.GetMessage("ButtonNotFound", this, player.UserIDString);
                player.ChatMessage(string.Format(message));
                return;
            }

            if (args.Length >=1 ) {
                if (!permission.UserHasPermission(player.UserIDString, PermissionUse)) {
                    string message = lang.GetMessage("NoTollSetPermission", this, player.UserIDString);
                    player.ChatMessage(string.Format(message));
                    return;
                }
                
                if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && button.OwnerID != player.userID) {
                    player.ChatMessage(string.Format(lang.GetMessage("NoButtonOwnership", this, player.UserIDString)));
                    return;
                }

                int cost = 0;
                if (!int.TryParse(args[0], out cost))
                {
                    player.ChatMessage(string.Format(lang.GetMessage("InvalidNumber", this, player.UserIDString)));
                    return;
                }

                int maximumPrice = (int) Config["MaximumPrice"];

                if (maximumPrice > 0 && cost > maximumPrice) {
                    player.ChatMessage(string.Format(lang.GetMessage("MaximumPrice", this, player.UserIDString), maximumPrice));
                    return;
                }

                if (!storedData.TollButtons.ContainsKey(button.net.ID)) {
                    ButtonData buttonData = new ButtonData();
                    buttonData.cost = cost;
                    buttonData.ownerID = player.UserIDString;
                    storedData.TollButtons.Add(button.net.ID, buttonData);
                } else {
                    storedData.TollButtons[button.net.ID].cost = cost;
                }

                player.ChatMessage(string.Format(lang.GetMessage("TollSet", this, player.UserIDString), cost));
            } else {
                int cost = CheckButtonCost(button as PressButton);
                player.ChatMessage(string.Format(lang.GetMessage("TollSet", this, player.UserIDString), cost));
            }
        }

        private object OnButtonPress(PressButton button, BasePlayer player)
        {
            if (button.OwnerID == 0) return null;

            int cost = CheckButtonCost(button);

            if (cost > 0) {
                if (permission.UserHasPermission(player.UserIDString, PermissionExclude)) {
                    return null;
                }

                if (button.OwnerID != player.userID) {
                    int balance = (int) ServerRewards?.Call("CheckPoints", player.userID);
                    if (cost > balance) {
                        player.ChatMessage(string.Format(lang.GetMessage("InsufficientFunds", this, player.UserIDString), cost));
                        return true;
                    } else {
                        ServerRewards?.Call("TakePoints", player.userID, cost);
                        player.ChatMessage(string.Format(lang.GetMessage("TollPaid", this, player.UserIDString), cost));

                        if ((bool) Config["TransferTollToOwner"] == true) {
                            ServerRewards?.Call("AddPoints", button.OwnerID, cost);
                        }

                        return null;
                    }
                } else {
                    return null;
                }
            } else {
                return null;
            }
        }

        private int CheckButtonCost(PressButton button) {
            if (!storedData.TollButtons.ContainsKey(button.net.ID)) {
                return 0;
            } else {
                return storedData.TollButtons[button.net.ID].cost;
            }
        }

        private class StoredData
        {
            public readonly Dictionary<uint, ButtonData> TollButtons = new Dictionary<uint, ButtonData>();
        }

        private class ButtonData
        {
            public int cost = 0;
            public string ownerID = "";
        }

        private void LoadData() => storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Name);

        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject(this.Name, storedData);

        private void OnServerSave() => SaveData();

        private void OnNewSave(string name)
        {
            PrintWarning("Map wipe detected - clearing TollButtons...");

            storedData.TollButtons.Clear();
            SaveData();
        }
    }
}
