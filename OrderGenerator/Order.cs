using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderGenerator
{
    public class Order
    {
        public int AccountUid;
        public int Time;
        public enum BidOrAsk { BID, ASK };
        public BidOrAsk Side;
        public int Price;
        public int Amount;

        public Order()
        {
        }
        public Order(int accountUid, int time, BidOrAsk side, int price, int amount)
        {
            this.AccountUid = accountUid;
            this.Time = time;
            this.Side = side;
            this.Price = price;
            this.Amount = amount;
        }
    }
}
