namespace CheZa_Core.Models
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; }

        public CartViewModel()
        {
            Items = new List<CartItemViewModel>();
        }

        public void AddItem(Product product, List<Product> modifiers, string comment)
        {
            var comparer = new ProductEqualityComparer();
            var listComparer = new ProductListEqualityComparer(comparer);

            var existingItem = Items.FirstOrDefault(item =>
                item.product.id == product.id &&
                listComparer.Equals(item.modifiers, modifiers));

            if (existingItem != null)
            {
                existingItem.quantity++;
            }
            else
            {
                Items.Add(new CartItemViewModel
                {
                    product = product,
                    modifiers = modifiers,
                    quantity = 1,
                    comment = comment
                });
            }
        }

        public void IncreaseQuantity(Product product, List<Product> modifiers)
        {
            var comparer = new ProductEqualityComparer();
            var listComparer = new ProductListEqualityComparer(comparer);

            var existingItem = Items.FirstOrDefault(item =>
            item.product.id == product.id && listComparer.Equals(item.modifiers, modifiers));
            //Console.WriteLine("Modifiers count: " + modifiers.Count + ", modifiers: ");
            //foreach (Product product1 in modifiers)
            //{
            //    Console.WriteLine("id: " + product1.id + ", name: " + product1.name);
            //}

            if ( existingItem != null )
            {
                existingItem.quantity++;
                Console.WriteLine("Add quantity. Quantity: " +  existingItem.quantity);
            }
        }

        public void DecreaseQuantity(Product product, List<Product> modifiers)
        {
            var comparer = new ProductEqualityComparer();
            var listComparer = new ProductListEqualityComparer(comparer);

            var existingItem = Items.FirstOrDefault(item =>
            item.product.id == product.id && listComparer.Equals(item.modifiers, modifiers));

            if ( existingItem != null )
            {
                if ( existingItem.quantity > 1 )
                {
                    existingItem.quantity--;
                }
                else
                {
                    Items.Remove(existingItem);
                }
            }
        }

        public int GetTotalItemCount()
        {
            return Items.Sum(item => item.quantity);
        }
    }

    public class ProductEqualityComparer : IEqualityComparer<Product>
    {
        public bool Equals(Product x, Product y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.id == y.id; // Сравнивайте по нужным вам полям
        }

        public int GetHashCode(Product obj)
        {
            return obj.id.GetHashCode(); // Используйте те же поля для вычисления хеша
        }
    }

    public class ProductListEqualityComparer : IEqualityComparer<List<Product>>
    {
        private readonly IEqualityComparer<Product> _productComparer;

        public ProductListEqualityComparer(IEqualityComparer<Product> productComparer)
        {
            _productComparer = productComparer;
        }

        public bool Equals(List<Product> x, List<Product> y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            if (x.Count != y.Count)
                return false;

            // Создаем копии списков и сортируем их для сравнения
            var sortedX = x.OrderBy(p => p.id).ToList();
            var sortedY = y.OrderBy(p => p.id).ToList();

            for (int i = 0; i < sortedX.Count; i++)
            {
                if (!_productComparer.Equals(sortedX[i], sortedY[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(List<Product> obj)
        {
            unchecked
            {
                int hash = 19;
                foreach (var product in obj.OrderBy(p => p.id))
                {
                    hash = hash * 31 + _productComparer.GetHashCode(product);
                }
                return hash;
            }
        }
    }
}
