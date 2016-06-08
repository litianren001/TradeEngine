using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchEngine
{
    public class OrderMatchArray
    {
        const int MinInt = -2147483648;
        const int MaxInt = 2147483647;
        const int AskMarketPrice = -2147483648;
        const int BidMarketPrice = 2147483647;
        const int ArrayMaxLength = 50;
        const int MarketOrderFlag = -1;

        public int CurrentPrice;

        Order[] BuyArray = new Order[ArrayMaxLength];
        int BuyLength;
        int MaxBuyPrice;

        Order[] SellArray = new Order[ArrayMaxLength];
        int SellLength;
        int MinSellPrice;

        public OrderMatchArray(int initialPrice)
        {
            BuyLength = 0;
            SellLength = 0;
            MaxBuyPrice = MinInt;
            MinSellPrice = MaxInt;
            CurrentPrice = initialPrice;
        }

        public TradeRecord[] OrderEntry(Order order)
        {
            if (order.Side == Order.BidOrAsk.BID)
            {
                AddBuyOrder(order);
                return MatchOrder();
            }
            else
            {
                AddSellOrder(order);
                return MatchOrder();

            }
        }

        void AddBuyOrder(Order order)
        {
            BuyArray[BuyLength] = order;
            BuyLength += 1;
            if (order.Price > MaxBuyPrice)
                MaxBuyPrice = order.Price;
            else
            {
                int i = BuyLength - 2;  // i indicates the index of a swap candidate
                while( i>=0 && order.Price<=BuyArray[i].Price)
                {
                    BuyArray[i + 1] = BuyArray[i];
                    i--;
                }
                i++;
                BuyArray[i] = order;
            }
        }
        void AddSellOrder(Order order)
        {
            SellArray[SellLength] = order;
            SellLength += 1;
            if (order.Price < MinSellPrice)
                MinSellPrice = order.Price;
            else
            {
                int i = SellLength - 2;  // i indicates the index of a swap candidate
                while (i >= 0 && order.Price >= SellArray[i].Price)
                {
                    SellArray[i + 1] = SellArray[i];
                    i--;
                }
                i++;
                SellArray[i] = order;
            }
        }

        TradeRecord[] MatchOrder()
        {
            return new TradeRecord[0];
        }

    }
}


/*
        TradeRecord[] BuyOrderMarketEntry(Order order)
        {
            int i = SellLength - 1;
            int amountLeft = order.Amount;
            int buyerUid=order.AccountUid;
            int sellerUid;
            int price;
            int amount;
            while (i >= 0 && amountLeft > 0)
            {
                amountLeft -= SellArray[i].Amount;
                i--;
            }
            i++;    // i now indicates the index of the last trade match
            int residualAmount = -amountLeft;   // residualAmount indicates the partial untraded amount of the seller
            TradeRecord[] tradeRecord = new TradeRecord[SellLength - i];
            for (int j=SellLength-1;j>= i; j--)
            {
                sellerUid = SellArray[j].AccountUid;
                price = SellArray[j].Price;
                amount = SellArray[j].Amount;
                tradeRecord[SellLength - j - 1] = new TradeRecord(buyerUid, sellerUid, price, amount);
            }
            if (amountLeft < 0) // partial sell order of i is untraded
                tradeRecord[SellLength - i - 1].Amount -= residualAmount;
                SellArray[i]
                Sell
            else if (amountLeft > 0)
                AddBuyOrder(order.AccountUid, order.Time, order.Side, order.Price, amountLeft);
            return tradeRecord;
        }
        TradeRecord[] BuyOrderLimitEntry(Order order)
        {
            return new TradeRecord[0];
        }
        TradeRecord[] SellOrderMarketEntry(Order order)
        {
            return new TradeRecord[0];
        }
        TradeRecord[] SellOrderLimitEntry(Order order)
        {
            return new TradeRecord[0];
        }
*/