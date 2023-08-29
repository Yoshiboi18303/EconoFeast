namespace ThingBot
{
    public class ItemPurchaseEventArgs
    {
        public User PurchasingUser { get; }
        public ShopItem Item { get; }

        public ItemPurchaseEventArgs(User purchasingUser, ShopItem item)
        {
            PurchasingUser = purchasingUser;
            Item = item;
        }
    }

    public class ShopItem
    {
        public string Id { get; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ulong Price { get; set; }

        public event EventHandler<ItemPurchaseEventArgs>? Purchased;

        protected virtual void OnPurchased(ItemPurchaseEventArgs args)
        {
            Purchased?.Invoke(this, args);
        }

        public ShopItem(string id, string name, string description, ulong price)
        {
            Id = id;
            Name = name;
            Description = description;
            Price = price;
        }

        /// <summary>
        /// Ensures the user provided has enough bones collected, if they do, remove that many from them, then invoke the Purchased event.
        /// </summary>
        /// <param name="user">The user document</param>
        /// <returns>Whether the item was successfully purchased or not.</returns>
        public async Task<bool> BuyAsync(User user)
        {
            var bonesCollected = user.AmountOfBonesCollected;

            if (bonesCollected >= Price)
            {
                await Utils.UpdateUserAsync(user.Id, x => x.AmountOfBonesCollected, bonesCollected - Price);

                OnPurchased(new ItemPurchaseEventArgs(user, this));
                return true;
            }
            else return false;
        }
    }
}
