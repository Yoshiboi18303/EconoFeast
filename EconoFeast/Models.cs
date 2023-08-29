using Newtonsoft.Json;

using Postgrest.Attributes;
using Postgrest.Models;

namespace ThingBot
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
    }

    [Table("users")]
    public class User : BaseModel
    {
        [Column("id")]
        public string Id { get; set; }
        [Column("peopleInStomach")]
        public List<string> PeopleInStomach { get; set; }
        [Column("amountOfPeopleInStomach")]
        public uint AmountOfPeopleInStomach { get; set; }
        [Column("bonesInStomach")]
        public ulong AmountOfBonesInStomach { get; set; }
        [Column("bonesCollected")]
        public ulong AmountOfBonesCollected { get; set; }
        [Column("stomachCapacity")]
        public ulong StomachCapacity { get; set; }
        [Column("skinColor")]
        public string SkinColor { get; set; }
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
        [Column("isBlacklisted")]
        public bool IsBlacklisted { get; set; }
        [Column("lastMeal")]
        public DateTime? LastMealTime { get; set; }
    }
}
