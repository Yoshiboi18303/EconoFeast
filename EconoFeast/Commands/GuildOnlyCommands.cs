using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace EconoFeast.Commands
{
    [Attributes.GuildOnly]
    [SlashCommandGroup("testing", "Some commands that have to be tested.")]
    public class GuildOnlyCommands : ApplicationCommandModule
    {
        [SlashCommand("bugreport", "Report a bug to the developer(s)")]
        public async Task ReportBugCommand(InteractionContext ctx, [Option("info", "Any additional info you would like to add, make sure to be descriptive")] string info)
        {
            await ctx.DeferAsync();

            var user = await Utils.GetUserAsync(ctx.User);

            if (user.IsReportBanned)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You are report banned, you cannot send in any reports!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }
        }

        [SlashCommand("sell", "Sell an item from your inventory")]
        public async Task SellItemCommand(InteractionContext ctx, [Option("id", "The item ID to sell")] string id, [Option("quantity", "How many of this item to sell, defaults to 1")] long quantity = 1)
        {
            await ctx.DeferAsync(true);

            if (!Globals.Items.ContainsKey(id))
            {
                var noItemEmbed = Utils.MakeErrorEmbed($"No item with an ID of `{id}` was found.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noItemEmbed));
                return;
            }

            var user = await Utils.GetUserAsync(ctx.User);
            var item = Globals.Items[id];

            if (!item.Sellable)
            {
                var badQuantityEmbed = Utils.MakeErrorEmbed("You cannot sell this item!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(badQuantityEmbed));
                return;
            }

            try
            {
                await item.SellAsync(user, Convert.ToInt32(quantity));
            }
            catch (ArgumentException)
            {

            }
        }
    }
}
