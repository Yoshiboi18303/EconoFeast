using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System.Web;

namespace EconoFeast.Commands
{
    [SlashCommandGroup("other", "Other commands")]
    public class OtherCommands : ApplicationCommandModule
    {
        public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
        {
            return await Utils.PreFlightChecks(ctx);
        }

        [SlashCommand("suggest", "Suggest something for the bot")]
        public async Task SuggestCommand(InteractionContext ctx, [Option("suggestion", "Your suggestion text, make sure to be descriptive")] string text)
        {
            await ctx.DeferAsync();

            using var httpClient = new HttpClient();

            var cardTitle = HttpUtility.UrlEncode($"{ctx.User.Username}'s Suggestion");
            var suggestionText = HttpUtility.UrlEncode(text);

            var url = $"{Globals.BaseTrelloApiUrl}/cards?idList=64f12a31b1a7f7f4f45853cb&name={cardTitle}&desc={suggestionText}&idLabels=64f23d57b08ca06518753633&key={Globals.Configuration.TrelloKey}&token={Globals.Configuration.TrelloToken}";
            var response = await httpClient.PostAsync(url, null);

            var content = await response.Content.ReadAsStringAsync();
            var linkModel = JsonConvert.DeserializeObject<TrelloCreatedCardLink>(content);

            if (!response.IsSuccessStatusCode)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"Failed to post suggestion to Trello board!\n\n**Status code:** {(int)response.StatusCode}");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var logChannel = await ctx.Client.GetChannelAsync(Globals.BotLogChannelId);

            var embed = Utils.MakeEmbed(DiscordColor.Cyan, "New Suggestion", $"A new suggestion was received, **{Utils.EvaluateBool(linkModel is not null, Utils.MakeMarkdownLink("View it here", linkModel!.Url, MarkdownFormat.Bolded), "No link given")}**");
            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, "Success", $"Your suggestion was posted to the official Trello board!\n\n{Utils.EvaluateBool(linkModel is not null, Utils.MakeMarkdownLink("View it here", linkModel!.Url, MarkdownFormat.Bolded), "No link given")}");

            await logChannel.SendMessageAsync(embed);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
        }
    }
}
