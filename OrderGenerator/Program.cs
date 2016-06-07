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
        static string CfgReadPath = "OrderGenerator.cfg";
        //static string XmlWritePath = "OrderQueue.xml";
        static string XmlWritePath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/OrderQueue.xml";
        static int MarketOrderFlag = -1;

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
        static Random rand = new Random(unchecked((int)DateTime.Now.Ticks));

        public static void Main()
        {
            ReadCfg();
            Order[] OrderQueue = new Order[OrderAmount];
            int uid;
            int time;
            double marketOrderChance = MarketOrderChance / 100.0;
            double bidChance = BidChance / 100.0;
            int price;
            int amount;
            Order.bidOrAsk side;


            for (int i = 0; i < OrderAmount; i++)
            {
                uid = i % AccountAmount;
                time = BaseTime + i;
                if (rand.NextDouble() < bidChance)
                {
                    side = Order.bidOrAsk.BID;
                    price = Round(Gaussian(BidPriceMean, BidPriceSD));
                    amount = Round(Gaussian(BidContractsPerOrderMean, BidContractsPerOrderSD));
                    if (amount < 1) amount = 1;
                }
                else
                {
                    side = Order.bidOrAsk.ASK;
                    price = Round(Gaussian(AskPriceMean, AskPriceSD));
                    amount = Round(Gaussian(AskContractsPerOrderMean, AskContractsPerOrderSD));
                    if (amount < 1) amount = 1;
                }
                if (rand.NextDouble() < marketOrderChance)
                    price = MarketOrderFlag;
                Order NewOrder = new Order(uid, time, side, price, amount);
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

        static void WriteOrderToXml(ref Order[] OrderQueue)
        {
            File.WriteAllText(XmlWritePath, Xml.XMLSerializer(typeof(Order[]), OrderQueue));
            Console.WriteLine("OrderQueue.xml created successfully.");
        }
        static double Gaussian(double mean = 0, double sd = 1)
        {
            return mean + sd * (Math.Sqrt(-2 * Math.Log(rand.NextDouble())) * Math.Cos(2 * Math.PI * rand.NextDouble()));
        }

        static int Round(double x)
        {
            return Convert.ToInt32(Math.Round(x, MidpointRounding.AwayFromZero));
        }


    }
}
