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

        const string OrderMessageQueuePath = @".\Private$\TLIOrderQueue";
        const string OrderMessageQueueName = "OrderQueue";
        const int OrderMessageQueueJournalSize = 1000;

        const string PriceMessageQueuePath = @".\Private$\TLIPriceQueue";
        const string PriceMessageQueueName = "PriceQueue";
        const int PriceMessageQueueJournalSize = 1000;

        static StreamReader sr;
        static int IsFileImplementation;
        static int IsMessageQueueImplementation;
        static int IsWebImplementation;
        static int AccountAmount;
        static int OrderAmount;
        static int OrderStartId;
        static double BidChance;
        static int CurrentPrice;
        static double NominalPriceStandardDeviationRatio;
        static double LogPriceStandardDeviation;
        static double CommisionFee;
        static double ContractsPerOrderMean;
        static double ContractsPerOrderStandardDeviation;
        static int OrderSendInterval;
        static int PriceReceiveInterval;

        static double LogCurrentPrice;
        static Random Rand;
        static MessageQueue OrderQueue;
        static MessageQueue PriceQueue;
        static int MessageOrderCount;

        public static void Main()
        {
            Rand = new Random(unchecked((int)DateTime.Now.Ticks));
            ReadCfg();
            Order.SetOrderStartId(OrderStartId);
            if (IsFileImplementation == 1)
                FileImplementation();
            else if (IsMessageQueueImplementation == 1)
                MessageQueueImplementation();
            else if (IsWebImplementation == 1)
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
                amount = Round(Gaussian(ContractsPerOrderMean, ContractsPerOrderStandardDeviation));
                if (amount < 1) amount = 1;
                if (Rand.NextDouble() < BidChance)
                {
                    price = Round(Math.Exp(Gaussian(LogCurrentPrice, LogPriceStandardDeviation)) - CommisionFee);
                    side = Order.enumSide.BUY;
                    if (price > CurrentPrice)
                        price = BuyMarketOrderPrice;
                }
                else
                {
                    price = Round(Math.Exp(Gaussian(LogCurrentPrice, LogPriceStandardDeviation)) + CommisionFee);
                    side = Order.enumSide.SELL;
                    if (price < CurrentPrice)
                        price = SellMarketOrderPrice;
                }
                Order newOrder = new Order(accountUid, side, price, amount);
                OrderQueue[i] = newOrder;
            }
            WriteOrderToFile(ref OrderQueue);
        }

        static void MessageQueueImplementation()
        {
            if (MessageQueue.Exists(OrderMessageQueuePath))
            {
                OrderQueue = new MessageQueue(OrderMessageQueuePath);
            }
            else
            {
                OrderQueue = MessageQueue.Create(OrderMessageQueuePath);
                OrderQueue.Label = OrderMessageQueueName;
                OrderQueue.UseJournalQueue = true;
                OrderQueue.MaximumJournalSize = OrderMessageQueueJournalSize;
            }
            if (MessageQueue.Exists(PriceMessageQueuePath))
            {
                PriceQueue = new MessageQueue(PriceMessageQueuePath);
            }
            else
            {
                PriceQueue = MessageQueue.Create(PriceMessageQueuePath);
                PriceQueue.Label = PriceMessageQueueName;
                PriceQueue.UseJournalQueue = true;
                PriceQueue.MaximumJournalSize = PriceMessageQueueJournalSize;
            }
            OrderQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(Order) });
            PriceQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(int) });

            Timer orderSendTimer = new Timer(OrderSendInterval);
            orderSendTimer.Elapsed += new ElapsedEventHandler(SendOrderToMessageQueue);
            orderSendTimer.AutoReset = true;
            MessageOrderCount = 0;

            orderSendTimer.Enabled = true;

            Timer priceReceiveTimer = new Timer(PriceReceiveInterval);
            priceReceiveTimer.Elapsed += new ElapsedEventHandler(ReceivePriceFromMessageQueue);
            priceReceiveTimer.AutoReset = true;
            priceReceiveTimer.Enabled = true;

        }

        static void ReceivePriceFromMessageQueue(Object source, ElapsedEventArgs e)
        {
            if (!IsQueueEmpty(PriceMessageQueuePath))
            {
                CurrentPrice = (int)PriceQueue.Receive().Body;
                LogCurrentPrice = Math.Log(CurrentPrice);
                Console.WriteLine($"New current price {CurrentPrice} is received.");
            }
        }

        static void SendOrderToMessageQueue(Object source, ElapsedEventArgs e)
        {
            int accountUid;
            int price;
            int amount;
            Order.enumSide side;
            accountUid = MessageOrderCount % AccountAmount;
            amount = Round(Gaussian(ContractsPerOrderMean, ContractsPerOrderStandardDeviation));
            if (amount < 1) amount = 1;
            if (Rand.NextDouble() < BidChance)
            {
                side = Order.enumSide.BUY;
                price = Round(Math.Exp(Gaussian(LogCurrentPrice, LogPriceStandardDeviation)) - CommisionFee);
                if (price > CurrentPrice)
                    price = BuyMarketOrderPrice;
            }
            else
            {
                side = Order.enumSide.SELL;
                price = Round(Math.Exp(Gaussian(LogCurrentPrice, LogPriceStandardDeviation)) + CommisionFee);
                if (price < CurrentPrice)
                    price = SellMarketOrderPrice;
            }
            Order newOrder = new Order(accountUid, side, price, amount);
            OrderQueue.Send(newOrder);
            Console.Write($"Order #{MessageOrderCount} is sent.\tId:{newOrder.Uid}\tAccount:{newOrder.AccountUid}\tSide:{newOrder.Side}\tAmount:{newOrder.Amount}\tType:{newOrder.FufillType}");
            if (newOrder.FufillType != Order.enumFufillType.MKT)
                Console.WriteLine($"\tPrice:{newOrder.Price}");
            else
                Console.WriteLine();
            MessageOrderCount++;

            /*
            if (MessageOrderCount == OrderAmount)
            {
                (source as Timer).Enabled = false;
            }
            */
        }

        static bool IsQueueEmpty(string path)
        {
            bool isQueueEmpty = false;
            var myQueue = new MessageQueue(path);
            try
            {
                myQueue.Peek(new TimeSpan(0));
                isQueueEmpty = false;
            }
            catch (MessageQueueException e)
            {
                if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    isQueueEmpty = true;
                }
            }
            return isQueueEmpty;
        }

        static void WebImplementation()
        {

        }

        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            IsFileImplementation = ReadIntFromCfg();
            IsMessageQueueImplementation = ReadIntFromCfg();
            IsWebImplementation = ReadIntFromCfg();
            AccountAmount = ReadIntFromCfg();
            OrderAmount = ReadIntFromCfg();
            OrderStartId = ReadIntFromCfg();
            BidChance = ReadDoubleFromCfg();
            CurrentPrice = ReadIntFromCfg();
            NominalPriceStandardDeviationRatio = ReadDoubleFromCfg();
            CommisionFee = ReadDoubleFromCfg();
            ContractsPerOrderMean = ReadDoubleFromCfg();
            ContractsPerOrderStandardDeviation = ReadDoubleFromCfg();
            OrderSendInterval = ReadIntFromCfg();
            PriceReceiveInterval = ReadIntFromCfg();

            LogCurrentPrice = Math.Log(CurrentPrice);
            LogPriceStandardDeviation = Math.Log(1 + NominalPriceStandardDeviationRatio);
        }

        static int ReadIntFromCfg()
        {
            String line = sr.ReadLine();
            return int.Parse(line.Substring(line.IndexOf('=') + 1));
        }

        static double ReadDoubleFromCfg()
        {
            String line = sr.ReadLine();
            return double.Parse(line.Substring(line.IndexOf('=') + 1));
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
