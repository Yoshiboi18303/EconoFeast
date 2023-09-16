using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Newtonsoft.Json;
using System.Globalization;
using System.Linq.Expressions;

namespace EconoFeast
{
    #region Data Classes

    public class FieldData
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Inline { get; set; }

        public FieldData(string name, string value, bool inline = false)
        {
            Name = name;
            Value = value;
            Inline = inline;
        }
    }

    public class ItemPrice
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("price")]
        public ulong Price { get; set; }
    }

    public class Configuration
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("supabase_url")]
        public string SupabaseUrl { get; set; }
        [JsonProperty("supabase_key")]
        public string SupabaseKey { get; set; }
        [JsonProperty("radarcord_key")]
        public string RadarcordKey { get; set; }
        [JsonProperty("trello_key")]
        public string TrelloKey { get; set; }
        [JsonProperty("trello_token")]
        public string TrelloToken { get; set; }
        [JsonProperty("item_prices")]
        public List<ItemPrice> Prices { get; set; }
        [JsonProperty("opt_out_users")]
        public List<ulong> OptedOutUserIds { get; set; }
        [JsonProperty("vcodes_key")]
        public string VCodesKey { get; set; }
    }

    #endregion

    #region Data Enums

    public enum MarkdownFormat
    {
        /// <summary>
        /// Normal markdown text.
        /// </summary>
        Normal,
        /// <summary>
        /// Bolded text. An example of this format would be: **Some text**
        /// </summary>
        Bolded,
        /// <summary>
        /// Italicized text. An example of this format would be: *Some text*
        /// </summary>
        Italicized,
        /// <summary>
        /// Underlined text. An example of this format would be: __Some text__
        /// </summary>
        Underlined
    }

    #endregion

    public static class Utils
    {
        #region Setup Methods

        /// <summary>
        /// Sets up ApplicationCommandModule(s) based on whether or not they have the GuildOnlyAttribute.
        /// </summary>
        /// <param name="extension">Your SlashCommandsExtension to use for setting up slash commands.</param>
        /// <param name="moduleTypes">All your ApplicationCommandModule(s).</param>
        public static async Task SetupCommands(SlashCommandsExtension extension, IEnumerable<Type> moduleTypes, ulong guildId)
        {
            await Task.Run(() =>
            {
                foreach (var moduleType in moduleTypes)
                {
                    var hasGuildOnlyAttribute = Attribute.IsDefined(moduleType, typeof(Attributes.GuildOnlyAttribute));

                    if (hasGuildOnlyAttribute)
                    {
                        extension.RegisterCommands(moduleType, guildId);
                    }
                    else
                    {
                        extension.RegisterCommands(moduleType);
                    }
                }
            });
        }

        public static Dictionary<string, ShopItem> SetupItems()
        {
            var items = new Dictionary<string, ShopItem>();
            var prices = Globals.Configuration.Prices;

            // Make variables for each item.
            var item1 = new ShopItem("competition", "Eating Competition", "Engage in an eating competition to increase your stomach capacity!", prices[0].Price, true, false);
            var item2 = new ShopItem("lube", "Lube", "Apply it all over yourself to make it easier to crawl out of someone!", prices[1].Price);
            var item3 = new ShopItem("medicine", "Digestion Medicine", "Use this to make your stomach digest bone slower than anything else!", prices[2].Price);
            var item4 = new ShopItem("acid", "Acid Spit", "Use this to melt someone's skin clean off (it'll grow back eventually)!", prices[3].Price);
            var item5 = new ShopItem("poison", "Rat Poison", "Use this to protect yourself from a predator!", prices[4].Price);
            var item6 = new ShopItem("revealer", "Secret Revealer", "View any secret fields in the userinfo command", prices[5].Price);
            var item7 = new ShopItem("super-crystal", "Super Crystal", "Make yourself completely immune to anything that happens!", prices[6].Price);

            // Set up purchase handlers.
            item1.Purchased += Item1_Purchased;
            item2.Purchased += AddItemToUser;
            item3.Purchased += AddItemToUser;
            item4.Purchased += AddItemToUser;
            item5.Purchased += AddItemToUser;
            item6.Purchased += AddItemToUser;
            item7.Purchased += AddItemToUser;

            // Set up sell handlers.
            item1.Sold += RemoveItemFromUser;
            item2.Sold += RemoveItemFromUser;
            item3.Sold += RemoveItemFromUser;
            item4.Sold += RemoveItemFromUser;
            item5.Sold += RemoveItemFromUser;
            item6.Sold += RemoveItemFromUser;
            item7.Sold += RemoveItemFromUser;

            // Add items to our dictionary.
            items.Add(item1.Id, item1);
            items.Add(item2.Id, item2);
            items.Add(item3.Id, item3);
            items.Add(item4.Id, item4);
            items.Add(item5.Id, item5);
            items.Add(item6.Id, item6);
            items.Add(item7.Id, item7);

            return items;
        }

        #endregion

        #region Creator Methods

        public static DiscordEmbed MakeEmbed(DiscordColor color, string? title = null, string? description = null, string? embedUrl = null, DiscordEmbedBuilder.EmbedAuthor? author = null, DiscordEmbedBuilder.EmbedFooter? footer = null, DiscordEmbedBuilder.EmbedThumbnail? thumbnail = null, IEnumerable<FieldData>? fields = null, string? imageUrl = null, DateTimeOffset? timestamp = null)
        {
            var builder = new DiscordEmbedBuilder()
                .WithColor(color)
                .WithTitle(title)
                .WithDescription(description)
                .WithTimestamp(timestamp)
                .WithUrl(embedUrl)
                .WithImageUrl(imageUrl);

            if (author is not null)
            {
                builder = builder.WithAuthor(author.Name, author.Url, author.IconUrl);
            }

            if (footer is not null)
            {
                builder = builder.WithFooter(footer.Text, footer.IconUrl);
            }

            if (thumbnail is not null)
            {
                builder = builder.WithThumbnail(thumbnail.Url, thumbnail.Height, thumbnail.Width);
            }

            if (fields is not null)
            {
                foreach (var field in fields)
                {
                    builder = builder.AddField(field.Name, field.Value, field.Inline);
                }
            }

            return builder.Build();
        }

        public static DiscordEmbed MakeErrorEmbed(string description)
        {
            return MakeEmbed(DiscordColor.Red, "Error", description);
        }

        public static string MakeMarkdownLink(string name, string value, MarkdownFormat format = MarkdownFormat.Normal)
        {
            var link = $"[{name}]({value})";

            return format switch
            {
                MarkdownFormat.Normal => link,
                MarkdownFormat.Bolded => $"**{link}**",
                MarkdownFormat.Italicized => $"*{link}*",
                MarkdownFormat.Underlined => $"__{link}__",
                _ => throw new ArgumentException("Invalid format provided."),
            };
        }

        public static DiscordActivity MakeStreamingActivity(string title, string url)
        {
            var activity = new DiscordActivity(title, ActivityType.Streaming)
            {
                StreamUrl = url
            };
            return activity;
        }

        #endregion

        #region Getter Methods

        public static string ToLocaleString(int number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(ulong number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(long number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(double number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(float number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(decimal number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(uint number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(byte number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(sbyte number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(short number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string ToLocaleString(ushort number)
        {
            return number.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string[] GetBasePath()
        {
            string[] paths;

            var currentDirectory = Directory.GetCurrentDirectory();

            if ((currentDirectory.Contains("bin") && currentDirectory.Contains("Debug") && currentDirectory.Contains("net7.0")) || (currentDirectory.Contains("bin") && currentDirectory.Contains("Release") && currentDirectory.Contains("net7.0")))
            {
                paths = new string[]
                {
                    "../",
                    "../",
                    "../"
                }; // Project starts in "bin/{Configuration}/net{Version}" because Visual Studio, we have to go back 3 directories to get the config.
            }
            else
            {
                paths = new string[]
                {
                    "./",
                }; // Useful for Visual Studio Code where you use "dotnet run".
            }

            return paths;
        }

        public static async Task<User?> GetCaptorAsync(User userToTest)
        {
            var allUsers = await GetAllUsersAsync();
            User? value = null;

            foreach (var user in allUsers)
            {
                if (user.PeopleInStomach.Contains(userToTest.Id))
                {
                    value = user;
                    break;
                }
            }

            return value;
        }

        public static async Task<Configuration> GetConfigurationAsync()
        {
            try
            {
                string path = string.Empty;

                foreach (var part in GetBasePath())
                {
                    path += part;
                }

                path += "config.json";

                var json = await File.ReadAllTextAsync(path);
                return JsonConvert.DeserializeObject<Configuration>(json) ?? throw new Exception("Couldn't get configuration!");
            }
            catch (Exception ex)
            {
                Logger.Error("Couldn't get configuration!");
                Logger.Error($"Error message: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public static async Task<User> GetUserAsync(string id)
        {
            var supabase = Globals.SupabaseClient;

            var user = await supabase
                .From<User>()
                .Select(x => new object[]
                {
                    x.AcidColor,
                    x.AmountOfBonesCollected,
                    x.AmountOfBonesInStomach,
                    x.AmountOfPeopleInStomach,
                    x.CanBeDmed,
                    x.CanBeEaten,
                    x.DailyStreak,
                    x.Id,
                    x.IsInStomach,
                    x.IsBlacklisted,
                    x.Items,
                    x.LastDailyReward,
                    x.PeopleInStomach,
                    x.SkinColor,
                    x.Softness,
                    x.StomachCapacity,
                    x.StomachColor,
                    x.StreakExpirationDate
                })
                .Where(x => x.Id == id)
                .Single();

            if (user is null)
            {
                var model = new User()
                {
                    Id = id,
                    Items = new ItemAmounts()
                    {
                        AcidSpit = 0,
                        DigestionMedicine = 0,
                        Lube = 0,
                        RatPoison = 0,
                        SecretRevealer = 0,
                        SuperCrystal = 0
                    },
                    PeopleInStomach = new List<string>(),
                    AmountOfBonesCollected = 0,
                    AmountOfBonesInStomach = 0,
                    AcidColor = "#336933",
                    AmountOfPeopleInStomach = 0,
                    CanBeDmed = true,
                    CanBeEaten = true,
                    StomachCapacity = 1,
                    StomachColor = "#FFC0CB",
                    IsInStomach = false,
                    DailyStreak = 0,
                    LastDailyReward = null,
                    SkinColor = "#000000",
                    Softness = 0.0f,
                    StreakExpirationDate = null
                };

                var response = await supabase
                    .From<User>()
                    .Insert(model, new Postgrest.QueryOptions()
                    {
                        Returning = Postgrest.QueryOptions.ReturnType.Representation
                    });

                user = response.Model;
            }

            return user!;
        }

        public static async Task<User> GetUserAsync(ulong id)
        {
            return await GetUserAsync(id.ToString());
        }

        public static async Task<User> GetUserAsync(DiscordUser user)
        {
            return await GetUserAsync(user.Id.ToString());
        }

        public static async Task<Guild> GetGuildAsync(string id)
        {
            var supabase = Globals.SupabaseClient;

            var guild = await supabase
                .From<Guild>()
                .Select(x => new object[]
                {
                    x.Id,
                    x.LogChannelId,
                    x.Options
                })
                .Where(x => x.Id == id)
                .Single();

            if (guild is null)
            {
                var model = new Guild()
                {
                    Id = id,
                    LogChannelId = null,
                    Options = Globals.DefaultGuildOptions
                };

                var response = await supabase
                    .From<Guild>()
                    .Insert(model, new Postgrest.QueryOptions()
                    {
                        Returning = Postgrest.QueryOptions.ReturnType.Representation
                    });

                guild = response.Model;
            }

            return guild;
        }

        public static async Task<Guild> GetGuildAsync(ulong id)
        {
            return await GetGuildAsync(id.ToString());
        }

        public static async Task<Guild> GetGuildAsync(DiscordGuild guild)
        {
            return await GetGuildAsync(guild.Id.ToString());
        }

        public static async Task<List<User>> GetAllUsersAsync()
        {
            var supabase = Globals.SupabaseClient;

            var response = await supabase
                .From<User>()
                .Get();

            var users = response.Models ?? throw new Exception("No models received from the database.");

            return users;
        }

        public static async Task<List<Guild>> GetAllGuildsAsync()
        {
            var supabase = Globals.SupabaseClient;

            var response = await supabase
                .From<Guild>()
                .Get();

            var guilds = response.Models ?? throw new Exception("No models received from the database.");

            return guilds;
        }

        #endregion

        #region Update Methods

        public static async Task UpdateUserAsync(string id, Expression<Func<User, object>> keySelector, object? value)
        {
            var supabase = Globals.SupabaseClient;

            await supabase
                .From<User>()
                .Where(x => x.Id == id)
                .Set(keySelector, value)
                .Update();
        }

        public static async Task UpdateUserAsync(ulong id, Expression<Func<User, object>> keySelector, object value)
        {
            await UpdateUserAsync(id.ToString(), keySelector, value);
        }

        public static async Task UpdateUserAsync(DiscordUser user, Expression<Func<User, object>> keySelector, object value)
        {
            await UpdateUserAsync(user.Id.ToString(), keySelector, value);
        }

        public static async Task UpdateUserAsync(string id, IEnumerable<Expression<Func<User, object>>> keySelectors, IEnumerable<object?> values)
        {
            var supabase = Globals.SupabaseClient;

            var table = supabase
                .From<User>()
                .Where(x => x.Id == id);

            var keySelectorList = keySelectors.ToList();
            var valueList = values.ToList();

            if (keySelectorList.Count != valueList.Count) throw new ArgumentException("Key selectors and values must have the same number of elements.");

            for (int i = 0; i < keySelectorList.Count; i++)
            {
                var keySelector = keySelectorList[i];
                var value = valueList[i];
                table.Set(keySelector, value);
            }

            await table.Update();
        }

        public static async Task UpdateUserAsync(ulong id, IEnumerable<Expression<Func<User, object>>> keySelectors, IEnumerable<object?> values)
        {
            await UpdateUserAsync(id.ToString(), keySelectors, values);
        }

        public static async Task UpdateUserAsync(DiscordUser user, IEnumerable<Expression<Func<User, object>>> keySelectors, IEnumerable<object?> values)
        {
            await UpdateUserAsync(user.Id.ToString(), keySelectors, values);
        }

        public static async Task UpdateGuildAsync(string id, Expression<Func<Guild, object>> keySelector, object? value)
        {
            var supabase = Globals.SupabaseClient;

            await supabase
                .From<Guild>()
                .Where(x => x.Id == id)
                .Set(keySelector, value)
                .Update();
        }

        public static async Task UpdateGuildAsync(ulong id, Expression<Func<Guild, object>> keySelector, object? value)
        {
            await UpdateGuildAsync(id.ToString(), keySelector, value);
        }

        public static async Task UpdateGuildAsync(DiscordGuild guild, Expression<Func<Guild, object>> keySelector, object? value)
        {
            await UpdateGuildAsync(guild.Id.ToString(), keySelector, value);
        }

        public static async Task UpdateGuildAsync(string id, IEnumerable<Expression<Func<Guild, object>>> keySelectors, IEnumerable<object?> values)
        {
            var supabase = Globals.SupabaseClient;

            var table = supabase
                .From<Guild>()
                .Where(x => x.Id == id);

            var keySelectorList = keySelectors.ToList();
            var valueList = values.ToList();

            if (keySelectorList.Count != valueList.Count) throw new ArgumentException("Key selectors and values must have the same number of elements.");

            for (int i = 0; i < keySelectorList.Count; i++)
            {
                var keySelector = keySelectorList[i];
                var value = valueList[i];
                table.Set(keySelector, value);
            }

            await table.Update();
        }

        public static async Task UpdateGuildAsync(ulong id, IEnumerable<Expression<Func<Guild, object>>> keySelectors, IEnumerable<object?> values)
        {
            await UpdateGuildAsync(id.ToString(), keySelectors, values);
        }

        public static async Task UpdateGuildAsync(DiscordGuild guild, IEnumerable<Expression<Func<Guild, object>>> keySelectors, IEnumerable<object?> values)
        {
            await UpdateGuildAsync(guild.Id.ToString(), keySelectors, values);
        }

        #endregion

        #region Checking Methods

        public static bool UserIsOwner(DiscordUser user)
        {
            return user.Id == Globals.OwnerId;
        }

        public static bool UserIsFull(User user)
        {
            return user.AmountOfPeopleInStomach >= user.StomachCapacity;
        }

        public static async Task<bool> PreFlightChecks(InteractionContext ctx)
        {
            var user = await GetUserAsync(ctx.User);

            if (user.IsBlacklisted)
            {
                var blacklistedEmbed = MakeErrorEmbed("You are blacklisted from the bot!");
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(blacklistedEmbed));
                return false;
            }

            return true;
        }

        public static async Task<bool> PreFlightChecksWithOptOut(InteractionContext ctx)
        {
            if (Globals.Configuration.OptedOutUserIds.Contains(ctx.User.Id))
            {
                var optOutEmbed = MakeErrorEmbed("You have opted out of the bot!\n\n**Contact a developer to use the commands this bot has to offer!**");
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(optOutEmbed));
                return false;
            }

            return await PreFlightChecks(ctx);
        }

        #endregion

        #region Other Methods

        public static T EvaluateBool<T>(bool boolToEvaluate, T ifTrue, T ifFalse)
        {
            return boolToEvaluate ? ifTrue : ifFalse;
        }

        public static string LanguageFilter(DiscordGuild guild, string familyFriendlyWord, string notFamilyFriendlyWord)
        {
            var guildDoc = GetGuildAsync(guild).Result;
            return EvaluateBool(guildDoc.Options.UsesLanguageFilter, familyFriendlyWord, notFamilyFriendlyWord);
        }

        public static string MakeFirstCharacterUppercase(string str)
        {
            var restOfString = str[1..];
            return $"{str[0]}".ToUpper() + restOfString;
        }

        /// <summary>
        /// Gets a random double value between 0 and maxValue
        /// </summary>
        /// <param name="maxValue">The maximum value for this double, cannot be negative.</param>
        /// <param name="random">Your instance of the Random class.</param>
        /// <returns>A double between 0 and maxValue.</returns>
        public static double GetRandomDouble(uint maxValue, Random random)
        {
            return random.NextDouble() * maxValue;
        }

        public static TValue LoadPage<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key)
        {
            return dict.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"{key} not found in dict.");
        }

        public static List<FieldData> MakeShopItemFields(IEnumerable<ShopItem> items, Func<ShopItem, string> valueFunc)
        {
            var fields = new List<FieldData>();

            foreach (var item in items)
            {
                fields.Add(new FieldData(item.Name, valueFunc.Invoke(item), true));
            }

            return fields;
        }

        public static async Task AnnounceAsync(string message, DiscordClient client)
        {
            // Get all guilds with a logChannelId set that is valid.
            var channelIds = new List<ulong>();
            var guilds = await GetAllGuildsAsync();

            foreach (var guild in guilds)
            {
                var logChannelId = Convert.ToUInt64(guild.LogChannelId);

                try
                {
                    await client.GetChannelAsync(logChannelId);
                }
                catch (DSharpPlus.Exceptions.NotFoundException)
                {
                    // Channel not found, move to the next one.
                    continue;
                }

                // Channel valid, add to list.
                channelIds.Add(logChannelId);
            }

            var fields = new List<FieldData>()
            {
                new FieldData("Message", message, true)
            };
            var embed = MakeEmbed(DiscordColor.Blue, "Announcement", "A new announcement was received from the developer!", author: new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = client.CurrentUser.Username,
                IconUrl = client.CurrentUser.AvatarUrl
            }, fields: fields);

            await SendAnnouncementAsync(channelIds, embed, client);
        }

        private static async Task SendAnnouncementAsync(List<ulong> channelIds, DiscordEmbed embed, DiscordClient client)
        {
            // Make a copy of channelIds
            var temp = channelIds;

            foreach (var id in channelIds)
            {
                var channel = await client.GetChannelAsync(id);

                await channel.SendMessageAsync(embed);

                // Remove `id` from the channelIds copy
                // Then delay the method execution for 5 seconds if `temp.Count` is greater than 0.
                temp.Remove(id);

                if (temp.Count > 0) await Task.Delay(5 * 1000);
            }
        }

        #endregion

        #region Event Handlers

        // Eating Competition handler.
        private static async void Item1_Purchased(object? sender, ItemPurchaseEventArgs e)
        {
            var random = new Random();
            var stomachCapacityIncrease = Convert.ToUInt64(Math.Ceiling(Convert.ToDecimal(GetRandomDouble(Convert.ToUInt32(20 * e.Quantity), random))));

            await UpdateUserAsync(e.PurchasingUser.Id, x => x.StomachCapacity, e.PurchasingUser.StomachCapacity + stomachCapacityIncrease);
        }

        private static async void AddItemToUser(object? sender, ItemPurchaseEventArgs e)
        {
            var item = e.Item;
            var user = e.PurchasingUser;

            ItemAmounts updatedAmounts = item.Id switch
            {
                "lube" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube + Convert.ToUInt64(1 * e.Quantity),
                    RatPoison = user.Items.RatPoison,
                    SecretRevealer = user.Items.SecretRevealer,
                    SuperCrystal = user.Items.SuperCrystal
                },
                "medicine" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine + Convert.ToUInt64(1 * e.Quantity),
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison,
                    SecretRevealer = user.Items.SecretRevealer,
                    SuperCrystal = user.Items.SuperCrystal
                },
                "acid" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit + Convert.ToUInt64(1 * e.Quantity),
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison,
                    SecretRevealer = user.Items.SecretRevealer,
                    SuperCrystal = user.Items.SuperCrystal
                },
                "poison" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison + Convert.ToUInt64(1 * e.Quantity),
                    SecretRevealer = user.Items.SecretRevealer,
                    SuperCrystal = user.Items.SuperCrystal
                },
                "revealer" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison,
                    SecretRevealer = user.Items.SecretRevealer + Convert.ToUInt64(1 * e.Quantity),
                    SuperCrystal = user.Items.SuperCrystal
                },
                "super-crystal" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison,
                    SecretRevealer = user.Items.SecretRevealer,
                    SuperCrystal = user.Items.SuperCrystal + Convert.ToUInt64(1 * e.Quantity)
                },
                _ => throw new ArgumentException("Invalid item id provided.")
            };

            await UpdateUserAsync(user.Id, x => x.Items, updatedAmounts);
        }

        private static async void RemoveItemFromUser(object? sender, ItemSoldEventArgs e)
        {
            var items = e.SellingUser.Items;
            var item = e.Item;
            ItemAmounts itemAmounts = item.Id switch
            {
                "lube" => new ItemAmounts()
                {
                    AcidSpit = items.AcidSpit,
                    DigestionMedicine = items.DigestionMedicine,
                    Lube = items.Lube - Convert.ToUInt64(e.Quantity),
                    RatPoison = items.RatPoison,
                    SecretRevealer = items.SecretRevealer,
                    SuperCrystal = items.SuperCrystal
                },
                "medicine" => new ItemAmounts()
                {
                    AcidSpit = items.AcidSpit,
                    DigestionMedicine = items.DigestionMedicine - Convert.ToUInt64(e.Quantity),
                    Lube = items.Lube,
                    RatPoison = items.RatPoison,
                    SecretRevealer = items.SecretRevealer,
                    SuperCrystal = items.SuperCrystal
                },
                "acid" => new ItemAmounts()
                {
                    AcidSpit = items.AcidSpit - Convert.ToUInt64(e.Quantity),
                    DigestionMedicine = items.DigestionMedicine,
                    Lube = items.Lube,
                    RatPoison = items.RatPoison,
                    SecretRevealer = items.SecretRevealer,
                    SuperCrystal = items.SuperCrystal
                },
                "poison" => new ItemAmounts()
                {
                    AcidSpit = items.AcidSpit,
                    DigestionMedicine = items.DigestionMedicine,
                    Lube = items.Lube,
                    RatPoison = items.RatPoison - Convert.ToUInt64(e.Quantity),
                    SecretRevealer = items.SecretRevealer,
                    SuperCrystal = items.SuperCrystal
                },
                "revealer" => new ItemAmounts()
                {
                    AcidSpit = items.AcidSpit,
                    DigestionMedicine = items.DigestionMedicine,
                    Lube = items.Lube,
                    RatPoison = items.RatPoison,
                    SecretRevealer = items.SecretRevealer - Convert.ToUInt64(e.Quantity),
                    SuperCrystal = items.SuperCrystal
                },
                "super-crystal" => new ItemAmounts()
                {
                    AcidSpit = items.AcidSpit,
                    DigestionMedicine = items.DigestionMedicine,
                    Lube = items.Lube,
                    RatPoison = items.RatPoison,
                    SecretRevealer = items.SecretRevealer,
                    SuperCrystal = items.SuperCrystal - Convert.ToUInt64(e.Quantity)
                },
                _ => throw new ArgumentException("Invalid item id provided.")
            };

            await UpdateUserAsync(e.SellingUser.Id, x => x.Items, itemAmounts);
        }

        public static async Task<bool> LoreEntryExistsAsync(string id)
        {
            // Check the database for the lore entry.
            var supabase = Globals.SupabaseClient;

            var lore = await supabase
                .From<LoreModel>()
                .Select(x => new object[]
                {
                    x.Id
                })
                .Where(x => x.Id == id)
                .Single();

            return lore != null;
        }

        public static async Task CreateLoreEntryAsync(string id, string name, List<LoreData> loreEntries)
        {
            var supabase = Globals.SupabaseClient;
            var exists = await LoreEntryExistsAsync(id);

            if (exists) throw new Exception("Lore entry already exists!");

            var model = new LoreModel()
            {
                Id = id,
                Name = name,
                Data = loreEntries
            };

            await supabase
                .From<LoreModel>()
                .Insert(model, new Postgrest.QueryOptions());
        }

        #endregion
    }
}
