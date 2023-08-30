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
            new DiscordActivity("The only economy bot you'll need, I think.", ActivityType.Playing),
            new DiscordActivity("for revenge", ActivityType.Playing),
            new DiscordActivity("Check the shop!", ActivityType.Playing),
            new DiscordActivity("the users duke it out", ActivityType.Watching)
        };
        public static List<ulong> NonParticipatingUsers { get; } = new();

        #endregion
    }
}
