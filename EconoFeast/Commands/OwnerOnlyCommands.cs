using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

namespace EconoFeast.Commands
{
    [SlashCommandGroup("owner", "Commands only the owner of the bot can execute.")]
    public class OwnerOnlyCommands : ApplicationCommandModule
    {
        [SlashCommand("syncitems", "Sync all users items")]
        public async Task SyncItemsCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var users = await Utils.GetAllUsersAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Syncing user items, this may take a while..."));

            foreach (var user in users)
            {
                await Utils.UpdateUserAsync(user.Id, x => x.Items, new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison,
                    SecretRevealer = user.Items.SecretRevealer,
                });
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("User items synced!"));
        }

        [SlashCommand("viewcode", "View a file from the bot")]
        public async Task ViewFileCodeCommand(InteractionContext ctx, [Option("path", "The path to the file")] string path)
        {
            await ctx.DeferAsync();

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

        [SlashCommand("setprice", "Change the global price of an item")]
        public async Task SetPriceCommand(InteractionContext ctx, [Option("id", "The ID of the item to update")] string id, [Option("price", "The new price of the item")] long price)
        {
            await ctx.DeferAsync();

            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var currentConfig = Globals.Configuration;
            var prices = currentConfig.Prices;

            ShopItem item;
            
            try
            {
                item = Globals.Items[id];

                // Test to make sure the item exists (if it doesn't, this will throw a KeyNotFoundException).
                var _ = item.Name;
            }
            catch (KeyNotFoundException)
            {
                var noItemEmbed = Utils.MakeErrorEmbed($"No item with an ID of `{id}` was found.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noItemEmbed));
                return;
            }

            var oldPrice = item.Price;

            if (price < 0)
            {
                var badPriceEmbed = Utils.MakeErrorEmbed("Price cannot be lower than 0!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(badPriceEmbed));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Writing configuration file..."));

            var newPrices = prices;

            newPrices[newPrices.FindIndex(0, x => x.Id == id)].Price = Convert.ToUInt64(price);

            var config = new Configuration()
            {
                Token = currentConfig.Token,
                SupabaseKey = currentConfig.SupabaseKey,
                SupabaseUrl = currentConfig.SupabaseUrl,
                RadarcordKey = currentConfig.RadarcordKey,
                Prices = newPrices
            };

            var serialized = JsonConvert.SerializeObject(config, Formatting.Indented);

            #region Get Config File Path

            string path = string.Empty;

            foreach (var part in Utils.GetBasePath())
            {
                path += part;
            }

            path += "config.json";

            #endregion

            await File.WriteAllTextAsync(path, serialized);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Retrieving new config..."));

            Globals.Configuration = await Utils.GetConfigurationAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Updating items..."));

            Globals.Items = Utils.SetupItems();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Done! **{item.Name}**'s price has been updated from **{oldPrice}** bones to **{price}** bones!"));
        }
    }
}
