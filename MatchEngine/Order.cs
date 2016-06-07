using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchEngine
{
    public class Order
    {
        public int uid;
        public int time;
        public enum bidOrAsk { BID, ASK };
        public bidOrAsk side;
        public int price;
        public int amount;

        public Order()
        {
        }
        public Order(int uid, int time, bidOrAsk side, int price, int amount)
        {
            this.uid = uid;
            this.time = time;
            this.side = side;
            this.price = price;
            this.amount = amount;
        }
    }
}
