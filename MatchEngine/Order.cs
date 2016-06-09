using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchEngine
{
    public class Order
    {
        public const int SellMarketOrderPrice = -2147483640;
        public const int BuyMarketOrderPrice = 2147483640;
        public static int UidCount = 0;
        public static void SetOrderStartId(int uidCount)
        {
            UidCount = uidCount;
        }

        public int Uid;
        public int AccountUid;
        public string Time;
        public enum enumSide { BUY, SELL };
        public enumSide Side;
        public enum enumFufillType { LMT, MKT };
        public enumFufillType FufillType;
        public int Price;
        public int Amount;

        public Order()
        {
        }

        public Order(int accountUid, enumSide side, int price, int amount)
        {
            Uid = UidCount;
            UidCount++;
            AccountUid = accountUid;
            Time = DateTime.UtcNow.ToString();
            Side = side;
            Price = price;
            Amount = amount;
            if (price == SellMarketOrderPrice || price == BuyMarketOrderPrice)
                FufillType = enumFufillType.MKT;
            else
                FufillType = enumFufillType.LMT;
        }
    }
}
