using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

using System.Linq.Expressions;

namespace ThingBot.Commands
{
    [SlashCommandGroup("economy", "The main part of the bot!")]
    public class EconomyCommands : ApplicationCommandModule
    {
        [SlashCommand("eat", "Turn a user into your meal, this can only be done 3 times throughout the day.")]
        [SlashCooldown(1, 300.0, SlashCooldownBucketType.User)] // This'll be updated with 3 uses per day eventually.
        public async Task EatUserCommand(InteractionContext ctx, [Option("user", "The user to target")] DiscordUser userToEat)
        {
            await Utils.DeferAsync(ctx);

            if (userToEat.IsBot)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You can't just eat my kind, what have they done to you?");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (userToEat.Id == ctx.User.Id)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You can't just eat yourself, what do you think this is?");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var currentUser = await Utils.GetUserAsync(ctx.User);
            var targetUser = await Utils.GetUserAsync(userToEat);

            // TODO: Fix the following piece of code later.
            /*
            if (currentUser.LastMealTime is not null)
            {
                var timeBetween = DateTime.Now.AddMinutes(3) - currentUser.LastMealTime;

                Console.WriteLine(timeBetween);

                if (timeBetween.Value.TotalMinutes < 3)
                {
                    var errorEmbed = Utils.MakeErrorEmbed("You should calm it, there needs to be 3 minutes between your last person. Sorry, I don't make the rules.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                    return;
                }
            }
            */

            if (currentUser.IsInStomach)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You're inside of someone, you can't do anything while you're inside of them!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (Utils.UserIsFull(currentUser))
            {
                var errorEmbed = Utils.MakeErrorEmbed("You're completely stuffed, one more person and you'll pop like a balloon *(and we can't have that)*!\n\n**Tip:** Use the `digest` command for a chance to increase your stomach capacity!");
            }

            if (targetUser.IsInStomach)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"{userToEat.Username} is already inside of someone, you cannot eat them while someone else has them!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (!targetUser.CanBeEaten)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"{userToEat.Username} has opted out of the bot, you cannot eat them!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var random = new Random();

            var peopleInStomachList = currentUser.PeopleInStomach;
            peopleInStomachList.Add(targetUser.Id);

            // Make value update lists.
            var currentUserValues = new List<object>()
            {
                peopleInStomachList,
                currentUser.AmountOfPeopleInStomach + 1,
                DateTime.Now
            };

            var targetUserValues = new List<object>()
            {
                true,
                random.NextSingle(),
            };

            // Update users
            await Utils.UpdateUserAsync(ctx.User, new List<Expression<Func<User, object>>>()
            {
                x => x.PeopleInStomach,
                x => x.AmountOfPeopleInStomach,
                x => x.LastMealTime
            }, currentUserValues);

            await Utils.UpdateUserAsync(userToEat, new List<Expression<Func<User, object>>>()
            {
                x => x.IsInStomach,
                x => x.Softness,
            }, targetUserValues);

            var embed = Utils.MakeEmbed(DiscordColor.Green, description: $"You have successfully swallowed <@{userToEat.Id}>, let's hope they get comfy inside of you!");
            var eatenEmbed = Utils.MakeEmbed(DiscordColor.Yellow, description: $"You were swallowed by **{Utils.MakeFirstCharacterUppercase(userToEat.Username)}**! Let's hope they're nice to you...");
            var member = await ctx.Guild.GetMemberAsync(userToEat.Id);

            if (member is not null && targetUser.CanBeDmed)
            {
                try
                {
                    await member.SendMessageAsync(embed);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Couldn't DM {Utils.MakeFirstCharacterUppercase(userToEat.Username)} the eaten embed!");

                    #if DEBUG

                    Logger.Error($"Exception message: {ex.Message}");

                    #endif
                }
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("digest", "Finish off your prey")]
        [SlashCooldown(1, 300.0, SlashCooldownBucketType.User)]
        public async Task DigestUsersCommand(InteractionContext ctx)
        {
            // Plan: Make this a minigame rather than an immediate reward.
            await Utils.DeferAsync(ctx);
            var user = await Utils.GetUserAsync(ctx.User);

            if (user.AmountOfPeopleInStomach == 0)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You don't have anyone inside of you, you can't digest nothing!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var random = new Random();

            // Formula: baseNumber * number of people inside the user
            // Adding in the amount of bones in each person inside (not the ones the user already removed, kinda like a bank), if they have any.
            //
            // Example: 206 * 3 + 412 + 206 + 0 = 824 bones, my math could be way off though.
            // Yes, this was taken from my old code. Sue me.
            var calculated = Globals.BaseBonesNumber * user.AmountOfPeopleInStomach;
            var shouldStomachCapacityIncrease = random.NextDouble() > 0.69;
            var increaseInStomachCapacity = Convert.ToUInt64(Math.Ceiling(Convert.ToDecimal(Utils.GetRandomDouble(5, random))));

            #region People Inside User Updates
            foreach (var personId in user.PeopleInStomach)
            {
                var discordId = Convert.ToUInt64(personId);
                var userInStomach = await Utils.GetUserAsync(personId);
                var bonesLost = userInStomach.AmountOfBonesInStomach;

                calculated += (uint)bonesLost;

                var personInStomachValues = new List<object>()
                {
                    Convert.ToUInt64(0), // amountOfBonesInStomach
                    false, // inStomach
                    0.0f // softness
                };

                await Utils.UpdateUserAsync(personId, new List<Expression<Func<User, object>>>()
                {
                    x => x.AmountOfBonesInStomach,
                    x => x.IsInStomach,
                    x => x.Softness
                }, personInStomachValues);

                var member = await ctx.Guild.GetMemberAsync(discordId);

                if (member is not null && userInStomach.CanBeDmed && userInStomach.AmountOfBonesInStomach > 0)
                {
                    var digestedEmbed = Utils.MakeEmbed(DiscordColor.Red, description: $"**Whoopsies!** You just got digested and lost all {bonesLost} of the bones inside of you!", footer: new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Tip: Remove those bones next time, please..."
                    });

                    try
                    {
                        await member.SendMessageAsync(digestedEmbed);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Couldn't DM {Utils.MakeFirstCharacterUppercase(member.Username)} the digested embed!");

                        #if DEBUG

                        Logger.Error($"Exception message: {ex.Message}");

                        #endif
                    }
                }
            }
            #endregion

            var currentUserValues = new List<object>()
            {
                new List<string>(), // peopleInStomach
                Convert.ToUInt32(0), // amountOfPeopleInStomach
                user.AmountOfBonesInStomach + Convert.ToUInt64(calculated), // bonesInStomach
            };
            var keySelectors = new List<Expression<Func<User, object>>>()
            {
                x => x.PeopleInStomach,
                x => x.AmountOfPeopleInStomach,
                x => x.AmountOfBonesInStomach
            };

            string description = $"You have successfully digested {Utils.EvaluateBool(user.AmountOfPeopleInStomach == 1, $"the {user.AmountOfPeopleInStomach} person", $"all {user.AmountOfPeopleInStomach} people")} inside of you, you have earned **{calculated}** bones.";

            if (shouldStomachCapacityIncrease)
            {
                currentUserValues.Add(increaseInStomachCapacity); // Add stomachCapacity to list
                description += $"\n\n**Also, lucky you! Your stomach decided to hold more people! Your stomach capacity has increased by {increaseInStomachCapacity}!**";
            }

            if (currentUserValues.Count == 4) keySelectors.Add(x => x.StomachCapacity);

            await Utils.UpdateUserAsync(user.Id, keySelectors, currentUserValues);

            var embed = Utils.MakeEmbed(DiscordColor.Green, description: description);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("extract", "Extract all bones from your stomach")]
        public async Task ExtractBonesCommand(InteractionContext ctx)
        {
            await Utils.DeferAsync(ctx);

            var user = await Utils.GetUserAsync(ctx.User);

            if (user.AmountOfBonesInStomach == 0)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You have no bones inside of you!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            // Formula for this:
            // Amount of bones in the user - Amount of bones lost due to digestion (0 if the user has the Digestion Medicine item).
            // Example: 206 (bones in stomach) - 52 (bones lost) = 152 (total)
            var random = new Random();
            var bonesLost = Utils.EvaluateBool(user.Items.DigestionMedicine > 0, 0, random.NextInt64(1, Convert.ToInt64(user.AmountOfBonesInStomach / 2)));
            var totalCollected = user.AmountOfBonesInStomach - Convert.ToUInt64(bonesLost);

            var keySelectors = new List<Expression<Func<User, object>>>()
            {
                x => x.AmountOfBonesInStomach,
                x => x.AmountOfBonesCollected
            };
            var values = new List<object>()
            {
                Convert.ToUInt64(0),
                user.AmountOfBonesCollected + totalCollected
            };

            await Utils.UpdateUserAsync(user.Id, keySelectors, values);

            string description = Utils.EvaluateBool(bonesLost == 0, $"Your digestion medicine has allowed you to extract all {totalCollected} bones from your stomach, good purchase!", $"You have extracted {totalCollected} bones from your stomach, however the other {user.AmountOfBonesInStomach - totalCollected} bones were lost to digestion.");

            var embed = Utils.MakeEmbed(Utils.EvaluateBool(bonesLost == 0, DiscordColor.Green, DiscordColor.Yellow), description: description);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("shop", "View the entire shop, or an item within it.")]
        public async Task ShopCommand(InteractionContext ctx, [Option("id", "The item ID to look for")] string? id = null)
        {
            await Utils.DeferAsync(ctx);

            #region Item Details

            if (id is not null)
            {
                if (!Globals.Items.ContainsKey(id))
                {
                    var noItemEmbed = Utils.MakeErrorEmbed($"No item with an ID of `{id}` was found.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noItemEmbed));
                    return;
                }

                if (Globals.Items.TryGetValue(id, out var item))
                {
                    var itemFields = new List<FieldData>()
                    {
                        new FieldData("Price", $"{item.Price} bones", true),
                        new FieldData("Purchase Command", $"`/buy {item.Id}`", true)
                    };
                    var itemEmbed = Utils.MakeEmbed(DiscordColor.Cyan, item.Name, item.Description, fields: itemFields);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(itemEmbed));
                    return;
                }
            }

            #endregion

            // Note to self: 5 items per page, remember this.
            var shopItems = Globals.Items.ToArray();
            var pages = new Dictionary<int, List<ShopItem>>()
            {
                {
                    0,
                    new List<ShopItem>()
                    {
                        shopItems[0].Value,
                        shopItems[1].Value,
                        shopItems[2].Value,
                        shopItems[3].Value,
                        shopItems[4].Value,
                    }
                },
                {
                    1,
                    new List<ShopItem>()
                    {
                        new ShopItem("test", "Test", "A test item, just because.", 99999)
                    }
                }
            };

            int pageNumber = 1;
            int maxPages = pages.Count;
            var items = Utils.LoadPage(pages, pageNumber - 1);

            var buttons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Danger, "previous-page", "Previous", pageNumber == 1, new DiscordComponentEmoji("◀️")),
                new DiscordButtonComponent(ButtonStyle.Success, "next-page", "Next", pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
            };

