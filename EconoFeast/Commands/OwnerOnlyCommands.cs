using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using Radarcord.Errors;
using System.Text;

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
                    SuperCrystal = user.Items.SuperCrystal
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
                TrelloKey = currentConfig.TrelloKey,
                TrelloToken = currentConfig.TrelloToken,
                Prices = newPrices,
                OptedOutUserIds = currentConfig.OptedOutUserIds,
                VCodesKey = currentConfig.VCodesKey
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

        [SlashCommand("announce", "Announce something to all servers that have subscribed to notifications")]
        public async Task AnnounceCommand(InteractionContext ctx, [Option("message", "The message to send to all subscribed channels")] string message)
        {
            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
                return;
            }

            var announcingEmbed = Utils.MakeEmbed(DiscordColor.Cyan, "Please wait", "Announcing message to all subscribed channel, please wait...");
            var doneEmbed = Utils.MakeEmbed(DiscordColor.Green, "Done", "Message announced!");
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(announcingEmbed));

            await Utils.AnnounceAsync(message, ctx.Client);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(doneEmbed));
        }

        [SlashCommand("blacklist", "Blacklist or un-blacklist a user!")]
        public async Task BlacklistCommand(InteractionContext ctx, [Option("id", "The ID of the user to (un-)blacklist")] string id /* string is used over DiscordUser to let me blacklist people from a different guild */, [Option("status", "Whether or not to blacklist the user")] bool blacklisted)
        {
            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
                return;
            }

            await ctx.DeferAsync();

            var user = await ctx.Client.GetUserAsync(Convert.ToUInt64(id));
            await Utils.GetUserAsync(user);

            await Utils.UpdateUserAsync(user, x => x.IsBlacklisted, blacklisted);
            var statusText = Utils.EvaluateBool(blacklisted, "Blacklisted", "Un-blacklisted");

            var embed = Utils.MakeEmbed(DiscordColor.Cyan, statusText, $"<@{user.Id}> ({user.Username}) was {statusText.ToLower()}!");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("post", "Manually post stats to Radarcord")]
        public async Task PostCommand(InteractionContext ctx)
        {
            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
                return;
            }

            await ctx.DeferAsync();

            var radar = Globals.GetRadarcordClient(ctx.Client);

            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, "Success", "Stats posted to Radarcord!");

            try
            {
                await radar.PostStatsAsync();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
            }
            catch (RadarcordException ex)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"Failed to post stats to Radarcord!\n\nException:\n```\n{ex.Message}\n```");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
            }
        }

        [SlashCommand("create_lore", "Create a new lore entry")]
        public async Task CreateLoreCommand(InteractionContext ctx, [Option("name", "The name of the lore entry")] string name, [Option("lore_file", "The path to the lore file")] string loreFilePath)
        {
            if (!Utils.UserIsOwner(ctx.User))
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Error", "You are not the owner of the bot!");
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
                return;
            }

            await ctx.DeferAsync(true);

            // Generate a random 10 to 20 digit ID, check if it already exists, if it does, try again.
            // If it doesn't, read the file path provided (which will be in the JSON format), and deserialize it into an array of LoreEntry objects.
            // Then send it to the database.

            var random = new Random();
            var id = string.Empty;

            var generatingEmbed = Utils.MakeEmbed(DiscordColor.Yellow, "Generating ID", "Generating a random ID for the lore entry, please wait...\n\n**This could take a while...**");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(generatingEmbed));

            while (true)
            {
                id = string.Empty;

                for (int i = 0; i < random.Next(10, 20); i++)
                {
                    id += random.Next(0, 10);
                }

                var exists = await Utils.LoreEntryExistsAsync(id);

                if (!exists) break;
            }

            // Encode the id in Base64
            var base64Id = Convert.ToBase64String(Encoding.UTF8.GetBytes(id));

            var loreFileContents = await File.ReadAllTextAsync(loreFilePath);
            var loreEntries = JsonConvert.DeserializeObject<List<LoreData>>(loreFileContents);

            await Utils.CreateLoreEntryAsync(id, name, loreEntries!);

            var embed = Utils.MakeEmbed(DiscordColor.Green, "Success", $"Lore entry **{name}** created!\n\n**Use this Base64 ID to send to users:** `{base64Id}`");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
