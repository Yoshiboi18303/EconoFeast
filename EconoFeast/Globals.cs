using DSharpPlus.Entities;
using Supabase;

namespace ThingBot
{
    public static class Globals
    {
        #region Constants

        public const ulong OwnerId = 697414293712273408;
        public const uint BaseBonesNumber = 206;

        #endregion

        #region Class Fields

        public static DateTime StartDate { get; set; }
        public static Client SupabaseClient { get; set; }
        public static Dictionary<string, ShopItem> Items { get; set; }
        public static List<DiscordActivity> Activities { get; } = new()
        {
            new DiscordActivity("AI fucking", ActivityType.Watching),
            new DiscordActivity("Backed up by a couple friends!", ActivityType.Playing),
            new DiscordActivity("The only economy bot you'll need, I think.", ActivityType.Playing)
        };

        #endregion
    }
}
