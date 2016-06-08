using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchEngine
{
    public class Order
    {
        public static int UidCount = 0;
        public static void SetOrderStartId(int uidCount)
        {
            UidCount = uidCount;
        }

        public int Uid;
        public int AccountUid;
        public string Time;
        public enum BidOrAsk { BID, ASK };
        public BidOrAsk Side;
        public int Price;
        public int Amount;

        public Order()
        {
        }

        public Order(int accountUid, int time, BidOrAsk side, int price, int amount)
        {
            this.Uid = UidCount;
            UidCount++;
            this.AccountUid = accountUid;
            this.Time = DateTime.UtcNow.ToString();
            this.Side = side;
            this.Price = price;
            this.Amount = amount;
        }
    }
}
