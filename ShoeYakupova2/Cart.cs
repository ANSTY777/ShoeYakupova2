using System.Collections.Generic;
using System.Linq;

namespace ShoeYakupova2
{
    public static class Cart
    {
        // Храню в словаре: ID позиции на складе -> количество в корзине.
        // static, чтобы корзина была общей для всех страниц.
        public static Dictionary<int, int> Items { get; private set; } = new Dictionary<int, int>();

        public static bool IsEmpty
        {
            get { return Items.Count == 0; }
        }

        public static void Add(int commodityPositionId, int qty)
        {
            using (var context = new ShoeYakupovaEntities1())
            {
                var cp = context.CommodityPosition
                    .FirstOrDefault(c => c.CommodityPositionID == commodityPositionId);

                if (cp == null) return;

                // Проверяю остаток на складе, чтобы не добавить лишнего
                int inStock = cp.QuantityInStock;

                int already = Items.ContainsKey(commodityPositionId) ? Items[commodityPositionId] : 0;
                int total = already + qty;

                if (total > inStock)
                    total = inStock;

                Items[commodityPositionId] = total;
            }
        }

        public static void ChangeQuantity(int commodityPositionId, int delta)
        {
            if (!Items.ContainsKey(commodityPositionId)) return;

            using (var context = new ShoeYakupovaEntities1())
            {
                var cp = context.CommodityPosition
                    .FirstOrDefault(c => c.CommodityPositionID == commodityPositionId);

                int inStock = cp != null ? cp.QuantityInStock : 0;
                int newQty = Items[commodityPositionId] + delta;

                // Если пытаемся накинуть больше, чем есть на складе — отмена
                if (newQty > inStock) return;

                // Если 0 или меньше — просто удаляю позицию из корзины
                if (newQty <= 0)
                {
                    Items.Remove(commodityPositionId);
                    return;
                }

                Items[commodityPositionId] = newQty;
            }
        }

        public static void Remove(int commodityPositionId)
        {
            if (Items.ContainsKey(commodityPositionId))
                Items.Remove(commodityPositionId);
        }

        public static void Clear()
        {
            Items.Clear();
        }
    }
}