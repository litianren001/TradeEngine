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
        const int AskMarketPrice = -2147483640;
        const int BidMarketPrice = 2147483640;
        const int MarketOrderFlag = -1;

        public int CurrentPrice;

        List<Order> BuyOrderArray = new List<Order>();
        int MaxBuyPrice;

        List<Order> SellOrderArray = new List<Order>();
        int MinSellPrice;

        public OrderMatchArray(int initialPrice)
        {
            MaxBuyPrice = MinInt;
            MinSellPrice = MaxInt;
            CurrentPrice = initialPrice;
        }

        public List<TradeRecord> AddOrderGetTradeRecord(Order order)
        {
            if (order.Side == Order.BidOrAsk.BID)
            {
                AddBuyOrder(order);
            }
            else
            {
                AddSellOrder(order);
            }
            return MatchOrder();
        }

        void AddBuyOrder(Order order)
        {
            BuyOrderArray.Add(order);
            if (order.Price > MaxBuyPrice)
                MaxBuyPrice = order.Price;
            else
            {
                int i = BuyOrderArray.Count - 2;  // i indicates the index of the insert postion candidate
                while (i >= 0 && order.Price <= BuyOrderArray[i].Price)
                {
                    BuyOrderArray[i + 1] = BuyOrderArray[i];
                    i--;
                }
                i++;
                BuyOrderArray[i] = order;
            }
        }
        void AddSellOrder(Order order)
        {
            SellOrderArray.Add(order);
            if (order.Price < MinSellPrice)
                MinSellPrice = order.Price;
            else
            {
                int i = SellOrderArray.Count - 2;  // i indicates the index of the insert postion candidate
                while (i >= 0 && order.Price >= SellOrderArray[i].Price)
                {
                    SellOrderArray[i + 1] = SellOrderArray[i];
                    i--;
                }
                i++;
                SellOrderArray[i] = order;
            }
        }

        List<TradeRecord> MatchOrder()
        {
            List<TradeRecord> tradeRecord = new List<TradeRecord>();
            int buyerUid;
            int sellerUid;
            int price;
            int amount;
            Order buyerOrder;
            Order sellerOrder;
            while (MaxBuyPrice >= MinSellPrice)
            {
                buyerOrder = BuyOrderArray[BuyOrderArray.Count - 1];
                sellerOrder = SellOrderArray[SellOrderArray.Count - 1];
                buyerUid = buyerOrder.AccountUid;
                sellerUid = sellerOrder.AccountUid;
                if (buyerOrder.Amount < sellerOrder.Amount)
                {
                    amount = buyerOrder.Amount;
                    if (buyerOrder.Uid < sellerOrder.Uid)
                    {
                        price = buyerOrder.Price;
                    }
                    else
                    {
                        price = sellerOrder.Price;
                    }
                    if (price == BidMarketPrice || price == AskMarketPrice)
                        price = CurrentPrice;
                    tradeRecord.Add(new TradeRecord(buyerUid, sellerUid, price, amount));
                    CurrentPrice = price;
                    sellerOrder.Amount = sellerOrder.Amount - buyerOrder.Amount;
                    BuyOrderArray.RemoveAt(BuyOrderArray.Count - 1);
                    if (BuyOrderArray.Count == 0)
                        MaxBuyPrice = MinInt;
                    else
                        MaxBuyPrice = BuyOrderArray[BuyOrderArray.Count - 1].Price;
                }
                else if (buyerOrder.Amount > sellerOrder.Amount)
                {
                    amount = sellerOrder.Amount;
                    if (buyerOrder.Uid < sellerOrder.Uid)
                    {
                        price = buyerOrder.Price;
                    }
                    else
                    {
                        price = sellerOrder.Price;
                    }
                    if (price == BidMarketPrice || price == AskMarketPrice)
                        price = CurrentPrice;
                    tradeRecord.Add(new TradeRecord(buyerUid, sellerUid, price, amount));
                    CurrentPrice = price;
                    buyerOrder.Amount = buyerOrder.Amount - sellerOrder.Amount;
                    SellOrderArray.RemoveAt(SellOrderArray.Count - 1);
                    if (SellOrderArray.Count == 0)
                        MinSellPrice = MaxInt;
                    else
                        MinSellPrice = SellOrderArray[SellOrderArray.Count - 1].Price;

                }
                else
                {
                    amount = buyerOrder.Amount;
                    if (buyerOrder.Uid < sellerOrder.Uid)
                    {
                        price = buyerOrder.Price;
                    }
                    else
                    {
                        price = sellerOrder.Price;
                    }
                    if (price == BidMarketPrice || price == AskMarketPrice)
                        price = CurrentPrice;
                    tradeRecord.Add(new TradeRecord(buyerUid, sellerUid, price, amount));
                    CurrentPrice = price;
                    BuyOrderArray.RemoveAt(BuyOrderArray.Count - 1);
                    if (BuyOrderArray.Count == 0)
                        MaxBuyPrice = MinInt;
                    else
                        MaxBuyPrice = BuyOrderArray[BuyOrderArray.Count - 1].Price;
                    SellOrderArray.RemoveAt(SellOrderArray.Count - 1);
                    if (SellOrderArray.Count == 0)
                        MinSellPrice = MaxInt;
                    else
                        MinSellPrice = SellOrderArray[SellOrderArray.Count - 1].Price;
                }
            }
            return tradeRecord;
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
