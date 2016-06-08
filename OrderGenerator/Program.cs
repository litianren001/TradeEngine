using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

namespace OrderGenerator
{
    class Program
    {
        const string CfgReadPath = "OrderGenerator.cfg";
#if DEBUG
        const string XmlWritePath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/OrderQueue.xml";
#else
        const string XmlWritePath = "OrderQueue.xml";
#endif
        const int AskMarketPrice = -2147483648;
        const int BidMarketPrice = 2147483647;

        static StreamReader sr;
        static int AccountAmount;
        static int OrderAmount;
        static int BaseTime;
        static int MarketOrderChance;
        static int BidChance;
        static int BidPriceMean;
        static int BidPriceSD;
        static int BidContractsPerOrderMean;
        static int BidContractsPerOrderSD;
        static int AskPriceMean;
        static int AskPriceSD;
        static int AskContractsPerOrderMean;
        static int AskContractsPerOrderSD;
        static Random Rand = new Random(unchecked((int)DateTime.Now.Ticks));

        public static void Main()
        {
            ReadCfg();
            Order[] OrderQueue = new Order[OrderAmount];
            int accountUid;
            int time;
            double marketOrderChance = MarketOrderChance / 100.0;
            double bidChance = BidChance / 100.0;
            int price;
            int amount;
            Order.BidOrAsk side;


            for (int i = 0; i < OrderAmount; i++)
            {
                accountUid = i % AccountAmount;
                time = BaseTime + i;
                if (Rand.NextDouble() < bidChance)
                {
                    side = Order.BidOrAsk.BID;
                    price = Round(Gaussian(BidPriceMean, BidPriceSD));
                    amount = Round(Gaussian(BidContractsPerOrderMean, BidContractsPerOrderSD));
                    if (amount < 1) amount = 1;
                    if (Rand.NextDouble() < marketOrderChance)
                        price = BidMarketPrice;
                }
                else
                {
                    side = Order.BidOrAsk.ASK;
                    price = Round(Gaussian(AskPriceMean, AskPriceSD));
                    amount = Round(Gaussian(AskContractsPerOrderMean, AskContractsPerOrderSD));
                    if (amount < 1) amount = 1;
                    if (Rand.NextDouble() < marketOrderChance)
                        price = AskMarketPrice;
                }
                Order NewOrder = new Order(accountUid, time, side, price, amount);
                OrderQueue[i] = NewOrder;
            }
            WriteOrderToXml(ref OrderQueue);
            Console.ReadKey();
        }

        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            AccountAmount = ReadLineFromCfg();
            OrderAmount = ReadLineFromCfg();
            BaseTime = ReadLineFromCfg();
            MarketOrderChance = ReadLineFromCfg();
            BidChance = ReadLineFromCfg();
            BidPriceMean = ReadLineFromCfg();
            BidPriceSD = ReadLineFromCfg();
            BidContractsPerOrderMean = ReadLineFromCfg();
            BidContractsPerOrderSD = ReadLineFromCfg();
            AskPriceMean = ReadLineFromCfg();
            AskPriceSD = ReadLineFromCfg();
            AskContractsPerOrderMean = ReadLineFromCfg();
            AskContractsPerOrderSD = ReadLineFromCfg();
        }

        static int ReadLineFromCfg()
        {
            String line = sr.ReadLine();
            return int.Parse(line.Substring(line.IndexOf('=') + 1));
        }

        static void WriteOrderToXml(ref Order[] orderQueue)
        {
            File.WriteAllText(XmlWritePath, Xml.XMLSerializer(typeof(Order[]), orderQueue));
            Console.WriteLine("OrderQueue.xml created successfully.");
        }
        static double Gaussian(double mean = 0, double sd = 1)
        {
            return mean + sd * (Math.Sqrt(-2 * Math.Log(Rand.NextDouble())) * Math.Cos(2 * Math.PI * Rand.NextDouble()));
        }

        static int Round(double x)
        {
            return Convert.ToInt32(Math.Round(x, MidpointRounding.AwayFromZero));
        }


    }
}
