using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Newtonsoft.Json;

using System.Linq.Expressions;

namespace ThingBot
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

    public class Configuration
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("supabase_url")]
        public string SupabaseUrl { get; set; }
        [JsonProperty("supabase_key")]
        public string SupabaseKey { get; set; }
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

            // Make variables for each item.
            var item1 = new ShopItem("competition", "Eating Competition", "Engage in an eating competition to increase your stomach capacity!", 750);
            var item2 = new ShopItem("lube", "Lube", "Apply it all over yourself to make it easier to crawl out of someone!", 550);
            var item3 = new ShopItem("medicine", "Digestion Medicine", "Use this to make your stomach digest bone slower than anything else!", 900);
            var item4 = new ShopItem("acid", "Acid Spit", "Use this to melt someone's skin clean off (it'll grow back eventually)!", 1250);
            var item5 = new ShopItem("poison", "Rat Poison", "Use this to protect yourself from a predator!", 2250);

            // Set up purchase handlers.
            item1.Purchased += Item1_Purchased;
            item2.Purchased += AddItemToUser;
            item3.Purchased += AddItemToUser;
            item4.Purchased += AddItemToUser;
            item5.Purchased += AddItemToUser;

            // Add items to our dictionary.
            items.Add(item1.Id, item1);
            items.Add(item2.Id, item2);
            items.Add(item3.Id, item3);
            items.Add(item4.Id, item4);
            items.Add(item5.Id, item5);

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

        #endregion

        #region Getter Methods

        public static string[] GetBasePath()
        {
            string[] paths;

            var currentDirectory = Directory.GetCurrentDirectory();

            if (currentDirectory.Contains("bin") && currentDirectory.Contains("Debug") && currentDirectory.Contains("net7.0"))
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
                        RatPoison = 0
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

            if (keySelectorList.Count != valueList.Count)
            {
                throw new ArgumentException("Key selectors and values must have the same number of elements.");
            }

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

        #endregion

        #region Other Methods

        public static async Task DeferAsync(InteractionContext context)
        {
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        }

        public static T EvaluateBool<T>(bool boolToEvaluate, T ifTrue, T ifFalse)
        {
            return boolToEvaluate ? ifTrue : ifFalse;
        }

        public static string MakeFirstCharacterUppercase(string str)
        {
            var restOfString = str[1..];
            return $"{str[1]}".ToUpper() + restOfString;
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

        #endregion

        #region Event Handlers

        // Eating Competition handler.
        private static async void Item1_Purchased(object? sender, ItemPurchaseEventArgs e)
        {
            var random = new Random();
            var stomachCapacityIncrease = Convert.ToUInt64(Math.Ceiling(Convert.ToDecimal(GetRandomDouble(20, random))));

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
                    Lube = user.Items.Lube + 1,
                    RatPoison = user.Items.RatPoison
                },
                "medicine" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine + 1,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison
                },
                "acid" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit + 1,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison
                },
                "poison" => new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison + 1
                },
                _ => throw new ArgumentException("Invalid item id provided.")
            };

            await UpdateUserAsync(user.Id, x => x.Items, updatedAmounts);
        }

        #endregion
    }
}
