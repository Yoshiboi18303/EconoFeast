using DSharpPlus;
using DSharpPlus.Entities;
using Radarcord;
using Supabase;

namespace EconoFeast
{
    public static class Globals
    {
        #region Constants

        public const ulong OwnerId = 697414293712273408;
        public const ulong BotLogChannelId = 1097513507441877072;
        public const uint BaseBonesNumber = 206;
        public const ulong DailyAmount = 500;
        public const string BaseTrelloApiUrl = "https://api.trello.com/1";
        public const string TwitchUrl = "https://twitch.tv/yoshiboi18303";

        #endregion

        #region Fields

        public static DateTime StartDate { get; set; }
        public static Client SupabaseClient { get; set; }
        public static Dictionary<string, ShopItem> Items { get; set; }
        public static List<DiscordActivity> Activities { get; } = new()
        {
            new DiscordActivity("The only economy bot you'll need, I think.", ActivityType.Playing),
            new DiscordActivity("for revenge", ActivityType.Playing),
            new DiscordActivity("Check the shop!", ActivityType.Playing),
            new DiscordActivity("the users duke it out", ActivityType.Watching),
            Utils.MakeStreamingActivity("FREE FIGHT, COME WATCH - !discord !8ball", TwitchUrl)
        };
        public static Configuration Configuration { get; set; }
        public static GuildOptions DefaultGuildOptions { get; } = new()
        {
            UsesLanguageFilter = false
        };

        #endregion

        #region Methods

        public static RadarcordClient GetRadarcordClient(DiscordClient client) => new(client, Configuration.RadarcordKey);

        public static RadarcordAutomater GetRadarcordAutomater(DiscordClient client) => new(client, Configuration.RadarcordKey);

        #endregion
    }
}