            Func<ShopItem, string> valueFunc = item => $"ID: {item.Id}";

            // Save repeated parts of making the embed
            const string title = "Shop";
            const string description = "Welcome to the shop, get some stuff to make you better than your competition!";
            var footer = new DiscordEmbedBuilder.EmbedFooter()
            {
                Text = $"View more info on any item by passing in the id argument! - Page {pageNumber}/{maxPages}"
            };

            var fields = Utils.MakeShopItemFields(items, valueFunc);

            var embed = Utils.MakeEmbed(DiscordColor.Cyan, title, description, footer: footer, fields: fields);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(buttons).AddEmbed(embed));

            #region Interaction Handler

            DSharpPlus.AsyncEvents.AsyncEventHandler<DiscordClient, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs> handlePagination = async (s, e) =>
            {
                if (e.User.Id != ctx.User.Id) return;

                if (e.Id == "next-page")
                {
                    pageNumber += 1;
                    buttons = new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, "previous-page", "Previous", pageNumber == 1, new DiscordComponentEmoji("◀️")),
                        new DiscordButtonComponent(ButtonStyle.Success, "next-page", "Next", pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
                    };
                    items = Utils.LoadPage(pages, pageNumber - 1);
                    fields = Utils.MakeShopItemFields(items, valueFunc);
                    footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = $"View more info on any item by passing in the id argument! - Page {pageNumber}/{maxPages}"
                    };
                    embed = Utils.MakeEmbed(DiscordColor.Cyan, title, description, footer: footer, fields: fields);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(buttons));
                }
                else if (e.Id == "previous-page")
                {
                    pageNumber -= 1;
                    buttons = new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, "previous-page", "Previous", pageNumber == 1, new DiscordComponentEmoji("◀️")),
                        new DiscordButtonComponent(ButtonStyle.Success, "next-page", "Next", pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
                    };
                    items = Utils.LoadPage(pages, pageNumber - 1);
                    fields = Utils.MakeShopItemFields(items, valueFunc);
                    footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = $"View more info on any item by passing in the id argument! - Page {pageNumber}/{maxPages}"
                    };
                    embed = Utils.MakeEmbed(DiscordColor.Cyan, title, description, footer: footer, fields: fields);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(buttons));
                }
            };

            #endregion

            ctx.Client.ComponentInteractionCreated += handlePagination;

            await Task.Delay(180 * 1000);

            ctx.Client.ComponentInteractionCreated -= handlePagination;

            embed = Utils.MakeEmbed(DiscordColor.Yellow, title, $"{description}\n\n**Interaction collector expired, please use this command again if you want to check out a different page.**", footer: footer,
                fields: fields);
            buttons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Danger, "previous-page", "Previous", true, new DiscordComponentEmoji("◀️")),
                new DiscordButtonComponent(ButtonStyle.Success, "next-page", "Next", true, new DiscordComponentEmoji("▶️"))
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(buttons).AddEmbed(embed));
        }

        [SlashCommand("buy", "Buy an item from the shop")]
        public async Task BuyItemCommand(InteractionContext ctx, [Option("id", "The item ID to buy")] string id)
        {
            await Utils.DeferAsync(ctx);

            if (!Globals.Items.ContainsKey(id))
            {
                var noItemEmbed = Utils.MakeErrorEmbed($"No item with an ID of `{id}` was found.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noItemEmbed));
                return;
            }

            var user = await Utils.GetUserAsync(ctx.User);
            var item = Globals.Items[id];

            var successfullyPurchased = await item.BuyAsync(user);

            if (!successfullyPurchased)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"Couldn't purchase **{item.Name}**! You might not have enough bones to purchase this item.\n\n**Item requires:** `{item.Price}` bones\n**You have:** `{user.AmountOfBonesCollected}` bones\n**You need:** `{item.Price - user.AmountOfBonesCollected}` more bones.\n\n**Please try again once you get enough to purchase this item.**");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, description: $"You have successfully purchased **{item.Name}**, if this item is not an auto-redeem item, it has been added to your inventory.\n\n**You now have `{user.AmountOfBonesCollected - item.Price}` bones left.**");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
        }
    }
}
