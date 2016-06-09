using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchEngine
{
    public class OrderMatchList
    {
        const int MinInt = -2147483648;
        const int MaxInt = 2147483647;
        const int SellMarketOrderPrice = -2147483640;
        const int BuyMarketOrderPrice = 2147483640;
        const int MarketOrderFlag = -1;

        public int CurrentPrice;

        List<Order> BuyOrderList = new List<Order>();
        int MaxBuyPrice;

        List<Order> SellOrderList = new List<Order>();
        int MinSellPrice;

        public OrderMatchList(int initialPrice)
        {
            MaxBuyPrice = MinInt;
            MinSellPrice = MaxInt;
            CurrentPrice = initialPrice;
        }

        public List<TradeRecord> AddOrderGetTradeRecord(Order order)
        {
            if (order.Side == Order.enumSide.BUY)
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
            BuyOrderList.Add(order);
            if (order.Price > MaxBuyPrice)
                MaxBuyPrice = order.Price;
            else
            {
                int i = BuyOrderList.Count - 2;  // i indicates the index of the insert postion candidate
                while (i >= 0 && order.Price <= BuyOrderList[i].Price)
                {
                    BuyOrderList[i + 1] = BuyOrderList[i];
                    i--;
                }
                i++;
                BuyOrderList[i] = order;
            }
        }
        void AddSellOrder(Order order)
        {
            SellOrderList.Add(order);
            if (order.Price < MinSellPrice)
                MinSellPrice = order.Price;
            else
            {
                int i = SellOrderList.Count - 2;  // i indicates the index of the insert postion candidate
                while (i >= 0 && order.Price >= SellOrderList[i].Price)
                {
                    SellOrderList[i + 1] = SellOrderList[i];
                    i--;
                }
                i++;
                SellOrderList[i] = order;
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
                buyerOrder = BuyOrderList[BuyOrderList.Count - 1];
                sellerOrder = SellOrderList[SellOrderList.Count - 1];
                buyerUid = buyerOrder.AccountUid;
                sellerUid = sellerOrder.AccountUid;
                if (buyerOrder.Amount < sellerOrder.Amount)
                {
                    amount = buyerOrder.Amount;
                    price = GetTradePrice(buyerOrder, sellerOrder);
                    tradeRecord.Add(new TradeRecord(buyerUid, sellerUid, price, amount));
                    CurrentPrice = price;
                    sellerOrder.Amount = sellerOrder.Amount - buyerOrder.Amount;
                    BuyOrderList.RemoveAt(BuyOrderList.Count - 1);
                    if (BuyOrderList.Count == 0)
                        MaxBuyPrice = MinInt;
                    else
                        MaxBuyPrice = BuyOrderList[BuyOrderList.Count - 1].Price;
                }
                else if (buyerOrder.Amount > sellerOrder.Amount)
                {
                    amount = sellerOrder.Amount;
                    price = GetTradePrice(buyerOrder, sellerOrder);
                    tradeRecord.Add(new TradeRecord(buyerUid, sellerUid, price, amount));
                    CurrentPrice = price;
                    buyerOrder.Amount = buyerOrder.Amount - sellerOrder.Amount;
                    SellOrderList.RemoveAt(SellOrderList.Count - 1);
                    if (SellOrderList.Count == 0)
                        MinSellPrice = MaxInt;
                    else
                        MinSellPrice = SellOrderList[SellOrderList.Count - 1].Price;

                }
                else
                {
                    amount = buyerOrder.Amount;
                    price = GetTradePrice(buyerOrder, sellerOrder);
                    tradeRecord.Add(new TradeRecord(buyerUid, sellerUid, price, amount));
                    CurrentPrice = price;
                    BuyOrderList.RemoveAt(BuyOrderList.Count - 1);
                    if (BuyOrderList.Count == 0)
                        MaxBuyPrice = MinInt;
                    else
                        MaxBuyPrice = BuyOrderList[BuyOrderList.Count - 1].Price;
                    SellOrderList.RemoveAt(SellOrderList.Count - 1);
                    if (SellOrderList.Count == 0)
                        MinSellPrice = MaxInt;
                    else
                        MinSellPrice = SellOrderList[SellOrderList.Count - 1].Price;
                }
            }
            return tradeRecord;
        }

        int GetTradePrice(Order buyerOrder,Order sellerOrder)
        {
            if (buyerOrder.Price == BuyMarketOrderPrice && sellerOrder.Price == SellMarketOrderPrice)
                return CurrentPrice;
            else if (buyerOrder.Price == BuyMarketOrderPrice)
                return sellerOrder.Price;
            else if (sellerOrder.Price == SellMarketOrderPrice)
                return buyerOrder.Price;
            else if (buyerOrder.Uid < sellerOrder.Uid)
            {
                return buyerOrder.Price;
            }
            else
            {
                return sellerOrder.Price;
            }
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
