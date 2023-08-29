using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace ThingBot.Commands
{
    [SlashCommandGroup("info", "Information commands, for info.")]
    public class InfoCommands : ApplicationCommandModule
    {
        [SlashCommand("ping", "Gets the latency of the bot")]
        public async Task PingCommand(InteractionContext ctx)
        {
            await Utils.DeferAsync(ctx); // Put the bot into a "thinking" state, giving us 15 minutes to make the embed and return it.

            var fields = new List<FieldData>()
            {
                new FieldData("Latency", $"{ctx.Client.Ping}ms")
            };
            var embed = Utils.MakeEmbed(DiscordColor.Cyan, "🏓 Pong!", fields: fields);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("botinfo", "View the information of the bot.")]
        public async Task BotInformationCommand(InteractionContext ctx)
        {
            await Utils.DeferAsync(ctx);

            var timeFromStartup = DateTime.Now - Globals.StartDate;
            var client = ctx.Client;
            var daysFromStartup = (int)timeFromStartup.TotalDays;
            DateTimeOffset timestamp = new(new DateTime(Globals.StartDate.Year, Globals.StartDate.Month, timeFromStartup.Days, timeFromStartup.Hours, timeFromStartup.Minutes, timeFromStartup.Seconds, timeFromStartup.Milliseconds, timeFromStartup.Microseconds));

            var fields = new List<FieldData>()
            {
                new FieldData("Guild Count", client.Guilds.Count.ToString(), true),
                new FieldData("Started", $"{(daysFromStartup == 0 ? $"Today ({(int)timeFromStartup.TotalHours} hours ago)" : $"{daysFromStartup} {(daysFromStartup == 1 ? "day" : "days")} ago")}", true),
                new FieldData(".NET Version", "v7.0", true),
                new FieldData("C# Version", "v11.0", true),
                new FieldData("Discord API Wrapper", Utils.MakeMarkdownLink("DSharpPlus", "https://dsharpplus.github.io", MarkdownFormat.Bolded), true)
            };
            var embed = Utils.MakeEmbed(DiscordColor.Cyan, $"{client.CurrentUser.Username} Info", fields: fields, timestamp: timestamp);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

        }
    }
}
