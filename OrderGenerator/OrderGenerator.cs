using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Messaging;
using System.Timers;

namespace OrderGenerator
{
    class OrderGenerator
    {
        const string CfgReadPath = "OrderGenerator.cfg";
#if DEBUG
        const string FileWritePath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/OrderQueue.xml";
#else
        const string FileWritePath = "OrderQueue.xml";
#endif
        const int SellMarketOrderPrice = -2147483640;
        const int BuyMarketOrderPrice = 2147483640;

        const string MessageQueuePath = @".\Private$\OrderQueue";
        const string MessageQueueName = "OrderQueue";
        const int MessageQueueJournalSize = 1000;

        static StreamReader sr;
        static int IsFileImplementation;
        static int IsMessageQueueImplementation;
        static int IsWebImplementation;
        static int AccountAmount;
        static int OrderAmount;
        static int OrderStartId;
        static int MarketOrderChance;
        static int BidChance;
        static double fMarketOrderChance;
        static double fBidChance;
        static int BidPriceMean;
        static int BidPriceSD;
        static int BidContractsPerOrderMean;
        static int BidContractsPerOrderSD;
        static int AskPriceMean;
        static int AskPriceSD;
        static int AskContractsPerOrderMean;
        static int AskContractsPerOrderSD;
        static Random Rand = new Random(unchecked((int)DateTime.Now.Ticks));

        static int MessageSendInterval;
        static MessageQueue OrderQueue;
        static int MessageOrderCount;

        public static void Main()
        {
            ReadCfg();
            Order.SetOrderStartId(OrderStartId);
            if (IsFileImplementation == 1)
                FileImplementation();
            else if (IsMessageQueueImplementation==1)
                MessageQueueImplementation();
            else if (IsWebImplementation==1)
                WebImplementation();
            Console.ReadKey();
        }

        static void FileImplementation()
        {
            Order[] OrderQueue = new Order[OrderAmount];
            int accountUid;
            int price;
            int amount;
            Order.enumSide side;
            for (int i = 0; i < OrderAmount; i++)
            {
                accountUid = i % AccountAmount;
                if (Rand.NextDouble() < fBidChance)
                {
                    side = Order.enumSide.BUY;
                    price = Round(Gaussian(BidPriceMean, BidPriceSD));
                    amount = Round(Gaussian(BidContractsPerOrderMean, BidContractsPerOrderSD));
                    if (amount < 1) amount = 1;
                    if (Rand.NextDouble() < fMarketOrderChance)
                        price = BuyMarketOrderPrice;
                }
                else
                {
                    side = Order.enumSide.SELL;
                    price = Round(Gaussian(AskPriceMean, AskPriceSD));
                    amount = Round(Gaussian(AskContractsPerOrderMean, AskContractsPerOrderSD));
                    if (amount < 1) amount = 1;
                    if (Rand.NextDouble() < fMarketOrderChance)
                        price = SellMarketOrderPrice;
                }
                Order newOrder = new Order(accountUid, side, price, amount);
                OrderQueue[i] = newOrder;
            }
            WriteOrderToFile(ref OrderQueue);
        }

        static void MessageQueueImplementation()
        {
            if (MessageQueue.Exists(MessageQueuePath))
            {
                OrderQueue = new MessageQueue(MessageQueuePath);
            }
            else
            {
                OrderQueue = MessageQueue.Create(MessageQueuePath);
                OrderQueue.Label = MessageQueueName;
                OrderQueue.UseJournalQueue = true;
                OrderQueue.MaximumJournalSize = MessageQueueJournalSize;
            }
            System.Type[] types = new Type[1];
            types[0] = typeof(Order);
            OrderQueue.Formatter = new XmlMessageFormatter(types);
            Timer timer = new Timer(MessageSendInterval);
            timer.Elapsed += new ElapsedEventHandler(SendOrderToMessageQueue);
            timer.AutoReset = true;

            MessageOrderCount = 0;

            timer.Enabled = true;

        }

        static void SendOrderToMessageQueue(Object source,ElapsedEventArgs e)
        {
            int accountUid;
            int price;
            int amount;
            Order.enumSide side;
            accountUid = MessageOrderCount % AccountAmount;
            if (Rand.NextDouble() < fBidChance)
            {
                side = Order.enumSide.BUY;
                price = Round(Gaussian(BidPriceMean, BidPriceSD));
                amount = Round(Gaussian(BidContractsPerOrderMean, BidContractsPerOrderSD));
                if (amount < 1) amount = 1;
                if (Rand.NextDouble() < fMarketOrderChance)
                    price = BuyMarketOrderPrice;
            }
            else
            {
                side = Order.enumSide.SELL;
                price = Round(Gaussian(AskPriceMean, AskPriceSD));
                amount = Round(Gaussian(AskContractsPerOrderMean, AskContractsPerOrderSD));
                if (amount < 1) amount = 1;
                if (Rand.NextDouble() < fMarketOrderChance)
                    price = SellMarketOrderPrice;
            }
            Order newOrder = new Order(accountUid, side, price, amount);
            Console.Write($"Order #{MessageOrderCount} is sent.\tId:{newOrder.Uid}\tAccount:{newOrder.AccountUid}\tSide:{newOrder.Side}\tAmount:{newOrder.Amount}\tType:{newOrder.FufillType}");
            if (newOrder.FufillType != Order.enumFufillType.MKT)
                Console.WriteLine($"\tPrice:{newOrder.Price}");
            else
                Console.WriteLine();
            OrderQueue.Send(newOrder);
            MessageOrderCount++;
            if (MessageOrderCount == OrderAmount)
            {
                (source as Timer).Enabled = false;
            }
        }

        static void WebImplementation()
        {

        }

        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            IsFileImplementation = ReadLineFromCfg();
            IsMessageQueueImplementation = ReadLineFromCfg();
            IsWebImplementation = ReadLineFromCfg();
            AccountAmount = ReadLineFromCfg();
            OrderAmount = ReadLineFromCfg();
            OrderStartId = ReadLineFromCfg();
            MarketOrderChance = ReadLineFromCfg();
            BidChance = ReadLineFromCfg();
            fMarketOrderChance = MarketOrderChance / 100.0;
            fBidChance = BidChance / 100.0;
            BidPriceMean = ReadLineFromCfg();
            BidPriceSD = ReadLineFromCfg();
            BidContractsPerOrderMean = ReadLineFromCfg();
            BidContractsPerOrderSD = ReadLineFromCfg();
            AskPriceMean = ReadLineFromCfg();
            AskPriceSD = ReadLineFromCfg();
            AskContractsPerOrderMean = ReadLineFromCfg();
            AskContractsPerOrderSD = ReadLineFromCfg();
            MessageSendInterval= ReadLineFromCfg();
        }

        static int ReadLineFromCfg()
        {
            String line = sr.ReadLine();
            return int.Parse(line.Substring(line.IndexOf('=') + 1));
        }

        static void WriteOrderToFile(ref Order[] orderQueue)
        {
            File.WriteAllText(FileWritePath, Xml.XMLSerializer(typeof(Order[]), orderQueue));
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
