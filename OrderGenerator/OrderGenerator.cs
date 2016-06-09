using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Messaging;
using System.Timers;
using System.Net;
using System.Net.Sockets;

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
        static int OrderStartId;
        static double BidChance;
        static int CurrentPrice;
        static double NominalPriceStandardDeviationRatio;
        static double LogPriceStandardDeviation;
        static double CommisionFee;
        static double ContractsPerOrderMean;
        static double ContractsPerOrderStandardDeviation;
        static int FileOrderAmount;
        static int MessageQueueOrderSendInterval;
        static int MessageQueuePriceReceiveInterval;
        static string WebHostMatchEngineIp;
        static int WebHostMatchEnginePort;
        static int WebOrderSendInterval;

        static double LogCurrentPrice;
        static Random Rand;
        static MessageQueue OrderQueue;
        static MessageQueue PriceQueue;
        static int MessageOrderCount;
        static int WebOrderCount;

        public static void Main()
        {
            Rand = new Random(unchecked((int)DateTime.Now.Ticks));
            ReadCfg();
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
            Order[] OrderQueue = new Order[FileOrderAmount];
            int accountUid;
            int price;
            int amount;
            Order.enumSide side;
            for (int i = 0; i < FileOrderAmount; i++)
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

            Timer orderSendTimer = new Timer(MessageQueueOrderSendInterval);
            orderSendTimer.Elapsed += new ElapsedEventHandler(SendOrderToMessageQueue);
            orderSendTimer.AutoReset = true;
            MessageOrderCount = 0;

            orderSendTimer.Enabled = true;

            Timer priceReceiveTimer = new Timer(MessageQueuePriceReceiveInterval);
            priceReceiveTimer.Elapsed += new ElapsedEventHandler(ReceivePriceFromMessageQueue);
            priceReceiveTimer.AutoReset = true;
            priceReceiveTimer.Enabled = true;

            Console.WriteLine("Sending orders and receiving new prices by MS Message Queue...");

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
            Console.Write($"Order is sent.\tId:{newOrder.Uid}\tAccount:{newOrder.AccountUid}\tSide:{newOrder.Side}\tAmount:{newOrder.Amount}\tType:{newOrder.FufillType}");
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
            Timer orderSendTimer = new Timer(WebOrderSendInterval);
            orderSendTimer.Elapsed += new ElapsedEventHandler(SendOrderToWeb);
            orderSendTimer.AutoReset = true;
            WebOrderCount = 0;

            orderSendTimer.Enabled = true;

            Console.WriteLine("Sending orders and receiving new prices by TCP...");
        }

        static void SendOrderToWeb(object sender, ElapsedEventArgs e)
        {
            try
            {
                TcpClient client = new TcpClient(WebHostMatchEngineIp, WebHostMatchEnginePort);
                NetworkStream ns = client.GetStream();

                int accountUid;
                int price;
                int amount;
                Order.enumSide side;
                accountUid = WebOrderCount % AccountAmount;
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
                byte[] orderBytes = Encoding.UTF8.GetBytes(Xml.XMLSerializer(typeof(Order), newOrder));
                ns.Write(orderBytes, 0, orderBytes.Length);
                Console.Write($"Order is sent.\tId:{newOrder.Uid}\tAccount:{newOrder.AccountUid}\tSide:{newOrder.Side}\tAmount:{newOrder.Amount}\tType:{newOrder.FufillType}");
                if (newOrder.FufillType != Order.enumFufillType.MKT)
                    Console.WriteLine($"\tPrice:{newOrder.Price}");
                else
                    Console.WriteLine();
                WebOrderCount++;

                byte[] newPriceByte = new byte[1024];
                int newPriceByteRealLength = ns.Read(newPriceByte, 0, newPriceByte.Length);
                int newPrice = int.Parse(Encoding.UTF8.GetString(newPriceByte, 0, newPriceByteRealLength));
                if (CurrentPrice != newPrice)
                {
                    CurrentPrice = newPrice;
                    LogCurrentPrice = Math.Log(CurrentPrice);
                    Console.WriteLine($"New current price {CurrentPrice} is received.");
                }

                client.Close();

            }
            catch (Exception)
            {
                Console.WriteLine("Connection fail.");
            }
        }

        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            IsFileImplementation = ReadIntFromCfg();
            IsMessageQueueImplementation = ReadIntFromCfg();
            IsWebImplementation = ReadIntFromCfg();
            AccountAmount = ReadIntFromCfg();
            OrderStartId = ReadIntFromCfg();
            BidChance = ReadDoubleFromCfg();
            CurrentPrice = ReadIntFromCfg();
            NominalPriceStandardDeviationRatio = ReadDoubleFromCfg();
            CommisionFee = ReadDoubleFromCfg();
            ContractsPerOrderMean = ReadDoubleFromCfg();
            ContractsPerOrderStandardDeviation = ReadDoubleFromCfg();
            FileOrderAmount = ReadIntFromCfg();
            MessageQueueOrderSendInterval = ReadIntFromCfg();
            MessageQueuePriceReceiveInterval = ReadIntFromCfg();
            WebHostMatchEngineIp = ReadStringFromCfg();
            WebHostMatchEnginePort = ReadIntFromCfg();
            WebOrderSendInterval = ReadIntFromCfg();

            Order.SetOrderStartId(OrderStartId);
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

        static string ReadStringFromCfg()
        {
            String line = sr.ReadLine();
            return line.Substring(line.IndexOf('=') + 1);
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
