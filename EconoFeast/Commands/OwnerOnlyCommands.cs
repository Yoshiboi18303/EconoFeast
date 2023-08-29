using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace ThingBot.Commands
{
    [SlashCommandGroup("owner", "Commands only the owner of the bot can execute.")]
    public class OwnerOnlyCommands : ApplicationCommandModule
    {
        [SlashCommand("updateinstomach", "Updates the inStomach property of a user")]
        public async Task UpdateInStomachCommand(InteractionContext ctx, [Option("value", "The value of inStomach")] bool value, [Option("user", "The user to update")] DiscordUser? userToUpdate = null)
        {
            await Utils.DeferAsync(ctx);

            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var discordUser = userToUpdate ?? ctx.User;

            await Utils.UpdateUserAsync(discordUser, x => x.IsInStomach, value);

            var embed = Utils.MakeEmbed(DiscordColor.Green, "Success", "The inStomach property should've been set, hopefully.");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("viewcode", "View a file from the bot")]
        public async Task ViewFileCodeCommand(InteractionContext ctx, [Option("path", "The path to the file")] string path)
        {
            await Utils.DeferAsync(ctx);

            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            string contents;

            var solutionDirectoryPath = @"C:\Users\yoshi\OneDrive\Desktop\EconoFeast";
            var filePath = path.Replace("_solution", solutionDirectoryPath).Replace("_project", @$"{solutionDirectoryPath}\EconoFeast").Replace('/', '\\');
            var pathSplit = filePath.Split('.');
            var extension = pathSplit[^1];

            try
            {
                contents = await File.ReadAllTextAsync(filePath);
            }
            catch (FileNotFoundException)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"Couldn't find a file with the path of `{filePath}`");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }
            catch (IOException ex)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"An error occurred while reading the file: `{ex.Message}`");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (contents.Length > 4096)
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("That file is too long for an embed, so here's a file!")
                    .AddFile($"code.{extension}", fs)
                );
                return;
            }

            var embed = Utils.MakeEmbed(DiscordColor.Cyan, "File code", $"```{extension}\n{contents}\n```", footer: new DiscordEmbedBuilder.EmbedFooter()
            {
                Text = $"Path: {filePath}"
            });
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
