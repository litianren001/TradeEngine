using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchEngine
{
    public class TradeRecord
    {
        public static int UidCount = 0;

        public int Uid;
        public string Time;
        public int BuyerUid;
        public int SellerUid;
        public int Price;
        public int Amount;

        public TradeRecord()
        {
        }
        public static void SetTradeRecordStartId(int uidCount)
        {
            UidCount = uidCount;
        }

        public TradeRecord(int buyerUid, int sellerUid, int price, int amount)
        {
            this.Uid = UidCount;
            UidCount++;
            this.Time = DateTime.UtcNow.ToString();
            this.BuyerUid = buyerUid;
            this.SellerUid = sellerUid;
            this.Price = price;
            this.Amount = amount;
        }

    }
}
