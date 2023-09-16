using Microsoft.ML.Data;
using Newtonsoft.Json;

using Postgrest.Attributes;
using Postgrest.Models;

namespace EconoFeast
{
    public class ItemAmounts
    {
        [JsonProperty("acid")]
        public ulong AcidSpit { get; set; }
        [JsonProperty("lube")]
        public ulong Lube { get; set; }
        [JsonProperty("poison")]
        public ulong RatPoison { get; set; }
        [JsonProperty("medicine")]
        public ulong DigestionMedicine { get; set; }
        [JsonProperty("revealer")]
        public ulong SecretRevealer { get; set; }
        [JsonProperty("super-crystal")]
        public ulong SuperCrystal { get; set; }
    }

    [Table("users")]
    public class User : BaseModel
    {
        /// <summary>
        /// The ID of this user
        /// </summary>
        [Column("id")]
        public string Id { get; set; }
        /// <summary>
        /// The user IDs of the people in this user's stomach
        /// </summary>
        [Column("peopleInStomach")]
        public List<string> PeopleInStomach { get; set; }
        /// <summary>
        /// A number representing the amount of people in this user's stomach
        /// </summary>
        [Column("amountOfPeopleInStomach")]
        public uint AmountOfPeopleInStomach { get; set; }
        /// <summary>
        /// A number representing the amount of bones in this user's stomach
        /// </summary>
        [Column("bonesInStomach")]
        public ulong AmountOfBonesInStomach { get; set; }
        /// <summary>
        /// A number representing the amount of bones this user has collected
        /// </summary>
        [Column("bonesCollected")]
        public ulong AmountOfBonesCollected { get; set; }
        /// <summary>
        /// How many people can be in this user's stomach at once
        /// </summary>
        [Column("stomachCapacity")]
        public ulong StomachCapacity { get; set; }
        [Column("skinColor")]
        public string SkinColor { get; set; }
        /// <summary>
        /// Whether or not this user is in someone's stomach
        /// </summary>
        [Column("inStomach")]
        public bool IsInStomach { get; set; }
        [Column("softness")]
        public float Softness { get; set; }
        [Column("acidColor")]
        public string AcidColor { get; set; }
        [Column("stomachColor")]
        public string StomachColor { get; set; }
        [Column("canBeDmed")]
        public bool CanBeDmed { get; set; }
        [Column("usable")]
        public bool CanBeEaten { get; set; }
        [Column("items")]
        public ItemAmounts Items { get; set; }
        [Column("dailyStreak")]
        public uint DailyStreak { get; set; }
        [Column("lastDailyReward")]
        public DateTime? LastDailyReward { get; set; }
        [Column("streakExpirationDate")]
        public DateTime? StreakExpirationDate { get; set; }
        [Column("blacklisted")]
        public bool IsBlacklisted { get; set; }
        [Column("lastMeal")]
        public DateTime? LastMealTime { get; set; }
        [Column("reportBanned")]
        public bool IsReportBanned { get; set; }
    }

    public class TrelloCreatedCardLink
    {
        [JsonProperty("shortUrl")]
        public string Url { get; set; }
    }

    public class LoreData
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class GuildOptions
    {
        [JsonProperty("language_filter")]
        public bool UsesLanguageFilter { get; set; }
    }

    [Table("guilds")]
    public class Guild : BaseModel
    {
        [Column("id")]
        public string Id { get; set; }
        [Column("logChannelId")]
        public string? LogChannelId { get; set; }
        [Column("options")]
        public GuildOptions Options { get; set; }
    }

    [Table("lore-data")]
    public class LoreModel : BaseModel
    {
        [Column("id")]
        public string Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("data")]
        public List<LoreData> Data { get; set; }
    }
}
