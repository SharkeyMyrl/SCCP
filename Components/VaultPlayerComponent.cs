using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace LandSharks
{
    public class VaultPlayerComponent : UnturnedPlayerComponent
    {
        public DateTime LastUsed = DateTime.MinValue;
        public List<Item> OldItems = new List<Item>();
        public Items VaultItems;
    }
}