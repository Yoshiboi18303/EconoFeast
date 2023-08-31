namespace EconoFeast
{
    public class ItemPurchaseEventArgs
    {
        public User PurchasingUser { get; }
        public ShopItem Item { get; }
        public int Quantity { get; }

        public ItemPurchaseEventArgs(User purchasingUser, ShopItem item, int quantity)
        {
            PurchasingUser = purchasingUser;
            Item = item;
            Quantity = quantity;
        }
    }

    public enum ItemPurchaseFailureReason
    {
        None,
        BadRequest,
        InsuffientFunds
    }

    public class ItemPurchaseResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public ItemPurchaseFailureReason? FailureReason { get; }

        public ItemPurchaseResult(bool isSuccess, string message, ItemPurchaseFailureReason failureReason)
        {
            IsSuccess = isSuccess;
            Message = message;
            FailureReason = failureReason;
        }
    }

    public class ShopItem
    {
        private readonly string _id;
        private string _name;
        private string _description;
        private ulong _price;

        /// <summary>
        /// Gets the id of this ShopItem.
        /// </summary>
        public string Id { get => _id; }
        /// <summary>
        /// Gets or sets the name of this ShopItem.
        /// </summary>
        public string Name
        { 
            get => _name;
            set {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value), "Value of Name can't be null, empty or just a whitespace!");
                _name = value;
            } 
        }
        /// <summary>
        /// Gets or sets the description of this ShopItem.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value), "Value of Description can't be null, empty or just a whitespace!");
                _description = value;
            }
        }
        /// <summary>
        /// Gets or sets the price of this ShopItem.
        /// </summary>
        public ulong Price
        {
            get => _price;
            set
            {
                if (value == Convert.ToUInt64(0)) throw new ArgumentException("Value of Price can't be 0!", nameof(value));
                _price = value;
            }
        }

        public event EventHandler<ItemPurchaseEventArgs>? Purchased;

        protected virtual void OnPurchased(ItemPurchaseEventArgs args)
        {
            Purchased?.Invoke(this, args);
        }

        public ShopItem(string id, string name, string description, ulong price)
        {
            _id = id;
            _name = name;
            _description = description;
            _price = price;
        }

        /// <summary>
        /// Ensures the user provided has enough bones collected, if they do, remove that many from them, then invoke the Purchased event.
        /// </summary>
        /// <param name="user">The user document</param>
        /// <param name="quantity">How many of this item to buy</param>
        /// <returns>Whether the item was successfully purchased or not.</returns>
        public async Task<ItemPurchaseResult> BuyAsync(User user, int quantity = 1)
        {
            if (quantity < 1) return new ItemPurchaseResult(false, "Quantity cannot be less than 1!", ItemPurchaseFailureReason.BadRequest);

            var bonesCollected = user.AmountOfBonesCollected;
            var price = Price * Convert.ToUInt64(quantity);

            if (bonesCollected >= price)
            {
                await Utils.UpdateUserAsync(user.Id, x => x.AmountOfBonesCollected, bonesCollected - Price);

                OnPurchased(new ItemPurchaseEventArgs(user, this, quantity));
                return new ItemPurchaseResult(true, "Successfully purchased item.", ItemPurchaseFailureReason.None);
            }
            else return new ItemPurchaseResult(false, "User doesn't have enough bones to buy this item.", ItemPurchaseFailureReason.InsuffientFunds);
        }
    }
}
